using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using DG.Tweening;

namespace Iridium.Patches
{
    /// <summary>
    /// 优化 ffx 脚本的性能 - 减少装饰物更新频率
    /// 核心策略：使用脏标记系统，只更新被修改的装饰物
    /// </summary>
    public static class FfxOptimizationPatches
    {
        // 脏标记系统 - 只更新被修改的装饰物
        private static readonly HashSet<scrDecoration> _dirtyDecorations = new();
        private static readonly object _dirtyLock = new();

        /// <summary>
        /// 标记装饰物为脏（需要更新）
        /// </summary>
        public static void MarkDirty(scrDecoration decoration)
        {
            if (decoration == null) return;
            lock (_dirtyLock)
            {
                _dirtyDecorations.Add(decoration);
            }
        }

        /// <summary>
        /// 优化 scrDecorationManager.LateUpdate - 只更新脏装饰物
        /// 原始代码每帧更新所有可见装饰物，即使它们没有变化
        /// </summary>
        [HarmonyPatch(typeof(scrDecorationManager), "LateUpdate")]
        public static class OptimizeDecorationManagerLateUpdate
        {
            static bool Prefix(scrDecorationManager __instance)
            {
                if (!Main.Settings.optimizer.enableOptimizer || !Main.Settings.optimizer.optimizeFfxDecorations)
                    return true;

                try
                {
                    // 只更新脏装饰物
                    lock (_dirtyLock)
                    {
                        if (_dirtyDecorations.Count == 0)
                        {
                            // 没有脏装饰物，跳过更新
                            return false;
                        }

                        foreach (var dec in _dirtyDecorations)
                        {
                            if (dec != null && dec.GetVisible())
                            {
                                dec.UpdatePosition();
                            }
                        }

                        // 清空脏标记
                        _dirtyDecorations.Clear();
                    }

                    return false; // 跳过原始方法
                }
                catch (Exception ex)
                {
                    Main.Logger?.Error($"[FfxOptimization] Error in OptimizeDecorationManagerLateUpdate: {ex}");
                    return true; // 出错时回退到原始方法
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetPositionX - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetPositionX")]
        public static class MarkDirtyOnSetPositionX
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetPositionY - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetPositionY")]
        public static class MarkDirtyOnSetPositionY
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetRotation - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetRotation")]
        public static class MarkDirtyOnSetRotation
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetScale - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetScale")]
        public static class MarkDirtyOnSetScale
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetColor - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetColor")]
        public static class MarkDirtyOnSetColor
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetOpacity - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetOpacity")]
        public static class MarkDirtyOnSetOpacity
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetParallaxOffsetX - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetParallaxOffsetX")]
        public static class MarkDirtyOnSetParallaxOffsetX
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetParallaxOffsetY - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetParallaxOffsetY")]
        public static class MarkDirtyOnSetParallaxOffsetY
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetPivotX - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetPivotX")]
        public static class MarkDirtyOnSetPivotX
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }

        /// <summary>
        /// 优化 scrDecoration.SetPivotY - 标记为脏
        /// </summary>
        [HarmonyPatch(typeof(scrDecoration), "SetPivotY")]
        public static class MarkDirtyOnSetPivotY
        {
            static void Postfix(scrDecoration __instance)
            {
                if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeFfxDecorations)
                {
                    MarkDirty(__instance);
                }
            }
        }
    }
}
