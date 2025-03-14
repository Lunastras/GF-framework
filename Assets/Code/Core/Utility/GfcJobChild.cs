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

public abstract class GfcJobChild : MonoBehaviour
{
    public Action OnJobSchedule = null;

    public class JobParentIndexes
    {
        public JobParentIndexes(int updateIndex = -1, int lateUpdateIndex = -1, int fixedUpdateIndex = -1)
        {
            this.UpdateIndex = updateIndex;
            this.LateUpdateIndex = updateIndex;
            this.FixedUpdateIndex = fixedUpdateIndex;
        }

        public int UpdateIndex = -1;
        public int LateUpdateIndex = -1;
        public int FixedUpdateIndex = -1;
    }

    [System.Serializable]
    public class JobParentSubscriberData
    {
        public bool Update;
        public bool LateUpdate;
        public bool FixedUpdate = true;
    }

    [SerializeField]
    private JobParentSubscriberData m_jobSubscriberType = default;
    private JobParentIndexes m_jobParentIndexes = new(-1, -1, -1);

    public int GetJobParentIndex(UpdateTypes updateType)
    {
        switch (updateType)
        {
            case (UpdateTypes.LATE_UPDATE):
                return m_jobParentIndexes.LateUpdateIndex;

            case (UpdateTypes.FIXED_UPDATE):
                return m_jobParentIndexes.FixedUpdateIndex;

            case (UpdateTypes.UPDATE):
                return m_jobParentIndexes.UpdateIndex;
        }

        return -1;
    }

    public void SetJobParentIndex(int index, UpdateTypes updateType)
    {
        switch (updateType)
        {
            case (UpdateTypes.LATE_UPDATE):
                m_jobParentIndexes.LateUpdateIndex = index;

                break;

            case (UpdateTypes.FIXED_UPDATE):
                m_jobParentIndexes.FixedUpdateIndex = index;
                break;

            case (UpdateTypes.UPDATE):
                m_jobParentIndexes.UpdateIndex = index;
                break;
        }
    }

    protected void InitJobChild()
    {
        Type type = GetType();
        if (m_jobSubscriberType.FixedUpdate)
            JobParent.AddChild(this, type, UpdateTypes.FIXED_UPDATE);

        if (m_jobSubscriberType.Update)
            JobParent.AddChild(this, type, UpdateTypes.UPDATE);

        if (m_jobSubscriberType.LateUpdate)
            JobParent.AddChild(this, type, UpdateTypes.LATE_UPDATE);
    }

    protected void DeinitJobChild()
    {
        Type type = GetType();
        JobParent.RemoveChild(this, type, UpdateTypes.FIXED_UPDATE);
        JobParent.RemoveChild(this, type, UpdateTypes.UPDATE);
        JobParent.RemoveChild(this, type, UpdateTypes.LATE_UPDATE);
    }

    public virtual void OnOperationStart(float deltaTime, UpdateTypes updateType) { }

    public abstract bool GetJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512);

    public abstract void OnJobFinished(float deltaTime, UpdateTypes updateType);

    public virtual void OnOperationFinished(float deltaTime, UpdateTypes updateType) { }
}
