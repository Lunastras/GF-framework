using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class CornBenchmarkTests : MonoBehaviour
{
    public float testNum = 51;

    public int numIterations = 5000;

    private float SqrtInternal(float aNum, float aLeft, float aRight)
    {
        float mid;
        int it = 0;
        while (System.MathF.Abs(aLeft - aRight) > 0.00001f)
        {
            it++;

            mid = 0.5f * (aLeft + aRight);

            if (mid * mid > aNum)
                aRight = mid;
            else
                aLeft = mid;
        }

        //UnityEngine.Debug.Log("The count is: " + it);
        return aLeft;
    }

    public float Sqrt(float aNum) { return SqrtInternal(aNum, 0, aNum); }

    // Update is called once per frame
    void Update()
    {
        UnityEngine.Debug.Log("TESTS BEGIN");


        Stopwatch stopwatch = new();

        stopwatch.Start();

        for (int i = 0; i < numIterations; ++i) System.MathF.Sqrt(testNum);
        UnityEngine.Debug.Log("Elapsed time 1: " + stopwatch.ElapsedTicks + " res : " + System.MathF.Sqrt(testNum));

        stopwatch.Stop();

        stopwatch.Reset();

        stopwatch.Start();

        for (int i = 0; i < numIterations; ++i) Sqrt(testNum);
        UnityEngine.Debug.Log("Elapsed time 2: " + stopwatch.ElapsedTicks + " res : " + Sqrt(testNum));

        stopwatch.Stop();
    }
}
