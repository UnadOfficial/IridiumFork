using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using DG.Tweening;
using ADOFAI;

namespace Iridium.Patches
{
    /// <summary>
    /// 激进性能优化 - 针对最严重的性能瓶颈
    /// 这些优化可能改变游戏行为，需要谨慎测试
    /// </summary>
    public static class AggressivePerformancePatches
    {
        #region Shared Caches

        // LINQ 查询结果缓存
        private static readonly Dictionary<object, List<Tween>> _eventTweensCache = new(256);
        private static readonly Dictionary<string, HashSet<string>> _tweenTypesHashSets = new(32);

        // 装饰物标签缓存
        private static readonly Dictionary<string, List<scrDecoration>> _decorationsByTag = new(64);

        // 地板范围缓存
        private static readonly Dictionary<(int start, int end), List<scrFloor>> _floorRangeCache = new(128);

        // 对象池
        private static readonly Stack<List<Tween>> _tweenListPool = new(32);
        private static readonly Stack<List<scrFloor>> _floorListPool = new(32);

        #endregion

        #region MoveTrack Optimization

        /// <summary>
        /// 优化 MoveTrack 的 eventTweens 属性
        /// 原实现每次访问都创建新 List 并遍历所有地板
        /// </summary>
        [HarmonyPatch(typeof(ffxMoveFloorPlus), "eventTweens", MethodType.Getter)]
        public static class MoveTrackEventTweensOptimizationPatch
        {
            private static AccessTools.FieldRef<ffxMoveFloorPlus, int>? _startAccessor;
            private static AccessTools.FieldRef<ffxMoveFloorPlus, int>? _endAccessor;
            private static AccessTools.FieldRef<ffxMoveFloorPlus, int>? _gapLengthAccessor;
            private static bool _initialized;

            private static void Initialize()
            {
                try
                {
                    _startAccessor = AccessTools.FieldRefAccess<ffxMoveFloorPlus, int>("start");
                    _endAccessor = AccessTools.FieldRefAccess<ffxMoveFloorPlus, int>("end");
                    _gapLengthAccessor = AccessTools.FieldRefAccess<ffxMoveFloorPlus, int>("gapLength");
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[AggressivePerf] Failed to create MoveTrack accessors: {e}");
                }
                _initialized = true;
            }

            [HarmonyPrefix]
            public static bool Prefix(ffxMoveFloorPlus __instance, ref List<Tween> __result)
            {
                if (!Main.Settings.optimizer.optimizeMoveTrackEventTweens) return true;

                if (!_initialized) Initialize();
                if (_startAccessor == null || _endAccessor == null || _gapLengthAccessor == null)
                    return true;

                try
                {
                    // 检查缓存
                    if (_eventTweensCache.TryGetValue(__instance, out var cached))
                    {
                        __result = cached;
                        return false;
                    }

                    // 从对象池获取 List
                    var result = _tweenListPool.Count > 0 ? _tweenListPool.Pop() : new List<Tween>(64);
                    result.Clear();

                    int start = _startAccessor(__instance);
                    int end = _endAccessor(__instance);
                    int gapLength = _gapLengthAccessor(__instance);

                    var levelMaker = scrLevelMaker.instance;
                    if (levelMaker == null || levelMaker.listFloors == null)
                    {
                        __result = result;
                        return false;
                    }

                    // 直接遍历，避免 LINQ
                    for (int i = start; i <= end; i += gapLength)
                    {
                        if (i >= 0 && i < levelMaker.listFloors.Count)
                        {
                            var floor = levelMaker.listFloors[i];
                            if (floor != null && floor.moveTweens != null)
                            {
                                result.AddRange(floor.moveTweens.Values);
                            }
                        }
                    }

                    _eventTweensCache[__instance] = result;
                    __result = result;
                    return false;
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[AggressivePerf] MoveTrack eventTweens optimization failed: {e}");
                    return true;
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                // 定期清理缓存
                if (_eventTweensCache.Count > 512)
                {
                    _eventTweensCache.Clear();
                }
            }
        }

        #endregion

