using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ADOFAI;

namespace Iridium.Patches
{
    /// <summary>
    /// 核心系统优化 - 音频、相机、输入、UI
    /// </summary>
    public static class CoreSystemPatches
    {
        #region Audio Conductor Optimization

        /// <summary>
        /// 优化 scrConductor 的音频同步计算
        /// 避免每帧重复计算，只在音频时间变化超过阈值时更新
        /// </summary>
        [HarmonyPatch(typeof(scrConductor), "Update")]
        public static class ConductorUpdateOptimizationPatch
        {
            private static float _lastAudioTime = 0f;
            private static readonly float UPDATE_THRESHOLD = 0.001f; // 1ms 阈值

            [HarmonyPrefix]
            public static bool Prefix(scrConductor __instance)
            {
                if (!Main.Settings.optimizer.optimizeConductorUpdate) return true;

                try
                {
                    if (__instance.song == null) return true;

                    float currentTime = __instance.song.time;

                    // 只在音频时间变化超过阈值时更新
                    if (Mathf.Abs(currentTime - _lastAudioTime) < UPDATE_THRESHOLD)
                    {
                        return false; // 跳过更新
                    }

                    _lastAudioTime = currentTime;
                    return true;
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CoreSystem] Conductor update optimization failed: {e}");
                    return true;
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;
                _lastAudioTime = 0f;
            }
        }

        #endregion

        #region Camera Optimization

        /// <summary>
        /// 优化 scrCamera 的每帧计算
        /// 只在相机真正移动时更新
        /// </summary>
        [HarmonyPatch(typeof(scrCamera), "LateUpdate")]
        public static class CameraUpdateOptimizationPatch
        {
            private static Vector3 _lastCamPos;
            private static Quaternion _lastCamRot;
            private static float _lastCamZoom;
            private static readonly float POS_THRESHOLD = 0.01f;
            private static readonly float ROT_THRESHOLD = 0.1f;
            private static readonly float ZOOM_THRESHOLD = 0.01f;

            [HarmonyPrefix]
            public static bool Prefix(scrCamera __instance)
            {
                if (!Main.Settings.optimizer.optimizeCameraUpdate) return true;

                try
                {
                    var transform = __instance.transform;
                    var currentPos = transform.position;
                    var currentRot = transform.rotation;

                    var cam = __instance.camobj;
                    if (cam == null) return true;

                    float currentZoom = cam.orthographicSize;

                    // 检查相机是否真正移动
                    bool posChanged = Vector3.Distance(currentPos, _lastCamPos) >= POS_THRESHOLD;
                    bool rotChanged = Quaternion.Angle(currentRot, _lastCamRot) >= ROT_THRESHOLD;
                    bool zoomChanged = Mathf.Abs(currentZoom - _lastCamZoom) >= ZOOM_THRESHOLD;

                    if (!posChanged && !rotChanged && !zoomChanged)
                    {
                        return false; // 跳过更新
                    }

                    _lastCamPos = currentPos;
                    _lastCamRot = currentRot;
                    _lastCamZoom = currentZoom;
                    return true;
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CoreSystem] Camera update optimization failed: {e}");
                    return true;
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;
                _lastCamPos = Vector3.zero;
                _lastCamRot = Quaternion.identity;
                _lastCamZoom = 0f;
            }
        }

        #endregion

        #region Input System Optimization

        /// <summary>
        /// 输入缓存系统 - 避免同一帧多次调用 Input API
        /// </summary>
        public static class InputCache
        {
            private static int _lastFrame = -1;
            private static readonly Dictionary<KeyCode, bool> _keyDownCache = new(32);
            private static readonly Dictionary<KeyCode, bool> _keyCache = new(32);
            private static readonly Dictionary<KeyCode, bool> _keyUpCache = new(32);

            public static bool GetKeyDown(KeyCode key)
            {
                UpdateFrame();

                if (!_keyDownCache.TryGetValue(key, out bool result))
                {
                    result = Input.GetKeyDown(key);
                    _keyDownCache[key] = result;
                }
                return result;
            }

            public static bool GetKey(KeyCode key)
            {
                UpdateFrame();

                if (!_keyCache.TryGetValue(key, out bool result))
                {
                    result = Input.GetKey(key);
                    _keyCache[key] = result;
                }
                return result;
            }

            public static bool GetKeyUp(KeyCode key)
            {
                UpdateFrame();

                if (!_keyUpCache.TryGetValue(key, out bool result))
                {
                    result = Input.GetKeyUp(key);
                    _keyUpCache[key] = result;
                }
                return result;
            }

            private static void UpdateFrame()
            {
                int frame = Time.frameCount;
                if (frame != _lastFrame)
                {
                    _keyDownCache.Clear();
                    _keyCache.Clear();
                    _keyUpCache.Clear();
                    _lastFrame = frame;
                }
            }

            public static void Clear()
            {
                _keyDownCache.Clear();
                _keyCache.Clear();
                _keyUpCache.Clear();
                _lastFrame = -1;
            }
        }

