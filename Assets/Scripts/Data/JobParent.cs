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
    private static JobParent Instance;
    private Dictionary<Type, List<JobChild>> m_inheritedMembers;
    private IEnumerable<Type> m_inheritedTypes;

    private NativeList<JobHandle> m_jobHandles;

    public static void AddInstance(JobChild child, Type type, bool checkExistence = false)
    {
        List<JobChild> list;
        if (child.GetJobParentIndex() < 0 && Instance.m_inheritedMembers.TryGetValue(type, out list))
        {
            bool addToList = true;
            if (checkExistence) addToList = !(list.Contains(child));

            if (addToList)
            {
                list.Add(child);
                child.SetJobParentIndex(list.Count - 1);
            }
        }
    }

    public static void RemoveInstance(int index, Type type)
    {
        List<JobChild> list;
        if (index >= 0 && Instance.m_inheritedMembers.TryGetValue(type, out list) && index < list.Count)
        {
            list[index].SetJobParentIndex(-1);
            int count = list.Count - 1;
            list[index] = list[count];
            list[index].SetJobParentIndex(index);
            list.RemoveAt(count);
        }
    }

    public void RemoveInstance(JobChild child, Type type)
    {
        RemoveInstance(child.GetJobParentIndex(), type);
    }

    void Awake()
    {
        if (Instance != null) Destroy(Instance);
        Instance = this;
        int countChildren = 0;

        m_inheritedTypes = Assembly.GetAssembly(typeof(JobChild)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(JobChild)));

        foreach (Type type in m_inheritedTypes)
            countChildren++;

        m_inheritedMembers = new(countChildren);

        foreach (Type type in m_inheritedTypes)
        {
            countChildren++;
            m_inheritedMembers.Add(type, new(2));
        }

        m_jobHandles = new(256, Allocator.Persistent);
        InternalStart();
    }

    protected virtual void InternalStart() { }

    // Update is called once per frame
    void FixedUpdate()
    {
        float deltaTime = Time.deltaTime;
        foreach (Type type in m_inheritedTypes)
        {
            List<JobChild> list = m_inheritedMembers[type];
            int i, count = list.Count;
            JobHandle handle;
            m_jobHandles.Clear();
            int jobCount = 0;

            for (i = 0; i < count; ++i)
            {
                if (list[i].ScheduleJob(out handle, deltaTime))
                {
                    m_jobHandles.Add(handle);
                    jobCount++;
                }
            }

            if (jobCount > 0)
                JobHandle.CompleteAll(m_jobHandles);

            for (i = 0; i < count; ++i)
                list[i].OnJobFinished();
        }

    }

    private void OnDestroy()
    {
        m_jobHandles.Dispose();
    }
}





