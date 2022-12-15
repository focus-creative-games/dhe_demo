using UnityEditorInternal;
using UnityEngine;
namespace HybridCLR.Editor
{
    [FilePath("ProjectSettings/HybridCLRSettings.asset")]
    public class HybridCLRSettings : ScriptableSingleton<HybridCLRSettings>
    {
        [Header("开启HybridCLR插件")]
        public bool enable = true;

        [Header("使用全局安装的il2cpp")]
        public bool useGlobalIl2cpp;

        [Header("hybridclr 仓库 URL")]
        public string hybridclrRepoURL = "https://gitee.com/focus-creative-games/hybridclr";

        [Header("il2cpp_plus 仓库 URL")]
        public string il2cppPlusRepoURL = "https://gitee.com/focus-creative-games/il2cpp_plus";

        [Header("热更新Assembly Definitions")]
        public AssemblyDefinitionAsset[] hotUpdateAssemblyDefinitions;

        [Header("热更新dlls")]
        public string[] hotUpdateAssemblies;

        [Header("预留的热更新dlls")]
        public string[] preserveHotUpdateAssemblies;

        [Header("热更新dll编译输出根目录")]
        public string hotUpdateDllCompileOutputRootDir = "HybridCLRData/HotUpdateDlls";

        [Header("外部热更新dll搜索路径")]
        public string[] externalHotUpdateAssembliyDirs;

        [Header("裁减后AOT dll输出根目录")]
        public string strippedAOTDllOutputRootDir = "HybridCLRData/AssembliesPostIl2CppStrip";

        [Header("补充元数据AOT dlls")]
        public string[] patchAOTAssemblies;

        [Header("差分混合热更新 dlls")]
        public string[] differentialHybridAssemblies;

        [Header("差分混合热更新配置数据输出目录")]
        public string differentialHybridOptionOutputDir = "HybridCLRData/DifferentialHybridOptionDatas";

        [Header("生成link.xml时扫描asset中引用的类型")]
        public bool collectAssetReferenceTypes;

        [Header("生成的link.xml路径")]
        public string outputLinkFile = "HybridCLRData/Generated/link.xml";

        [Header("自动扫描生成的AOTGenericReferences.cs路径")]
        public string outputAOTGenericReferenceFile = "HybridCLRData/Generated/AOTGenericReferences.cs";

        [Header("AOT泛型实例化搜索迭代次数")]
        public int maxGenericReferenceIteration = 10;

        [Header("MethodBridge泛型搜索迭代次数")]
        public int maxMethodBridgeGenericIteration = 10;
    }
}