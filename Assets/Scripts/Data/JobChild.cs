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
    public struct JobParentIndexes
    {
        public JobParentIndexes(int updateIndex = -1, int lateUpdateIndex = -1, int fixedUpdateIndex = -1)
        {
            this.updateIndex = updateIndex;
            this.lateUpdateIndex = updateIndex;
            this.fixedUpdateIndex = fixedUpdateIndex;
        }

        public int updateIndex;
        public int lateUpdateIndex;
        public int fixedUpdateIndex;
    }

    [System.Serializable]
    public struct JobParentSubscriberData
    {
        public bool update;
        public bool lateUpdate;
        public bool fixedUpdate;
    }

    [SerializeField]
    private JobParentSubscriberData m_jobSubscriberType = default;
    private JobParentIndexes m_jobParentIndexes = new(-1, -1, -1);

    public int GetJobParentIndex(UpdateTypes updateType)
    {
        switch (updateType)
        {
            case (UpdateTypes.LATE_UPDATE):
                return m_jobParentIndexes.lateUpdateIndex;

            case (UpdateTypes.FIXED_UPDATE):
                return m_jobParentIndexes.fixedUpdateIndex;

            case (UpdateTypes.UPDATE):
                return m_jobParentIndexes.updateIndex;
        }

        return -1;
    }

    public void SetJobParentIndex(int index, UpdateTypes updateType)
    {
        switch (updateType)
        {
            case (UpdateTypes.LATE_UPDATE):
                m_jobParentIndexes.lateUpdateIndex = index;
                break;

            case (UpdateTypes.FIXED_UPDATE):
                m_jobParentIndexes.fixedUpdateIndex = index;
                break;

            case (UpdateTypes.UPDATE):
                m_jobParentIndexes.updateIndex = index;
                break;
        }
    }

    protected void InitJobChild()
    {
        Type type = GetType();
        if (m_jobSubscriberType.fixedUpdate)
            JobParent.AddInstance(this, type, UpdateTypes.FIXED_UPDATE);

        if (m_jobSubscriberType.update)
            JobParent.AddInstance(this, type, UpdateTypes.UPDATE);

        if (m_jobSubscriberType.lateUpdate)
            JobParent.AddInstance(this, type, UpdateTypes.LATE_UPDATE);
    }

    protected void DeinitJobChild()
    {
        Type type = GetType();
        if (m_jobSubscriberType.fixedUpdate)
            JobParent.RemoveInstance(m_jobParentIndexes.fixedUpdateIndex, type, UpdateTypes.FIXED_UPDATE);

        if (m_jobSubscriberType.update)
            JobParent.RemoveInstance(m_jobParentIndexes.updateIndex, type, UpdateTypes.UPDATE);

        if (m_jobSubscriberType.lateUpdate)
            JobParent.RemoveInstance(m_jobParentIndexes.lateUpdateIndex, type, UpdateTypes.LATE_UPDATE);
    }

    public abstract bool ScheduleJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512);

    public abstract void OnJobFinished(float deltaTime, UpdateTypes updateType);
}
