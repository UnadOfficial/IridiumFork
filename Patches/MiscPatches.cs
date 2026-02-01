using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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

        [HarmonyPatch(typeof(scrUIController), "Update")]
        public static class AutoplayTextPositionPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MiscPatches), nameof(RefreshAutoplayTextPosition)));
                foreach (var instruction in instructions)
                {
                    yield return instruction;
                }
            }
        }

        private static bool _isAutoplayModified = false;
        private static Vector3 _originalAutoplayPos;

        public static void RefreshAutoplayTextPosition()
        {
            if (Main.Settings.moveAutoplayText)
            {
                if (scrUIController.instance?.txtDebug == null) return;
                
                // 仅在第一次修改前记录位置
                if (!_isAutoplayModified)
                {
                    _originalAutoplayPos = scrUIController.instance.txtDebug.transform.localPosition;
                    _isAutoplayModified = true;
                }

                scrUIController.instance.txtDebug.transform.localPosition = new Vector3(Main.Settings.autoplayTextX, Main.Settings.autoplayTextY, 0f);
            }
            else if (_isAutoplayModified)
            {
                // 如果之前修改过，则恢复一次并重置标记位
                if (scrUIController.instance?.txtDebug != null)
                {
                    scrUIController.instance.txtDebug.transform.localPosition = _originalAutoplayPos;
                }
                _isAutoplayModified = false;
            }
            // 如果 moveAutoplayText 为 false 且 _isAutoplayModified 也为 false，则完全不执行任何逻辑，不触碰对象
        }

        [HarmonyPatch(typeof(scrConductor), "Update")]
        public static class TailTweakPatch
        {
            private static readonly ConditionalWeakTable<scrPlanet, ParticleSystem> _psCache = new();

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
                if (!Main.Settings.enableTailTweak || scrController.instance?.planetarySystem is null)
                {
                    return;
                }
                foreach (var planet in scrController.instance.planetarySystem.availablePlanets)
                {
                    if (planet?.planetRenderer?.tailParticles is null) continue;

                    if (!_psCache.TryGetValue(planet, out var ps))
                    {
                        ps = planet.planetRenderer.tailParticles.GetComponent<ParticleSystem>();
                        if (ps is not null) _psCache.Add(planet, ps);
                    }

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

            public static void ResetTails()
            {
                if (scrController.instance?.planetarySystem is null) return;
                foreach (var planet in scrController.instance.planetarySystem.availablePlanets)
                {
                    if (planet?.planetRenderer?.tailParticles is null) continue;

                    if (!_psCache.TryGetValue(planet, out var ps))
                    {
                        ps = planet.planetRenderer.tailParticles.GetComponent<ParticleSystem>();
                        if (ps is not null) _psCache.Add(planet, ps);
                    }

                    if (ps is null) continue;
                    var main = ps.main;
                    var emission = ps.emission;
                    main.simulationSpeed = 1f;
                    emission.rateOverTime = 20f; // Default ADOFAI emission rate is usually around here
                }
            }
        }
    }
}
