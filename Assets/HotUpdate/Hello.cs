using System.Collections;
using UnityEngine;

public class Hello
{
    public static void Run()
    {
        Debug.Log("Hello, HybridCLR");
        Benchmark();
    }


    public static void Benchmark()
    {
        Debug.Log("Benchmark");
        int round = 10;

        Debug.Log("========= ���� �ȸ��º�δ�ı�ĺ��� Test1");
        for (int i = 0; i < round; i++)
        {
            BenchmarkTest1(i);
        }

        Debug.Log("========= ���� �ȸ��º�ı�ĺ��� Test2");
        for (int i = 0; i < round; i++)
        {
            BenchmarkTest2(i);
        }
    }

    /// <summary>
    /// �˺����ȸ���ǰ��δ�����仯����AOT����
    /// </summary>
    /// <param name="round"></param>
    /// <returns></returns>
    public static int BenchmarkTest1(int round)
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        int n = 0;
        for (int i = 0; i < 10000000; i++)
        {
            n = n * round + 1;
        }
        sw.Stop();
        Debug.Log($"Test1 [{round}]: cost {sw.ElapsedMilliseconds}ms");
        return n;
    }

    /// <summary>
    /// �˺����ȸ���ǰ�����仯���߽���ִ��
    /// </summary>
    /// <param name="round"></param>
    /// <returns></returns>
    public static int BenchmarkTest2(int round)
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        int n = 0;
        // �ȸ��º󣬴˴�����ĳɴ�1��ʼ�����������仯���߽���ִ��
        for (int i = 1; i < 10000000; i++)
        //for (int i = 0; i < 10000000; i++)
        {
            n = n * round + 1;
        }
        sw.Stop();
        Debug.Log($"Test1 [{round}]: cost {sw.ElapsedMilliseconds}ms");
        return n;
    }
}