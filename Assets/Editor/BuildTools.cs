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


    public static string GetDhaoDir(BuildTarget target)
    {
        return $"{SettingsUtil.HybridCLRDataDir}/DHAO/{target}";
    }

    [MenuItem("Build/CreateAotSnapshot")]
    public static void CreateAOTSnapshot()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string snapshotDir = GetAOTSnapshotDir(target);
        MetaVersionWorkflow.CreateAotSnapshot(target, snapshotDir);
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
        string dhaoOutputDir = GetDhaoDir(target);
        DhaoWorkflow.GenerateDhaoFiles(latestSnapshotSolutionDir, newHotUpdateSolutionDir, dhaoOutputDir);
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

    [MenuItem("Build/CopyHotUpdateDllAndMetaVersionFilesToHotUpdateDataDir")]
    public static void CopyHotUpdateDllAndMetaVersionFilesToHotUpdateDataDir()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string outputHotUpdateResDir = $"{Application.dataPath}/../HotUpdateSnapshot/{target}";
        BashUtil.RecreateDir(outputHotUpdateResDir);

        // Copy HotUpdate dlls and meta version files
        string hotUpdateSnapshotDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        string dhaoDir = GetDhaoDir(target);
        foreach (var dll in SettingsUtil.DifferentialHybridAssemblyNames)
        {
            // copy dll
            string srcFile = $"{hotUpdateSnapshotDir}/{dll}.dll";
            string dstFile = $"{outputHotUpdateResDir}/{dll}.dll.bytes";
            System.IO.File.Copy(srcFile, dstFile, true);
            Debug.Log($"Copy: {srcFile} -> {dstFile}");

            // copy dhao files
            string srcDhaoFile = $"{dhaoDir}/{dll}.dhao.bytes";
            string dstDhaoFile = $"{outputHotUpdateResDir}/{dll}.dhao.bytes";
            System.IO.File.Copy(srcDhaoFile, dstDhaoFile, true);
            Debug.Log($"Copy: {srcDhaoFile} -> {dstDhaoFile}");
        }
    }
}
