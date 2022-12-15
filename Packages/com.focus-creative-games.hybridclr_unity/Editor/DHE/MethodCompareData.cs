using dnlib.DotNet;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HybridCLR.Editor.DHE
{
    public class MethodCompareData
    {
        public static MethodCompareData CreateEqual(MethodDef method)
        {
            return new MethodCompareData
            {
                method = method,
                state = MethodCompareState.Equal,
            };
        }

        public MethodDef method;

        public MethodDef oldMethod;

        public MethodCompareState state;

        public List<MethodCompareData> relyOtherMethods;


        public List<MethodCompareData> relySelfMethods;

        public void AddRelyOtherMethod(MethodCompareData other)
        {
            if (relyOtherMethods == null)
            {
                relyOtherMethods = new List<MethodCompareData>(4);
            }
            if (relyOtherMethods.Contains(other))
            {
                return;
            }
            relyOtherMethods.Add(other);
            if (other.relySelfMethods == null)
            {
                other.relySelfMethods = new List<MethodCompareData>(4);
            }
            other.relySelfMethods.Add(this);
        }


        public void FireRelyMethods()
        {
            if (relySelfMethods == null)
            {
                return;
            }
            var oldRelySelfMethod = relySelfMethods;
            relySelfMethods = null;
            foreach (var method in oldRelySelfMethod)
            {
                if (method.state != MethodCompareState.Comparing)
                {
                    continue;
                }
                if (state == MethodCompareState.NotEqual)
                {
                    method.state = MethodCompareState.NotEqual;
                    relyOtherMethods = null;
                    method.FireRelyMethods();
                }
                else if (state == MethodCompareState.Equal)
                {
                    bool removed = method.relyOtherMethods.Remove(this);
                    Debug.Assert(removed);
                    if (method.relyOtherMethods.Count == 0)
                    {
                        method.state = MethodCompareState.Equal;
                        relyOtherMethods = null;
                        method.FireRelyMethods();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
