using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Iridium.Core
{
    [Flags]
    public enum PatchTypes
    {
        NONE = 0,
        Prefix = 1,
        Postfix = 2,
        PP = 3,
    }

    internal static class PatchBridgeGenerator
    {
        private static readonly AssemblyBuilder s_assembly;
        private static readonly ModuleBuilder s_module;
        private static int s_counter;

        static PatchBridgeGenerator()
        {
            s_assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("Iridium.PatchBridges"),
                AssemblyBuilderAccess.Run);
            s_module = s_assembly.DefineDynamicModule("PatchBridges");
        }

        public static (MethodInfo prefixInst, MethodInfo prefixStatic,
                       MethodInfo postfixInst, MethodInfo postfixStatic,
                       MethodInfo transpiler, Type generatedType)
            GenerateBridge(int patchId)
        {
            int id = System.Threading.Interlocked.Increment(ref s_counter);
            TypeBuilder tb = s_module.DefineType(
                $"__Bridge_{id}",
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);

            var transpiler = DefineTranspiler(tb, patchId);
            var prefixInst = DefinePrefix_Instance(tb, patchId);
            var prefixStc = DefinePrefix_Static(tb, patchId);
            var postfixInst = DefinePostfix_Instance(tb, patchId);
            var postfixStc = DefinePostfix_Static(tb, patchId);

            Type generatedType = tb.CreateType()!;
            return (
                generatedType.GetMethod(prefixInst.Name, BindingFlags.Static | BindingFlags.Public)!,
                generatedType.GetMethod(prefixStc.Name, BindingFlags.Static | BindingFlags.Public)!,
                generatedType.GetMethod(postfixInst.Name, BindingFlags.Static | BindingFlags.Public)!,
                generatedType.GetMethod(postfixStc.Name, BindingFlags.Static | BindingFlags.Public)!,
                generatedType.GetMethod(transpiler.Name, BindingFlags.Static | BindingFlags.Public)!,
                generatedType
            );
        }

        private static MethodBuilder DefineTranspiler(TypeBuilder tb, int patchId)
        {
            MethodBuilder mb = tb.DefineMethod(
                "Transpiler",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(IEnumerable<CodeInstruction>),
                new[] { typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator), typeof(MethodBase) });

            ILGenerator gen = mb.GetILGenerator();

            gen.Emit(OpCodes.Ldsfld,
                typeof(BasePatchMethod).GetField(nameof(BasePatchMethod._methods),
                    BindingFlags.Static | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ldc_I4, patchId);
            gen.Emit(OpCodes.Call,
                typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Callvirt,
                typeof(BasePatchMethod).GetMethod(nameof(BasePatchMethod.ILDispatch),
                    BindingFlags.Instance | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ret);

            return mb;
        }

        private static MethodBuilder DefinePrefix_Instance(TypeBuilder tb, int patchId)
        {
            MethodBuilder mb = tb.DefineMethod(
                "Prefix_Inst",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(bool),
                new[] { typeof(object), typeof(object[]) });

            ILGenerator gen = mb.GetILGenerator();

            EmitLoadAndSetInstance(gen, patchId);

            gen.Emit(OpCodes.Ldsfld,
                typeof(BasePatchMethod).GetField(nameof(BasePatchMethod._methods),
                    BindingFlags.Static | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ldc_I4, patchId);
            gen.Emit(OpCodes.Call,
                typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Callvirt,
                typeof(BasePatchMethod).GetMethod(nameof(BasePatchMethod.MethodDispatch),
                    BindingFlags.Instance | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ret);

            return mb;
        }

        private static MethodBuilder DefinePrefix_Static(TypeBuilder tb, int patchId)
        {
            MethodBuilder mb = tb.DefineMethod(
                "Prefix_Stc",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(bool),
                new[] { typeof(object[]) });

            ILGenerator gen = mb.GetILGenerator();

            gen.Emit(OpCodes.Ldsfld,
                typeof(BasePatchMethod).GetField(nameof(BasePatchMethod._methods),
                    BindingFlags.Static | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ldc_I4, patchId);
            gen.Emit(OpCodes.Call,
                typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Callvirt,
                typeof(BasePatchMethod).GetMethod(nameof(BasePatchMethod.MethodDispatch),
                    BindingFlags.Instance | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ret);

            return mb;
        }

        private static MethodBuilder DefinePostfix_Instance(TypeBuilder tb, int patchId)
        {
            MethodBuilder mb = tb.DefineMethod(
                "Postfix_Inst",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(void),
                new[] { typeof(object), typeof(object[]), typeof(object).MakeByRefType() });

            ILGenerator gen = mb.GetILGenerator();

            EmitLoadAndSetInstance(gen, patchId);

            EmitLoadAndSetResult(gen, patchId);

            EmitCallMethodDispatch(gen, patchId, 1);

            EmitGetAndStoreResult(gen, patchId);

            gen.Emit(OpCodes.Ret);

            return mb;
        }

        private static MethodBuilder DefinePostfix_Static(TypeBuilder tb, int patchId)
        {
            MethodBuilder mb = tb.DefineMethod(
                "Postfix_Stc",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(void),
                new[] { typeof(object[]), typeof(object).MakeByRefType() });

            ILGenerator gen = mb.GetILGenerator();

            EmitLoadAndSetResult(gen, patchId);

            EmitCallMethodDispatch(gen, patchId, 0);

            EmitGetAndStoreResult(gen, patchId);

            gen.Emit(OpCodes.Ret);

            return mb;
        }

        private static void EmitLoadAndSetInstance(ILGenerator gen, int patchId)
        {
            gen.Emit(OpCodes.Ldsfld,
                typeof(BasePatchMethod).GetField(nameof(BasePatchMethod._methods),
                    BindingFlags.Static | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ldc_I4, patchId);
            gen.Emit(OpCodes.Call,
                typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Callvirt,
                typeof(BasePatchMethod).GetMethod(nameof(BasePatchMethod.SetInstanceDispatch),
                    BindingFlags.Instance | BindingFlags.Public)!);
        }

        private static void EmitLoadAndSetResult(ILGenerator gen, int patchId)
        {
            gen.Emit(OpCodes.Ldsfld,
                typeof(BasePatchMethod).GetField(nameof(BasePatchMethod._methods),
                    BindingFlags.Static | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ldc_I4, patchId);
            gen.Emit(OpCodes.Call,
                typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldind_Ref);
            gen.Emit(OpCodes.Callvirt,
                typeof(BasePatchMethod).GetMethod(nameof(BasePatchMethod.SetResultDispatch),
                    BindingFlags.Instance | BindingFlags.Public)!);
        }

        private static void EmitCallMethodDispatch(ILGenerator gen, int patchId, int argsIndex)
        {
            gen.Emit(OpCodes.Ldsfld,
                typeof(BasePatchMethod).GetField(nameof(BasePatchMethod._methods),
                    BindingFlags.Static | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ldc_I4, patchId);
            gen.Emit(OpCodes.Call,
                typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
            gen.Emit(OpCodes.Ldarg, argsIndex);
            gen.Emit(OpCodes.Callvirt,
                typeof(BasePatchMethod).GetMethod(nameof(BasePatchMethod.MethodDispatch),
                    BindingFlags.Instance | BindingFlags.Public)!);
            gen.Emit(OpCodes.Pop);
        }

        private static void EmitGetAndStoreResult(ILGenerator gen, int patchId)
        {
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldsfld,
                typeof(BasePatchMethod).GetField(nameof(BasePatchMethod._methods),
                    BindingFlags.Static | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ldc_I4, patchId);
            gen.Emit(OpCodes.Call,
                typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
            gen.Emit(OpCodes.Callvirt,
                typeof(BasePatchMethod).GetMethod(nameof(BasePatchMethod.GetResultDispatch),
                    BindingFlags.Instance | BindingFlags.Public)!);
            gen.Emit(OpCodes.Stind_Ref);
        }
    }

    public abstract class BasePatchMethod
    {
        public static List<BasePatchMethod> _methods = new(16);

        public BasePatchMethod(PatchTypes t)
        {
            type = t;
            il = Main.Settings?.patchMode.useILPatch ?? false;
            lock (_methods)
            {
                _methods.Add(this);
                id = _methods.Count - 1;
            }
        }

        public readonly PatchTypes type;

        protected internal readonly int id;

        public volatile bool il;

        internal MethodInfo? patchedResult;
        internal Type? _bridgeType;

        public abstract MethodBase GetTargetMethod();

        public abstract void StartPatch();

        public abstract void StopPatch();

        public void SetILMode(bool useIL)
        {
            if (useIL == il) return;
            il = useIL;
            StopPatch();
            StartPatch();
        }

        public bool IsPatched => patchedResult != null;

        public static void SyncILModeFromSettings()
        {
            bool useIL = Main.Settings?.patchMode.useILPatch ?? false;
            lock (_methods)
            {
                foreach (var m in _methods)
                {
                    if (m.IsPatched && m.il != useIL)
                    {
                        m.SetILMode(useIL);
                    }
                    else
                    {
                        m.il = useIL;
                    }
                }
            }
        }

        internal virtual void ForceReset()
        {
            patchedResult = null;
            _bridgeType = null;
        }

        public abstract void SetInstanceDispatch(object instance);
        public abstract void SetResultDispatch(object result);
        public abstract object? GetResultDispatch();
        public abstract bool MethodDispatch(object[] args);
        public abstract IEnumerable<CodeInstruction> ILDispatch(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase originalMethod);
    }

    public abstract class StdPatchMethod<T, Res> : BasePatchMethod
    {
        public StdPatchMethod(PatchTypes t) : base(t)
        {
        }

        public Res? result;

        public T? instance;

        public abstract bool Method(object[] args);

        public override void SetInstanceDispatch(object inst) => instance = (T)inst;
        public override void SetResultDispatch(object res) => result = (Res)res;
        public override object? GetResultDispatch() => result;
        public override bool MethodDispatch(object[] args) => Method(args);

        public override IEnumerable<CodeInstruction> ILDispatch(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase originalMethod)
        {
            return IL(instructions, generator, originalMethod);
        }

        protected internal IEnumerable<CodeInstruction> IL(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase originalMethod)
        {
            if (type == 0 || (type & PatchTypes.PP) == 0)
                throw new Exception("你这不是Prefix的 又不是Postfix的 要几把干啥");

            var instrList = instructions.ToList();
            var parameters = originalMethod.GetParameters();
            int paramCount = parameters.Length;
            bool isStatic = originalMethod.IsStatic;

            bool hasPrefix = (type & PatchTypes.Prefix) != 0;
            bool hasPostfix = (type & PatchTypes.Postfix) != 0;

            if (hasPrefix)
            {
                if (!isStatic)
                {
                    foreach (var ci in EmitStoreInstance(OpCodes.Ldarg_0))
                        yield return ci;
                }

                var argsLocal = generator.DeclareLocal(typeof(object[]));
                foreach (var ci in EmitBuildArgs(argsLocal, parameters, isStatic))
                    yield return ci;

                var skipLabel = generator.DefineLabel();
                foreach (var ci in EmitCallMethod(argsLocal))
                    yield return ci;
                yield return new CodeInstruction(OpCodes.Brtrue_S, skipLabel);
                yield return new CodeInstruction(OpCodes.Ret);
                yield return new CodeInstruction(OpCodes.Nop).WithLabels(skipLabel);
            }

            foreach (CodeInstruction ci in instrList)
            {
                if (hasPostfix && ci.opcode == OpCodes.Ret)
                {
                    if (!isStatic)
                    {
                        foreach (var ci2 in EmitStoreInstance(OpCodes.Ldarg_0))
                            yield return ci2;
                    }

                    var postArgsLocal = generator.DeclareLocal(typeof(object[]));
                    foreach (var ci2 in EmitBuildArgs(postArgsLocal, parameters, isStatic))
                        yield return ci2;

                    var postSkip = generator.DefineLabel();
                    foreach (var ci2 in EmitCallMethod(postArgsLocal))
                        yield return ci2;
                    yield return new CodeInstruction(OpCodes.Brtrue_S, postSkip);
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ret);
                    ci.WithLabels(postSkip);
                }

                yield return ci;
            }

            yield break;

            IEnumerable<CodeInstruction> EmitStoreInstance(OpCode loadValue)
            {
                yield return new CodeInstruction(OpCodes.Ldsfld,
                    typeof(BasePatchMethod).GetField(nameof(_methods))!);
                yield return new CodeInstruction(OpCodes.Ldc_I4, id);
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
                yield return new CodeInstruction(OpCodes.Castclass, typeof(StdPatchMethod<T, Res>));
                yield return new CodeInstruction(loadValue);
                yield return new CodeInstruction(OpCodes.Stfld,
                    typeof(StdPatchMethod<T, Res>).GetField(nameof(instance))!);
            }

            IEnumerable<CodeInstruction> EmitBuildArgs(LocalBuilder argsLocal, ParameterInfo[] parms, bool staticMethod)
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4, paramCount);
                yield return new CodeInstruction(OpCodes.Newarr, typeof(object));
                yield return new CodeInstruction(OpCodes.Stloc, argsLocal.LocalIndex);

                for (int i = 0; i < paramCount; i++)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, argsLocal.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, i);
                    yield return new CodeInstruction(OpCodes.Ldarg,
                        i + (staticMethod ? 0 : 1));
                    if (parms[i].ParameterType.IsValueType)
                        yield return new CodeInstruction(OpCodes.Box, parms[i].ParameterType);
                    yield return new CodeInstruction(OpCodes.Stelem_Ref);
                }
            }

            IEnumerable<CodeInstruction> EmitCallMethod(LocalBuilder argsLocal)
            {
                yield return new CodeInstruction(OpCodes.Ldsfld,
                    typeof(BasePatchMethod).GetField(nameof(_methods))!);
                yield return new CodeInstruction(OpCodes.Ldc_I4, id);
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
                yield return new CodeInstruction(OpCodes.Castclass, typeof(StdPatchMethod<T, Res>));
                yield return new CodeInstruction(OpCodes.Ldloc, argsLocal.LocalIndex);
                yield return new CodeInstruction(OpCodes.Callvirt,
                    typeof(StdPatchMethod<T, Res>).GetMethod(nameof(Method),
                        BindingFlags.Instance | BindingFlags.Public)!);
            }
        }

        public override void StartPatch()
        {
            var target = GetTargetMethod();
            if (target == null)
            {
                Main.Logger?.Error($"[PatchMethod] GetTargetMethod() returned null for {GetType().Name}");
                return;
            }

            var harmony = Main.Harmony;
            if (harmony == null)
            {
                Main.Logger?.Error("[PatchMethod] Main.Harmony is null");
                return;
            }

            var (prefixInst, prefixStc, postfixInst, postfixStc, transpiler, bridgeType) =
                PatchBridgeGenerator.GenerateBridge(id);
            _bridgeType = bridgeType;

            if (il)
            {
                harmony.Patch(target, transpiler: new HarmonyMethod(transpiler));
                patchedResult = transpiler;
                Main.Logger?.Log($"[PatchMethod] {GetType().Name}[{id}] patched as Transpiler");
            }
            else
            {
                bool isStatic = target.IsStatic;
                HarmonyMethod? prefix = null;
                HarmonyMethod? postfix = null;

                if ((type & PatchTypes.Prefix) != 0)
                {
                    prefix = new HarmonyMethod(isStatic ? prefixStc : prefixInst);
                }

                if ((type & PatchTypes.Postfix) != 0)
                {
                    postfix = new HarmonyMethod(isStatic ? postfixStc : postfixInst);
                }

                harmony.Patch(target, prefix: prefix, postfix: postfix);
                patchedResult = prefix?.method ?? postfix?.method;
                Main.Logger?.Log($"[PatchMethod] {GetType().Name}[{id}] patched as Prefix/Postfix");
            }
        }

        public override void StopPatch()
        {
            var target = GetTargetMethod();
            if (target == null) return;

            var harmony = Main.Harmony;
            if (harmony == null) return;

            if (_bridgeType != null)
            {
                var methods = _bridgeType.GetMethods(BindingFlags.Static | BindingFlags.Public);
                foreach (var m in methods)
                {
                    try { harmony.Unpatch(target, m); } catch { }
                }
            }

            patchedResult = null;
            _bridgeType = null;
            Main.Logger?.Log($"[PatchMethod] {GetType().Name}[{id}] unpatched");
        }
    }
}