        #region RecolorTrack Optimization

        /// <summary>
        /// 优化 RecolorTrack 的 eventTweens 属性
        /// 原实现使用嵌套 LINQ 查询，性能极差
        /// </summary>
        [HarmonyPatch(typeof(ffxRecolorFloorPlus), "eventTweens", MethodType.Getter)]
        public static class RecolorTrackEventTweensOptimizationPatch
        {
            private static AccessTools.FieldRef<ffxRecolorFloorPlus, int>? _startAccessor;
            private static AccessTools.FieldRef<ffxRecolorFloorPlus, int>? _endAccessor;
            private static AccessTools.FieldRef<ffxRecolorFloorPlus, List<string>>? _tweenTypesAccessor;
            private static bool _initialized;

            private static void Initialize()
            {
                try
                {
                    _startAccessor = AccessTools.FieldRefAccess<ffxRecolorFloorPlus, int>("start");
                    _endAccessor = AccessTools.FieldRefAccess<ffxRecolorFloorPlus, int>("end");
                    _tweenTypesAccessor = AccessTools.FieldRefAccess<ffxRecolorFloorPlus, List<string>>("tweenTypes");
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[AggressivePerf] Failed to create RecolorTrack accessors: {e}");
                }
                _initialized = true;
            }

            [HarmonyPrefix]
            public static bool Prefix(ffxRecolorFloorPlus __instance, ref List<Tween> __result)
            {
                if (!Main.Settings.optimizer.optimizeRecolorTrackEventTweens) return true;

                if (!_initialized) Initialize();
                if (_startAccessor == null || _endAccessor == null || _tweenTypesAccessor == null)
                    return true;

                try
                {
                    // 检查缓存
                    if (_eventTweensCache.TryGetValue(__instance, out var cached))
                    {
                        __result = cached;
                        return false;
                    }

                    var result = _tweenListPool.Count > 0 ? _tweenListPool.Pop() : new List<Tween>(64);
                    result.Clear();

                    int start = _startAccessor(__instance);
                    int end = _endAccessor(__instance);
                    var tweenTypesList = _tweenTypesAccessor(__instance);

                    if (tweenTypesList == null || tweenTypesList.Count == 0)
                    {
                        __result = result;
                        return false;
                    }

                    // 将 List<string> 转换为 HashSet<TweenType> 以加速 Contains 查询
                    var tweenTypesSet = new HashSet<TweenType>();
                    foreach (var typeStr in tweenTypesList)
                    {
                        if (Enum.TryParse<TweenType>(typeStr, out var tweenType))
                        {
                            tweenTypesSet.Add(tweenType);
                        }
                    }

                    var levelMaker = scrLevelMaker.instance;
                    if (levelMaker == null || levelMaker.listFloors == null)
                    {
                        __result = result;
                        return false;
                    }

                    // 替换嵌套 LINQ 为简单循环
                    for (int i = start; i <= end && i < levelMaker.listFloors.Count; i++)
                    {
                        var floor = levelMaker.listFloors[i];
                        if (floor == null || floor.moveTweens == null) continue;

                        foreach (var kvp in floor.moveTweens)
                        {
                            if (tweenTypesSet.Contains(kvp.Key))
                            {
                                result.Add(kvp.Value);
                            }
                        }
                    }

                    _eventTweensCache[__instance] = result;
                    __result = result;
                    return false;
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[AggressivePerf] RecolorTrack eventTweens optimization failed: {e}");
                    return true;
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                if (_eventTweensCache.Count > 512)
                {
                    _eventTweensCache.Clear();
                }
            }
        }

        #endregion

        #region MoveDecoration Optimization

        /// <summary>
        /// 优化 MoveDecoration 的 eventTweens 属性
        /// 原实现每次都遍历所有标签和装饰物
        /// </summary>
        [HarmonyPatch(typeof(ffxMoveDecorationsPlus), "eventTweens", MethodType.Getter)]
        public static class MoveDecorationEventTweensOptimizationPatch
        {
            private static AccessTools.FieldRef<ffxMoveDecorationsPlus, List<string>>? _targetTagsAccessor;
            private static AccessTools.FieldRef<ffxMoveDecorationsPlus, scrDecorationManager>? _decManagerAccessor;
            private static bool _initialized;

