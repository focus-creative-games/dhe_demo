using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.DHE;
using HybridCLR.Editor.Installer;
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

    public static string GetAOTSnapshotDir(BuildTarget target)
    {
        return $"{SettingsUtil.HybridCLRDataDir}/Snapshot/{target}";
    }

    [MenuItem("Build/CreateAotSnapshot")]
    public static void CreateAOTSnapshot()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string snapshotDir = GetAOTSnapshotDir(target);
        MetaVersionWorkflow.CreateAotSnapshot(target, snapshotDir);

        MetaVersionWorkflow.GenerateAotSnapshotMetaVersionFiles(null, snapshotDir);
    }

    //[MenuItem("Build/GenerateHotUpdateMetaVersionFiles")]
    public static void GenerateHotUpdateMetaVersionFiles()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        GenerateHotUpdateMetaVersionFiles(target);
    }

    public static void GenerateHotUpdateMetaVersionFiles(BuildTarget target)
    {
        var latestSnapshotSolutionDir = GetAOTSnapshotDir(target);
        var newHotUpdateSolutionDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        MetaVersionWorkflow.GenerateHotUpdateMetaVersionFiles(latestSnapshotSolutionDir, newHotUpdateSolutionDir);
    }


    [MenuItem("Build/CompileHotUpdateDlls")]
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


    [MenuItem("Build/CompileAndGenerateHotUpdateMetaVersionFiles")]
    public static void CompileAndGenerateHotUpdateMetaVersionFiles()
    {
        CompileHotUpdateDlls();
        GenerateHotUpdateMetaVersionFiles();
    }


    private static void CopyOriginalMetaVersions(string originalMetaVersionDir, string outputMetaVersionDir)
    {
        Directory.CreateDirectory(outputMetaVersionDir);
        foreach (var dll in SettingsUtil.DifferentialHybridAssemblyNames)
        {
            string srcMetaVersionFile = $"{originalMetaVersionDir}/{dll}.mv.bytes";
            string dstMetaVersionFile = $"{outputMetaVersionDir}/{dll}.mv.bytes";
            System.IO.File.Copy(srcMetaVersionFile, dstMetaVersionFile, true);
            Debug.Log($"Copy: {srcMetaVersionFile} -> {dstMetaVersionFile}");
        }
    }

    [MenuItem("Build/CopyHotUpdateDllAndMetaVersionFilesToHotUpdateDataDir")]
    public static void CopyHotUpdateDllAndMetaVersionFilesToHotUpdateDataDir()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string outputHotUpdateResDir = $"{Application.dataPath}/../HotUpdateSnapshot/{target}";
        BashUtil.RecreateDir(outputHotUpdateResDir);


        // Copy OriginalMetaVersions
        // 演示项目出于方便，在发布热更新时才复制这个目录。
        // 实际项目推荐在CreateAotSnapshot时就复制这个目录到StreamingAssets目录下，随包发布。
        CopyOriginalMetaVersions(Snapshot.GetMetaVersionDir(GetAOTSnapshotDir(target)), $"{outputHotUpdateResDir}/OriginalMetaVersions");

        // Copy HotUpdate dlls and meta version files
        string hotUpdateSnapshotDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        foreach (var dll in SettingsUtil.DifferentialHybridAssemblyNames)
        {
            // copy dll
            string srcFile = $"{hotUpdateSnapshotDir}/{dll}.dll";
            string dstFile = $"{outputHotUpdateResDir}/{dll}.dll.bytes";
            System.IO.File.Copy(srcFile, dstFile, true);
            Debug.Log($"Copy: {srcFile} -> {dstFile}");

            // copy MetaVersion files
            string srcMetaVersionFile = $"{Snapshot.GetMetaVersionDir(hotUpdateSnapshotDir)}/{dll}.mv.bytes";
            string dstMetaVersionFile = $"{outputHotUpdateResDir}/{dll}.mv.bytes";
            System.IO.File.Copy(srcMetaVersionFile, dstMetaVersionFile, true);
            Debug.Log($"Copy: {srcMetaVersionFile} -> {dstMetaVersionFile}");
        }
    }
}
