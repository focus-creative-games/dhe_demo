using HybridCLR.Editor;
using HybridCLR.Editor.DHE;
using HybridCLR.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildTools
{
    public const string BackupAOTDllDir = "HybridCLRData/BackupAOT";

    public const string EncrypedDllDir = "HybridCLRData/EncryptedDll";

    public const string DhaoDir = "HybridCLRData/Dhao";


    /// <summary>
    /// ���ݹ�������ʱ���ɵĲü�AOT dll
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

    /// <summary>
    /// �����װ���û���κδ���Ķ���Ӧ��dhao����
    /// </summary>
    [MenuItem("BuildTools/GenerateUnchangedDHAODatas")]
    public static void GenerateUnchangedDHAODatas()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string backupDir = $"{BackupAOTDllDir}/{target}";
        string dhaoDir = $"{DhaoDir}/{target}";
        BuildUtils.GenerateUnchangedDHAODatas(SettingsUtil.DifferentialHybridAssemblyNames, backupDir, dhaoDir);
    }

    /// <summary>
    /// �����ȸ�����dhao����
    /// </summary>
    [MenuItem("BuildTools/GenerateDHAODatas")]
    public static void GenerateDHAODatas()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string backupDir = $"{BackupAOTDllDir}/{target}";
        string dhaoDir = $"{DhaoDir}/{target}";
        string currentDllDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        BuildUtils.GenerateDHAODatas(SettingsUtil.DifferentialHybridAssemblyNames, backupDir, currentDllDir, dhaoDir);
    }

    /// <summary>
    /// �����װ��ļ���dll��û���κδ���Ķ���Ӧ��dhao����
    /// </summary>
    [MenuItem("BuildTools/GenerateUnchangedEncryptedDllAndDhaoDatas")]
    public static void GenerateUnchangedEncryptedDllAndDhaoDatas()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string backupDir = $"{BackupAOTDllDir}/{target}";
        string dhaoDir = $"{DhaoDir}/{target}";
        string encryptedDllDir = $"{EncrypedDllDir}/{target}";
        BuildUtils.EncryptDllAndGenerateUnchangedDHAODatas(SettingsUtil.DifferentialHybridAssemblyNames, backupDir, encryptedDllDir, dhaoDir);
    }


    /// <summary>
    /// �����ȸ����ļ���dll��dhao����
    /// </summary>
    [MenuItem("BuildTools/GenerateEncryptedDllAndDhaoDatas")]
    public static void GenerateEncryptedDllAndDhaoDatas()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string backupDir = $"{BackupAOTDllDir}/{target}";
        string dhaoDir = $"{DhaoDir}/{target}";
        string currentDllDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        string encryptedDllDir = $"{EncrypedDllDir}/{target}";
        BuildUtils.EncryptDllAndGenerateDHAODatas(SettingsUtil.DifferentialHybridAssemblyNames, backupDir, currentDllDir, encryptedDllDir, dhaoDir);
    }

    /// <summary>
    /// ����dhe manifest�ļ�����ʽΪÿ��һ�� 'dll����ԭʼdll��md5����ǰdll��md5'
    /// </summary>
    /// <param name="outputDir"></param>
    [MenuItem("BuildTools/CreateManifestAtStreamingAssets")]
    public static void CreateManifest()
    {
        CreateManifest(Application.streamingAssetsPath);
    }

    public static void CreateManifest(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var lines = new List<string>();
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string backupDir = $"{BackupAOTDllDir}/{target}";
        foreach (string dheDll in SettingsUtil.DifferentialHybridAssemblyNames)
        {
            string originalDll = $"{backupDir}/{dheDll}.dll";
            string originalDllMd5 = AssemblyOptionDataGenerator.CreateMD5Hash(File.ReadAllBytes(originalDll));
            string currentDll = $"{outputDir}/{dheDll}.dll.bytes";
            string currentDllMd5 = AssemblyOptionDataGenerator.CreateMD5Hash(File.ReadAllBytes(currentDll));
            lines.Add($"{dheDll},{originalDllMd5},{currentDllMd5}");
        }
        string manifestFile = $"{outputDir}/manifest.txt";
        File.WriteAllBytes(manifestFile, System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        Debug.Log($"CreateManifest: {manifestFile}");
    }

    /// <summary>
    /// ����û�иĶ����װ�dll��dhao�ļ���StreamingAssets
    /// </summary>
    [MenuItem("BuildTools/CopyUnchangedDllAndDhaoFileToStreamingAssets")]
    public static void CopyUnchangedDllAndDhaoFileToStreamingAssets()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string streamingAssetsDir = Application.streamingAssetsPath;
        Directory.CreateDirectory(streamingAssetsDir);
        string dllDir = $"{BackupAOTDllDir}/{target}";
        string dhaoDir = $"{DhaoDir}/{target}";
        foreach (var dll in SettingsUtil.DifferentialHybridAssemblyNames)
        {
            string srcFile = $"{dllDir}/{dll}.dll";
            string dstFile = $"{streamingAssetsDir}/{dll}.dll.bytes";
            System.IO.File.Copy(srcFile, dstFile, true);
            Debug.Log($"CopyUnchangedDllAndDhaoFileToStreamingAssets: {srcFile} -> {dstFile}");
            string dhaoFile = $"{dhaoDir}/{dll}.dhao.bytes";
            dstFile = $"{streamingAssetsDir}/{dll}.dhao.bytes";
            System.IO.File.Copy(dhaoFile, dstFile, true);
            Debug.Log($"CopyUnchangedDllAndDhaoFileToStreamingAssets: {dhaoFile} -> {dstFile}");
        }
    }

    /// <summary>
    /// �����ȸ���dll��dhao�ļ���StreamingAssets
    /// </summary>
    [MenuItem("BuildTools/CopyDllAndDhaoFileToStreamingAssets")]
    public static void CopyDllAndDhaoFileToStreamingAssets()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string streamingAssetsDir = Application.streamingAssetsPath;
        Directory.CreateDirectory(streamingAssetsDir);
        string dllDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        string dhaoDir = $"{DhaoDir}/{target}";
        foreach (var dll in SettingsUtil.DifferentialHybridAssemblyNames)
        {
            string srcFile = $"{dllDir}/{dll}.dll";
            string dstFile = $"{streamingAssetsDir}/{dll}.dll.bytes";
            System.IO.File.Copy(srcFile, dstFile, true);
            Debug.Log($"CopyUnchangedDllAndDhaoFileToStreamingAssets: {srcFile} -> {dstFile}");
            string dhaoFile = $"{dhaoDir}/{dll}.dhao.bytes";
            dstFile = $"{streamingAssetsDir}/{dll}.dhao.bytes";
            System.IO.File.Copy(dhaoFile, dstFile, true);
            Debug.Log($"CopyUnchangedDllAndDhaoFileToStreamingAssets: {dhaoFile} -> {dstFile}");
        }
    }
}