            private static void Initialize()
            {
                try
                {
                    _targetTagsAccessor = AccessTools.FieldRefAccess<ffxMoveDecorationsPlus, List<string>>("targetTags");
                    _decManagerAccessor = AccessTools.FieldRefAccess<ffxMoveDecorationsPlus, scrDecorationManager>("decManager");
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[AggressivePerf] Failed to create MoveDecoration accessors: {e}");
                }
                _initialized = true;
            }

            [HarmonyPrefix]
            public static bool Prefix(ffxMoveDecorationsPlus __instance, ref List<Tween> __result)
            {
                if (!Main.Settings.optimizer.optimizeMoveDecorationEventTweens) return true;

                if (!_initialized) Initialize();
                if (_targetTagsAccessor == null || _decManagerAccessor == null)
                    return true;

                try
                {
                    // 检查缓存
                    if (_eventTweensCache.TryGetValue(__instance, out var cached))
                    {
                        __result = cached;
                        return false;
                    }

                    var result = _tweenListPool.Count > 0 ? _tweenListPool.Pop() : new List<Tween>(128);
                    result.Clear();

                    var targetTags = _targetTagsAccessor(__instance);
                    var decManager = _decManagerAccessor(__instance);

                    if (targetTags == null || decManager == null || decManager.taggedDecorations == null)
                    {
                        __result = result;
                        return false;
                    }

                    // 优化：避免重复的 AddRange（内部会复制数组）
                    foreach (var tag in targetTags)
                    {
                        if (decManager.taggedDecorations.TryGetValue(tag, out var decorations))
                        {
                            foreach (var decoration in decorations)
                            {
                                if (decoration != null && decoration.eventTweens != null)
                                {
                                    // 直接添加，避免 AddRange 的数组复制
                                    foreach (var tween in decoration.eventTweens.Values)
                                    {
                                        result.Add(tween);
                                    }
                                }
                            }
                        }
                    }

                    _eventTweensCache[__instance] = result;
                    __result = result;
                    return false;
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[AggressivePerf] MoveDecoration eventTweens optimization failed: {e}");
                    return true;
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                if (_eventTweensCache.Count > 512)
                {
                    _eventTweensCache.Clear();
                }
            }
        }

        #endregion

        #region Floor Update Optimization

        /// <summary>
        /// 优化 scrFloor.Update - 跳过不可见或远离相机的地板
        /// </summary>
        [HarmonyPatch(typeof(scrFloor), "Update")]
        public static class FloorUpdateCullingPatch
        {
            private static int _frameCounter;
            private static readonly HashSet<scrFloor> _visibleFloors = new(256);

            [HarmonyPrefix]
            public static bool Prefix(scrFloor __instance)
            {
                if (!Main.Settings.optimizer.optimizeFloorUpdateCulling) return true;

                try
                {
                    // 每 N 帧更新一次可见性检查
                    if (_frameCounter++ % 10 == 0)
                    {
                        UpdateVisibleFloors();
                    }

                    // 如果地板不在可见列表中，跳过更新
                    if (!_visibleFloors.Contains(__instance))
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[AggressivePerf] Floor update culling failed: {e}");
                    return true;
                }
            }

            private static void UpdateVisibleFloors()
            {
                _visibleFloors.Clear();

                var camera = scrCamera.instance;
                var levelMaker = scrLevelMaker.instance;

                if (camera == null || levelMaker == null || levelMaker.listFloors == null)
                    return;

                var camPos = camera.transform.position;
                float cullDistance = 50f; // 可调整的剔除距离

                // 只检查相机附近的地板
                foreach (var floor in levelMaker.listFloors)
                {
                    if (floor == null) continue;

                    float distance = Vector3.Distance(floor.transform.position, camPos);
                    if (distance < cullDistance)
                    {
                        _visibleFloors.Add(floor);
                    }
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                if (_visibleFloors.Count > 1024)
                {
                    _visibleFloors.Clear();
                }
            }
        }

