using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LoadDll : MonoBehaviour
{

    void Start()
    {
        // Editor�����£�HotUpdate.dll.bytes�Ѿ����Զ����أ�����Ҫ���أ��ظ����ط���������⡣
#if !UNITY_EDITOR
        Assembly hotUpdateAss = LoadDifferentialHybridAssembly("HotUpdate");
#else
        // Editor��������أ�ֱ�Ӳ��һ��HotUpdate����
        Assembly hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#endif
        Type helloType = hotUpdateAss.GetType("Hello");
        MethodInfo runMethod = helloType.GetMethod("Run");
        runMethod.Invoke(null, null);
    }

    private Assembly LoadDifferentialHybridAssembly(string assName)
    {
        byte[] dllBytes =  File.ReadAllBytes($"{Application.streamingAssetsPath}/{assName}.dll.bytes");
        string dhaoPath = $"{Application.streamingAssetsPath}/{assName}.dhao.bytes";
        byte[] dhaoBytes = File.Exists(dhaoPath) ? File.ReadAllBytes(dhaoPath) : null;
        LoadImageErrorCode err = RuntimeApi.LoadDifferentialHybridAssembly(dllBytes, dhaoBytes, true);
        if (err == LoadImageErrorCode.OK)
        {
            Debug.Log($"LoadDifferentialHybridAssembly {assName} OK");
            return System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == assName);
        }
        else
        {
            Debug.LogError($"LoadDifferentialHybridAssembly {assName} failed, err={err}");
            return null;
        }
    }
}
