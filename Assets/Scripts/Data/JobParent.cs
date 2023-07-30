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


public class JobParent : MonoBehaviour
{
    private struct UpdateTypesLists
    {
        public UpdateTypesLists(int empty = 0)
        {
            updateList = null;
            lateUpdateList = null;
            fixedUpdateList = null;
        }
        public List<JobChild> updateList;
        public List<JobChild> lateUpdateList;
        public List<JobChild> fixedUpdateList;
    }

    private static JobParent Instance = null;
    private Dictionary<Type, List<JobChild>[]> m_inheritedMembers = null;
    private List<Type> m_inheritedTypes = null;

    private NativeList<JobHandle> m_jobHandles;

    public static void AddChild(JobChild child, Type type, UpdateTypes updateType, bool checkExistence = false)
    {
        if (!Instance.m_inheritedMembers.ContainsKey(type))
        {
            Instance.m_inheritedMembers.Add(type, new List<JobChild>[3]);
            Instance.m_inheritedTypes.Add(type);
        }

        int updateTypeIndex = (int)updateType;
        List<JobChild>[] lists = Instance.m_inheritedMembers[type];

        if (null == lists[updateTypeIndex])
            lists[updateTypeIndex] = new(4);

        List<JobChild> list = lists[updateTypeIndex];

        if (child.GetJobParentIndex(updateType) < 0)
        {
            bool addToList = true;
            if (checkExistence)
                addToList = !(list.Contains(child));

            if (addToList)
            {
                list.Add(child);
                child.SetJobParentIndex(list.Count - 1, updateType);
            }
        }
    }

    public static void RemoveChild(JobChild child, Type type, UpdateTypes updateType)
    {
        List<JobChild>[] lists;
        int indexToRemove = child.GetJobParentIndex(updateType);
        if (indexToRemove >= 0 && Instance.m_inheritedMembers.TryGetValue(type, out lists))
        {
            List<JobChild> list = lists[(int)updateType];
            int lastIndex = list.Count - 1;

            list[indexToRemove] = list[lastIndex];
            list[indexToRemove].SetJobParentIndex(indexToRemove, updateType);
            list.RemoveAt(lastIndex);
            child.SetJobParentIndex(-1, updateType);
        }
    }

    void Awake()
    {
        if (Instance != null) Destroy(Instance);
        Instance = this;
        int countChildren = 0;

        var inheritedTypes = Assembly.GetAssembly(typeof(JobChild)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(JobChild)));

        foreach (Type type in inheritedTypes)
            countChildren++;

        m_inheritedMembers = new(countChildren);
        m_inheritedTypes = new(countChildren);
        m_jobHandles = new(128, Allocator.Persistent);
        InternalStart();
    }

    protected virtual void InternalStart() { }

    // Update is called once per frame

    protected void RunJobs(UpdateTypes updateType)
    {
        float deltaTime = Time.deltaTime;
        foreach (Type type in m_inheritedTypes)
        {
            List<JobChild> list = m_inheritedMembers[type][(int)updateType];
            if (null != list)
            {
                int i, count = list.Count;
                JobHandle handle;
                m_jobHandles.Clear();
                int jobCount = 0;

                for (i = 0; i < count; ++i)
                {
                    if (list[i].GetJob(out handle, deltaTime, updateType))
                    {
                        m_jobHandles.Add(handle);
                        jobCount++;
                    }
                }

                if (jobCount > 0)
                    JobHandle.CompleteAll(m_jobHandles);

                for (i = 0; i < count; ++i)
                    list[i].OnJobFinished(deltaTime, updateType);
            }
        }
    }
    void FixedUpdate()
    {
        RunJobs(UpdateTypes.FIXED_UPDATE);
    }

    void Update()
    {
        RunJobs(UpdateTypes.UPDATE);
    }

    void LateUpdate()
    {
        RunJobs(UpdateTypes.LATE_UPDATE);
    }

    private void OnDestroy()
    {
        if (m_jobHandles.IsCreated)
            m_jobHandles.Dispose();
    }
}


public enum UpdateTypes
{
    UPDATE,
    FIXED_UPDATE,
    LATE_UPDATE
}




