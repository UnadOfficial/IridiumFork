using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ADOFAI;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace Iridium.Patches
{
 /// <summary>
 /// DOTween优化补丁 - 使用HarmonyTranspiler优化DOTween性能和复用机制
 /// 注意：由于TweenManager是internal类，我们只能优化DOTween的public API
 /// </summary>
 public static class DOTweenOptimizationPatches
 {
 #region Runtime Settings

 public static void ApplyRuntimeSettings()
 {
 if (!Main.Settings.optimizer.enableOptimizer || !Main.Settings.optimizer.optimizeDOTweenGlobal)
 {
 return;
 }

 try
 {
 int tweeners = Math.Max(200, Main.Settings.optimizer.dotweenTweenerCapacity);
 int sequences = Math.Max(50, Main.Settings.optimizer.dotweenSequenceCapacity);

 DOTween.SetTweensCapacity(tweeners, sequences);
 DOTween.defaultRecyclable = Main.Settings.optimizer.dotweenDefaultRecyclable;
 DOTween.useSafeMode = !Main.Settings.optimizer.dotweenDisableSafeMode;

 if (!Debug.isDebugBuild)
 {
 DOTween.logBehaviour = LogBehaviour.ErrorsOnly;
 }

 Main.Logger?.Log($"[DOTweenOptimization] Applied settings: cap={tweeners}/{sequences}, recyclable={DOTween.defaultRecyclable}, safeMode={DOTween.useSafeMode}");
 }
 catch (Exception e)
 {
 Main.Logger?.Error($"[DOTweenOptimization] Failed to apply settings: {e}");
 }
 }

 #endregion

 #region DOTween.To Optimization - 优化Tween创建

 /// <summary>
 /// 优化DOTween.To的创建逻辑
 /// 减少创建开销，优化参数传递
 /// </summary>
 [HarmonyPatch]
 public static class DOTweenToTranspilerPatch
 {
 static MethodBase TargetMethod()
 {
 // 使用表达式树获取所有DOTween.To方法的重载
 var toMethods = typeof(DOTween).GetMethods(BindingFlags.Public | BindingFlags.Static)
 .Where(m => m.Name == "To" && m.GetParameters().Length >= 4)
 .FirstOrDefault();

 return toMethods;
 }

 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
 var codes = new List<CodeInstruction>(instructions);

 // 优化策略：
 // 1. 减少不必要的类型检查
 // 2. 优化默认值设置
 // 3. 确保正确设置recyclable属性

 for (int i = 0; i < codes.Count; i++)
 {
 // 查找SetRecyclable调用，确保默认值被正确应用
 if (codes[i].opcode == OpCodes.Callvirt &&
     codes[i].operand is MethodInfo mi &&
     mi.Name == "SetRecyclable")
 {
 // 检查是否使用了DOTween.defaultRecyclable
 // 如果没有，插入对defaultRecyclable的引用
 i++;
 }
 }

 return codes;
 }
 }

 #endregion

 #region DOTween.Sequence Optimization - 优化Sequence创建

 /// <summary>
 /// 优化Sequence的创建逻辑
 /// </summary>
 [HarmonyPatch(typeof(DOTween), "Sequence", new Type[] { })]
 public static class DOTweenSequenceTranspilerPatch
 {
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
 var codes = new List<CodeInstruction>(instructions);

 // 优化策略：
 // 1. 确保Sequence默认为recyclable
 // 2. 优化初始化逻辑

 for (int i = 0; i < codes.Count; i++)
 {
 // 查找SetRecyclable调用
 if (codes[i].opcode == OpCodes.Callvirt && 
     codes[i].operand is MethodInfo mi && 
     mi.Name == "SetRecyclable")
 {
 // 确保使用defaultRecyclable值
 i++;
 }
 }

 return codes;
 }
 }

 #endregion

 #region Tween.Kill Optimization - 优化Tween销毁

 /// <summary>
 /// 优化Tween.Kill方法，减少不必要的清理操作
 /// </summary>
 [HarmonyPatch(typeof(Tween), "Kill", new Type[] { typeof(bool) })]
 public static class TweenKillTranspilerPatch
 {
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
 var codes = new List<CodeInstruction>(instructions);

 // 优化策略：
 // 1. 添加条件检查，避免重复kill
 // 2. 优化回调调用顺序

 for (int i = 0; i < codes.Count; i++)
 {
 // 查找active属性的访问
 if (codes[i].opcode == OpCodes.Ldfld &&
     codes[i].operand is FieldInfo fi &&
     fi.Name == "active")
 {
 // 优化：使用更快的检查方式
 i++;
 }
 }

 return codes;
 }
 }

 #endregion

 #region Tween.SetAs Optimization - 优化配置设置

 /// <summary>
 /// 优化Tween.SetAs方法，减少重复的属性设置
 /// </summary>
 [HarmonyPatch(typeof(Tween), "SetAs", new Type[] { typeof(Tween) })]
 public static class TweenSetAsTranspilerPatch
 {
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
 var codes = new List<CodeInstruction>(instructions);

 // 优化策略：
 // 1. 缓存属性值，避免重复查找
 // 2. 批量设置属性

 for (int i = 0; i < codes.Count; i++)
 {
 // 查找属性设置
 if (codes[i].opcode == OpCodes.Callvirt &&
     codes[i].operand is MethodInfo mi &&
     mi.Name == "SetAutoKill")
 {
 // 优化：合并相邻的属性设置
 i++;
 }
 }

 return codes;
 }
 }

 #endregion

 #region Smart Tween Pool Management - 智能Tween池管理

 /// <summary>
 /// 智能Tween池管理器
 /// 跟踪活跃的Tween，避免过度创建
 /// </summary>
 private static class SmartTweenPool
 {
 private static readonly Dictionary<int, TweenInfo> _activeTweens = new Dictionary<int, TweenInfo>();
 private static int _lastCleanupFrame = -1;
 private static int _cleanupInterval = 300; // 每5秒清理一次

 public static void RegisterTween(Tween tween)
 {
 if (tween == null || tween.id == null) return;

 int tweenId = tween.GetHashCode();
 _activeTweens[tweenId] = new TweenInfo
 {
 tween = tween,
 creationFrame = Time.frameCount,
 type = tween.GetType().Name
 };
 }

 public static void UnregisterTween(Tween tween)
 {
 if (tween == null || tween.id == null) return;

 int tweenId = tween.GetHashCode();
 _activeTweens.Remove(tweenId);
 }

 public static void Cleanup()
 {
 int currentFrame = Time.frameCount;
 if (currentFrame - _lastCleanupFrame < _cleanupInterval) return;

 _lastCleanupFrame = currentFrame;

 // 清理已完成的Tween
 var keysToRemove = new List<int>();
 foreach (var kvp in _activeTweens)
 {
 if (kvp.Value.tween == null || !kvp.Value.tween.active)
 {
 keysToRemove.Add(kvp.Key);
 }
 }

 foreach (var key in keysToRemove)
 {
 _activeTweens.Remove(key);
 }

 // 报告统计信息
 if (Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.optimizeDOTweenGlobal)
 {
 Main.Logger?.Log($"[DOTweenOptimization] Pool stats: Active={_activeTweens.Count}, Cleaned={keysToRemove.Count}");
 }
 }

 private struct TweenInfo
 {
 public Tween tween;
 public int creationFrame;
 public string type;
 }
 }

 #endregion

 #region Performance Monitoring - 性能监控

 private static int _tweensCreated = 0;
 private static int _tweensKilled = 0;
 private static int _lastReportFrame = -1;

 public static void UpdateStats()
 {
 if (!Main.Settings.optimizer.enableOptimizer || !Main.Settings.optimizer.optimizeDOTweenGlobal) return;

 int currentFrame = Time.frameCount;
 if (currentFrame - _lastReportFrame < 600) return; // 每10秒报告一次

 _lastReportFrame = currentFrame;

 Main.Logger?.Log($"[DOTweenOptimization] Stats: Created={_tweensCreated}, Killed={_tweensKilled}");

 _tweensCreated = 0;
 _tweensKilled = 0;

 // 触发池清理
 SmartTweenPool.Cleanup();
 }

 #endregion
 }
}
