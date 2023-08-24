using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Unity.Mathematics;

public class AngleDegTestSpeed : MonoBehaviour
{
    Stopwatch m_watch;
    // Start is called before the first frame update
    void Start()
    {
        m_watch = new();
    }

    public int NumLoops = 10000;

    // Update is called once per frame
    void Update()
    {
        UnityEngine.Debug.Log("Starting tests!");

        double miliseconds;
        float angle = 0;
        Vector3 upVec = Vector3.up;
        Vector3 rightVec = Vector3.right;
        Vector3 aux;

        float3 up3 = new float3(0, 1, 0);
        aux = Vector3.zero;
        float3 aux3 = new(0, 0, 0);

        m_watch.Reset();
        m_watch.Start();

        for (int i = 0; i < NumLoops; ++i)
        {
            aux3 += up3;
        }

        m_watch.Stop();
        miliseconds = m_watch.Elapsed.TotalMilliseconds;

        UnityEngine.Debug.Log(miliseconds + " miliseconds have passed for test 1");

        m_watch.Reset();
        m_watch.Start();

        for (int i = 0; i < NumLoops; ++i)
        {
            aux += upVec;
        }

        m_watch.Stop();
        miliseconds = m_watch.Elapsed.TotalMilliseconds;

        UnityEngine.Debug.Log(miliseconds + " miliseconds have passed for test 2");

        aux = Vector3.zero;

        m_watch.Reset();
        m_watch.Start();

        for (int i = 0; i < NumLoops; ++i)
        {
            GfTools.Add3(ref aux, upVec);
        }

        m_watch.Stop();
        miliseconds = m_watch.Elapsed.TotalMilliseconds;

        UnityEngine.Debug.Log(miliseconds + " miliseconds have passed for test 3");
    }
}
