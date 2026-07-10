using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Iridium.Patches
{
    internal sealed class PatchApplier
    {
        private readonly Harmony _harmony;
        private readonly Dictionary<Type, List<(MethodBase Original, MethodInfo PatchMethod)>> _patchedBindings = new();

        public PatchApplier(Harmony harmony)
        {
            _harmony = harmony;
        }

        public void Apply(Type type)
        {
            try
            {
                Main.Logger?.Log(Localization.Get("PatchManagerAttemptApply", type.Name));

                if (type == typeof(BugfixPatches.CoopPauseLockFix))
                {
                    ApplyCoopPauseLockFix(type);
                    return;
                }

                var processor = _harmony.CreateClassProcessor(type);
                var originals = processor.Patch();

                if (originals != null && originals.Count > 0)
                {
                    var bindings = CollectBindings(type, originals);
                    _patchedBindings[type] = bindings;
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

        public void Remove(Type type)
        {
            try
            {
                Main.Logger?.Log(Localization.Get("PatchManagerAttemptRemove", type.Name));

                if (type == typeof(BugfixPatches.CoopPauseLockFix))
                {
                    BugfixPatches.CoopPauseLockFix.Unapply(_harmony);
                    _patchedBindings.Remove(type);
                    Main.Logger?.Log(Localization.Get("PatchManagerSuccessRemove", type.Name));
                    return;
                }

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
                    Main.Logger?.Log(Localization.Get("PatchManagerUsingFallback", type.Name));
                    UnpatchMethod(type);
                    _patchedBindings.Remove(type);
                }

                Main.Logger?.Log(Localization.Get("PatchManagerSuccessRemove", type.Name));
            }
            catch (Exception e)
            {
                Main.Logger?.Error(Localization.Get("PatchManagerFailedRemove", type.Name, e.ToString()));
            }
        }

        public void ClearBindings()
        {
            _patchedBindings.Clear();
        }

        private void ApplyCoopPauseLockFix(Type type)
        {
            var original = BugfixPatches.CoopPauseLockFix.Apply(_harmony);
            if (original != null)
            {
                var prefix = SymbolExtensions.GetMethodInfo(() => BugfixPatches.CoopPauseLockFix.Prefix());
                _patchedBindings[type] = new List<(MethodBase Original, MethodInfo PatchMethod)> { (original, prefix) };
                Main.Logger?.Log(Localization.Get("PatchManagerSuccessApply", type.Name, "1"));
            }
            else
            {
                Main.Logger?.Log(Localization.Get("PatchManagerNoMethods", type.Name));
            }
        }

        private List<(MethodBase Original, MethodInfo PatchMethod)> CollectBindings(Type type, IReadOnlyCollection<MethodBase> originals)
        {
            var bindings = new List<(MethodBase Original, MethodInfo PatchMethod)>();

            foreach (var original in originals)
            {
                var info = Harmony.GetPatchInfo(original);
                if (info == null) continue;

                AddOwnedBindings(type, original, info.Prefixes, bindings);
                AddOwnedBindings(type, original, info.Postfixes, bindings);
                AddOwnedBindings(type, original, info.Transpilers, bindings);
                AddOwnedBindings(type, original, info.Finalizers, bindings);
            }

            return bindings;
        }

        private void UnpatchMethod(Type patchClass)
        {
            var allPatchedMethods = _harmony.GetPatchedMethods();
            foreach (var original in allPatchedMethods)
            {
                var info = Harmony.GetPatchInfo(original);
                if (info == null) continue;

                UnpatchOwnedBindings(patchClass, original, info.Prefixes);
                UnpatchOwnedBindings(patchClass, original, info.Postfixes);
                UnpatchOwnedBindings(patchClass, original, info.Transpilers);
                UnpatchOwnedBindings(patchClass, original, info.Finalizers);
            }
        }

        private void AddOwnedBindings(
            Type type,
            MethodBase original,
            IEnumerable<Patch> patches,
            List<(MethodBase Original, MethodInfo PatchMethod)> bindings)
        {
            foreach (var patch in patches)
            {
                if (patch.owner == _harmony.Id && patch.PatchMethod.DeclaringType == type)
                    bindings.Add((original, patch.PatchMethod));
            }
        }

        private void UnpatchOwnedBindings(Type patchClass, MethodBase original, IEnumerable<Patch> patches)
        {
            foreach (var patch in patches)
            {
                if (patch.owner == _harmony.Id && patch.PatchMethod.DeclaringType == patchClass)
                    _harmony.Unpatch(original, patch.PatchMethod);
            }
        }
    }
}
