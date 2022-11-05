using HybridCLR.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateByCode : MonoBehaviour
{
    [Unchanged]
    void Start()
    {
        Debug.Log("这个函数应该在AOT中执行");
    }

    [Unchanged(false)]
    void Start3()
    {
        Debug.Log("在热更新中运行3");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
