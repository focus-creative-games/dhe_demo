using dnlib.DotNet;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HybridCLR.Editor.DHE
{
    public class MethodMeta
    {
        public static MethodMeta CreateEqual(MethodDef method)
        {
            return new MethodMeta
            {
                method = method,
                state = MethodCompareState.Equal,
            };
        }

        public MethodDef method;

        public MethodDef oldMethod;

        public MethodCompareState state;
    }
}
