using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Iridium.Config;

namespace Iridium.Patches
{
    public static class PatchManager
    {
        private static Harmony _harmony => Main.Harmony!;

        // Status
        private static readonly Dictionary<Type, bool> _activePatches = new();
        // Optimization: Cache exact patch bindings for each patch class to speed up and isolate unpatching
        private static readonly Dictionary<Type, List<(MethodBase Original, MethodInfo PatchMethod)>> _patchedBindings = new();

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
        private static readonly Dictionary<Type, PatchDef> _definitionDict = new();

        static PatchManager()
        {
            RegisterPatches();
        }

        private static void RegisterPatches()
        {
            _definitions.Clear();

            // --- Optimizer ---
            var optCond = () => Main.Settings.optimizer.enableOptimizer;
            // Register all nested patch classes in OptimizerPatches
            foreach (var type in typeof(OptimizerPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                // Only register types that have HarmonyPatch attribute
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    var def = new PatchDef(type, optCond);
                    _definitions.Add(def);
                    _definitionDict[def.Type] = def;
                }
            }
            {
                var def = new PatchDef(typeof(TrackOptimizationPatches), optCond);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }

            // --- Ffx Optimization Patches ---
            foreach (var type in typeof(FfxOptimizationPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    var def = new PatchDef(type, optCond);
                    _definitions.Add(def);
                    _definitionDict[def.Type] = def;
                }
            }

            // --- Scene Optimization Patches ---
            foreach (var type in typeof(SceneOptimizationPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    var def = new PatchDef(type, optCond);
                    _definitions.Add(def);
                    _definitionDict[def.Type] = def;
                }
            }

            // --- Loading Optimization Patches ---
            foreach (var type in typeof(LoadingOptimizationPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    var def = new PatchDef(type, optCond);
                    _definitions.Add(def);
                    _definitionDict[def.Type] = def;
                }
            }

            // --- DOTween Optimization Patches ---
            // 注意：DOTween优化现在不使用任何HarmonyPatch，只使用运行时配置
            // 所以不需要注册任何补丁

            // --- Extreme Optimization Patches ---
            foreach (var type in typeof(ExtremeOptimizationPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    var def = new PatchDef(type, () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.enableExtremeOptimization);
                    _definitions.Add(def);
                    _definitionDict[def.Type] = def;
                }
            }

            // --- Tween Safety Patches ---
            // 解决 DOTween.defaultRecyclable=true 时，ADOFAI 中 Tween 引用过期导致的动画异常
            var tweenSafetyCond = () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.dotweenDefaultRecyclable;
            foreach (var type in typeof(TweenSafetyPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    var def = new PatchDef(type, tweenSafetyCond);
                    _definitions.Add(def);
                    _definitionDict[def.Type] = def;
                }
            }

            // --- UI / Misc ---
            {
                var def = new PatchDef(typeof(MiscPatches.RemoveNewsPatch), () => Main.Settings.ui.removeNews);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            {
                var def = new PatchDef(typeof(MiscPatches.HideBetaWatermarkPatch), () => Main.Settings.ui.hideBetaWatermark);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            {
                var def = new PatchDef(typeof(MiscPatches.ForceDifficultyUIPatch), () => Main.Settings.ui.forceDifficultyUI);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            {
                var def = new PatchDef(typeof(MiscPatches.CircleArcPatch), () => Main.Settings.ui.enableCircleArc);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            {
                var def = new PatchDef(typeof(MiscPatches.AutoplayTextPositionPatch), () => Main.Settings.ui.moveAutoplayText);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            {
                var def = new PatchDef(typeof(MiscPatches.AlwaysCountdownPatch), () => Main.Settings.ui.alwaysCountdown);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }

            // Lobby music
            {
                var def = new PatchDef(typeof(MiscPatches.LobbyMusicPatch), () => Main.Settings.lobbyMusic.enableLobbyMusicPatch);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }

            // Memory
            var memCond = () => Main.Settings.memory.enableMemoryOptimization;
            {
                var def = new PatchDef(typeof(MiscPatches.SmartGCPatch), () => memCond() && Main.Settings.memory.enableSmartGC);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }

            // Compatibility
            var pauseFixCond = () => Main.Settings.compatibility.enableLegacyPauseFix;
            {
                var def = new PatchDef(typeof(CompatibilityPatches.LegacyPauseFixPatch_Play), pauseFixCond);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            {
                var def = new PatchDef(typeof(CompatibilityPatches.LegacyPauseFixPatch_Apply), pauseFixCond);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            {
                var def = new PatchDef(typeof(CompatibilityPatches.NoFailTooEarlyPatch), () => Main.Settings.compatibility.enableNoFailTooEarly);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            {
                var def = new PatchDef(typeof(JsonPatches.ForceAngleDataPatch), () => Main.Settings.compatibility.forceAngleData);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            {
                var def = new PatchDef(typeof(JsonPatches.LegacyBehaviorPatch), () =>
                    Main.Settings.compatibility.legacyFlashMode != LegacyBehaviorMode.Default ||
                    Main.Settings.compatibility.legacyCamRelativeToMode != LegacyBehaviorMode.Default);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }

            // Hit Sound
            {
                var def = new PatchDef(typeof(HitSoundPatch), () => Main.Settings.hitSound.enableHitSoundPitch);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }

            // Judge Text
            // InitPatch: Handles custom text mode
            {
                var def = new PatchDef(typeof(JudgeTextPatches.HitTextMeshInitPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            // ShowPatch: Handles offset mode
            {
                var def = new PatchDef(typeof(JudgeTextPatches.HitTextMeshShowPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization && Main.Settings.judgeText.showAsOffset);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
            // Rewind: Reset
            {
                var def = new PatchDef(typeof(JudgeTextPatches.ResetTimingOnRewindPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization);
                _definitions.Add(def);
                _definitionDict[def.Type] = def;
            }
        }

        /// <summary>
        /// 更新所有patch（仅用于初始化或全量更新）
        /// </summary>
        public static void UpdateAllPatches()
        {
            if (_harmony == null) return;

            foreach (var def in _definitions)
            {
                UpdateSinglePatch(def);
            }
        }

        /// <summary>
        /// 按类型更新单个patch - 用于增量更新
        /// </summary>
        public static void UpdatePatchByType(Type patchType)
        {
            if (_harmony == null) return;

            _definitionDict.TryGetValue(patchType, out var def);
            if (def != null)
            {
                UpdateSinglePatch(def);
            }
        }

        /// <summary>
        /// 更新所有优化器相关的patch（当 enableOptimizer 改变时调用）
        /// </summary>
        public static void UpdateOptimizerPatches()
        {
            if (_harmony == null) return;

            // 优化器相关的 patch 类型
            var optimizerParentTypes = new HashSet<Type>
            {
                typeof(OptimizerPatches),
                typeof(TrackOptimizationPatches),
                typeof(SceneOptimizationPatches),
                typeof(LoadingOptimizationPatches),
                typeof(ExtremeOptimizationPatches)
            };

            foreach (var def in _definitions)
            {
                // 检查是否是优化器相关的 patch
                bool isOptimizerPatch = optimizerParentTypes.Contains(def.Type) ||
                    (def.Type.DeclaringType != null && optimizerParentTypes.Contains(def.Type.DeclaringType));

                if (isOptimizerPatch)
                {
                    UpdateSinglePatch(def);
                }
            }
        }

        /// <summary>
        /// 更新满足条件的patch - 用于批量增量更新
        /// </summary>
        public static void UpdatePatchesByCondition(Func<Type, bool> predicate)
        {
            if (_harmony == null) return;

            foreach (var def in _definitions)
            {
                if (predicate(def.Type))
                {
                    UpdateSinglePatch(def);
                }
            }
        }

        /// <summary>
        /// 更新单个patch定义
        /// </summary>
        private static void UpdateSinglePatch(PatchDef def)
        {
            bool shouldBeActive = CalculateEffectiveStatus(def);
            bool trackedActive = _activePatches.TryGetValue(def.Type, out bool currentActive) && currentActive;

            if (trackedActive != shouldBeActive)
            {
                Main.Logger?.Log(Localization.Get("PatchManagerStatusChanged", def.Name, trackedActive.ToString(), shouldBeActive.ToString()));
                if (shouldBeActive) ApplyPatch(def.Type);
                else RemovePatch(def.Type);

                _activePatches[def.Type] = shouldBeActive;
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

        // 移除不再使用的 IsActuallyPatched 辅助方法

        private static void ApplyPatch(Type type)
        {
            try
            {
                Main.Logger?.Log(Localization.Get("PatchManagerAttemptApply", type.Name));
                var processor = _harmony.CreateClassProcessor(type);
                var originals = processor.Patch();

                if (originals != null && originals.Count > 0)
                {
                    var bindings = new List<(MethodBase Original, MethodInfo PatchMethod)>();
                    foreach (var original in originals)
                    {
                        var info = Harmony.GetPatchInfo(original);
                        if (info == null) continue;

                        foreach (var p in info.Prefixes)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                        foreach (var p in info.Postfixes)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                        foreach (var p in info.Transpilers)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                        foreach (var p in info.Finalizers)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                    }

                    _patchedBindings[type] = bindings;
                    _activePatches[type] = true;
                    Main.Logger?.Log(Localization.Get("PatchManagerSuccessApply", type.Name, bindings.Count.ToString()));
                }
                else
                {
                    Main.Logger?.Log(Localization.Get("PatchManagerNoMethods", type.Name));
                }
            }
            catch (Exception e)
            {
                Main.Logger?.Error(Localization.Get("PatchManagerFailedApply", type.Name, e.ToString()));
            }
        }

        private static void RemovePatch(Type type)
        {
            try
            {
                Main.Logger?.Log(Localization.Get("PatchManagerAttemptRemove", type.Name));
                if (_patchedBindings.TryGetValue(type, out var bindings) && bindings.Count > 0)
                {
                    Main.Logger?.Log(Localization.Get("PatchManagerUsingCache", type.Name, bindings.Count.ToString()));
                    foreach (var (original, patchMethod) in bindings)
                    {
                        _harmony.Unpatch(original, patchMethod);
                    }
                    _patchedBindings.Remove(type);
                }
                else
                {
                    // Fallback to slow method if cache is missing or empty
                    Main.Logger?.Log(Localization.Get("PatchManagerUsingFallback", type.Name));
                    UnpatchMethod(type);
                    _patchedBindings.Remove(type);
                }

                _activePatches[type] = false;
                Main.Logger?.Log(Localization.Get("PatchManagerSuccessRemove", type.Name));
            }
            catch (Exception e)
            {
                Main.Logger?.Error(Localization.Get("PatchManagerFailedRemove", type.Name, e.ToString()));
            }
        }

        private static void UnpatchMethod(Type patchClass)
        {
            // Slow fallback: search all patched methods in the game
            var allPatchedMethods = _harmony.GetPatchedMethods();
            foreach (var original in allPatchedMethods)
            {
                var info = Harmony.GetPatchInfo(original);
                if (info == null) continue;

                foreach (var p in info.Prefixes)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
                foreach (var p in info.Postfixes)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
                foreach (var p in info.Transpilers)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
                foreach (var p in info.Finalizers)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
            }
        }

        public static void UnpatchAll()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _activePatches.Clear();
            _patchedBindings.Clear();
            Main.Logger?.Log(Localization.Get("PatchManagerUnpatchedAll"));
        }
    }
}
