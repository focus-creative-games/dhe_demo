using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Meta;
using HybridCLR.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using IAssemblyResolver = HybridCLR.Editor.Meta.IAssemblyResolver;

namespace HybridCLR.Editor.DHE
{
    public class AssemblyOptionDataGenerator : IDisposable
    {
        public class Options
        {
            public BuildTarget Target { get; set; }

            public string OutputDir { get; set; }

            public List<string> DifferentialHybridAssembyList { get; set; }

            public IAssemblyResolver OldAssemblyResolver { get; set; }

            public IAssemblyResolver NewAssemblyResolver { get; set; }

            public bool ProxyAOTMethod { get; set; }
        }

        public class AssemblyMeta
        {
            public ModuleDefMD curMoudle;

            public ModuleDefMD oldModule;

            public List<MethodMeta> methods;
        }

        private readonly Options _options;

        private AssemblyCache _oldAssCache;
        private AssemblyCache _curAssCache;

        private readonly TypeCompareCache _typeCompareCache;

        private readonly MethodCompareCache _methodCompareCache;

        private readonly Dictionary<string, AssemblyMeta> _assemblyMetas = new Dictionary<string, AssemblyMeta>();

        public Dictionary<string, AssemblyMeta> AssemblyMetas => _assemblyMetas;

        private readonly Dictionary<TypeDef, TypeMeta> _types = new Dictionary<TypeDef, TypeMeta>();

        private readonly Dictionary<MethodDef, MethodMeta> _methods = new Dictionary<MethodDef, MethodMeta>();

        private bool disposedValue;

        public AssemblyOptionDataGenerator(Options options)
        {
            _options = options;
            _typeCompareCache = new TypeCompareCache(options.DifferentialHybridAssembyList);
            _methodCompareCache = new MethodCompareCache(new MethodCompareCache.Options
            {
                TypeCompareCache = _typeCompareCache,
                DHEAssemblies = options.DifferentialHybridAssembyList,
                ProxyAOTMethod = options.ProxyAOTMethod,
            });
        }

        public void Init()
        {
            InitAssemblyDatas();
        }

        private void InitAssemblyDatas()
        {
            _oldAssCache = new AssemblyCache(_options.OldAssemblyResolver);
            _curAssCache = new AssemblyCache(_options.NewAssemblyResolver);
            
            foreach (string assName in _options.DifferentialHybridAssembyList)
            {
                var data = new AssemblyMeta()
                {
                    oldModule = _oldAssCache.LoadModule(assName),
                    curMoudle = _curAssCache.LoadModule(assName),
                    methods = new List<MethodMeta>(),
                };
                _assemblyMetas.Add(assName, data);
            }
        }

        private bool IsUnchanged(CustomAttributeCollection cac)
        {
            var attr = cac.Where(a => a.AttributeType.FullName == "HybridCLR.Runtime.UnchangedAttribute").FirstOrDefault();
            if (attr != null)
            {
                return (bool)attr.ConstructorArguments[0].Value;
            }
            return false;
        }

        private void ComputUnchangedCustomAttributeMetas()
        {
            var unchangedMethods = new List<MethodDef>();
            foreach (string assName in _options.DifferentialHybridAssembyList)
            {
                var data = _assemblyMetas[assName];
                ModuleDefMD ass = data.curMoudle;
                foreach (var type in ass.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        if (IsUnchanged(method.CustomAttributes))
                        {
                            _methods.Add(method, MethodMeta.CreateEqual(method));
                            unchangedMethods.Add(method);
                            //Debug.Log($"[PreprocessCustomAttributeMarks]  unchanged method:{method} {method.MDToken.Raw}");
                        }
                    }
                }
            }
            _methodCompareCache.Init(unchangedMethods);
        }

        private void GenerateData()
        {
            foreach (var e in _assemblyMetas)
            {
                string assName = e.Key;
                AssemblyMeta data = e.Value;
                string outOptionFile = $"{_options.OutputDir}/{assName}.dhao.bytes";

                var layoutNotChangeTypes = new SortedSet<uint>(data.curMoudle.GetTypes().Where(t => t.IsValueType && !t.IsEnum).Select(t => _types[t])
                    .Where(t => t.instanceState == TypeCompareState.MemoryLayoutEqual)
                    .Select(t => t.type.MDToken.Raw)).ToList();
                var logicNotChangeMethods = new SortedSet<uint>(data.methods.Where(m => m.state == MethodCompareState.Equal)
                    .Select(m => m.method.MDToken.Raw)).ToList();
                var dhaOptions = new DifferentialHybridAssemblyOptions()
                {
                    notChangeMethodTokens = logicNotChangeMethods,
                };
                File.WriteAllBytes(outOptionFile, dhaOptions.Marshal());
                Debug.Log($"[AssemblyOptionDataGenerator] assembly:{data.curMoudle} notChangeTypeCount:{layoutNotChangeTypes.Count} notChangeMethodCount:{logicNotChangeMethods.Count} output:{outOptionFile}");
            }
        }

