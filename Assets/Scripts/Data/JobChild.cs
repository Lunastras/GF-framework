using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using Unity.Collections;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using Unity.Mathematics;

public abstract class JobChild : MonoBehaviour
{
    private int m_jobParentIndex = -1;

    public int GetJobParentIndex()
    {
        return m_jobParentIndex;
    }

    public void SetJobParentIndex(int index)
    {
        m_jobParentIndex = index;
    }

    protected void Init()
    {
        JobParent.AddInstance(this, GetType());
    }

    protected void Deinit()
    {
        JobParent.RemoveInstance(m_jobParentIndex, GetType());
    }

    public abstract bool ScheduleJob(out JobHandle handle, float deltaTime, int batchSize = 512);

    public abstract void OnJobFinished();
}
