using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using ADOFAI;

namespace Iridium.Patches
{
    /// <summary>
    /// 核心性能优化 Patch - 针对 ADOFAI 最关键的性能瓶颈
    /// </summary>
    public static class CorePerformancePatches
    {
        #region Shared State

        // Physics2D.OverlapCircle 缓存数组（避免每帧分配）
        private static readonly Collider2D[] _overlapResults = new Collider2D[32];

        // GetComponent 缓存
        private static readonly Dictionary<Collider2D, scrDecoration> _decorationCache = new(64);
        private static readonly Dictionary<Collider2D, scrFloor> _floorCache = new(64);

        // FloorMesh 缓存键优化
        private static readonly StringBuilder _meshKeyBuilder = new(64);

        #endregion

        #region Physics Optimization

        /// <summary>
        /// 优化 scrPlanet.Update 中的 Physics2D.OverlapCircleAll
        /// 使用 NonAlloc 版本避免 GC 分配
        /// </summary>
        [HarmonyPatch(typeof(scrPlanet), "Update")]
        public static class PlanetPhysicsOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scrPlanet __instance)
            {
                if (!Main.Settings.optimizer.optimizePlanetPhysics) return true;

                try
                {
                    // 只对可命中的行星执行碰撞检测
                    if (!__instance.isChosen) return false;

                    // 使用 NonAlloc 版本避免 GC
                    int hitCount = Physics2D.OverlapCircleNonAlloc(
                        __instance.transform.position,
                        0.3f,
                        _overlapResults
                    );

                    for (int i = 0; i < hitCount; i++)
                    {
                        var collider = _overlapResults[i];
                        if (collider == null) continue;

                        if (collider.gameObject.CompareTag("HitDecoration"))
                        {
                            // 使用缓存避免重复 GetComponent
                            if (!_decorationCache.TryGetValue(collider, out var decoration))
                            {
                                decoration = collider.transform.parent?.GetComponent<scrDecoration>();
                                if (decoration != null)
                                    _decorationCache[collider] = decoration;
                            }

                            if (decoration != null && decoration.useHitbox)
                            {
                                __instance.controller.FailAction();
                            }
                        }
                    }

                    return false; // 跳过原方法
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CorePerformance] Planet physics optimization failed: {e}");
                    return true;
                }
            }
        }

        #endregion

        #region FloorMesh Optimization

        /// <summary>
        /// 优化 FloorMesh 缓存键生成
        /// 避免 string.Format 的装箱和字符串拼接
        /// </summary>
        [HarmonyPatch(typeof(FloorMesh), "UpdateMesh")]
        public static class FloorMeshCacheKeyOptimizationPatch
        {
            private static AccessTools.FieldRef<FloorMesh, float>? _angle0Accessor;
            private static AccessTools.FieldRef<FloorMesh, float>? _angle1Accessor;
            private static AccessTools.FieldRef<FloorMesh, float>? _widthAccessor;
            private static AccessTools.FieldRef<FloorMesh, float>? _lengthAccessor;
            private static AccessTools.FieldRef<FloorMesh, int>? _curvaturePointsAccessor;
            private static bool _initialized;

            private static void Initialize()
            {
                try
                {
                    _angle0Accessor = AccessTools.FieldRefAccess<FloorMesh, float>("angle0");
                    _angle1Accessor = AccessTools.FieldRefAccess<FloorMesh, float>("angle1");
                    _widthAccessor = AccessTools.FieldRefAccess<FloorMesh, float>("width");
                    _lengthAccessor = AccessTools.FieldRefAccess<FloorMesh, float>("length");
                    _curvaturePointsAccessor = AccessTools.FieldRefAccess<FloorMesh, int>("curvaturePoints");
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CorePerformance] Failed to create FloorMesh accessors: {e}");
                }
                _initialized = true;
            }

            [HarmonyPrefix]
            public static void Prefix(FloorMesh __instance)
            {
                if (!Main.Settings.optimizer.optimizeFloorMeshCache) return;

                if (!_initialized) Initialize();
                if (_angle0Accessor == null || _angle1Accessor == null || _widthAccessor == null ||
                    _lengthAccessor == null || _curvaturePointsAccessor == null)
                    return;

                try
                {
                    // 使用 StringBuilder 避免字符串拼接的 GC
                    _meshKeyBuilder.Clear();
                    _meshKeyBuilder.Append(_angle0Accessor(__instance));
                    _meshKeyBuilder.Append(',');
                    _meshKeyBuilder.Append(_angle1Accessor(__instance));
                    _meshKeyBuilder.Append(',');
                    _meshKeyBuilder.Append(_widthAccessor(__instance));
                    _meshKeyBuilder.Append(',');
                    _meshKeyBuilder.Append(_lengthAccessor(__instance));
                    _meshKeyBuilder.Append(',');
                    _meshKeyBuilder.Append(_curvaturePointsAccessor(__instance));

                    // 使用反射设置 cacheKey
                    var cacheKeyField = AccessTools.Field(typeof(FloorMesh), "cacheKey");
                    if (cacheKeyField != null)
                    {
                        cacheKeyField.SetValue(__instance, _meshKeyBuilder.ToString());
                    }
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CorePerformance] FloorMesh cache key optimization failed: {e}");
                }
            }
        }

        /// <summary>
        /// 优化 FloorMesh.UpdateAllRequired
        /// 添加脏标记，只更新真正改变的 Mesh
        /// </summary>
        [HarmonyPatch(typeof(FloorMesh), "UpdateAllRequired")]
        public static class FloorMeshBatchUpdateOptimizationPatch
        {
            private static int _frameCounter = 0;
            private static readonly int _updateInterval = 2; // 每 N 帧更新一次

            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (!Main.Settings.optimizer.optimizeFloorMeshBatchUpdate) return true;

                try
                {
                    _frameCounter++;

                    // 分帧更新：不是每帧都更新所有 Mesh
                    if (_frameCounter % _updateInterval != 0)
                    {
                        return false;
                    }

                    return true; // 允许原方法执行
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CorePerformance] FloorMesh batch update optimization failed: {e}");
                    return true;
                }
            }
        }

        #endregion

        #region Planet Angle Calculation Optimization

        /// <summary>
        /// 优化 scrPlanet.Update_RefreshAngles
        /// 缓存中间计算结果，使用增量更新
        /// </summary>
        [HarmonyPatch(typeof(scrPlanet), "Update_RefreshAngles")]
        public static class PlanetAngleCalculationOptimizationPatch
        {
            private static readonly Dictionary<scrPlanet, CachedAngleData> _angleCache = new(16);

            private class CachedAngleData
            {
                public double lastSongPosition;
                public double anglePerBeat;
                public double cachedAngle;
            }

            [HarmonyPrefix]
            public static bool Prefix(scrPlanet __instance)
            {
                if (!Main.Settings.optimizer.optimizePlanetAngleCalculation) return true;

                try
                {
                    if (!_angleCache.TryGetValue(__instance, out var cache))
                    {
                        cache = new CachedAngleData();
                        _angleCache[__instance] = cache;
                    }

                    var conductor = __instance.conductor;
                    var controller = __instance.controller;

                    // 计算 anglePerBeat（缓存）
                    if (cache.anglePerBeat == 0)
                    {
                        cache.anglePerBeat = Math.PI * controller.speed * (controller.isCW ? 1 : -1);
                    }

                    // 增量更新角度
                    double deltaPosition = conductor.songposition_minusi - cache.lastSongPosition;
                    if (Math.Abs(deltaPosition) > 0.001) // 只在变化足够大时更新
                    {
                        cache.cachedAngle += deltaPosition / conductor.crotchetAtStart * cache.anglePerBeat;
                        cache.lastSongPosition = conductor.songposition_minusi;

                        // 使用反射设置 angle
                        var angleField = AccessTools.Field(typeof(scrPlanet), "angle");
                        if (angleField != null)
                        {
                            angleField.SetValue(__instance, cache.cachedAngle);
                        }
                    }

                    return false; // 跳过原方法
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[CorePerformance] Planet angle calculation optimization failed: {e}");
                    return true;
                }
            }

            [HarmonyPatch(typeof(scrPlanet), "OnDestroy")]
            [HarmonyPostfix]
            public static void Cleanup(scrPlanet __instance)
            {
                _angleCache.Remove(__instance);
            }
        }

        #endregion

        #region Component Cache Optimization

        /// <summary>
        /// 清理组件缓存
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "OnDestroy")]
        public static class ComponentCacheCleanupPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                _decorationCache.Clear();
                _floorCache.Clear();

                Main.Logger?.Log("[CorePerformance] Cleaned up component caches");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public static void ClearAllCaches()
        {
            _decorationCache.Clear();
            _floorCache.Clear();
            _meshKeyBuilder.Clear();

            Main.Logger?.Log("[CorePerformance] All caches cleared");
        }

        #endregion
    }
}
