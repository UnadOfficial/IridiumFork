using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
                bool shouldBeActive = !Main.Settings.ui.removeNews;
                if (newsContainer.activeSelf != shouldBeActive) newsContainer.SetActive(shouldBeActive);
            }
        }

        [HarmonyPatch(typeof(scrMisc), "DetermineDifficultyUIMode")]
        public static class ForceDifficultyUIPatch
        {
            public static void Postfix(ref DifficultyUIMode __result)
            {
                if (ADOBase.isCLSLevel) __result = DifficultyUIMode.ShowAll;
            }
        }

        [HarmonyPatch(typeof(FloorMesh), "SmallestAngleBetweenTwoAngles")]
        public static class CircleArcPatch
        {
            public static bool Prefix(float angleA, float angleB, ref float __result)
            {
                float minDiff = Mathf.Abs(Mathf.DeltaAngle(angleA * Mathf.Rad2Deg, angleB * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                float minDiffDeg = minDiff * Mathf.Rad2Deg;
                if (minDiffDeg >= 89.9f && minDiffDeg <= 105.1f)
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
                __instance.gameObject.SetActive(false);
            }
        }

        public static void RefreshBetaWatermark()
        {
            foreach (var watermark in Resources.FindObjectsOfTypeAll<scrEnableIfBeta>())
            {
                watermark.gameObject.SetActive(false);
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
            if (scrUIController.instance?.txtDebug == null) return;
            
            // 仅在第一次修改前记录位置
            if (!_isAutoplayModified)
            {
                _originalAutoplayPos = scrUIController.instance.txtDebug.transform.localPosition;
                _isAutoplayModified = true;
            }

            scrUIController.instance.txtDebug.transform.localPosition = new Vector3(Main.Settings.ui.autoplayTextX, Main.Settings.ui.autoplayTextY, 0f);
        }

        [HarmonyPatch(typeof(scrConductor), "Update")]
        public static class TailTweakPatch
        {
            private static readonly ConditionalWeakTable<scrPlanet, ParticleSystem> _psCache = new();
            private static readonly HashSet<string> _allowedScenes = new()
            {
                "scnLevelSelect",
                "scnCLS",
                "scnTaroMenu0"
            };

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // 在方法开始时注入，确保一定会被执行
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TailTweakPatch), nameof(UpdateTail)));
                foreach (var instruction in instructions)
                {
                    yield return instruction;
                }
            }

            private static float _lastTailLength = -1f;
            private static float _lastTailEmission = -1f;
            private static bool _lastTailFollowPitch = false;
            private static bool _tailDisabledSynced = false;

            private static bool IsAllowedScene()
            {
                string sceneName = ADOBase.sceneName;
                return !string.IsNullOrEmpty(sceneName) && _allowedScenes.Contains(sceneName);
            }

            public static void UpdateTail()
            {
                if (!Main.Settings.tail.enableTailTweak || !IsAllowedScene())
                {
                    if (!_tailDisabledSynced)
                    {
                        ResetTails();
                        _tailDisabledSynced = true;
                    }
                    return;
                }

                _tailDisabledSynced = false;

                if (scrController.instance is null)
                {
                    return;
                }

                // 使用 allPlanets 获取所有星球实例
                var planetarySystem = scrController.instance.planetarySystem;
                if (planetarySystem?.allPlanets == null) return;

                float currentLength = Main.Settings.tail.tailLength;
                float currentEmission = Main.Settings.tail.tailEmission;
                bool currentFollowPitch = Main.Settings.tail.tailFollowPitch;
                float songPitch = scrConductor.instance.song.pitch * (scnEditor.instance != null ? scnEditor.instance.playbackSpeed : 1f);

                // 只有在设置改变或者启用了跟随音高（音高可能实时变化）时才更新
                bool needsUpdate = !Mathf.Approximately(currentLength, _lastTailLength) ||
                                   !Mathf.Approximately(currentEmission, _lastTailEmission) ||
                                   currentFollowPitch != _lastTailFollowPitch ||
                                   currentFollowPitch; // 如果跟随音高，则每帧都需要更新

                if (!needsUpdate) return;

                _lastTailLength = currentLength;
                _lastTailEmission = currentEmission;
                _lastTailFollowPitch = currentFollowPitch;

                foreach (var planet in planetarySystem.allPlanets)
                {
                    if (planet?.planetRenderer?.tailParticles is null) continue;

                    if (!_psCache.TryGetValue(planet, out var ps) || ps == null)
                    {
                        ps = planet.planetRenderer.tailParticles.GetComponent<ParticleSystem>();
                        if (ps != null)
                        {
                            _psCache.Remove(planet);
                            _psCache.Add(planet, ps);
                        }
                    }

                    if (ps == null) continue;

                    var main = ps.main;
                    var emission = ps.emission;

                    float speed = currentFollowPitch ? songPitch : currentLength;

                    main.simulationSpeed = speed;
                    emission.rateOverTime = currentEmission;
                }
            }

            public static void ResetTails()
            {
                if (scrController.instance?.planetarySystem?.allPlanets == null) return;

                foreach (var planet in scrController.instance.planetarySystem.allPlanets)
                {
                    if (planet?.planetRenderer?.tailParticles is null) continue;

                    var ps = planet.planetRenderer.tailParticles.GetComponent<ParticleSystem>();
                    if (ps == null) continue;

                    var main = ps.main;
                    var emission = ps.emission;
                    main.simulationSpeed = 1f;
                    emission.rateOverTime = 20f;
                }
            }
        }

        [HarmonyPatch(typeof(scrConductor), "Update")]
        public static class CustomBpmPatch
        {
            private static readonly HashSet<string> _allowedScenes = new()
            {
                "scnLevelSelect",
                "scnCLS",
                "scnTaroMenu0"
            };

            [HarmonyPrefix]
            public static void Prefix()
            {
                if (!Main.Settings.tail.enableCustomBpm || scrConductor.instance == null)
                {
                    return;
                }

                string sceneName = ADOBase.sceneName;
                if (string.IsNullOrEmpty(sceneName) || !_allowedScenes.Contains(sceneName))
                {
                    return;
                }

                scrConductor.instance.bpm = Main.Settings.tail.customBpm;
            }
        }

        [HarmonyPatch(typeof(scnLevelSelect), "Awake")]
        public static class LobbyMusicPatch
        {
            private static bool _loadingDefault;
            private static bool _loadingFast;
            private static AudioClip? _defaultBgm;
            private static AudioClip? _fastBgm;

            [HarmonyPostfix]
            public static void Postfix()
            {
                ReloadFromSettings();
            }

            public static void ReloadFromSettings()
            {
                if (!Main.Settings.lobbyMusic.customMusic)
                {
                    TryApplyLoadedClips();
                    return;
                }

                StartLoad(true, Main.Settings.lobbyMusic.defaultMusicPath);
                StartLoad(false, Main.Settings.lobbyMusic.fastMusicPath);
            }

            public static void StartLoad(bool loadDefault, string? path)
            {
                if (scrConductor.instance == null) return;
                scrConductor.instance.StartCoroutine(LoadMusicCo(loadDefault, path));
            }

            private static IEnumerator LoadMusicCo(bool loadDefault, string? path)
            {
                if (loadDefault)
                {
                    _loadingDefault = true;
                    _defaultBgm = null;
                }
                else
                {
                    _loadingFast = true;
                    _fastBgm = null;
                }

                AudioClip? clip = null;
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    Main.Logger?.Log($"[LobbyMusic] start loading '{path}', default={loadDefault}");

                    clip = AudioManager.Instance.FindOrLoadAudioClip(Path.GetFileName(path) + "*external", null);
                    if (clip == null)
                    {
                        IEnumerator load = AudioManager.Instance.FindOrLoadAudioClipExternal(path, false, 0f);
                        yield return load;
                        RDAudioLoadResult result = (RDAudioLoadResult)load.Current;
                        if ((int)result.type == 0)
                        {
                            clip = result.clip;
                        }
                        else
                        {
                            Main.Logger?.Log($"[LobbyMusic] load failed: {result.type}");
                        }
                    }

                    Main.Logger?.Log($"[LobbyMusic] end loading '{path}', default={loadDefault}");
                }

                if (loadDefault)
                {
                    _loadingDefault = false;
                    _defaultBgm = clip;
                }
                else
                {
                    _loadingFast = false;
                    _fastBgm = clip;
                }

                TryApplyLoadedClips();
            }

            public static void TryApplyLoadedClips()
            {
                if (scrConductor.instance == null || !ADOBase.isLevelSelect) return;

                if (!Main.Settings.lobbyMusic.customMusic)
                {
                    return;
                }

                if (!_loadingDefault)
                {
                    if ((scrConductor.instance.song.clip = _defaultBgm) == null)
                    {
                        scrConductor.instance.song.Stop();
                    }
                    else
                    {
                        scrConductor.instance.song.volume = 1f;
                        scrConductor.instance.song.pitch = 1f;
                    }
                }

                if (!_loadingFast)
                {
                    if ((scrConductor.instance.song2.clip = _fastBgm) == null)
                    {
                        scrConductor.instance.song2.Stop();
                    }
                    else
                    {
                        scrConductor.instance.song2.pitch = 1f;
                        if (!Main.Settings.lobbyMusic.fastMusic)
                        {
                            scrConductor.instance.song2.volume = 0f;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UnityEngine.SceneManagement.SceneManager), "GetSceneAt")]
        public static class SceneGC
        {
            public static void Prefix()
            {
            }
        }

        [HarmonyPatch(typeof(scrController), "Awake")]
        public static class SmartGCPatch
        {
            private static float _lastCleanTime = 0f;
            private static bool _isCleaning = false;

            public static void Postfix(scrController __instance)
            {
                __instance.StartCoroutine(GCLoop());
            }

            private static IEnumerator GCLoop()
            {
                while (true)
                {
                    yield return new WaitForSeconds(5f);

                    // 检查是否达到间隔
                    if (Time.realtimeSinceStartup - _lastCleanTime < Main.Settings.memory.gcInterval) continue;

                    // 安全性检查：如果在关卡内且未开启 gcInGame，则跳过
                    bool isInLevel = scrController.instance != null && !scrController.instance.paused && scrController.instance.gameworld;
                    if (isInLevel && !Main.Settings.memory.gcInGame) continue;

                    // 避免重叠清理
                    if (_isCleaning) continue;

                    yield return CleanMemoryRoutine();
                }
            }

            private static IEnumerator CleanMemoryRoutine()
            {
                _isCleaning = true;
                _lastCleanTime = Time.realtimeSinceStartup;

                Main.Logger?.Log(Localization.Get("CleaningMemory"));

                // 1. 异步卸载未使用的资源 (Unity 推荐方式)
                AsyncOperation asyncUnload = Resources.UnloadUnusedAssets();
                while (!asyncUnload.isDone)
                {
                    yield return null;
                }

                // 2. 只有在不在关卡内时，才尝试卸载 AssetBundles (防止画面内容缺失)
                bool isInLevel = scrController.instance != null && scrController.instance.gameworld;
                if (!isInLevel)
                {
                    // 使用 false 表示只卸载 bundle 容器，不销毁已加载的对象
                    AssetBundle.UnloadAllAssetBundles(false);
                }

                // 3. 强制 GC (分步进行以减缓卡顿)
                GC.Collect(0, GCCollectionMode.Optimized, false);
                yield return null;
                
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false);
                yield return null;

                Main.Logger?.Log(Localization.Get("CleanedMemory"));
                _isCleaning = false;
            }
        }

        [HarmonyPatch(typeof(scnGame), "Play")]
        public static class AlwaysCountdownPatch
        {
            private static bool _tempAuto;

            public static void Prefix()
            {
                if (!Main.Settings.ui.alwaysCountdown || !ADOBase.isLevelEditor) return;
                _tempAuto = RDC.auto;
                RDC.auto = false;
            }

            public static void Postfix()
            {
                if (!Main.Settings.ui.alwaysCountdown || !ADOBase.isLevelEditor) return;
                RDC.auto = _tempAuto;
            }
        }
    }
}
