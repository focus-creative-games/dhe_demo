using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.DHE;
using HybridCLR.Editor.Settings;
using HybridCLR.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

public static class BuildTools
{
    public const string BackupAOTDllDir = "HybridCLRData/BackupAOT";

    public const string EncrypedDllDir = "HybridCLRData/EncryptedDll";

    public const string DhaoDir = "HybridCLRData/Dhao";

    public const string ManifestFile = "manifest.txt";


    /// <summary>
    /// 备份构建主包时生成的裁剪AOT dll
    /// </summary>
    [MenuItem("BuildTools/BackupAOTDll")]
    public static void BackupAOTDllFromAssemblyPostStrippedDir()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        var backupDir = $"{BackupAOTDllDir}/{target}";
        System.IO.Directory.CreateDirectory(backupDir);
        var dlls = System.IO.Directory.GetFiles(SettingsUtil.GetAssembliesPostIl2CppStripDir(target));
        foreach (var dll in dlls)
        {
            var fileName = System.IO.Path.GetFileName(dll);
            string dstFile = $"{BackupAOTDllDir}/{target}/{fileName}";
            System.IO.File.Copy(dll, dstFile, true);
            Debug.Log($"BackupAOTDllFromAssemblyPostStrippedDir: {dll} -> {dstFile}");
        }
    }

    ///// <summary>
    ///// 创建dhe manifest文件，格式为每行一个 'dll名，原始dll的md5'
    ///// </summary>
    ///// <param name="outputDir"></param>
    //[MenuItem("BuildTools/CreateManifestAtBackupDir")]
    //public static void CreateManifest()
    //{
    //    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    //    string backupDir = $"{BackupAOTDllDir}/{target}";
    //    CreateManifest(backupDir);
    //}

    //public static void CreateManifest(string outputDir)
    //{
    //    Directory.CreateDirectory(outputDir);
    //    var lines = new List<string>();
    //    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    //    string backupDir = $"{BackupAOTDllDir}/{target}";
    //    foreach (string dheDll in SettingsUtil.DifferentialHybridAssemblyNames)
    //    {
    //        string originalDll = $"{backupDir}/{dheDll}.dll";
    //        string originalDllMd5 = AssemblyOptionDataGenerator.CreateMD5Hash(File.ReadAllBytes(originalDll));
    //        lines.Add($"{dheDll},{originalDllMd5}");
    //    }
    //    string manifestFile = $"{outputDir}/{ManifestFile}";
    //    File.WriteAllBytes(manifestFile, System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines)));
    //    Debug.Log($"CreateManifest: {manifestFile}");
    //}

    ///// <summary>
    ///// 生成首包的没有任何代码改动对应的dhao数据
    ///// </summary>
    //[MenuItem("BuildTools/GenerateUnchangedDHAODatas")]
    //public static void GenerateUnchangedDHAODatas()
    //{
    //    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    //    string backupDir = $"{BackupAOTDllDir}/{target}";
    //    string dhaoDir = $"{DhaoDir}/{target}";
    //    BuildUtils.GenerateUnchangedDHAODatas(SettingsUtil.DifferentialHybridAssemblyNames, backupDir, dhaoDir);
    //}


    [MenuItem("BuildTools/CompileHotUpdateDlls")]
    public static void CompileHotUpdateDlls()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string outputDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        Directory.CreateDirectory(outputDir);
        var group = BuildPipeline.GetBuildTargetGroup(target);

        ScriptCompilationSettings scriptCompilationSettings = new ScriptCompilationSettings();
        scriptCompilationSettings.group = group;
        scriptCompilationSettings.target = target;
        scriptCompilationSettings.extraScriptingDefines = new string[] { "HOT_UPDATE_CHANGE" };
        ScriptCompilationResult scriptCompilationResult = PlayerBuildInterface.CompilePlayerScripts(scriptCompilationSettings, outputDir);
        Debug.Log("compile finish!!!");
    }

    /// <summary>
    /// 生成热更包的dhao数据
    /// </summary>
    [MenuItem("BuildTools/GenerateDHAODatas")]
    public static void GenerateDHAODatas()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string backupDir = $"{BackupAOTDllDir}/{target}";
        string dhaoDir = $"{DhaoDir}/{target}";
        string currentDllDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        BuildUtils.GenerateDHAODatas(SettingsUtil.DifferentialHybridAssemblyNames, backupDir, currentDllDir, null, HybridCLRSettings.Instance.injectRuleFiles, dhaoDir);
    }

    [MenuItem("BuildTools/CompileHotUpdateDllsAndGenerateDHAODatas")]
    public static void CompileHotUpdateDllsAndGenerateDHAODatas()
    {
        CompileHotUpdateDlls();
        GenerateDHAODatas();
    }

    ///// <summary>
    ///// 生成首包的加密dll和没有任何代码改动对应的dhao数据
    ///// </summary>
    //[MenuItem("BuildTools/GenerateUnchangedEncryptedDllAndDhaoDatas")]
    //public static void GenerateUnchangedEncryptedDllAndDhaoDatas()
    //{
    //    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    //    string backupDir = $"{BackupAOTDllDir}/{target}";
    //    string dhaoDir = $"{DhaoDir}/{target}";
    //    string encryptedDllDir = $"{EncrypedDllDir}/{target}";
    //    BuildUtils.EncryptDllAndGenerateUnchangedDHAODatas(SettingsUtil.DifferentialHybridAssemblyNames, backupDir, encryptedDllDir, dhaoDir);
    //}


    ///// <summary>
    ///// 生成热更包的加密dll和dhao数据
    ///// </summary>
    //[MenuItem("BuildTools/GenerateEncryptedDllAndDhaoDatas")]
    //public static void GenerateEncryptedDllAndDhaoDatas()
    //{
    //    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    //    string backupDir = $"{BackupAOTDllDir}/{target}";
    //    string dhaoDir = $"{DhaoDir}/{target}";
    //    string currentDllDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
    //    string encryptedDllDir = $"{EncrypedDllDir}/{target}";
    //    BuildUtils.EncryptDllAndGenerateDHAODatas(SettingsUtil.DifferentialHybridAssemblyNames, backupDir, currentDllDir, null, HybridCLRSettings.Instance.injectRuleFiles, encryptedDllDir, dhaoDir);
    //}

    ///// <summary>
    ///// 复制没有改动的首包dll和dhao文件到StreamingAssets
    ///// </summary>
    //[MenuItem("BuildTools/CopyUnchangedDllAndDhaoFileAndManifestToStreamingAssets")]
    //public static void CopyUnchangedDllAndDhaoFileToStreamingAssets()
    //{
    //    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    //    string streamingAssetsDir = Application.streamingAssetsPath;
    //    Directory.CreateDirectory(streamingAssetsDir);

    //    string manifestFile = $"{BackupAOTDllDir}/{target}/{ManifestFile}";
    //    string dstManifestFile = $"{streamingAssetsDir}/{ManifestFile}";
    //    System.IO.File.Copy(manifestFile, dstManifestFile, true);
    //    Debug.Log($"CopyUnchangedDllAndDhaoFileToStreamingAssets: {manifestFile} -> {dstManifestFile}");

    //    string dllDir = $"{BackupAOTDllDir}/{target}";
    //    string dhaoDir = $"{DhaoDir}/{target}";
    //    foreach (var dll in SettingsUtil.DifferentialHybridAssemblyNames)
    //    {
    //        string srcFile = $"{dllDir}/{dll}.dll";
    //        string dstFile = $"{streamingAssetsDir}/{dll}.dll.bytes";
    //        System.IO.File.Copy(srcFile, dstFile, true);
    //        Debug.Log($"CopyUnchangedDllAndDhaoFileToStreamingAssets: {srcFile} -> {dstFile}");
    //        string dhaoFile = $"{dhaoDir}/{dll}.dhao.bytes";
    //        dstFile = $"{streamingAssetsDir}/{dll}.dhao.bytes";
    //        System.IO.File.Copy(dhaoFile, dstFile, true);
    //        Debug.Log($"CopyUnchangedDllAndDhaoFileToStreamingAssets: {dhaoFile} -> {dstFile}");
    //    }
    //}

    /// <summary>
    /// 复制热更新dll和dhao文件到StreamingAssets
    /// </summary>
    [MenuItem("BuildTools/CopyDllAndDhaoFileToHotUpdateDataDir")]
    public static void CopyDllAndDhaoFileToHotUpdateDataDir()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string hotUpdateDatasDir = $"{Application.dataPath}/../HotUpdateDatas";
        Directory.CreateDirectory(hotUpdateDatasDir);

        string dllDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        string dhaoDir = $"{DhaoDir}/{target}";
        foreach (var dll in SettingsUtil.DifferentialHybridAssemblyNames)
        {
            string srcFile = $"{dllDir}/{dll}.dll";
            string dstFile = $"{hotUpdateDatasDir}/{dll}.dll.bytes";
            System.IO.File.Copy(srcFile, dstFile, true);
            Debug.Log($"Copy: {srcFile} -> {dstFile}");
            string dhaoFile = $"{dhaoDir}/{dll}.dhao.bytes";
            dstFile = $"{hotUpdateDatasDir}/{dll}.dhao.bytes";
            System.IO.File.Copy(dhaoFile, dstFile, true);
            Debug.Log($"Copy: {dhaoFile} -> {dstFile}");
        }
    }
}
