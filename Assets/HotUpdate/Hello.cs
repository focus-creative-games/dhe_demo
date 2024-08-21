using System.Collections;
using UnityEngine;

public class Hello
{
    public static void Run()
    {
        int round = 10;

        for (int i = 0; i < round; i++)
        {
            BenchmarkTest(i);
        }
    }

    public static int BenchmarkTest(int round)
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        int n = 0;

        // �ȸ���ǰ���˴������0��ʼ������δ�����仯����AOT����
        // �ȸ��º󣬴˴�����ĳɴ�1��ʼ�����������仯���߽���ִ��
#if HOT_UPDATE_CHANGE
        Debug.Log("========= Run Changed BenchmarkTest");
        for (int i = 1; i < 10000000; i++)
#else
        Debug.Log("========= Run Unchanged BenchmarkTest");
        for (int i = 0; i < 10000000; i++)
#endif
        {
            n = n * round + 1;
        }
        sw.Stop();
        Debug.Log($"Test1 [{round}]: cost {sw.ElapsedMilliseconds}ms, n={n}");
        return n;
    }
}