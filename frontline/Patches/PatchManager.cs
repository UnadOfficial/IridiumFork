using HarmonyLib;
using Iridium.Core;
using System;
using System.Collections.Generic;

namespace Iridium.Patches
{
    public static class PatchManager
    {
        private static Harmony _harmony => Main.Harmony!;

        private static readonly Dictionary<Type, bool> _activePatches = new();
        private static readonly List<BasePatchMethod> _methodPatches = new();
        private static readonly List<PatchDefinition> _definitions = PatchRegistry.Build();
        private static PatchApplier? _applier;

        private static PatchApplier Applier => _applier ??= new PatchApplier(_harmony);

        public static void RegisterMethodPatch(BasePatchMethod patch)
        {
            lock (_methodPatches)
            {
                if (!_methodPatches.Contains(patch))
                    _methodPatches.Add(patch);
            }
        }

        public static void UnregisterMethodPatch(BasePatchMethod patch)
        {
            lock (_methodPatches)
            {
                _methodPatches.Remove(patch);
            }
        }

        public static void UpdateAllPatches()
        {
            if (_harmony == null) return;

            foreach (var def in _definitions)
            {
                UpdateSinglePatch(def);
            }

            BasePatchMethod.SyncILModeFromSettings();
        }

        public static void UpdatePatchByType(Type patchType)
        {
            if (_harmony == null) return;

            var def = _definitions.Find(d => d.Type == patchType);
            if (def != null)
            {
                UpdateSinglePatch(def);
            }
        }

        public static void UpdateOptimizerPatches()
        {
            if (_harmony == null) return;

            foreach (var def in _definitions)
            {
                if (PatchRegistry.IsOptimizerPatch(def.Type))
                {
                    UpdateSinglePatch(def);
                }
            }
        }

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

        public static void UnpatchAll()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _activePatches.Clear();
            _applier?.ClearBindings();

            lock (_methodPatches)
            {
                foreach (var methodPatch in _methodPatches)
                {
                    if (methodPatch.IsPatched)
                        methodPatch.StopPatch();
                }
            }

            Main.Logger?.Log(Localization.Get("PatchManagerUnpatchedAll"));
        }

        private static void UpdateSinglePatch(PatchDefinition def)
        {
            bool shouldBeActive = CalculateEffectiveStatus(def);
            bool trackedActive = _activePatches.TryGetValue(def.Type, out bool currentActive) && currentActive;

            if (trackedActive == shouldBeActive) return;

            Main.Logger?.Log(Localization.Get("PatchManagerStatusChanged", def.Name, trackedActive.ToString(), shouldBeActive.ToString()));
            if (shouldBeActive) Applier.Apply(def.Type);
            else Applier.Remove(def.Type);

            _activePatches[def.Type] = shouldBeActive;
        }

        private static bool CalculateEffectiveStatus(PatchDefinition def)
        {
            if (!def.Condition()) return false;

            if (def.Parent != null)
            {
                _activePatches.TryGetValue(def.Parent, out bool parentActive);
                if (!parentActive) return false;
            }

            return true;
        }
    }
}
