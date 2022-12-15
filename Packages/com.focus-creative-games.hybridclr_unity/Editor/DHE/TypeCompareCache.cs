using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.DHE
{

    public class TypeCompareCache
    {
        private readonly HashSet<string> _dheAssemblies;


        private readonly Dictionary<TypePair, bool> _typeCompareCache = new Dictionary<TypePair, bool>();
        private readonly Dictionary<TypePair, bool> _typeStaticCompareCache = new Dictionary<TypePair, bool>();
        private readonly Dictionary<TypePair, bool> _typeThreadStaticCompareCache = new Dictionary<TypePair, bool>();

        public TypeCompareCache(IEnumerable<string> dheAssemblies)
        {
            _dheAssemblies = new HashSet<string>(dheAssemblies.Select(ass => ass + ".dll"));
        }

        public bool TryGetCacheCompareResult(ITypeDefOrRef t1, ITypeDefOrRef t2, out bool result)
        {
            return _typeCompareCache.TryGetValue(new TypePair(t1, t2), out result);
        }

        private void AddCacheCompareResult(ITypeDefOrRef t1, ITypeDefOrRef t2, bool result)
        {
            _typeCompareCache.Add(new TypePair(t1, t2), result);
        }

        private bool TryGetOrComputeAddCompare(TypeDef t1, TypeDef t2, Func<TypeDef, TypeDef, bool> compareFunc)
        {
            if (TryGetCacheCompareResult(t1, t2, out var ret))
            {
                return ret;
            }
            ret = compareFunc(t1, t2);
            AddCacheCompareResult(t1, t2, ret);
            return ret;
        }

        private bool TryGetOrComputeAddCompareStatic(TypeDef t1, TypeDef t2, Func<TypeDef, TypeDef, bool> compareFunc)
        {
            var key = new TypePair(t1, t2);
            if (_typeStaticCompareCache.TryGetValue(key, out var ret))
            {
                return ret;
            }
            ret = compareFunc(t1, t2);
            _typeStaticCompareCache.Add(key, ret);
            return ret;
        }

        private bool TryGetOrComputeAddCompareThreadStatic(TypeDef t1, TypeDef t2, Func<TypeDef, TypeDef, bool> compareFunc)
        {
            var key = new TypePair(t1, t2);
            if (_typeThreadStaticCompareCache.TryGetValue(key, out var ret))
            {
                return ret;
            }
            ret = compareFunc(t1, t2);
            _typeThreadStaticCompareCache.Add(key, ret);
            return ret;
        }

        private bool IsSameTypeFamily(TypeDef t1, TypeDef t2)
        {
            //if (t1 == null)
            //{
            //    return t2 == null;
            //}
            //if (t2 == null)
            //{
            //    return false;
            //}
            if (t1.IsEnum ^ t2.IsEnum)
            {
                return false;
            }
            if (t1.IsValueType ^ t2.IsValueType)
            {
                return false;
            }
            if (t1.IsClass ^ t2.IsClass)
            {
                return false;
            }
            if (t1.IsInterface ^ t2.IsInterface)
            {
                return false;
            }
            return true;
        }

        public bool CompareTypeLayout(ITypeDefOrRef t1, ITypeDefOrRef t2)
        {
            //Debug.Log($"CompareTypeLayout {t1} {t2}");
            if (t1 == null)
            {
                return t2 == null;
            }
            else
            {
                if (t2 == null)
                {
                    return false;
                }
            }
            if (t1.IsTypeRef)
            {
                t1 = t1.ResolveTypeDefThrow();
            }
            if (t2.IsTypeRef)
            {
                t2 = t2.ResolveTypeDefThrow();
            }
            
            if (t1 is TypeDef td1 && t2 is TypeDef td2)
            {
                //Debug.Log($"CompareTypeLayout 2 {t1} {t2}");
                if (!IsSameTypeFamily(td1, td2))
                {
                    return false;
                }
                if (td1.IsEnum)
                {
                    return td1.GetEnumUnderlyingType().ElementType == td2.GetEnumUnderlyingType().ElementType;
                }
                if (td1.IsClass)
                {
                    return TryGetOrComputeAddCompare(td1, td2, CompareClassLayout);
                }
                return TryGetOrComputeAddCompare(td1, td2, CompareValueTypeLayout);
            }
            if (t1 is TypeSpec ts1 && t2 is TypeSpec ts2)
            {
                GenericInstSig gis1 = ts1.TryGetGenericInstSig();
                GenericInstSig gis2 = ts2.TryGetGenericInstSig();

                if (gis1 == null || gis2 == null)
                {
                    return false;
                }

                TypeDef gt1 = gis1.GenericType.TypeDefOrRef.ResolveTypeDef();
                TypeDef gt2 = gis2.GenericType.TypeDefOrRef.ResolveTypeDef();
                if (!IsSameTypeFamily(gt1, gt2))
                {
                    return false;
                }
                if (gt1.IsEnum)
                {
                    return gt1.GetEnumUnderlyingType().ElementType == gt2.GetEnumUnderlyingType().ElementType;
                }
                if (gt1.IsClass)
                {
                    if (TryGetCacheCompareResult(t1, t2, out var ret))
                    {
                        return ret;
                    }
                    ret = CompareGenericInstClassLayout(gis1, gis2);
                    AddCacheCompareResult(t1, t2, ret);
                    return ret;
                }

                if (gt1.IsValueType)
                {
                    if (TryGetCacheCompareResult(t1, t2, out var ret))
                    {
                        return ret;
                    }
                    ret = CompareGenericInstValueTypeLayout(gis1, gis2);
                    AddCacheCompareResult(t1, t2, ret);
                    return ret;
                }
            }
            return false;
        }

        private bool CompareFieldsLayout(FieldDef[] instanceFields1, FieldDef[] instanceFields2)
        {
            if (instanceFields1.Length != instanceFields2.Length)
            {
                return false;
            }
            for (int i = 0; i < instanceFields1.Length; i++)
            {
                var f1 = instanceFields1[i];
                var f2 = instanceFields2[i];
                if (!CompareFieldOrParamOrVariableType(f1.FieldType, f2.FieldType))
                {
                    //Debug.Log($"not equal. {f1.FieldType} {f2.FieldType}");
                    return false;
                }
            }
            return true;
        }

        private bool TryFastCompareClassLayout(TypeDef t1, TypeDef t2, out bool result)
        {
            if (_dheAssemblies.Contains(t1.Module.Name))
            {
                result = false;
                if (_dheAssemblies.Contains(t2.Module.Name))
                {
                    return !TypeEqualityComparer.Instance.Equals(t1, t2);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                result = !_dheAssemblies.Contains(t2.Module.Name) && TypeEqualityComparer.Instance.Equals(t1, t2);
                return true;
            }
        }

        private bool CompareClassLayout(TypeDef t1, TypeDef t2)
        {
            //Debug.Log($"CompareClassLayout {t1} {t2}");
            if (TryFastCompareClassLayout(t1, t2, out var result))
            {
                //Debug.Log($"fast compare class layout:{t1} {t2} result:{result}");
                return result;
            }
            FieldDef[] instanceFields1 = t1.Fields.Where(f => !f.IsStatic && !f.IsLiteral).ToArray();
            FieldDef[] instanceFields2 = t2.Fields.Where(f => !f.IsStatic && !f.IsLiteral).ToArray();
            return CompareFieldsLayout(instanceFields1, instanceFields2);
        }

        private bool CompareValueTypeExcplicitLayout(TypeDef t1, TypeDef t2)
        {
            FieldDef[] instanceFields1 = t1.Fields.Where(f => !f.IsStatic && !f.IsLiteral).ToArray();
            FieldDef[] instanceFields2 = t2.Fields.Where(f => !f.IsStatic && !f.IsLiteral).ToArray();
            if (instanceFields1.Length != instanceFields2.Length)
            {
                return false;
            }
            for (int i = 1; i < instanceFields1.Length; i++)
            {
                var f1 = instanceFields1[i];
                var f2 = instanceFields2[i];
                if (f1.FieldOffset != f2.FieldOffset)
                {
                    return false;
                }
                if (!CompareFieldOrParamOrVariableType(f1.FieldType, f2.FieldType))
                {
                    return false;
                }
            }
            return true;
        }

        private bool CompareGenericInstClassLayout(GenericInstSig t1, GenericInstSig t2)
        {
            if (!CompareGenericInstArguments(t1, t2))
            {
                return false;
            }
            return TryGetOrComputeAddCompare(t1.GenericType.TypeDefOrRef.ResolveTypeDef(), t2.GenericType.TypeDefOrRef.ResolveTypeDef(), this.CompareClassLayout);
        }

        private bool CompareValueTypeLayout(TypeDef t1, TypeDef t2)
        {
            //Debug.Log($"CompareClassLayout {t1} {t2}");
            if (t1.HasClassLayout)
            {
                if (!t2.HasClassLayout)
                    return false;
                if (t1.ClassSize != t2.ClassSize
                    || t1.PackingSize != t2.PackingSize)
                {
                    return false;
                }
                if (t1.IsExplicitLayout ^ t2.IsExplicitLayout)
                {
                    return false;
                }
                if (t1.IsExplicitLayout)
                {
                    return CompareValueTypeExcplicitLayout(t1, t2);
                }
            }
            else if (t2.HasClassLayout)
            {
                return false;
            }
            return CompareClassLayout(t1, t2);
        }

        private bool CompareGenericInstArguments(GenericInstSig t1, GenericInstSig t2)
        {
            if (t1.GenericArguments.Count != t2.GenericArguments.Count)
            {
                return false;
            }
            for (int i = 0, n = t1.GenericArguments.Count; i < n; i++)
            {
                if (!CompareFieldOrParamOrVariableType(t1.GenericArguments[i], t2.GenericArguments[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private bool CompareGenericInstValueTypeLayout(GenericInstSig t1, GenericInstSig t2)
        {
            if (!CompareGenericInstArguments(t1, t2))
            {
                return false;
            }
            return TryGetOrComputeAddCompare(t1.GenericType.TypeDefOrRef.ResolveTypeDef(), t2.GenericType.TypeDefOrRef.ResolveTypeDef(), this.CompareValueTypeLayout);
        }


        public ElementType ComputeReduceElementType(TypeSig t)
        {
            ElementType pt = ElementType.I;
            if (t.IsByRef)
            {
                return pt;
            }
            ElementType obj = ElementType.Object;
            ElementType et = t.ElementType;
            switch (et)
            {
                case ElementType.Void:
                case ElementType.Boolean:
                case ElementType.Char:
                case ElementType.I1:
                case ElementType.U1:
                case ElementType.I2:
                case ElementType.U2:
                case ElementType.I4:
                case ElementType.U4:
                case ElementType.I8:
                case ElementType.U8:
                case ElementType.R4:
                case ElementType.R8: return et;
                case ElementType.String: return obj;
                case ElementType.Ptr: 
                case ElementType.ByRef: return pt;
                case ElementType.ValueType: return et;
                case ElementType.Class: return obj;
                case ElementType.Var:
                case ElementType.MVar: return et;
                case ElementType.Array: return obj;
                case ElementType.GenericInst:
                {
                    GenericInstSig gis1 = t.ToGenericInstSig();
                    TypeDef gt1 = gis1.GenericType.TypeDefOrRef.ResolveTypeDef();
                    return gt1.IsClass ? obj : et;
                }
                case ElementType.TypedByRef: return et;
                case ElementType.ValueArray: throw new NotSupportedException();
                case ElementType.I:
                case ElementType.U:
                case ElementType.R: return et;
                case ElementType.FnPtr: return pt;
                case ElementType.Object:
                case ElementType.SZArray: return obj;
                //case ElementType.CModReqd:
                //case ElementType.CModOpt:
                //case ElementType.Internal:
                //case ElementType.Module:
                //case ElementType.Sentinel:
                //case ElementType.Pinned:
                default: return et;
            }
        }

        public bool CompareFieldOrParamOrVariableType(TypeSig t1, TypeSig t2)
        {
            t1 = t1.RemovePinnedAndModifiers();
            t2 = t2.RemovePinnedAndModifiers();
            ElementType reduceEleType1 = ComputeReduceElementType(t1);
            ElementType reduceEleType2 = ComputeReduceElementType(t2);
            if (reduceEleType1 != reduceEleType2)
            {
                return false;
            }
            //if (t1.IsByRef ^ t2.IsByRef)
            //{
            //    return false;
            //}
            switch (t1.ElementType)
            {
                case ElementType.Void:
                case ElementType.Boolean:
                case ElementType.Char:
                case ElementType.I1:
                case ElementType.U1:
                case ElementType.I2:
                case ElementType.U2:
                case ElementType.I4:
                case ElementType.U4:
                case ElementType.I8:
                case ElementType.U8:
                case ElementType.R4:
                case ElementType.R8:
                case ElementType.String: return true;
                case ElementType.Ptr:
                case ElementType.ByRef: return true;
                case ElementType.ValueType: return CompareTypeLayout(t1.ToTypeDefOrRef(), t2.ToTypeDefOrRef());
                case ElementType.Class: return true;
                case ElementType.Var:
                {
                    GenericVar gv1 = t1.ToGenericVar();
                    GenericVar gv2 = t2.ToGenericVar();
                    return gv1.Number == gv2.Number;
                }
                case ElementType.MVar:
                {
                    GenericMVar gv1 = t1.ToGenericMVar();
                    GenericMVar gv2 = t2.ToGenericMVar();
                    return gv1.Number == gv2.Number;
                }
                case ElementType.Array: return true;
                case ElementType.GenericInst:
                {
                    GenericInstSig gis1 = t1.ToGenericInstSig();
                    GenericInstSig gis2 = t2.ToGenericInstSig();
                    TypeDef gt1 = gis1.GenericType.TypeDefOrRef.ResolveTypeDef();
                    TypeDef gt2 = gis2.GenericType.TypeDefOrRef.ResolveTypeDef();
                    if (gt1 == null || gt2 == null)
                    {
                        Debug.Log($"GenericType is null. {t1}, {t2}, {gt1}, {gt2}");
                    }
                    if (gt1.IsClass)
                    {
                        return gt2.IsClass;
                    }
                    if (gt2.IsClass)
                    {
                        return false;
                    }
                    if (gt1.IsEnum)
                    {
                        return gt2.IsEnum && gt1.GetEnumUnderlyingType().ElementType == gt2.GetEnumUnderlyingType().ElementType;
                    }
                    if (gt2.IsEnum)
                    {
                        return false;
                    }
                    return CompareGenericInstValueTypeLayout(gis1, gis2);
                }
                case ElementType.TypedByRef: return true;
                case ElementType.ValueArray: throw new NotSupportedException();
                case ElementType.I:
                case ElementType.U:
                case ElementType.R:
                case ElementType.FnPtr:
                case ElementType.Object:
                case ElementType.SZArray: return true;
                //case ElementType.CModReqd:
                //case ElementType.CModOpt:
                //case ElementType.Internal:
                //case ElementType.Module:
                //case ElementType.Sentinel:
                //case ElementType.Pinned:
                default: throw new NotSupportedException(t1.ElementType.ToString());
            }
        }

        private bool CompareTypeStaticLayout0(TypeDef t1, TypeDef t2)
        {
            if (t1.IsEnum)
            {
                return t2.IsEnum;
            }
            if (t2.IsEnum)
            {
                return false;
            }

            if (TryFastCompareClassLayout(t1, t2, out var result))
            {
                return result;
            }

            FieldDef[] staticFields1 = t1.Fields.Where(f => f.IsStatic && !f.CustomAttributes.Any(ca => ca.TypeFullName == "System.ThreadStaticAttribute")).ToArray();
            FieldDef[] staticFields2 = t2.Fields.Where(f => f.IsStatic && !f.CustomAttributes.Any(ca => ca.TypeFullName == "System.ThreadStaticAttribute")).ToArray();
            return CompareFieldsLayout(staticFields1, staticFields2);
        }

        public bool CompareTypeStaticLayout(ITypeDefOrRef t1, ITypeDefOrRef t2)
        {
            if (t1.IsTypeRef)
            {
                t1 = t1.ResolveTypeDefThrow();
            }
            if (t2.IsTypeRef)
            {
                t2 = t2.ResolveTypeDefThrow();
            }

            if (t1 is TypeDef td1 && t2 is TypeDef td2)
            {
                return TryGetOrComputeAddCompareStatic(td1, td2, CompareTypeStaticLayout0);

            }
            if (t1 is TypeSpec ts1 && t2 is TypeSpec ts2)
            {
                GenericInstSig gis1 = ts1.TryGetGenericInstSig();
                GenericInstSig gis2 = ts2.TryGetGenericInstSig();

                if (gis1 == null || gis2 == null)
                {
                    return false;
                }

                if (!CompareGenericInstArguments(gis1, gis2))
                {
                    return false;
                }
                TypeDef gt1 = gis1.GenericType.TypeDefOrRef.ResolveTypeDef();
                TypeDef gt2 = gis2.GenericType.TypeDefOrRef.ResolveTypeDef();
                return TryGetOrComputeAddCompareStatic(gt1, gt2, CompareTypeStaticLayout0);
            }
            return false;
        }

        private bool CompareTypeThreadStaticLayout0(TypeDef t1, TypeDef t2)
        {
            if (t1.IsEnum)
            {
                return t2.IsEnum;
            }
            if (t2.IsEnum)
            {
                return false;
            }

            if (TryFastCompareClassLayout(t1, t2, out var result))
            {
                return result;
            }

            FieldDef[] staticFields1 = t1.Fields.Where(f => f.IsStatic && f.CustomAttributes.Any(ca => ca.TypeFullName == "System.ThreadStaticAttribute")).ToArray();
            FieldDef[] staticFields2 = t2.Fields.Where(f => f.IsStatic && f.CustomAttributes.Any(ca => ca.TypeFullName == "System.ThreadStaticAttribute")).ToArray();
            return CompareFieldsLayout(staticFields1, staticFields2);
        }

        public bool CompareTypeThreadStaticLayout(ITypeDefOrRef t1, ITypeDefOrRef t2)
        {
            if (t1.IsTypeRef)
            {
                t1 = t1.ResolveTypeDefThrow();
            }
            if (t2.IsTypeRef)
            {
                t2 = t2.ResolveTypeDefThrow();
            }

            if (t1 is TypeDef td1 && t2 is TypeDef td2)
            {
                return TryGetOrComputeAddCompareThreadStatic(td1, td2, CompareTypeThreadStaticLayout0);

            }
            if (t1 is TypeSpec ts1 && t2 is TypeSpec ts2)
            {
                GenericInstSig gis1 = ts1.TryGetGenericInstSig();
                GenericInstSig gis2 = ts2.TryGetGenericInstSig();

                if (gis1 == null || gis2 == null)
                {
                    return false;
                }

                if (!CompareGenericInstArguments(gis1, gis2))
                {
                    return false;
                }
                TypeDef gt1 = gis1.GenericType.TypeDefOrRef.ResolveTypeDef();
                TypeDef gt2 = gis2.GenericType.TypeDefOrRef.ResolveTypeDef();
                return TryGetOrComputeAddCompareThreadStatic(gt1, gt2, CompareTypeThreadStaticLayout0);
            }
            return false;
        }
    }
}