        public void Generate()
        {
            ComputUnchangedCustomAttributeMetas();
            ComputeUnchangeTypes();
            ComputeUnchangeMethods();
            GenerateData();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _oldAssCache.Dispose();
                    _curAssCache.Dispose();
                }
                _oldAssCache = null;
                _curAssCache = null;
                disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public TypeMeta GetOrInitTypeMeta(TypeDef type)
        {
            if (!_types.TryGetValue(type, out var meta))
            {
                meta = new TypeMeta
                {
                    type = type,
                    instanceState = TypeCompareState.NotCompared,
                    fields = new List<FieldMeta>(),
                    fieldDef2Metas = new Dictionary<FieldDef, FieldMeta>(),
                };
                _types.Add(type, meta);
            }
            return meta;
        }

        private void ComputeUnchangeTypes()
        {
            foreach (string assName in _options.DifferentialHybridAssembyList)
            {
                var ass = _assemblyMetas[assName];
                ModuleDefMD mod = ass.curMoudle;
                foreach (var type in mod.GetTypes())
                {
                    //ComputeTypeMeta(type, ass);
                    TypeMeta meta = GetOrInitTypeMeta(type);
                    if (meta.instanceState == TypeCompareState.NotCompared)
                    {
                        TypeDef type2 = ass.oldModule.Find(type.FullName, false);
                        if (type2 != null)
                        {
                            meta.instanceState = _typeCompareCache.CompareTypeLayout(type, type2) ? TypeCompareState.MemoryLayoutEqual : TypeCompareState.NotEqual;
                            meta.staticState = _typeCompareCache.CompareTypeStaticLayout(type, type2) ? TypeCompareState.MemoryLayoutEqual : TypeCompareState.NotEqual;
                            meta.threadStaticState = _typeCompareCache.CompareTypeThreadStaticLayout(type, type2) ? TypeCompareState.MemoryLayoutEqual : TypeCompareState.NotEqual;
                        }
                        else
                        {
                            meta.instanceState = meta.staticState = meta.threadStaticState = TypeCompareState.NotEqual;
                        }
                    }
                    if (meta.instanceState == TypeCompareState.NotEqual)
                    {
                        //Debug.Log($"change instance type:{type} token:{type.MDToken.Raw}");
                    }
                    if (meta.staticState == TypeCompareState.NotEqual)
                    {
                        //Debug.Log($"change static type:{type} token:{type.MDToken.Raw}");
                    }
                    if (meta.threadStaticState == TypeCompareState.NotEqual)
                    {
                        //Debug.Log($"change thread static type:{type} token:{type.MDToken.Raw}");
                    }
                }
            }
        }

        public MethodMeta GetOrInitMethod(MethodDef method)
        {
            if (_methods.TryGetValue(method, out var result))
            {
                return result;
            }
            result = new MethodMeta { method = method, state = MethodCompareState.NotCompared };
            _methods.Add(method, result);
            return result;
        }

        private MethodDef FindMatchMethod(MethodDef method, TypeDef oldType)
        {
            foreach (var oldMethod in oldType.Methods)
            {
                if (method.Name != oldMethod.Name)
                {
                    continue;
                }

                if (_methodCompareCache.IsMethodSignatureMatch(method, oldMethod))
                {
                    return oldMethod;
                }
            }
            return null;
        }

        private void ComputeUnchangeMethods()
        {
            foreach (string assName in _options.DifferentialHybridAssembyList)
            {
                var assMeta = _assemblyMetas[assName];
                ModuleDefMD curMod = assMeta.curMoudle;
                foreach (var type in curMod.GetTypes())
                {
                    uint token = type.MDToken.Raw;
                    TypeDef oldType = assMeta.oldModule.Find(type.FullName, false);
                    TypeMeta tm = _types[type];
                    
                    bool typeNotEqual = oldType == null || !tm.IsLocationLayoutEqual();
                    foreach (var method in type.Methods)
                    {
                        MethodMeta mcr = GetOrInitMethod(method);
                        assMeta.methods.Add(mcr);
                        if (mcr.state != MethodCompareState.NotCompared)
                        {
                            continue;
                        }
                        MethodDef method2 = oldType != null ? FindMatchMethod(method, oldType) : null;
                        if (method2 == null)
                        {
                            mcr.state = MethodCompareState.NotEqual;
                            continue;
                        }
                        else
                        {
                            mcr.oldMethod = method2;
                            mcr.state = MethodCompareState.Comparing;
                            _methodCompareCache.AddCompareMethod(method, method2);
                        }
                    }
                }
            }

            foreach (var data in _assemblyMetas.Values)
            {

                foreach (var method in data.methods)
                {
                    if (method.state == MethodCompareState.Comparing)
                    {
                        method.state = _methodCompareCache.CompareMethodFinal(method.method, method.oldMethod) ? MethodCompareState.Equal : MethodCompareState.NotEqual;
                    }
                    if (method.state == MethodCompareState.NotEqual)
                    {
                        //Debug.Log($"change mehtod:{method.method} token:{method.method.MDToken.Raw}");
                    }
                }
            }
        }
    }
}
