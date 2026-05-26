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
    }
}
