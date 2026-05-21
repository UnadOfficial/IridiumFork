using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches
{
    public static class BugfixPatches
    {
        [HarmonyPatch(typeof(scrController), "PortalTravelAction")]
        public static class PortalTravelFixPatch
        {
            private static FieldInfo? _f_transitioningLevel;
            private static System.Reflection.PropertyInfo? _p_isWipingToBlack;

            [HarmonyPrefix]
            public static bool Prefix(scrController __instance, Portal destination)
            {
                if (!Main.Settings.compatibility.portalTravelFix) return true;

                _f_transitioningLevel ??= AccessTools.Field(typeof(scrController), "transitioningLevel");

                if ((bool)_f_transitioningLevel.GetValue(__instance))
                    return false;

                var loader = ADOBase.loader;
                if (loader != null)
                {
                    _p_isWipingToBlack ??= AccessTools.Property(typeof(scrLoader), "isWipingToBlack");
                    if (_p_isWipingToBlack != null && (bool)_p_isWipingToBlack.GetValue(loader))
                        return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(OptionsPanelsCLS), "Awake")]
        public static class SyncSpeedTrialPatch
        {
            [HarmonyPostfix]
            public static void Postfix(OptionsPanelsCLS __instance)
            {
                if (!Main.Settings.compatibility.syncSpeedTrialOnLoad) return;

                if (GCS.speedTrialMode)
                {
                    __instance.speedTrial = true;
                    __instance.bgSprite.color = __instance.bgColorSpeedTrial;
                }
            }
        }

        /// <summary>
        /// scnEditor.Awake() in v2.10.0 calls scrPlayerManager.SetPlayerCount(1)
        /// which creates a new marginTrackers array. The active scrPlayer instances
        /// still reference the old marginTracker objects, so mistaksManager.Reset()
        /// (called later in SwitchToEditMode) only clears the new unused trackers,
        /// leaving stale checkpoint/death margins on the players' real trackers.
        /// This postfix ensures every player's marginTracker is cleared too.
        /// </summary>
        [HarmonyPatch(typeof(scrMistakesManager), "Reset")]
        public static class MarginTrackerResetFix
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                foreach (var player in scrPlayerManager.instance?.players ?? [])
                    player?.marginTracker?.Reset();
            }
        }

        /// <summary>
        /// v2.10.0: When scnGame.Play() runs in editor mode, it skips WaitForStartCo()
        /// and calls Awake_Rewind() + Start_Rewind() directly. Neither resets the
        /// mistakes manager, so hardestDifficulty retains a stale value (defaults to
        /// Lenient). SwitchToEditMode() calls mistakesManager.Reset() (fixed by
        /// MarginTrackerResetFix) which is why ESC+replay shows the strict clear text.
        /// This ensures Reset() is also called on the first Play from the editor.
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "Play")]
        public static class EditorPlayResetMistakesPatch
        {
            public static void Prefix()
            {
                if (!ADOBase.isLevelEditor) return;
                scrController.instance?.mistakesManager.Reset();
            }
        }

        /// <summary>
        /// v2.10.0: scrPlayer.Hit() reads planetarySystem.isCW AFTER SwitchChosen(),
        /// so the isCW value reflects the NEW tile's direction. On Twirl tiles (which
        /// flip isCCW), this gives the opposite sign convention for the error meter,
        /// causing "early" to display as "late" and vice versa.
        /// Fix: read scrPlanet2.currfloor.isCCW instead — the OLD tile's direction.
        /// !planetarySystem.isCW(new) == scrPlanet2.currfloor.isCCW(old) when tiles
        /// have the same direction, but NOT when Twirl flips isCCW between tiles.
        /// </summary>
        [HarmonyPatch(typeof(scrPlayer), "Hit")]
        public static class TwirlErrorMeterSignFix
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var codes = instructions.ToList();
                var f_isCW = AccessTools.Field(typeof(PlanetarySystem), "isCW");
                var f_planetarySystem = AccessTools.Field(typeof(scrPlayer), "planetarySystem");
                var f_cachedAngle = AccessTools.Field(typeof(scrPlanet), "cachedAngle");
                var f_currfloor = AccessTools.Field(typeof(scrPlanet), "currfloor");
                var f_isCCW = AccessTools.Field(typeof(scrFloor), "isCCW");

                // Find scrPlanet2 local index: scan for ldloc right before ldfld cachedAngle
                int scrPlanet2Idx = -1;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].LoadsField(f_cachedAngle) && i > 0)
                    {
                        // Handle both ldloc.s N and ldloc.N forms
                        var prev = codes[i - 1];
                        if (prev.opcode == OpCodes.Ldloc_0) scrPlanet2Idx = 0;
                        else if (prev.opcode == OpCodes.Ldloc_1) scrPlanet2Idx = 1;
                        else if (prev.opcode == OpCodes.Ldloc_2) scrPlanet2Idx = 2;
                        else if (prev.opcode == OpCodes.Ldloc_3) scrPlanet2Idx = 3;
                        else if (prev.opcode == OpCodes.Ldloc_S || prev.opcode == OpCodes.Ldloc)
                            scrPlanet2Idx = Convert.ToInt32(prev.operand);
                        if (scrPlanet2Idx >= 0) break;
                    }
                }

                if (scrPlanet2Idx < 0)
                {
                    Main.Logger?.Log("[TwirlErrorMeterSignFix] Could not find scrPlanet2 local index");
                    return codes;
                }

                // Match the IL pattern for the error meter's sign flip:
                //   ldarg.0
                //   ldfld scrPlayer.planetarySystem
                //   ldfld PlanetarySystem.isCW
                //   brtrue.s SKIP
                // Replace with:
                //   ldloc.s scrPlanet2Idx
                //   ldfld scrPlanet.currfloor
                //   ldfld scrFloor.isCCW
                //   brfalse.s SKIP   (branch polarity flipped: !isCW → isCCW)
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].LoadsField(f_isCW)
                        && i >= 2
                        && codes[i - 2].IsLdarg()
                        && codes[i - 1].LoadsField(f_planetarySystem))
                    {
                        codes[i - 2] = new CodeInstruction(OpCodes.Ldloc_S, scrPlanet2Idx);
                        codes[i - 1] = new CodeInstruction(OpCodes.Ldfld, f_currfloor);
                        codes[i]     = new CodeInstruction(OpCodes.Ldfld, f_isCCW);

                        if (i + 1 < codes.Count)
                        {
                            if (codes[i + 1].opcode == OpCodes.Brtrue_S)
                                codes[i + 1].opcode = OpCodes.Brfalse_S;
                            else if (codes[i + 1].opcode == OpCodes.Brtrue)
                                codes[i + 1].opcode = OpCodes.Brfalse;
                        }
                        break;
                    }
                }

                return codes;
            }
        }

        /// <summary>
        /// v2.10.0: scrHitErrorMeter.AddHit hardcodes playerOne.currFloor.prevfloor.speed
        /// for bpmTimesSpeed. This gives wrong speed because currFloor after SwitchChosen
        /// belongs to the newly active planet whose prevfloor is the tile before the hit.
        /// Fix: use currFloor.speed directly (like v2.9.8's scrController.speed), and
        /// use the planet parameter instead of hardcoding playerOne.
        /// </summary>
        [HarmonyPatch(typeof(scrHitErrorMeter), "AddHit")]
        public static class AddHitSpeedFix
        {
            private static float GetSpeed(scrPlanet? planet)
            {
                if (planet == null)
                    planet = scrController.instance?.playerOne?.planetarySystem?.chosenPlanet;
                return planet?.currfloor?.speed ?? 1f;
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                // IL pattern:
                //   call scrController::get_instance()
                //   callvirt scrController::get_playerOne()
                //   callvirt scrPlayer::get_currFloor()
                //   ldfld scrFloor::prevfloor
                //   ldfld scrFloor::speed
                // Replace with:
                //   ldarg.3  (planet)
                //   call AddHitSpeedFix::GetSpeed(scrPlanet)
                var m_getPlayerOne = AccessTools.PropertyGetter(typeof(scrController), "playerOne");
                var m_getCurrFloor = AccessTools.PropertyGetter(typeof(scrPlayer), "currFloor");
                var f_prevfloor = AccessTools.Field(typeof(scrFloor), "prevfloor");
                var f_speed = AccessTools.Field(typeof(scrFloor), "speed");
                var m_GetSpeed = AccessTools.Method(typeof(AddHitSpeedFix), nameof(GetSpeed));

                if (m_getPlayerOne == null || m_getCurrFloor == null || f_prevfloor == null || f_speed == null)
                    return codes;

                for (int i = 0; i < codes.Count - 4; i++)
                {
                    if (codes[i].Calls(m_getPlayerOne)
                        && codes[i + 1].Calls(m_getCurrFloor)
                        && codes[i + 2].LoadsField(f_prevfloor)
                        && codes[i + 3].LoadsField(f_speed))
                    {
                        // Match the get_instance call one instruction before
                        if (i > 0 && codes[i - 1].opcode == OpCodes.Call)
                        {
                            codes[i - 1] = new CodeInstruction(OpCodes.Ldarg_3);   // planet
                            codes[i]     = new CodeInstruction(OpCodes.Call, m_GetSpeed);
                        }
                        else
                        {
                            codes[i]     = new CodeInstruction(OpCodes.Ldarg_3);   // planet
                            codes[i + 1] = new CodeInstruction(OpCodes.Call, m_GetSpeed);
                        }
                        codes[i + 2] = new CodeInstruction(OpCodes.Nop);
                        codes[i + 3] = new CodeInstruction(OpCodes.Nop);
                        if (i + 4 < codes.Count)
                            codes[i + 4] = new CodeInstruction(OpCodes.Nop);
                        break;
                    }
                }

                return codes;
            }
        }
    }
}
