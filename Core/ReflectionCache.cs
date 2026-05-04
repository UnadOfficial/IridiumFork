using System;
using UnityEngine;

namespace Iridium.Core
{
    /// <summary>
    /// 反射结果缓存 - 避免重复反射造成的性能开销
    /// 缓存 MethodInfo、PropertyInfo、FieldInfo 等反射结果
    /// </summary>
    public static class ReflectionCache
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _cache = new();
        
        /// <summary>
        /// 获取或添加缓存项
        /// </summary>
        public static T GetOrAdd<T>(string key, Func<T> factory) where T : class
        {
            if (_cache.TryGetValue(key, out var value))
            {
                return (T)value;
            }
            
            T newValue = factory();
            _cache[key] = newValue;
            
            Main.Logger?.Log($"[ReflectionCache] Cached: {key}");
            return newValue;
        }

        /// <summary>
        /// 尝试获取缓存项
        /// </summary>
        public static bool TryGetValue<T>(string key, out T value) where T : class
        {
            if (_cache.TryGetValue(key, out var obj))
            {
                value = (T)obj;
                return true;
            }
            
            value = null!;
            return false;
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
            Main.Logger?.Log("[ReflectionCache] Cleared");
        }

        /// <summary>
        /// 获取缓存大小
        /// </summary>
        public static int Count => _cache.Count;

        /// <summary>
        /// 获取类型的方法并缓存
        /// </summary>
        public static System.Reflection.MethodInfo GetMethod(Type type, string methodName, 
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | 
                                                  System.Reflection.BindingFlags.NonPublic | 
                                                  System.Reflection.BindingFlags.Instance | 
                                                  System.Reflection.BindingFlags.Static)
        {
            string key = $"Method:{type.FullName}:{methodName}:{flags}";
            return GetOrAdd(key, () => 
            {
                var method = type.GetMethod(methodName, flags);
                if (method == null)
                    throw new ArgumentException($"Method '{methodName}' not found on type '{type.Name}'");
                return method;
            });
        }

        /// <summary>
        /// 获取类型的属性并缓存
        /// </summary>
        public static System.Reflection.PropertyInfo GetProperty(Type type, string propertyName,
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | 
                                                  System.Reflection.BindingFlags.NonPublic | 
                                                  System.Reflection.BindingFlags.Instance | 
                                                  System.Reflection.BindingFlags.Static)
        {
            string key = $"Property:{type.FullName}:{propertyName}:{flags}";
            return GetOrAdd(key, () => 
            {
                var prop = type.GetProperty(propertyName, flags);
                if (prop == null)
                    throw new ArgumentException($"Property '{propertyName}' not found on type '{type.Name}'");
                return prop;
            });
        }

        /// <summary>
        /// 获取类型的字段并缓存
        /// </summary>
        public static System.Reflection.FieldInfo GetField(Type type, string fieldName,
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | 
                                                System.Reflection.BindingFlags.NonPublic | 
                                                System.Reflection.BindingFlags.Instance | 
                                                System.Reflection.BindingFlags.Static)
        {
            string key = $"Field:{type.FullName}:{fieldName}:{flags}";
            return GetOrAdd(key, () => 
            {
                var field = type.GetField(fieldName, flags);
                if (field == null)
                    throw new ArgumentException($"Field '{fieldName}' not found on type '{type.Name}'");
                return field;
            });
        }

        /// <summary>
        /// 打印缓存统计信息
        /// </summary>
        public static void LogStats()
        {
            Main.Logger?.Log($"[ReflectionCache] Total cached items: {_cache.Count}");
        }
    }
}
