using HybridCLR.Editor.DHE;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{
    public static class DifferentialHybridExecutionCommand
    {
        [MenuItem("HybridCLR/Generate/DHEAssemblyList", priority = 110)]
        public static void GenerateAssemblyList()
        {
            var options = new AssemblyListGenerator.Options()
            {
                DifferentialHybridAssembyList = (HybridCLRSettings.Instance.differentialHybridAssemblies ?? Array.Empty<string>()).ToList(),
                OutputFile = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr/Il2CppCompatibleDef.cpp",
            };

            var g = new AssemblyListGenerator(options);
            g.Generate();
        }


        [MenuItem("HybridCLR/Generate/DHEAssemblyOptionDatas", priority = 111)]
        public static void GenerateAssemblyOptionDatas()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            GenerateAssemblyOptionDatas(target);
        }

        public static void GenerateAssemblyOptionDatas(BuildTarget target)
        {
            HybridCLRSettings settings = HybridCLRSettings.Instance;
            string[] differentialHybridAssemblyList = settings.differentialHybridAssemblies;
            if (differentialHybridAssemblyList == null || differentialHybridAssemblyList.Length == 0)
            {
                Debug.Log("[DifferentialHybridExecutionCommand.GenerateAssemblyOptionDatas] differentialHybridAssemblies is empty. skip generation.");
                return;
            }
            var options = new AssemblyOptionDataGenerator.Options()
            {
                Target = target,
                DifferentialHybridAssembyList = differentialHybridAssemblyList.ToList(),
                OldAssemblyResolver = MetaUtil.CreateAOTAssemblyResolver(target),
                NewAssemblyResolver = MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, SettingsUtil.HotUpdateAssemblyNames.Concat(differentialHybridAssemblyList).ToList()),
                OutputDir = $"{SettingsUtil.ProjectDir}/{settings.differentialHybridOptionOutputDir}",
                ProxyAOTMethod = false,
            };

            using (var g = new AssemblyOptionDataGenerator(options))
            {
                g.Init();
                g.Generate();
            }
        }
    }
}
