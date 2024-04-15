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
    [SerializeField]
    protected float[] m_updateIntervalForScheduleTypes = new float[(int)JobScheduleTypes.NUM_TYPES];

    protected float[] m_timeUntilScheduleTypeUpdate = new float[(int)JobScheduleTypes.NUM_TYPES];

    private struct UpdateTypesLists
    {
        public UpdateTypesLists(int empty = 0)
        {
            updateList = null;
            lateUpdateList = null;
            fixedUpdateList = null;
        }
        public List<GfcJobChild> updateList;
        public List<GfcJobChild> lateUpdateList;
        public List<GfcJobChild> fixedUpdateList;
    }

    private static JobParent Instance = null;
    private Dictionary<Type, List<GfcJobChild>[]> m_jobChildren = null;
    private List<Type> m_inheritedTypes = null;

    private NativeList<JobHandle> m_jobHandles;


    protected void Awake()
    {
        Array.Resize<float>(ref m_updateIntervalForScheduleTypes, (int)JobScheduleTypes.NUM_TYPES);

        if (Instance != this) Destroy(Instance);
        Instance = this;

        int countChildren = 0;

        var inheritedTypes = Assembly.GetAssembly(typeof(GfcJobChild)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(GfcJobChild)));

        foreach (Type type in inheritedTypes)
            countChildren++;

        m_jobChildren = new(countChildren);
        m_inheritedTypes = new(countChildren);
        m_jobHandles = new(128, Allocator.Persistent);
    }

    public static bool CanSchedule(JobScheduleTypes type)
    {
        return Instance && 0 >= Instance.m_timeUntilScheduleTypeUpdate[(int)type];
    }

    public static float GetScheduleInterval(JobScheduleTypes type)
    {
        return System.MathF.Max(Time.deltaTime, Instance.m_updateIntervalForScheduleTypes[(int)type]);
    }

    void FixedUpdate()
    {
        int countTypes = (int)JobScheduleTypes.NUM_TYPES;
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < countTypes; ++i)
        {
            if (m_timeUntilScheduleTypeUpdate[i] <= 0)
                m_timeUntilScheduleTypeUpdate[i] = m_updateIntervalForScheduleTypes[i];

            m_timeUntilScheduleTypeUpdate[i] -= deltaTime;
        }

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

    protected void OnDestroy()
    {
        if (m_jobHandles.IsCreated)
            m_jobHandles.Dispose();

        for (int i = 0; i < (int)UpdateTypes.TYPES_COUNT; ++i)
        {
            foreach (Type type in m_inheritedTypes)
            {
                List<GfcJobChild> list = m_jobChildren[type][i];
                if (null != list)
                {
                    int count = list.Count;

                    for (int j = 0; j < count; ++j)
                    {
                        list[j].SetJobParentIndex(-1, (UpdateTypes)i);
                    }
                }
            }
        }

        Instance = null;
    }


    public static void AddChild(GfcJobChild child, Type type, UpdateTypes updateType, bool checkExistence = false)
    {
        if (!Instance.m_jobChildren.ContainsKey(type))
        {
            Instance.m_jobChildren.Add(type, new List<GfcJobChild>[3]);
            Instance.m_inheritedTypes.Add(type);
        }

        int updateTypeIndex = (int)updateType;
        List<GfcJobChild>[] lists = Instance.m_jobChildren[type];

        if (null == lists[updateTypeIndex])
            lists[updateTypeIndex] = new(4);

        List<GfcJobChild> list = lists[updateTypeIndex];

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

    public static void RemoveChild(GfcJobChild child, Type type, UpdateTypes updateType)
    {
        List<GfcJobChild>[] lists;
        int indexToRemove = child.GetJobParentIndex(updateType);
        if (indexToRemove >= 0 && Instance.m_jobChildren.TryGetValue(type, out lists))
        {
            List<GfcJobChild> list = lists[(int)updateType];
            int lastIndex = list.Count - 1;

            list[indexToRemove] = list[lastIndex];
            list[indexToRemove].SetJobParentIndex(indexToRemove, updateType);
            list.RemoveAt(lastIndex);
            child.SetJobParentIndex(-1, updateType);
        }
    }

    // Update is called once per frame

    protected void RunJobs(UpdateTypes updateType)
    {
        float deltaTime = Time.deltaTime;
        foreach (Type type in m_inheritedTypes)
        {
            List<GfcJobChild> list = m_jobChildren[type][(int)updateType];
            if (null != list)
            {
                int i, count = list.Count;

                for (i = 0; i < count; ++i)
                    list[i].OnOperationStart(deltaTime, updateType);

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

                for (i = 0; i < count; ++i)
                    list[i].OnOperationFinished(deltaTime, updateType);
            }
        }
    }

    public static List<GfcJobChild> GetJobChildren(Type type, UpdateTypes updateTypes)
    {
        return Instance.m_jobChildren[type][(int)updateTypes];
    }

    public static bool HasInstance() { return null != Instance; }
}


public enum UpdateTypes
{
    UPDATE,
    FIXED_UPDATE,
    LATE_UPDATE,
    TYPES_COUNT
}