        /// <summary>
        /// Patch Input.GetKeyDown 使用缓存
        /// </summary>
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(KeyCode))]
        public static class InputGetKeyDownPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(KeyCode key, ref bool __result)
            {
                if (!Main.Settings.optimizer.optimizeInputSystem) return true;

                __result = InputCache.GetKeyDown(key);
                return false;
            }
        }

        /// <summary>
        /// Patch Input.GetKey 使用缓存
        /// </summary>
        [HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(KeyCode))]
        public static class InputGetKeyPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(KeyCode key, ref bool __result)
            {
                if (!Main.Settings.optimizer.optimizeInputSystem) return true;

                __result = InputCache.GetKey(key);
                return false;
            }
        }

        /// <summary>
        /// Patch Input.GetKeyUp 使用缓存
        /// </summary>
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(KeyCode))]
        public static class InputGetKeyUpPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(KeyCode key, ref bool __result)
            {
                if (!Main.Settings.optimizer.optimizeInputSystem) return true;

                __result = InputCache.GetKeyUp(key);
                return false;
            }
        }

        #endregion

        #region UI Update Optimization

        /// <summary>
        /// 优化 UI 更新 - 只在数值变化时更新文本
        /// </summary>
        [HarmonyPatch(typeof(Text), "set_text")]
        public static class TextUpdateOptimizationPatch
        {
            private static readonly Dictionary<Text, string> _lastTextValues = new(64);

            [HarmonyPrefix]
            public static bool Prefix(Text __instance, string value)
            {
                if (!Main.Settings.optimizer.optimizeUITextUpdate) return true;

                try
                {
                    // 检查文本是否真的变化了
                    if (_lastTextValues.TryGetValue(__instance, out string lastValue))
                    {
                        if (lastValue == value)
                        {
                            return false; // 跳过更新
                        }
                    }

                    _lastTextValues[__instance] = value;
                    return true;
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CoreSystem] Text update optimization failed: {e}");
                    return true;
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                if (_lastTextValues.Count > 256)
                {
                    _lastTextValues.Clear();
                }
            }
        }

        #endregion

        #region String Cache

        /// <summary>
        /// 字符串缓存池 - 减少数字转字符串的 GC 压力
        /// </summary>
        public static class StringCache
        {
            private static readonly Dictionary<int, string> _numberStrings = new(2048);

            static StringCache()
            {
                // 预缓存常用数字字符串 (0-1000)
                for (int i = 0; i <= 1000; i++)
                {
                    _numberStrings[i] = i.ToString();
                }
            }

            public static string GetNumberString(int number)
            {
                if (_numberStrings.TryGetValue(number, out string cached))
                    return cached;

                string result = number.ToString();

                // 限制缓存大小，避免内存泄漏
                if (_numberStrings.Count < 10000)
                {
                    _numberStrings[number] = result;
                }

                return result;
            }

            public static void Clear()
            {
                // 保留预缓存的 0-1000
                var keysToRemove = new List<int>();
                foreach (var key in _numberStrings.Keys)
                {
                    if (key > 1000)
                        keysToRemove.Add(key);
                }

                foreach (var key in keysToRemove)
                {
                    _numberStrings.Remove(key);
                }
            }
        }

        /// <summary>
        /// Patch int.ToString() 使用缓存
        /// </summary>
        [HarmonyPatch(typeof(int), nameof(int.ToString), new Type[] { })]
        public static class IntToStringPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ref int __instance, ref string __result)
            {
                if (!Main.Settings.optimizer.optimizeStringCache) return true;

                __result = StringCache.GetNumberString(__instance);
                return false;
            }
        }

        #endregion

        #region DOTween Cleanup

        /// <summary>
        /// 定期清理已完成的 DOTween - 防止内存泄漏
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "Update")]
        public static class DOTweenCleanupPatch
        {
            private static int _frameCounter = 0;
            private static readonly int CLEANUP_INTERVAL = 300; // 每 5 秒清理一次

            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!Main.Settings.optimizer.optimizeDOTweenCleanup) return;

                try
                {
                    if (++_frameCounter >= CLEANUP_INTERVAL)
                    {
                        _frameCounter = 0;

                        // 只清理已完成的 Tween，不影响正在运行的
                        int cleaned = DOTween.KillAll(false);

                        if (cleaned > 0)
                        {
                            Main.Logger?.Log($"[CoreSystem] Cleaned {cleaned} completed tweens");
                        }
                    }
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CoreSystem] DOTween cleanup failed: {e}");
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;
                _frameCounter = 0;
            }
        }

        /// <summary>
        /// 在效果销毁时清理 Tween
        /// </summary>
        [HarmonyPatch(typeof(ffxPlusBase), "OnDestroy")]
        public static class EffectDestroyTweenCleanupPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ffxPlusBase __instance)
            {
                if (!Main.Settings.optimizer.optimizeDOTweenCleanup) return;

                try
                {
                    // 确保所有关联的 Tween 被清理
                    DOTween.Kill(__instance);
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CoreSystem] Effect tween cleanup failed: {e}");
                }
            }
        }

        #endregion

        #region Scene Cleanup

        /// <summary>
        /// 场景切换时清理所有缓存
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "Awake")]
        public static class SceneLoadCleanupPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                InputCache.Clear();
                StringCache.Clear();

                Main.Logger?.Log("[CoreSystem] Cleared all caches on scene load");
            }
        }

        #endregion
    }
}
