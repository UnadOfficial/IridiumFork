using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Iridium.Core
{
    /// <summary>
    /// StringBuilder 对象池 - 用于减少字符串拼接时的 GC 分配
    /// 专门优化 GetOffsetText 等频繁创建临时字符串的场景
    /// </summary>
    public static class StringBuilderPool
    {
        private static ObjectPool<StringBuilder>? _pool;
        private const int InitialCapacity = 32;
        private const int MaxCapacity = 512;
        private const int InitialSize = 20;
        private const int MaxSize = 100;

        /// <summary>
        /// 获取 StringBuilder 实例
        /// </summary>
        public static StringBuilder Get()
        {
            if (_pool == null) Initialize();
            return _pool!.Get();
        }

        /// <summary>
        /// 归还 StringBuilder 到池中
        /// </summary>
        public static void Return(StringBuilder sb)
        {
            if (_pool == null) return;
            
            // 清空 StringBuilder 再返回
            sb.Clear();
            _pool.Return(sb);
        }

        /// <summary>
        /// 初始化 StringBuilder 池
        /// </summary>
        public static void Initialize()
        {
            if (_pool != null) return;
            
            _pool = new ObjectPool<StringBuilder>(
                factory: () => new StringBuilder(InitialCapacity),
                onGet: sb => sb.Clear(),
                onReturn: sb => sb.Clear(),
                initialSize: InitialSize,
                maxSize: MaxSize
            );
            
            PoolManager.RegisterPool(_pool);
            Main.Logger?.Log("[StringBuilderPool] Initialized");
        }

        /// <summary>
        /// 使用 StringBuilder 池执行操作
        /// 自动管理获取和归还，避免忘记归还导致的内存泄漏
        /// </summary>
        public static TResult With<TResult>(Func<StringBuilder, TResult> action)
        {
            var sb = Get();
            try
            {
                return action(sb);
            }
            finally
            {
                Return(sb);
            }
        }

        /// <summary>
        /// 使用 StringBuilder 池执行操作（无返回值）
        /// </summary>
        public static void With(Action<StringBuilder> action)
        {
            var sb = Get();
            try
            {
                action(sb);
            }
            finally
            {
                Return(sb);
            }
        }

        /// <summary>
        /// 快速获取格式化的偏移文本 (如 "+5ms" 或 "-10ms")
        /// 这是 GetOffsetText 的优化版本，减少 GC 分配
        /// </summary>
        public static string GetOffsetText(double timing)
        {
            if (double.IsNaN(timing) || double.IsInfinity(timing))
                return "0ms";
            
            long ms = (long)Math.Round(timing);
            string sign = ms >= 0 ? "+" : "-";
            long absMs = Math.Abs(ms);
            
            // 使用池化的 StringBuilder
            return With(sb =>
            {
                sb.Append(sign);
                sb.Append(absMs);
                sb.Append("ms");
                return sb.ToString();
            });
        }

        /// <summary>
        /// 预热池
        /// </summary>
        public static void Warmup(int count = 20)
        {
            if (_pool == null) Initialize();
            _pool?.Warmup(count);
        }

        /// <summary>
        /// 获取池统计信息
        /// </summary>
        public static PoolStats? GetStats()
        {
            return _pool?.GetStats();
        }
    }
}
