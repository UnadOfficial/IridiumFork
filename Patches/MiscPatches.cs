using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ADOFAI;
using HarmonyLib;
using UnityEngine;

namespace Iridium.Patches
{
    public static class MiscPatches
    {
        [HarmonyPatch(typeof(scnLevelSelect))]
        public static class RemoveNewsPatch
        {
            internal static GameObject? newsContainer = null;

            [HarmonyPatch("Awake"), HarmonyPostfix]
            public static void Postfix()
            {
                newsContainer = GameObject.Find("News Container");
            }

            [HarmonyPatch("Update"), HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RemoveNewsPatch), nameof(UpdateNews)));
                foreach (CodeInstruction c in instructions)
                {
                    yield return c;
                }
                yield break;
            }

            public static void UpdateNews()
            {
                if (newsContainer is null) return;
                bool shouldBeActive = !Main.Settings.removeNews;
                if (newsContainer.activeSelf != shouldBeActive) newsContainer.SetActive(shouldBeActive);
            }
        }

        [HarmonyPatch(typeof(scrMisc), "DetermineDifficultyUIMode")]
        public static class ForceDifficultyUIPatch
        {
            public static void Postfix(ref DifficultyUIMode __result)
            {
                if (Main.Settings.forceDifficultyUI && ADOBase.isCLSLevel) __result = DifficultyUIMode.ShowAll;
            }
        }

        [HarmonyPatch(typeof(FloorMesh), "SmallestAngleBetweenTwoAngles")]
        public static class CircleArcPatch
        {
            public static bool Prefix(float angleA, float angleB, ref float __result)
            {
                if (!Main.Settings.enableCircleArc) return true;
                float minDiff = Mathf.Abs(Mathf.DeltaAngle(angleA * Mathf.Rad2Deg, angleB * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                if (Mathf.Abs(minDiff - Mathf.PI / 2f) < 0.01f)
                {
                    __result = minDiff * 5f / 180f * Mathf.PI;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(scrEnableIfBeta), "Awake")]
        public static class HideBetaWatermarkPatch
        {
            public static void Postfix(scrEnableIfBeta __instance)
            {
                if (Main.Settings.hideBetaWatermark) __instance.gameObject.SetActive(false);
            }
        }

        public static void RefreshBetaWatermark()
        {
            foreach (var watermark in Resources.FindObjectsOfTypeAll<scrEnableIfBeta>())
            {
                if (Main.Settings.hideBetaWatermark) watermark.gameObject.SetActive(false);
                else
                {
                    bool isBeta = SteamIntegration.initialized && !string.IsNullOrEmpty(GCS.steamBranchName);
                    watermark.gameObject.SetActive(isBeta);
                }
            }
        }

        [HarmonyPatch(typeof(scrConductor), "Update")]
        public static class TailTweakPatch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    yield return instruction;
                }
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TailTweakPatch), nameof(UpdateTail)));
            }

            public static void UpdateTail()
            {
                if (!Main.Settings.enableTailTweak || scrController.instance?.planetarySystem == null) return;
                foreach (var planet in scrController.instance.planetarySystem.availablePlanets)
                {
                    if (planet?.planetRenderer?.tailParticles is null) continue;
                    var ps = planet.planetRenderer.tailParticles.GetComponent<ParticleSystem>();
                    if (ps is null) continue;
                    var main = ps.main;
                    var emission = ps.emission;
                    if (Main.Settings.tailFollowPitch)
                        main.simulationSpeed = scrConductor.instance.song.pitch * (scnEditor.instance != null ? scnEditor.instance.playbackSpeed : 1f);
                    else
                        main.simulationSpeed = Main.Settings.tailLength;
                    emission.rateOverTime = Main.Settings.tailEmission;
                }
            }
        }
    }
}
