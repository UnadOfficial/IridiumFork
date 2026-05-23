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
        /// Source-level fix: after SetPlayerCount allocates new marginTrackers,
        /// update existing scrPlayer instances to reference the new trackers.
        /// This prevents the orphaned tracker problem at the root.
        /// </summary>
        [HarmonyPatch(typeof(scrMistakesManager), "SetPlayerCount")]
        public static class MarginTrackerSetPlayerCountFix
        {
            [HarmonyPostfix]
            public static void Postfix(int playerCount)
            {
                var players = scrPlayerManager.instance?.players;
                if (players != null)
                {
                    int count = Math.Min(players.Length, playerCount);
                    for (int i = 0; i < count; i++)
                        players[i].marginTracker = scrMistakesManager.marginTrackers[i];
                }
            }
        }

        /// <summary>
        /// Defensive safety net: even with MarginTrackerSetPlayerCountFix active,
        /// ensure Reset() always clears the actual trackers on players.
        /// Catches any future code path that could desync player references.
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
                if (scrController.coopMode) return;
                if (__instance.hitMargin == HitMargin.Perfect) return;

                __instance.transform.DOLocalRotate(
                    new Vector3(0f, 0f, JudgeTextPatches._pendingMissAngle * 20f),
                    2f,
                    RotateMode.LocalAxisAdd
                );
            }
        }
    }
}