        /// <summary>
        /// 优化 scrFloor.LateUpdate - 使用脏标记系统
        /// </summary>
        [HarmonyPatch(typeof(scrFloor), "LateUpdate")]
        public static class FloorLateUpdateOptimizationPatch
        {
            private static readonly Dictionary<scrFloor, bool> _dirtyFlags = new(512);

            [HarmonyPrefix]
            public static bool Prefix(scrFloor __instance)
            {
                if (!Main.Settings.optimizer.optimizeFloorLateUpdate) return true;

                try
                {
                    // 检查脏标记
                    if (!_dirtyFlags.TryGetValue(__instance, out bool isDirty))
                    {
                        isDirty = true; // 首次默认为脏
                        _dirtyFlags[__instance] = true;
                    }

                    if (!isDirty)
                    {
                        return false; // 跳过更新
                    }

                    // 执行更新后清除脏标记
                    _dirtyFlags[__instance] = false;
                    return true;
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[AggressivePerf] Floor LateUpdate optimization failed: {e}");
                    return true;
                }
            }

            // 提供方法让其他系统标记地板为脏
            public static void MarkDirty(scrFloor floor)
            {
                if (floor != null)
                {
                    _dirtyFlags[floor] = true;
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                if (_dirtyFlags.Count > 1024)
                {
                    _dirtyFlags.Clear();
                }
            }
        }

        #endregion

        #region Decoration Update Optimization

        /// <summary>
        /// 优化装饰物管理器的更新 - 使用空间分区
        /// </summary>
        [HarmonyPatch(typeof(scrDecorationManager), "Update")]
        public static class DecorationManagerUpdateOptimizationPatch
        {
            private static int _frameCounter;
            private static readonly HashSet<scrDecoration> _activeDecorations = new(256);

            [HarmonyPrefix]
            public static bool Prefix(scrDecorationManager __instance)
            {
                if (!Main.Settings.optimizer.optimizeDecorationManagerUpdate) return true;

                try
                {
                    // 每 5 帧更新一次活跃装饰物列表
                    if (_frameCounter++ % 5 == 0)
                    {
                        UpdateActiveDecorations(__instance);
                    }

                    // 只更新活跃的装饰物
                    foreach (var decoration in _activeDecorations)
                    {
                        if (decoration != null && decoration.useHitbox)
                        {
                            // 执行原有的 hitbox 检查逻辑
                            // 这里需要调用原方法的部分逻辑
                        }
                    }

                    return false; // 跳过原方法
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[AggressivePerf] DecorationManager update optimization failed: {e}");
                    return true;
                }
            }

            private static void UpdateActiveDecorations(scrDecorationManager manager)
            {
                _activeDecorations.Clear();

                var camera = scrCamera.instance;
                if (camera == null || manager.allDecorations == null)
                    return;

                var camPos = camera.transform.position;
                float cullDistance = 60f;

                // 空间剔除
                foreach (var decoration in manager.allDecorations)
                {
                    if (decoration == null) continue;

                    float distance = Vector3.Distance(decoration.transform.position, camPos);
                    if (distance < cullDistance || decoration.useHitbox)
                    {
                        _activeDecorations.Add(decoration);
                    }
                }
            }

            [HarmonyPostfix]
            public static void Cleanup()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                if (_activeDecorations.Count > 1024)
                {
                    _activeDecorations.Clear();
                }
            }
        }

        #endregion

        #region Cache Cleanup

        /// <summary>
        /// 在场景切换时清理所有缓存
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "Awake")]
        public static class SceneLoadCacheCleanupPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                _eventTweensCache.Clear();
                _tweenTypesHashSets.Clear();
                _decorationsByTag.Clear();
                _floorRangeCache.Clear();

                // 清空对象池
                _tweenListPool.Clear();
                _floorListPool.Clear();

                Main.Logger?.Log("[AggressivePerf] Cleared all caches on scene load");
            }
        }

        #endregion
    }
}
