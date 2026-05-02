using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Iridium.Core
{
    /// <summary>
    /// 补丁的类型
    /// </summary>
    [Flags]
    public enum PatchTypes
    {
        /// <summary>
        /// 占位符
        /// </summary>
        NONE = 0,
        /// <summary>
        /// 先补丁后运行
        /// </summary>
        Prefix = 1,
        /// <summary>
        /// 先运行后补丁
        /// </summary>
        Postfix = 2,
        /// <summary>
        /// 语法糖 同时使用<see cref="Prefix"/>和<see cref="Postfix"/>
        /// </summary>
        PP = 3,
    }
    /// <summary>
    /// 所有补丁方法的基类<br/>
    /// 仅放置补丁中最基础的功能
    /// </summary>
    public abstract class BasePatchMethod
    {
        public static List<BasePatchMethod> _methods = new(16);

        public BasePatchMethod(PatchTypes t)
        {
            type = t;
            lock (_methods)
            {
                _methods.Add(this);
                id = _methods.Count - 1;
            }
        }
        /// <summary>
        /// 补丁类型, <b>不可修改</b>
        /// </summary>
        public readonly PatchTypes type;
        /// <summary>
        /// 补丁的<b>唯一识别码</b>
        /// </summary>
        protected internal readonly int id;
        /// <summary>
        /// 是否使用IL补丁
        /// </summary>
        public volatile bool il;
        /// <summary>
        /// 补丁后返回的<see cref="MethodInfo"/>
        /// </summary>
        internal MethodInfo? patchedaResult;
        public abstract void StartPatch();
        public abstract void StopPatch();
    }
    public abstract class StdPatchMethod<T, Res> : BasePatchMethod
    {
        public StdPatchMethod(PatchTypes t) : base(t)
        {
        }

        public Res? result;
        public T? instance;

        public abstract bool Method(object[] args);

        protected internal IEnumerable<CodeInstruction> IL(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase originalMethod)
        {
            if (type == 0 || (type & PatchTypes.PP) == 0)
            {
                throw new System.Exception("你这不是Prefix的 又不是Postfix的 要几把干啥");
            }
            // 针对Prefix
            if ((type & PatchTypes.Prefix) != 0)
            {
                // IL代码 通过ldarg把所有的参数都塞入就可以
                ParameterInfo[] parameters = originalMethod.GetParameters();
                int paramCount = parameters.Length;

                // 对于动态写入实例
                if (!originalMethod.IsStatic)
                {
                    yield return new CodeInstruction(OpCodes.Ldsfld, typeof(BasePatchMethod).GetField(nameof(_methods)));
                    yield return new CodeInstruction(OpCodes.Ldc_I4, id);
                    yield return new CodeInstruction(OpCodes.Call, typeof(List<BasePatchMethod>).GetMethod("get_Item"));
                    yield return new CodeInstruction(OpCodes.Castclass, typeof(StdPatchMethod<T, Res>));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Stfld, typeof(StdPatchMethod<T, Res>).GetField(nameof(instance)));
                }
                // 创建数组实例并写入
                LocalBuilder paramArrayLocal = generator.DeclareLocal(typeof(object[]));
                yield return new CodeInstruction(OpCodes.Ldc_I4, paramCount);
                yield return new CodeInstruction(OpCodes.Newarr, typeof(object));
                yield return new CodeInstruction(OpCodes.Stloc, paramArrayLocal.LocalIndex);
                for (int i = 0; i < paramCount; i++)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, paramArrayLocal);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, i);
                    yield return new CodeInstruction(OpCodes.Ldarg, i + (originalMethod.IsStatic ? 0 : 1));
                    if (parameters[i].ParameterType.IsValueType)
                        yield return new CodeInstruction(OpCodes.Box, parameters[i].ParameterType);
                    yield return new CodeInstruction(OpCodes.Stelem_Ref);
                }
                // 调用
                Label label_ret = generator.DefineLabel();
                yield return new CodeInstruction(OpCodes.Ldsfld, typeof(BasePatchMethod).GetField(nameof(_methods)));
                yield return new CodeInstruction(OpCodes.Ldc_I4, id);
                yield return new CodeInstruction(OpCodes.Call, typeof(List<BasePatchMethod>).GetMethod("get_Item"));
                yield return new CodeInstruction(OpCodes.Castclass, typeof(StdPatchMethod<T, Res>));
                yield return new CodeInstruction(OpCodes.Ldloc, paramArrayLocal.LocalIndex);
                yield return new CodeInstruction(OpCodes.Call, typeof(StdPatchMethod<T, Res>).GetMethod(nameof(Method)));
                yield return new CodeInstruction(OpCodes.Brtrue_S, label_ret);
                yield return new CodeInstruction(OpCodes.Ret);
                yield return new CodeInstruction(OpCodes.Nop).WithLabels(label_ret);
                // 剩下的全部返回
                foreach (CodeInstruction ci in instructions)
                {
                    yield return ci;
                }
            }
            if ((type & PatchTypes.Postfix) != 0)
            {
                foreach (CodeInstruction ci in instructions)
                {
                    if (ci.opcode == OpCodes.Ret)
                    {
                        // IL代码 通过ldarg把所有的参数都塞入就可以
                        ParameterInfo[] parameters = originalMethod.GetParameters();
                        int paramCount = parameters.Length;

                        // 对于动态写入实例
                        if (!originalMethod.IsStatic)
                        {
                            yield return new CodeInstruction(OpCodes.Ldsfld, typeof(BasePatchMethod).GetField(nameof(_methods)));
                            yield return new CodeInstruction(OpCodes.Ldc_I4, id);
                            yield return new CodeInstruction(OpCodes.Call, typeof(List<BasePatchMethod>).GetMethod("get_Item"));
                            yield return new CodeInstruction(OpCodes.Castclass, typeof(StdPatchMethod<T, Res>));
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Stfld, typeof(StdPatchMethod<T, Res>).GetField(nameof(instance)));
                        }
                        // 创建数组实例并写入
                        LocalBuilder paramArrayLocal = generator.DeclareLocal(typeof(object[]));
                        yield return new CodeInstruction(OpCodes.Ldc_I4, paramCount);
                        yield return new CodeInstruction(OpCodes.Newarr, typeof(object));
                        yield return new CodeInstruction(OpCodes.Stloc, paramArrayLocal.LocalIndex);
                        for (int i = 0; i < paramCount; i++)
                        {
                            yield return new CodeInstruction(OpCodes.Ldloc, paramArrayLocal);
                            yield return new CodeInstruction(OpCodes.Ldc_I4, i);
                            yield return new CodeInstruction(OpCodes.Ldarg, i + (originalMethod.IsStatic ? 0 : 1));
                            if (parameters[i].ParameterType.IsValueType)
                                yield return new CodeInstruction(OpCodes.Box, parameters[i].ParameterType);
                            yield return new CodeInstruction(OpCodes.Stelem_Ref);
                        }
                        Label label_ret = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Ldsfld, typeof(BasePatchMethod).GetField(nameof(_methods)));
                        yield return new CodeInstruction(OpCodes.Ldc_I4, id);
                        yield return new CodeInstruction(OpCodes.Call, typeof(List<BasePatchMethod>).GetMethod("get_Item"));
                        yield return new CodeInstruction(OpCodes.Castclass, typeof(StdPatchMethod<T, Res>));
                        yield return new CodeInstruction(OpCodes.Ldloc, paramArrayLocal.LocalIndex);
                        yield return new CodeInstruction(OpCodes.Call, typeof(StdPatchMethod<T, Res>).GetMethod(nameof(Method)));
                        yield return new CodeInstruction(OpCodes.Brtrue_S, label_ret);
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Ret);
                    }
                    yield return ci;
                }
            }

            yield break;
        }

        protected internal bool Prefix(in T __instance, object[] __args, out Res? __result)
        {
            instance = __instance;
            bool res = Method(__args);
            __result = result;
            return res;
        }

        protected internal bool Postfix(in T __instance, object[] __args, ref Res? __result)
        {
            instance = __instance;
            result = __result;
            bool res = Method(__args);
            __result = result;
            return res;
        }
    }
}
