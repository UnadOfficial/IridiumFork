using System;
using System.Collections.Generic;
using UnityEngine;

namespace Iridium.Core
{
    /// <summary>
    /// 通用对象池 - 用于减少 GC 分配
    /// 支持任意引用类型对象的池化管理
    /// </summary>
    /// <typeparam name="T">要池化的类型</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _pool;
        private readonly Func<T> _factory;
        private readonly Action<T>? _onGet;
        private readonly Action<T>? _onReturn;
        private readonly int _maxSize;
        private int _totalCreated;
        private int _totalPooled;
        private int _currentActive;

        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="factory">对象创建工厂</param>
        /// <param name="onGet">从池获取时的回调</param>
        /// <param name="onReturn">返回池时的回调</param>
        /// <param name="initialSize">初始大小</param>
        /// <param name="maxSize">最大容量</param>
        public ObjectPool(
            Func<T> factory,
            Action<T>? onGet = null,
            Action<T>? onReturn = null,
            int initialSize = 10,
            int maxSize = 100)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onReturn = onReturn;
            _maxSize = Math.Max(initialSize, maxSize);
            _pool = new Stack<T>(initialSize);
            
            // 预创建初始对象
            for (int i = 0; i < initialSize; i++)
            {
                T obj = _factory();
                _pool.Push(obj);
                _totalCreated++;
            }
            
            Main.Logger?.Log($"[ObjectPool<{typeof(T).Name}>] Created with initial={initialSize}, max={maxSize}");
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        public T Get()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
                _totalPooled++;
            }
            else
            {
                // 池为空，创建新对象
                obj = _factory();
                _totalCreated++;
            }
            
            _currentActive++;
            _onGet?.Invoke(obj);
            
            return obj;
        }

        /// <summary>
        /// 将对象返回池中
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;
            
            _currentActive--;
            _onReturn?.Invoke(obj);
            
            if (_pool.Count < _maxSize)
            {
                _pool.Push(obj);
            }
            // 如果池已满，对象将被 GC 回收（不推入池）
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
            _currentActive = 0;
            Main.Logger?.Log($"[ObjectPool<{typeof(T).Name}>] Cleared");
        }

        /// <summary>
        /// 获取池状态信息
        /// </summary>
        public PoolStats GetStats()
        {
            return new PoolStats
            {
                Type = typeof(T).Name,
                PoolSize = _pool.Count,
                ActiveCount = _currentActive,
                TotalCreated = _totalCreated,
                TotalPooled = _totalPooled,
                MaxSize = _maxSize
            };
        }

        /// <summary>
        /// 预热池 - 预先创建指定数量的对象
        /// </summary>
        public void Warmup(int count)
        {
            int toCreate = Math.Min(count, _maxSize - _pool.Count);
            for (int i = 0; i < toCreate; i++)
            {
                T obj = _factory();
                _pool.Push(obj);
                _totalCreated++;
            }
            Main.Logger?.Log($"[ObjectPool<{typeof(T).Name}>] Warmed up {toCreate} objects");
        }
    }

    /// <summary>
    /// 对象池统计信息
    /// </summary>
    public struct PoolStats
    {
        public string Type;
        public int PoolSize;
        public int ActiveCount;
        public int TotalCreated;
        public int TotalPooled;
        public int MaxSize;

        public override string ToString()
        {
            return $"{Type}: Pool={PoolSize}, Active={ActiveCount}, Created={TotalCreated}, Pooled={TotalPooled}, Max={MaxSize}";
        }
    }

    /// <summary>
    /// 对象池管理器 - 管理所有对象池的生命周期
    /// </summary>
    public static class PoolManager
    {
        private static readonly List<IPool> _pools = new();
        private static bool _isInitialized;

        private interface IPool
        {
            void Clear();
            PoolStats GetStats();
        }

        private class PoolWrapper<T> : IPool where T : class
        {
            private readonly ObjectPool<T> _pool;
            public PoolWrapper(ObjectPool<T> pool) => _pool = pool;
            public void Clear() => _pool.Clear();
            public PoolStats GetStats() => _pool.GetStats();
        }

        /// <summary>
        /// 初始化池管理器
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            Main.Logger?.Log("[PoolManager] Initialized");
        }

        /// <summary>
        /// 注册对象池
        /// </summary>
        public static void RegisterPool<T>(ObjectPool<T> pool) where T : class
        {
            lock (_pools)
            {
                _pools.Add(new PoolWrapper<T>(pool));
            }
        }

        /// <summary>
        /// 清除所有池
        /// </summary>
        public static void ClearAll()
        {
            lock (_pools)
            {
                foreach (var pool in _pools)
                {
                    pool.Clear();
                }
                _pools.Clear();
            }
            Main.Logger?.Log("[PoolManager] All pools cleared");
        }

        /// <summary>
        /// 获取所有池的统计信息
        /// </summary>
        public static List<PoolStats> GetAllStats()
        {
            var stats = new List<PoolStats>();
            lock (_pools)
            {
                foreach (var pool in _pools)
                {
                    stats.Add(pool.GetStats());
                }
            }
            return stats;
        }

        /// <summary>
        /// 打印所有池的状态到日志
        /// </summary>
        public static void LogAllStats()
        {
            var stats = GetAllStats();
            if (stats.Count == 0)
            {
                Main.Logger?.Log("[PoolManager] No pools registered");
                return;
            }

            Main.Logger?.Log("[PoolManager] Pool Statistics:");
            foreach (var stat in stats)
            {
                Main.Logger?.Log($"  {stat}");
            }
        }
    }
}
