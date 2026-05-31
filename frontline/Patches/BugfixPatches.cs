using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ADOFAI;
using DG.Tweening;
using UnityEngine;

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

        /// <summary>
        /// v2.10.0+: scrPlayer.marginTracker changed to a read-only property
        /// that directly returns scrMistakesManager.marginTrackers[playerID],
        /// so the sync from SetPlayerCount is now automatic. This patch is
        /// retained as a no-op stub for documentation clarity.
        /// </summary>
        // MarginTrackerSetPlayerCountFix removed in v2.10.0 — game handles sync natively.

        /// <summary>
        /// Forces a snap calibration each time a level starts playing. Without this,
        /// offsetTick starts at 0 and the per-frame slew (error/100) takes ~1.6s to
        /// converge to the true audio latency, causing incorrect timing at level start.
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "Play")]
        public static class AsyncInputPlaySnapPatch
        {
            public static void Prefix()
            {
                if (AsyncInputManager.isActive)
                    AsyncInputUtils.UpdateOffsetTime(1L);
            }
        }

        /// <summary>
        /// v2.10.0: When scnGame.Play() runs in editor mode, it skips WaitForStartCo()
        /// and calls Awake_Rewind() + Start_Rewind() directly. Neither resets the
        /// mistakes manager, so hardestDifficulty retains a stale value (defaults to
        /// Lenient). SwitchToEditMode() calls mistakesManager.Reset() (which now uses
        /// the current marginTrackers thanks to MarginTrackerSetPlayerCountFix).
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
        /// v2.10.0: CalculateSingleFloorAngleLength sets turnaround=true when
        /// |GetAngleMoved| <= 1e-6 OR >= 2π-ε. v2.9.8 only detected this when
        /// |GetAngleMoved - 2π| < 0.0001. The broader condition (1e-6 branch)
        /// catches wrap-around entry==exit floors that v2.9.8 did not, causing
        /// Pause events on those floors to incorrectly add an extra beat.
        /// This postfix re-checks turnaround using v2.9.8's exact condition.
        /// </summary>
        [HarmonyPatch(typeof(scrLevelMaker), "CalculateSingleFloorAngleLength")]
        public static class TurnaroundConditionFix
        {
            [HarmonyPostfix]
            public static void Postfix(scrFloor cf)
            {
                if (cf.turnaround)
                {
                    double angleMoved = scrMisc.GetAngleMoved(cf.entryangle, cf.exitangle, !cf.isCCW);
                    if (Math.Abs(angleMoved - 6.2831854820251465) >= 0.0001)
                        cf.turnaround = false;
                }
            }
        }


        /// <summary>
        /// Fixes vanilla v2.10.0 bug: non-coop ShowHitText doesn't forward missAngle
        /// to scrHitTextMesh.Show(), so non-Perfect judgments don't get the rotation
        /// animation. Uses _pendingMissAngle captured by HitTextManagerShowPatch.
        /// </summary>
        [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
        public static class HitTextMeshShowRotationFixPatch
        {
            public static void Postfix(scrHitTextMesh __instance)
            {
                if (scrPlayerManager.playerCount > 1) return;
                if (__instance.hitMargin == HitMargin.Perfect) return;

                __instance.transform.DOLocalRotate(
                    new Vector3(0f, 0f, JudgeTextPatches._pendingMissAngle * 20f),
                    2f,
                    RotateMode.LocalAxisAdd
                );
            }
        }

        /// <summary>
        /// Fixes v2.10.0 regression: after Twirl direction change, a miss on the
        /// first floor uses prevfloor.isCCW instead of the judged floor's isCCW,
        /// causing the error meter hand to point the wrong direction.
        ///
        /// Replaces the prevfloor load chain:
        ///   planetarySystem.chosenPlanet.player.currFloor.prevfloor.isCCW
        /// with:
        ///   scrPlanet2.currfloor.isCCW
        /// (scrPlanet2 = chosenPlanet captured BEFORE SwitchChosen)
        /// </summary>
        [HarmonyPatch(typeof(scrPlayer), "Hit")]
        public static class FixErrorMeterCCW
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var f_planetarySystem = AccessTools.Field(typeof(scrPlayer), "planetarySystem");
                var f_chosenPlanet = AccessTools.Field(typeof(PlanetarySystem), "chosenPlanet");
                var f_player = AccessTools.Field(typeof(scrPlanet), "player");
                var m_get_currFloor = AccessTools.PropertyGetter(typeof(scrPlayer), "currFloor");
                var f_prevfloor = AccessTools.Field(typeof(scrFloor), "prevfloor");
                var f_currfloor = AccessTools.Field(typeof(scrPlanet), "currfloor");

                var codes = instructions.ToList();

                for (int i = 0; i <= codes.Count - 6; i++)
                {
                    // Match: ldarg.0 → ldfld planetarySystem → ldfld chosenPlanet
                    //        → ldfld player → callvirt get_currFloor → ldfld prevfloor
                    if (codes[i].opcode == OpCodes.Ldarg_0 &&
                        codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 1].operand is FieldInfo fi1 && fi1 == f_planetarySystem &&
                        codes[i + 2].opcode == OpCodes.Ldfld && codes[i + 2].operand is FieldInfo fi2 && fi2 == f_chosenPlanet &&
                        codes[i + 3].opcode == OpCodes.Ldfld && codes[i + 3].operand is FieldInfo fi3 && fi3 == f_player &&
                        codes[i + 4].opcode == OpCodes.Callvirt && codes[i + 4].operand is MethodInfo mi && mi == m_get_currFloor &&
                        codes[i + 5].opcode == OpCodes.Ldfld && codes[i + 5].operand is FieldInfo fi4 && fi4 == f_prevfloor)
                    {
                        // Replace with: ldloc.1 (scrPlanet2); ldfld currfloor
                        codes[i].opcode = OpCodes.Ldloc_1;
                        codes[i].operand = null;
                        codes[i + 1].opcode = OpCodes.Ldfld;
                        codes[i + 1].operand = f_currfloor;
                        codes.RemoveRange(i + 2, 4);
                        break;
                    }
                }

                return codes;
            }
        }
    }
}
