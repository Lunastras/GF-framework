using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

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

        m_watch.Reset();
        m_watch.Start();

        for (int i = 0; i < NumLoops; ++i)
        {
            //angle = GfTools.AngleDeg(upVec, rightVec);
        }

        m_watch.Stop();
        miliseconds = m_watch.Elapsed.TotalMilliseconds;

        //  UnityEngine.Debug.Log(miliseconds + " miliseconds have passed for test 1, angle is: " + angle);

        m_watch.Reset();
        m_watch.Start();

        for (int i = 0; i < NumLoops; ++i)
        {
            angle = GfTools.AngleDegNorm(upVec, rightVec);
        }

        m_watch.Stop();
        miliseconds = m_watch.Elapsed.TotalMilliseconds;

        UnityEngine.Debug.Log(miliseconds + " miliseconds have passed for test 1, angle is: " + angle);

        m_watch.Reset();
        m_watch.Start();

        for (int i = 0; i < NumLoops; ++i)
        {
            angle = Vector3.Angle(upVec, rightVec);
        }

        m_watch.Stop();
        miliseconds = m_watch.Elapsed.TotalMilliseconds;

        UnityEngine.Debug.Log(miliseconds + " miliseconds have passed for test 2, angle is: " + angle);
    }
}
