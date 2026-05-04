using System;
using UnityEngine;

namespace Iridium.Core
{
    /// <summary>
    /// GC 优化工具类 - 提供 GC 相关的辅助功能
    /// 包括智能 GC 触发、内存监控等功能
    /// </summary>
    public static class GCUtils
    {
        private static int _lastGCFrame = -1;
        private static float _lastGCTime = -1f;
        private static long _lastAllocatedMemory = 0;
        private static bool _isInitialized;
        
        // GC 统计信息
        private static int _totalGCCount = 0;
        private static float _totalGCTime = 0f;
        private static int _gcCountThisSession = 0;

        /// <summary>
        /// 初始化 GC 工具
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            
            _isInitialized = true;
            _lastGCFrame = Time.frameCount;
            _lastGCTime = Time.time;
            _lastAllocatedMemory = GC.GetTotalMemory(false);
            
            Main.Logger?.Log("[GCUtils] Initialized");
        }

        /// <summary>
        /// 智能执行 GC - 只在合适时机触发
        /// 避免在帧率敏感时刻（如音符判定）执行 GC
        /// </summary>
        /// <param name="force">是否强制执行</param>
        /// <param name="wait">是否等待 GC 完成</param>
        public static void SmartGC(bool force = false, bool wait = true)
        {
            if (!Main.Settings.memory.enableSmartGC && !force)
                return;

            // 检查是否过于频繁
            float timeSinceLastGC = Time.time - _lastGCTime;
            int framesSinceLastGC = Time.frameCount - _lastGCFrame;
            
            // 最小间隔：至少间隔 N 秒或 N 帧
            float minInterval = Main.Settings.memory.gcInterval / 1000f; // 转换为秒
            int minFrames = 60; // 至少间隔 60 帧
            
            if (!force && (timeSinceLastGC < minInterval || framesSinceLastGC < minFrames))
                return;

            // 避免在游戏进行中执行 GC（除非配置允许）
            if (!Main.Settings.memory.gcInGame && IsInGameplay() && !force)
                return;

            // 执行 GC
            ExecuteGC(wait);
        }

        /// <summary>
        /// 执行垃圾回收
        /// </summary>
        private static void ExecuteGC(bool wait)
        {
            try
            {
                long beforeMemory = GC.GetTotalMemory(false);
                float startTime = Time.realtimeSinceStartup;

                if (wait)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                else
                {
                    // 异步 GC（不推荐，但可用于非关键路径）
                    System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    });
                }

                long afterMemory = GC.GetTotalMemory(false);
                float gcDuration = Time.realtimeSinceStartup - startTime;
                long freedMemory = beforeMemory - afterMemory;

                // 更新统计
                _lastGCFrame = Time.frameCount;
                _lastGCTime = Time.time;
                _totalGCCount++;
                _totalGCTime += gcDuration;
                _gcCountThisSession++;

                Main.Logger?.Log($"[GCUtils] GC executed: Freed={FormatSize(freedMemory)}, Duration={gcDuration:F3}ms, Total={_gcCountThisSession}");
            }
            catch (Exception e)
            {
                Main.Logger?.Error($"[GCUtils] GC failed: {e}");
            }
        }

        /// <summary>
        /// 检查是否在游戏进行中
        /// </summary>
        private static bool IsInGameplay()
        {
            // 简单判断：如果有 scrController 实例且游戏未暂停，则认为在游戏进行中
            var controller = scrController.instance;
            return controller != null && !controller.paused;
        }

        /// <summary>
        /// 获取当前内存使用量
        /// </summary>
        public static long GetCurrentMemoryUsage()
        {
            return GC.GetTotalMemory(false);
        }

        /// <summary>
        /// 获取自上次检查以来的内存分配量
        /// </summary>
        public static long GetMemoryDelta()
        {
            long currentMemory = GC.GetTotalMemory(false);
            long delta = currentMemory - _lastAllocatedMemory;
            _lastAllocatedMemory = currentMemory;
            return delta;
        }

        /// <summary>
        /// 获取 GC 统计信息
        /// </summary>
        public static GCStats GetStats()
        {
            return new GCStats
            {
                TotalGCCount = _totalGCCount,
                TotalGCTime = _totalGCTime,
                GCTimeThisSession = _gcCountThisSession,
                CurrentMemory = GetCurrentMemoryUsage(),
                LastGCFrame = _lastGCFrame,
                LastGCTime = _lastGCTime
            };
        }

        /// <summary>
        /// 打印 GC 统计信息到日志
        /// </summary>
        public static void LogStats()
        {
            var stats = GetStats();
            Main.Logger?.Log($"[GCUtils] Statistics:");
            Main.Logger?.Log($"  Total GC Count: {stats.TotalGCCount}");
            Main.Logger?.Log($"  Total GC Time: {stats.TotalGCTime:F3}s");
            Main.Logger?.Log($"  GC This Session: {stats.GCTimeThisSession}");
            Main.Logger?.Log($"  Current Memory: {FormatSize(stats.CurrentMemory)}");
            Main.Logger?.Log($"  Last GC Frame: {stats.LastGCFrame}");
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public static void ResetStats()
        {
            _totalGCCount = 0;
            _totalGCTime = 0f;
            _gcCountThisSession = 0;
            Main.Logger?.Log("[GCUtils] Statistics reset");
        }

        /// <summary>
        /// 格式化内存大小
        /// </summary>
        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// GC 统计信息结构
        /// </summary>
        public struct GCStats
        {
            public int TotalGCCount;
            public float TotalGCTime;
            public int GCTimeThisSession;
            public long CurrentMemory;
            public int LastGCFrame;
            public float LastGCTime;
        }
    }
}
