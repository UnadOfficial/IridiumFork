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
        [HarmonyPatch(typeof(scnLevelSelect), "Awake")]
        public static class RemoveNewsPatch_Awake
        {
            public static void Postfix() { RemoveNewsPatch.newsContainer = GameObject.Find("News Container"); }
        }

        [HarmonyPatch(typeof(scnLevelSelect), "Update")]
        public static class RemoveNewsPatch
        {
            internal static GameObject newsContainer = null;
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                codes.Insert(0, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RemoveNewsPatch), nameof(UpdateNews))));
                return codes;
            }
            public static void UpdateNews()
            {
                if (newsContainer != null)
                {
                    bool shouldBeActive = !Main.Settings.removeNews;
                    if (newsContainer.activeSelf != shouldBeActive) newsContainer.SetActive(shouldBeActive);
                }
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
                float diff = Mod(angleB - angleA, Mathf.PI * 2);
                float diff2 = Mod(angleA - angleB, Mathf.PI * 2);
                float minDiff = Mathf.Min(diff, diff2);
                if (Mathf.Abs(minDiff - Mathf.PI / 2f) < 0.01f)
                {
                    __result = minDiff * 0.08726645f;
                    return false;
                }
                return true;
            }
            private static float Mod(float a, float b) => (a % b + b) % b;
        }

        [HarmonyPatch(typeof(scrConductor), "Update")]
        public static class TailTweakPatch
        {
            public static void Postfix()
            {
                if (!Main.Settings.enableTailTweak || scrController.instance == null || scrController.instance.planetarySystem == null) return;
                foreach (var planet in scrController.instance.planetarySystem.availablePlanets)
                {
                    if (planet == null || planet.planetRenderer == null || planet.planetRenderer.tailParticles == null) continue;
                    var ps = planet.planetRenderer.tailParticles.GetComponent<ParticleSystem>();
                    if (ps == null) continue;
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
