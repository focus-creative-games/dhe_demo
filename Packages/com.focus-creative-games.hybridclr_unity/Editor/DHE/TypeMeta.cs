using dnlib.DotNet;
using System.Collections.Generic;
using System.Linq;

namespace HybridCLR.Editor.DHE
{
    public class TypeMeta
    {
        public TypeDef type;

        public TypeCompareState instanceState;

        public TypeCompareState staticState;

        public TypeCompareState threadStaticState;

        public List<FieldMeta> fields = new List<FieldMeta>();

        public Dictionary<FieldDef, FieldMeta> fieldDef2Metas = new Dictionary<FieldDef, FieldMeta>();

        public bool IsLocationLayoutEqual()
        {
            return instanceState <= TypeCompareState.MemoryLayoutEqual;
        }


        public static TypeMeta CreateEqual(TypeDef type)
        {
            var fields = type.Fields.Select(f => new FieldMeta { field = f, state = FieldCompareState.Equal }).ToList();

            return new TypeMeta
            {
                type = type,
                instanceState = TypeCompareState.MemoryLayoutEqual,
                staticState = TypeCompareState.MemoryLayoutEqual,
                threadStaticState = TypeCompareState.MemoryLayoutEqual,
                fields = fields,
                fieldDef2Metas = fields.ToDictionary(f => f.field),
            };
        }
    }
}
