using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Iridium.Patches
{
    public static class PatchManager
    {
        private static Harmony _harmony => Main.Harmony!;
        
        // Status
        private static readonly Dictionary<Type, bool> _activePatches = new();
        
        // Patch Declaration
        private class PatchDef
        {
            public Type Type;
            public Func<bool> Condition;
            public Type? Parent;
            public string Name;

            public PatchDef(Type type, Func<bool> condition, Type? parent = null)
            {
                Type = type;
                Condition = condition;
                Parent = parent;
                Name = type.Name;
            }
        }

        private static readonly List<PatchDef> _definitions = new();

        static PatchManager()
        {
            RegisterPatches();
        }

        private static void RegisterPatches()
        {
            _definitions.Clear();

            // --- Optimizer ---
            var optCond = () => Main.Settings.optimizer.enableOptimizer;
            _definitions.Add(new PatchDef(typeof(OptimizerPatches), optCond));
            _definitions.Add(new PatchDef(typeof(TrackOptimizationPatches), optCond));

            // --- Appearance ---
            var appCond = () => Main.Settings.appearance.enableMenuSkin || Main.Settings.appearance.enableTrackCustomization;
            // 
            _definitions.Add(new PatchDef(typeof(AppearancePatches), appCond));
            // 
            _definitions.Add(new PatchDef(typeof(AppearancePatches.FloorStartPatch), appCond, typeof(AppearancePatches)));
            _definitions.Add(new PatchDef(typeof(AppearancePatches.FloorRefreshColorPatch), appCond, typeof(AppearancePatches)));
            _definitions.Add(new PatchDef(typeof(AppearancePatches.EditorFloorUpdatePatch), appCond, typeof(AppearancePatches)));

            // --- UI / Misc ---
            _definitions.Add(new PatchDef(typeof(MiscPatches.RemoveNewsPatch), () => Main.Settings.ui.removeNews));
            _definitions.Add(new PatchDef(typeof(MiscPatches.HideBetaWatermarkPatch), () => Main.Settings.ui.hideBetaWatermark));
            _definitions.Add(new PatchDef(typeof(MiscPatches.ForceDifficultyUIPatch), () => Main.Settings.ui.forceDifficultyUI));
            _definitions.Add(new PatchDef(typeof(MiscPatches.CircleArcPatch), () => Main.Settings.ui.enableCircleArc));
            _definitions.Add(new PatchDef(typeof(MiscPatches.AutoplayTextPositionPatch), () => Main.Settings.ui.moveAutoplayText));
            _definitions.Add(new PatchDef(typeof(CustomLevelIslandPatch), () => Main.Settings.ui.enableCustomLevelIsland));
            
            // Tail
            _definitions.Add(new PatchDef(typeof(MiscPatches.TailTweakPatch), () => Main.Settings.tail.enableTailTweak));

            // Memory
            var memCond = () => Main.Settings.memory.enableMemoryOptimization;
            _definitions.Add(new PatchDef(typeof(MiscPatches.SceneGC), () => memCond() && Main.Settings.memory.gcInLoadScene));
            _definitions.Add(new PatchDef(typeof(MiscPatches.SmartGCPatch), () => memCond() && Main.Settings.memory.enableSmartGC));

            // Compatibility
            var pauseFixCond = () => Main.Settings.compatibility.enableLegacyPauseFix;
            _definitions.Add(new PatchDef(typeof(CompatibilityPatches.LegacyPauseFixPatch_Play), pauseFixCond));
            _definitions.Add(new PatchDef(typeof(CompatibilityPatches.LegacyPauseFixPatch_Apply), pauseFixCond));
            _definitions.Add(new PatchDef(typeof(CompatibilityPatches.NoFailTooEarlyPatch), () => Main.Settings.compatibility.enableNoFailTooEarly));

            // Filter Optimization
            var filterOptCond = () => Main.Settings.optimizer.enableOptimizer;
            _definitions.Add(new PatchDef(typeof(OptimizerPatches.FilterPlusPatch), filterOptCond));
            _definitions.Add(new PatchDef(typeof(OptimizerPatches.FilterAdvancedPlusPatch), filterOptCond));
        }

        public static void UpdateAllPatches()
        {
            if (_harmony == null) return;

            // Re-Register
            
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var def in _definitions)
                {
                    bool shouldBeActive = CalculateEffectiveStatus(def);
                    _activePatches.TryGetValue(def.Type, out bool currentActive);

                    if (shouldBeActive != currentActive)
                    {
                        if (shouldBeActive) ApplyPatch(def.Type);
                        else RemovePatch(def.Type);
                        changed = true; // Coutinue Loop
                    }
                }
            }
        }

        private static bool CalculateEffectiveStatus(PatchDef def)
        {
            // Condition
            if (!def.Condition()) return false;
            
            // Check Parent
            if (def.Parent != null)
            {
                _activePatches.TryGetValue(def.Parent, out bool parentActive);
                if (!parentActive) return false;
            }

            return true;
        }

        private static void ApplyPatch(Type type)
        {
            try
            {
                _harmony.CreateClassProcessor(type).Patch();
                _activePatches[type] = true;
                Main.Logger?.Log($"[PatchManager] Applied {type.Name}");
            }
            catch (Exception e)
            {
                Main.Logger?.Error($"[PatchManager] Failed to apply {type.Name}: {e}");
            }
        }

        private static void RemovePatch(Type type)
        {
            try
            {
                // Harmony 卸载特定类的所有 Patch
                var processor = _harmony.CreateClassProcessor(type);
                // 遍历该类中定义的所有 Patch 方法并逐个移除
                // 注意：Harmony 的 Unpatch 需要原始方法和 Patch 方法
                // 简单做法是使用 Unpatch(original, HarmonyPatchType.All, id)
                
                // 获取类中所有标记了 HarmonyPatch 的方法
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    var patchAttr = method.GetCustomAttribute<HarmonyPatch>();
                    if (patchAttr != null || method.Name == "Prefix" || method.Name == "Postfix" || method.Name == "Transpiler")
                    {
                        // 这是一个 Patch 方法，需要找到它对应的原始方法
                        // ClassProcessor 已经处理了这些逻辑，但 Harmony 没有直接暴露 "UnpatchClass"
                        // 所以手动处理：(
                        UnpatchMethod(type);
                        break;
                    }
                }
                
                _activePatches[type] = false;
                Main.Logger?.Log($"[PatchManager] Removed {type.Name}");
            }
            catch (Exception e)
            {
                Main.Logger?.Error($"[PatchManager] Failed to remove {type.Name}: {e}");
            }
        }

        private static void UnpatchMethod(Type patchClass)
        {
            // Harmony 提供的 Unpatch 逻辑通常基于原始方法。
            // 如果要完全卸载一个类，最稳妥且不影响其他 Mod 的方式是：
            // 找到该类 Patch 的所有 Original 方法，并从中移除属于我们 ID 的 Patch。
            
            var allPatchedMethods = _harmony.GetPatchedMethods();
            foreach (var original in allPatchedMethods)
            {
                var info = Harmony.GetPatchInfo(original);
                if (info == null) continue;

                // 检查这个原始方法的 Prefixes, Postfixes, Transpilers 中是否有来自我们这个 patchClass 的
                bool containsOurPatch = info.Prefixes.Any(p => p.PatchMethod.DeclaringType == patchClass) ||
                                       info.Postfixes.Any(p => p.PatchMethod.DeclaringType == patchClass) ||
                                       info.Transpilers.Any(p => p.PatchMethod.DeclaringType == patchClass);

                if (containsOurPatch)
                {
                    // 移除这个 ID 在该原始方法上的所有 Patch
                    _harmony.Unpatch(original, HarmonyPatchType.All, _harmony.Id);
                    
                    // 注意：如果该类中有多个方法 Patch 了同一个 Original，Unpatch(..., id) 会把它们都删掉，这符合预期。
                    // 但如果该类中只有一部分方法要卸载（比如我们想细粒度到方法），则需要更复杂的逻辑。
                    // 目前按类（Type）管理已经满足“单个功能”的需求（只要功能在独立类或嵌套类中）。
                }
            }
        }

        public static void UnpatchAll()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _activePatches.Clear();
            Main.Logger?.Log("[PatchManager] Unpatched all");
        }
    }
}
