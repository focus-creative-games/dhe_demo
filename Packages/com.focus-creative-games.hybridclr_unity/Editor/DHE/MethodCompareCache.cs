using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.DHE
{

    public class MethodCompareCache
    {
        public class Options
        {
            public TypeCompareCache TypeCompareCache { get; set; }

            public IEnumerable<string> DHEAssemblies { get; set; }

            public bool ProxyAOTMethod { get; set; }
        }

        private class TypeVirtualTableInfo
        {
            public TypeVirtualTableInfo baseTypeVtableInfo;

            public int totalSlotCount;

            public List<(MethodDef, int)> virtualMethods = new List<(MethodDef, int)>();
        }

        private readonly TypeCompareCache _typeCompareCache;
        private readonly HashSet<string> _dheAssemblies;
        private readonly bool _proxyAOTMethod;

        private readonly Dictionary<MethodDef, MethodCompareData> _methods = new Dictionary<MethodDef, MethodCompareData>(MethodEqualityComparer.CompareDeclaringTypes);

        private readonly Dictionary<MethodDef, int> _aotMethodVirtualTableSlots = new Dictionary<MethodDef, int>(MethodEqualityComparer.CompareDeclaringTypes);

        // 注意，这个使用默认Comparer
        private readonly Dictionary<MethodDef, int> _dheMethodVirtualTableSlots = new Dictionary<MethodDef, int>();

        private readonly Dictionary<TypeDef, TypeVirtualTableInfo> _aotTypes = new Dictionary<TypeDef, TypeVirtualTableInfo>(TypeEqualityComparer.Instance);
        private readonly Dictionary<TypeDef, TypeVirtualTableInfo> _dheTypes = new Dictionary<TypeDef, TypeVirtualTableInfo>();

        public MethodCompareCache(Options options)
        {
            _typeCompareCache = options.TypeCompareCache;
            _dheAssemblies = new HashSet<string>(options.DHEAssemblies.Select(ass => ass + ".dll"));
            _proxyAOTMethod = options.ProxyAOTMethod;
        }



        /// <summary>
        /// 作为要求类型名相同，并且同时是class或enum或valuetype
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        private bool IsMethodSignatureParamTypeEqual(TypeSig t1, TypeSig t2)
        {
            if (!TypeEqualityComparer.Instance.Equals(t1, t2))
            {
                return false;
            }
            return _typeCompareCache.CompareFieldOrParamOrVariableType(t1, t2);
        }

        public bool IsMethodSignatureMatch(MethodDef method, MethodDef oldMethod)
        {
            if (method.IsStatic != oldMethod.IsStatic)
            {
                return false;
            }
            if (method.GenericParameters.Count != oldMethod.GenericParameters.Count)
            {
                return false;
            }

            if (!method.IsStatic && !IsMethodSignatureParamTypeEqual(method.DeclaringType.ToTypeSig(), oldMethod.DeclaringType.ToTypeSig()))
            {
                return false;
            }
            if (method.GetParamCount() != oldMethod.GetParamCount())
            {
                return false;
            }
            if (!IsMethodSignatureParamTypeEqual(method.ReturnType, oldMethod.ReturnType))
            {
                return false;
            }
            for (int i = 0, n = method.Parameters.Count; i < n; i++)
            {
                if (!IsMethodSignatureParamTypeEqual(method.Parameters[i].Type, oldMethod.Parameters[i].Type))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsGenericMethodSignatureMatch(MethodDef method, MethodDef oldMethod, GenericArgumentContext gac)
        {
            if (method.IsStatic != oldMethod.IsStatic)
            {
                return false;
            }
            if (method.GenericParameters.Count != oldMethod.GenericParameters.Count)
            {
                return false;
            }

            if (!method.IsStatic && !IsMethodSignatureParamTypeEqual(method.DeclaringType.ToTypeSig(), MetaUtil.Inflate(oldMethod.DeclaringType.ToTypeSig(), gac)))
            {
                return false;
            }
            if (method.GetParamCount() != oldMethod.GetParamCount())
            {
                return false;
            }
            if (!IsMethodSignatureParamTypeEqual(method.ReturnType, MetaUtil.Inflate(oldMethod.ReturnType, gac)))
            {
                return false;
            }
            for (int i = 0, n = method.Parameters.Count; i < n; i++)
            {
                if (!IsMethodSignatureParamTypeEqual(method.Parameters[i].Type, MetaUtil.Inflate(oldMethod.Parameters[i].Type, gac)))
                {
                    return false;
                }
            }
            return true;
        }

        private MethodCompareData GetOrInitMethod(MethodDef method)
        {
            if (!_methods.TryGetValue(method, out var data))
            {
                data = new MethodCompareData() { method = method, state = MethodCompareState.NotCompared };
                _methods.Add(method, data);
            }
            return data;
        }

        public void Init(IEnumerable<MethodDef> unchangedMethods)
        {
            foreach (var method in unchangedMethods)
            {
                GetOrInitMethod(method).state = MethodCompareState.Equal;
            }
        }

        public void AddCompareMethod(MethodDef m1, MethodDef m2)
        {
            var data = GetOrInitMethod(m1);
            if (data.state == MethodCompareState.NotCompared)
            {
                data.oldMethod = m2;
                CompareMethodImplements(data);
            }
            //Debug.Log($"=== AddCompareMethod m1:{m1} m2:{m2}");
        }

        public bool CompareMethodFinal(MethodDef m1, MethodDef m2)
        {
            return _methods[m1].state != MethodCompareState.NotEqual;
        }

        private bool IsLocationLayoutEqual(TypeSig t1, TypeSig t2)
        {
            return _typeCompareCache.CompareFieldOrParamOrVariableType(t1, t2);
        }

        private bool IsLocationLayoutEqual(ITypeDefOrRef t1, ITypeDefOrRef t2)
        {
            return _typeCompareCache.CompareFieldOrParamOrVariableType(t1.ToTypeSig(), t2.ToTypeSig());
        }

        private bool CompareEqualTypeAndMemoryLayout(ITypeDefOrRef t1, ITypeDefOrRef t2)
        {
            return TypeEqualityComparer.Instance.Equals(t1, t2) && _typeCompareCache.CompareFieldOrParamOrVariableType(t1.ToTypeSig(), t2.ToTypeSig());
        }

        private bool CompareField(IField f1, IField f2)
        {
            Debug.Assert(f1.IsField && f2.IsField);
            if (!_typeCompareCache.CompareTypeLayout(f1.DeclaringType, f2.DeclaringType))
            {
                return false;
            }
            return f1.Name == f2.Name;
        }

        private bool IsDHEModule(ModuleDef module)
        {
            return _dheAssemblies.Contains(module.Name);
        }

        private int FindOverrideMethodSlot(MethodDef method, ITypeDefOrRef parentType)
        {
            if (!parentType.IsTypeSpec)
            {
                return FindOverrideMethodSlot(method, parentType.ResolveTypeDefThrow());
            }
            else
            {
                TypeSpec ts = (TypeSpec)parentType;
                GenericClass gc = GenericClass.ResolveClass(ts, null);
                return FindOverrideMethodSlot(method, gc);
            }
        }

        private int FindOverrideMethodSlot(MethodDef method, GenericClass parentType)
        {
            var gac = new GenericArgumentContext(parentType.KlassInst, null);
            TypeDef genericProtoType = parentType.Type;
            foreach (var m in genericProtoType.Methods)
            {
                if (!m.IsNewSlot)
                {
                    continue;
                }
                if (IsGenericMethodSignatureMatch(method, m, gac))
                {
                    return GetVirtualTableIndex(m);
                }
            }

            ITypeDefOrRef baseType = genericProtoType.BaseType;
            if (baseType != null)
            {
                if (baseType is TypeSpec ts)
                {
                    return FindOverrideMethodSlot(method, GenericClass.ResolveClass(ts, gac));
                }
                else
                {
                    return FindOverrideMethodSlot(method, baseType.ResolveTypeDefThrow());
                }
            }
            else
            {
                throw new Exception("not find override method");
            }
        }

        private int FindOverrideMethodSlot(MethodDef method, TypeDef parentTypeDef)
        {
            foreach (var m in parentTypeDef.Methods)
            {
                if (!m.IsNewSlot)
                {
                    continue;
                }
                if (IsMethodSignatureMatch(method, m))
                {
                    return GetVirtualTableIndex(m);
                }
            }
            if (parentTypeDef.BaseType != null)
            {
                return FindOverrideMethodSlot(method, parentTypeDef.BaseType);
            }
            else
            {
                throw new Exception("not find override method");
            }
        }

        private TypeVirtualTableInfo ComputeClassVirtualTable(TypeDef type, bool isDHEClass)
        {
            var typeInfos = isDHEClass ? _dheTypes : _aotTypes;
            var methodVirtualTableSlots = isDHEClass ? _dheMethodVirtualTableSlots : _aotMethodVirtualTableSlots;
            if (typeInfos.TryGetValue(type, out var tvti))
            {
                return tvti;
            }

            tvti = new TypeVirtualTableInfo();
            typeInfos.Add(type, tvti);

            if (type.IsInterface)
            {
                int slot = 0;
                foreach (var method in type.Methods)
                {
                    if (!method.IsVirtual)
                    {
                        continue;
                    }
                    methodVirtualTableSlots.Add(method, slot);
                    //Debug.Log($"[ComputeClassVirtualTable][interface] method:{method} slot:{slot}");
                    tvti.virtualMethods.Add((method, slot));
                    ++slot;
                }
                tvti.totalSlotCount = slot;
                tvti.baseTypeVtableInfo = null;
            }
            else
            {
                int slot = 0;
                if (type.BaseType != null)
                {
                    TypeDef baseType;
                    if (type.BaseType is TypeSpec ts)
                    {
                        baseType = ts.TryGetGenericInstSig().GenericType.TypeDef;
                    }
                    else
                    {
                        baseType = type.ResolveTypeDefThrow();
                    }
                    tvti.baseTypeVtableInfo = ComputeClassVirtualTable(baseType, IsDHEModule(baseType.Module));
                    slot = tvti.totalSlotCount;
                }
                foreach (var method in type.Methods)
                {
                    if (!method.IsVirtual)
                    {
                        continue;
                    }
                    if (method.IsNewSlot)
                    {
                        methodVirtualTableSlots.Add(method, slot);
                        tvti.virtualMethods.Add((method, slot));

                        //Debug.Log($"[ComputeClassVirtualTable][newslot] method:{method} slot:{slot}");
                        ++slot;
                        continue;
                    }
                    int newSlot = FindOverrideMethodSlot(method, type.BaseType);

                    methodVirtualTableSlots.Add(method, newSlot);
                    tvti.virtualMethods.Add((method, newSlot));

                    //Debug.Log($"[ComputeClassVirtualTable][override] method:{method} slot:{newSlot}");
                    //_methodVirtualTableSlots.Add(method, slot++);
                }
                tvti.totalSlotCount = slot;
            }
            return tvti;
        }

        private int GetVirtualTableIndex(MethodDef method)
        {
            bool isDHEMethod = IsDHEModule(method.Module);
            var vtable = isDHEMethod ? _dheMethodVirtualTableSlots : _aotMethodVirtualTableSlots;
            if (!vtable.TryGetValue(method, out int index))
            {
                ComputeClassVirtualTable(method.DeclaringType, isDHEMethod);
                return vtable[method];
            }
            return index;
        }

        private bool IsSameVirtualTableIndex(MethodDef m1, MethodDef m2)
        {
            int index1 = GetVirtualTableIndex(m1);
            int index2 = GetVirtualTableIndex(m2);
            return index1 == index2;
        }

        private bool CompareCallVirtualMethod(IMethod m1, IMethod m2, MethodCompareData callerData)
        {
            if (!MethodEqualityComparer.CompareDeclaringTypes.Equals(m1, m2))
            {
                return false;
            }
            if (!_typeCompareCache.CompareFieldOrParamOrVariableType(m1.DeclaringType.ToTypeSig(), m2.DeclaringType.ToTypeSig()))
            {
                return false;
            }
            if (TryGetDHEMethodDef(m1, out var md1) && TryGetDHEMethodDef(m2, out var md2))
            {
                if (!IsMethodSignatureMatch(md1, md2))
                {
                    return false;
                }
                if (md1.IsVirtual ^ md2.IsVirtual)
                {
                    return false;
                }
                if (!md1.IsVirtual)
                {
                    return _proxyAOTMethod || CompareMethodDefInternal(md1, md2, callerData);
                }
                if (md1.DeclaringType.IsInterface ^ md2.DeclaringType.IsInterface)
                {
                    return false;
                }
                // FIXME 还需要小心将il2cpp虚函数变成直接的call调用
                if (!_proxyAOTMethod && !CompareMethodDefInternal(md1, md2, callerData))
                {
                    return false;
                }
                return IsSameVirtualTableIndex(md1, md2);
            }
            // 没错，这儿返回true，因为非DHE函数只要MethodEqualityComparer比较相同，我们都假定它们是一样的
            return true;
        }


        private bool CompareMethodDefInternal(MethodDef m1, MethodDef m2, MethodCompareData callerData)
        {
            var data = GetOrInitMethod(m1);
            if (data.state == MethodCompareState.Equal)
            {
                return true;
            }
            if (data.state == MethodCompareState.NotEqual)
            {
                return false;
            }
            data.AddRelyOtherMethod(callerData);
            return true;
        }

        private bool TryGetDHEMethodDef(IMethod method, out MethodDef md)
        {
            if (!_dheAssemblies.Contains(method.Module.Name))
            {
                md = null;
                return false;
            }
            md = method.ResolveMethodDefThrow();
            return true;
        }

        private bool CompareCallNotVirtualMethod(IMethod m1, IMethod m2, MethodCompareData callerMethod)
        {
            if (!MethodEqualityComparer.CompareDeclaringTypes.Equals(m1, m2))
            {
                return false;
            }
            if (!_typeCompareCache.CompareFieldOrParamOrVariableType(m1.DeclaringType.ToTypeSig(), m2.DeclaringType.ToTypeSig()))
            {
                return false;
            }
            if (TryGetDHEMethodDef(m1, out var md1) && TryGetDHEMethodDef(m2, out var md2))
            {
                return _proxyAOTMethod || CompareMethodDefInternal(md1, md2, callerMethod);
            }
            // 没错，这儿返回true，因为非DHE函数只要MethodEqualityComparer比较相同，我们都假定它们是一样的
            return true;
        }

        private bool CompareToken(ITokenOperand t1, ITokenOperand t2, MethodCompareData callerMethod)
        {
            if (t1.GetType() != t2.GetType())
            {
                return false;
            }
            if (t1 is ITypeDefOrRef td1 && t2 is ITypeDefOrRef td2)
            {
                //return CompareEqualTypeAndMemoryLayout(td1, td2);
                return TypeEqualityComparer.Instance.Equals(td1, td2);
            }
            if (t1 is IMethod m1 && t2 is IMethod m2)
            {
                //return CompareCallNotVirtualMethod(m1, m2, callerMethod);
                return MethodEqualityComparer.CompareDeclaringTypes.Equals(m1, m2);
            }
            if (t1 is IField f1 && t2 is IField f2)
            {
                //return CompareField(f1, f2);
                return FieldEqualityComparer.CompareDeclaringTypes.Equals(f1, f2);
            }
            return false;
        }

        /// <summary>
        /// 只要求所有参数 Layout Equal即可
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        private bool CompareMethodSigParamLayoutEqual(MethodSig m1, MethodSig m2)
        {
            if (m1.CallingConvention != m2.CallingConvention)
            {
                return false;
            }
            if (m1.Params.Count != m2.Params.Count)
            {
                return false;
            }
            if (!IsLocationLayoutEqual(m1.RetType, m2.RetType))
            {
                return false;
            }
            for (int i = 0, n = m1.Params.Count; i < n; i++)
            {
                if (!IsLocationLayoutEqual(m1.Params[i], m2.Params[i]))
                {
                    return false;
                }
            }
            return false;
        }

        private bool CompareExceptionHandler(ExceptionHandler e1, ExceptionHandler e2)
        {
            if (e1.HandlerType != e2.HandlerType)
            {
                return false;
            }
            if (e1.TryStart.Offset != e2.TryStart.Offset)
            {
                return false;
            }
            if (e1.TryEnd.Offset != e2.TryEnd.Offset)
            {
                return false;
            }
            if (e1.HandlerStart.Offset != e2.HandlerStart.Offset)
            {
                return false;
            }
            if (e1.HandlerEnd.Offset != e2.HandlerEnd.Offset)
            {
                return false;
            }
            if (e1.HandlerType == ExceptionHandlerType.Filter)
            {
                if (e1.FilterStart.Offset != e2.FilterStart.Offset)
                {
                    return false;
                }
            }
            else if (e1.HandlerType == ExceptionHandlerType.Catch)
            {
                //if (!IsLocationLayoutEqual(e1.CatchType.ToTypeSig(), e2.CatchType.ToTypeSig()))
                //{
                //    return false;
                //}
                // Exception类型肯定是LocationLayoutEqual
            }
            return true;
        }

        private bool CompareInstruction(Instruction c1, Instruction c2, MethodCompareData method)
        {
            OpCode opCode1 = c1.OpCode;
            OpCode opCode2 = c2.OpCode;
            Code code1 = opCode1.Code;
            if (code1 != opCode2.Code)
            {
                return false;
            }
            object op1 = c1.Operand;
            object op2 = c2.Operand;
            if (op1 == null)
            {
                return op2 == null;
            }
            if (op2 == null)
            {
                return false;
            }
            // ???
            if (op1.GetType() != op2.GetType())
            {
                return false;
            }

            switch (opCode1.OperandType)
            {
                case OperandType.InlineNone:
                    return true;
                case OperandType.InlineI:
                case OperandType.InlineI8:
                case OperandType.InlineR:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineR:
                case OperandType.InlineString:
                    return op1.Equals(op2);
                case OperandType.InlineVar:
                case OperandType.ShortInlineVar:
                {
                    if (op1 is Local local1)
                    {
                        return local1.Index == ((Local)op2).Index;
                    }
                    else
                    {
                        return ((Parameter)op1).Index == ((Parameter)op2).Index;
                    }
                }
                case OperandType.InlineBrTarget:
                case OperandType.ShortInlineBrTarget:
                    return ((Instruction)op1).Offset == ((Instruction)op2).Offset;
                case OperandType.InlineField:
                    return CompareField((IField)op1, (IField)op2);
                case OperandType.InlineMethod:
                {
                    if (code1 == Code.Callvirt || code1 == Code.Ldvirtftn)
                    {
                        return CompareCallVirtualMethod((IMethod)op1, (IMethod)op2, method);
                    }
                    else
                    {
                        return CompareCallNotVirtualMethod((IMethod)op1, (IMethod)op2, method);
                    }
                }
                case OperandType.InlineSig:
                    return CompareMethodSigParamLayoutEqual((MethodSig)op1, (MethodSig)op2);
                case OperandType.InlineSwitch:
                {
                    IList<Instruction> case1 = (IList<Instruction>)op1;
                    IList<Instruction> case2 = (IList<Instruction>)op2;
                    if (case1.Count != case2.Count)
                    {
                        return false;
                    }
                    for (int i = 0, n = case1.Count; i < n; i++)
                    {
                        if (case1[i].Offset != case2[i].Offset)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                case OperandType.InlineTok:
                    return CompareToken((ITokenOperand)op1, (ITokenOperand)op2, method);
                case OperandType.InlineType:
                {
                    if (code1 == Code.Initobj || code1 == Code.Cpobj || code1 == Code.Ldobj || code1 == Code.Stobj)
                    {
                        return IsLocationLayoutEqual((ITypeDefOrRef)op1, (ITypeDefOrRef)op2);
                    }
                    else
                    {
                        return CompareEqualTypeAndMemoryLayout((ITypeDefOrRef)op1, (ITypeDefOrRef)op2);
                    }
                }
                //case OperandType.InlinePhi:
                //case OperandType.NOT_USED_8:
                default:
                    throw new NotSupportedException($"not support operandType:{opCode1.OperandType}");
            }
            throw new NotSupportedException($"not support instruction op:{opCode1} operand:{op1}");
        }

        private bool CompareMethodBody(MethodDef m1, MethodDef m2, MethodCompareData data)
        {
            if (m1.IsStatic != m2.IsStatic)
            {
                return false;
            }

            if (m1.HasBody != m2.HasBody)
            {
                return false;
            }
            if (!m1.HasBody)
            {
                return true;
            }
            CilBody b1 = m1.Body;
            CilBody b2 = m2.Body;
            if (b1.Variables.Count != b2.Variables.Count)
            {
                return false;
            }
            for (int i = 0, n = b1.Variables.Count; i < n; i++)
            {
                var v1 = b1.Variables[i];
                var v2 = b2.Variables[i];
                if (!IsLocationLayoutEqual(v1.Type, v2.Type))
                {
                    //Debug.Log($"variable not eqal:{v1.Type} {v2.Type}");
                    return false;
                }
            }
            if (b1.ExceptionHandlers.Count != b2.ExceptionHandlers.Count)
            {
                //Debug.Log($"ExceptionHandlers.Count not equal. {b1.ExceptionHandlers.Count} {b2.ExceptionHandlers.Count}");
                return false;
            }
            for (int i = 0, n = b1.ExceptionHandlers.Count; i < n; i++)
            {
                ExceptionHandler e1 = b1.ExceptionHandlers[i];
                ExceptionHandler e2 = b2.ExceptionHandlers[i];
                if (!CompareExceptionHandler(e1, e2))
                {
                    //Debug.Log($"ExceptionHandler not equal. index:{i}");
                    return false;
                }
            }
            if (b1.Instructions.Count != b2.Instructions.Count)
            {
                //Debug.Log($"Instructions.Count not equal. {b1.Instructions.Count} {b2.Instructions.Count}");
                return false;
            }
            for (int i = 0, n = b1.Instructions.Count; i < n; i++)
            {
                var c1 = b1.Instructions[i];
                var c2 = b2.Instructions[i];
                if (!CompareInstruction(c1, c2, data))
                {
                    //Debug.Log($"Instruction not equal. [{i}] {c1} {c2}");
                    return false;
                }
            }

            return true;
        }

        private void CompareMethodImplements(MethodCompareData mm)
        {
            Debug.Assert(mm.state == MethodCompareState.NotCompared && mm.oldMethod != null);
            mm.state = MethodCompareState.Comparing;
            if (CompareMethodBody(mm.method, mm.oldMethod, mm))
            {
                mm.state = mm.relyOtherMethods == null ? MethodCompareState.Equal : MethodCompareState.Comparing;
                //Debug.Log($"CompareMethodImplements method:{mm.method} state:{mm.state}");
            }
            else
            {
                mm.state = MethodCompareState.NotEqual;
                //Debug.Log($"method:{mm.method} not equal");
            }
            if (mm.state == MethodCompareState.Equal || mm.state == MethodCompareState.NotEqual)
            {
                mm.FireRelyMethods();
            }
        }

    }
}
