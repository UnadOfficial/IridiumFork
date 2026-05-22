using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ADOFAI;
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
        /// Fixes vanilla v2.10.0 regression: the per-frame AudioSettings.dspTime
        /// calibration for AsyncInputManager was removed, causing offsetTick to
        /// drift over time. A coroutine is started from Awake to perform the
        /// calibration independently of Harmony's per-frame patching overhead.
        /// </summary>
        [HarmonyPatch(typeof(scrConductor), "Awake")]
        public static class AsyncInputDspTimeCalibrationFix
        {
            [HarmonyPostfix]
            public static void Postfix(scrConductor __instance)
            {
                __instance.StartCoroutine(CalibrationLoop());
            }

            private static System.Collections.IEnumerator CalibrationLoop()
            {
                while (true)
                {
                    if (AsyncInputManager.isActive)
                    {
                        double current = AudioSettings.dspTime;
                        if (current != AsyncInputManager.dspTime)
                        {
                            AsyncInputManager.dspTime = current;
                            AsyncInputManager.offsetTick = AsyncInputManager.currFrameTick
                                - (ulong)(current * 10000000.0);
                            AsyncInputManager.offsetTickUpdated = true;
                        }
                    }
                    yield return null;
                }
            }
        }
    }
}
