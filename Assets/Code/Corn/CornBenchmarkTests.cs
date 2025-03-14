using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class CornBenchmarkTests : MonoBehaviour
{
    public float testNum = 51;

    public int numIterations = 50000;

    public unsafe int FastInt(bool aBool) { return *(int*)&aBool; }
    [SerializeField] ulong cnt = 0;

    // Update is called once per frame
    void Update()
    {
        UnityEngine.Debug.Log("TESTS BEGIN");
        Stopwatch stopwatch = new();
        stopwatch.Start();

        int val = 0;

        for (int i = 0; i < numIterations; ++i)
        {
            cnt += (ulong)FastInt(i % 2 == 0);
        }

        UnityEngine.Debug.Log("Elapsed time 1: " + stopwatch.ElapsedTicks + " res : " + val);

        stopwatch.Stop();
        stopwatch.Reset();
        stopwatch.Start();

        for (int i = 0; i < numIterations; ++i)
        {
            cnt += (ulong)(i % 2 == 0 ? 1 : 0);
        }

        UnityEngine.Debug.Log("Elapsed time 2: " + stopwatch.ElapsedTicks + " res : " + val);

        stopwatch.Stop();
    }
}
