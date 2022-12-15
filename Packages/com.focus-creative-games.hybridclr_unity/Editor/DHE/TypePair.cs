using dnlib.DotNet;
using System;

namespace HybridCLR.Editor.DHE
{
    struct TypePair : IEquatable<TypePair>
    {
        public ITypeDefOrRef type1;

        public ITypeDefOrRef type2;

        private readonly int _hashCode;

        public TypePair(ITypeDefOrRef t1, ITypeDefOrRef t2)
        {
            type1 = t1;
            type2 = t2;
            _hashCode = HashUtil.CombineHash(TypeEqualityComparer.Instance.GetHashCode(t1), TypeEqualityComparer.Instance.GetHashCode(t2));
        }

        public bool Equals(TypePair other)
        {
            return TypeEqualityComparer.Instance.Equals(type1, other.type1)
                && TypeEqualityComparer.Instance.Equals(type2, other.type2);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            return Equals((TypePair)obj);
        }
    }
}
