using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfcMutableSingletonBase : MonoBehaviour
{
    protected static Dictionary<Type, GfcMutableSingletonBase> Instances = new(4);

    public static T GetInstance<T>() where T : GfcMutableSingletonBase
    {
        GfcMutableSingletonBase value = null;
        Instances?.TryGetValue(typeof(T), out value);
        return value as T;
    }
}
public abstract class GfcMutableSingleton<SingletonType, EnumType> : GfcMutableSingletonBase where SingletonType : Component where EnumType : unmanaged, Enum
{
    [Serializable]
    protected struct SingletonInstance
    {
        public SingletonType Component;
        public EnumType Type;
    }

    //used when we want to se an instance type but the singleton isn't available yet
    private int m_queuedInstanceTypeIndex = -1;

    private EnumType m_activeInstanceType;

    [SerializeField] protected SingletonInstance[] m_singletonInstances;

    [SerializeField] protected bool m_singletonIsOptional;

    protected void Awake()
    {
        Instances[GetType()] = this;

        if (m_singletonInstances.Length == 0 || m_singletonInstances[0].Component == null)
            m_queuedInstanceTypeIndex = 0;

        for (int i = 0; i < m_singletonInstances.Length; ++i)
        {
            Debug.Assert(i == m_singletonInstances[i].Type.Index(), "Please put the elements in order of their singleton type.");
            if (m_singletonInstances[i].Component) m_singletonInstances[i].Component.gameObject.SetActive(i == 0);
        }

        Array.Resize(ref m_singletonInstances, Enum.GetValues(typeof(EnumType)).Length);

        for (int i = 0; i < m_singletonInstances.Length; ++i)
            m_singletonInstances[i].Type = i.IndexToEnum<EnumType>();
    }

    void OnDestroy()
    {
        Instances[GetType()] = null;
    }

    public virtual void SetSingleton(EnumType anInstanceType, bool anQueueIfNull = false)
    {
        int instanceIndex = anInstanceType.Index();
        m_queuedInstanceTypeIndex = -1;

        if (instanceIndex != m_activeInstanceType.Index())
        {
            if (m_singletonInstances[instanceIndex].Component)
            {
                //null coalescence doesn't work properly with gameobjects
                var currentSingletonComponent = m_singletonInstances[m_activeInstanceType.Index()].Component;
                if (currentSingletonComponent)
                    Debug.Log("Disabling object: " + currentSingletonComponent.name);
                else
                    Debug.Log("Object for type " + m_activeInstanceType + " is null");

                if (currentSingletonComponent) currentSingletonComponent.gameObject.SetActive(false);

                m_singletonInstances[instanceIndex].Component.gameObject.SetActive(true);
                m_activeInstanceType = instanceIndex.IndexToEnum<EnumType>();
            }
            else if (instanceIndex != 0)
            {
                if (anQueueIfNull)
                {
                    m_queuedInstanceTypeIndex = instanceIndex;
                    Debug.Log("Queueing object for type " + anInstanceType);
                }
                else if (!m_singletonIsOptional)
                    Debug.LogError("The " + typeof(SingletonType) + " singleton for instance " + instanceIndex.IndexToEnum<EnumType>() + " is null.");
            }
        }
    }

    public virtual void UnregisterSingleton(SingletonType aSingletonInstance, EnumType anInstanceType)
    {
        int instanceIndex = anInstanceType.Index();
        Component currentSingleton = m_singletonInstances[instanceIndex].Component;

        if (instanceIndex == 0)
            Debug.LogError("Cannot unregister the default " + typeof(SingletonType) + " singleton.");

        if (currentSingleton != null && currentSingleton != aSingletonInstance)
            Debug.LogError("The " + typeof(SingletonType) + " singleton type " + instanceIndex.IndexToEnum<EnumType>() + " is assigned to a different object " + currentSingleton.name + ", cannot unregister singleton type.");

        if (currentSingleton == aSingletonInstance && instanceIndex != 0)
        {
            var data = m_singletonInstances[instanceIndex];
            data.Component = null;
            m_singletonInstances[instanceIndex] = data;

            ValidateSingleton();
        }
    }

    public virtual void RegisterSingleton(SingletonType aSingletonInstance, EnumType anInstanceType, bool aSetActive = false)
    {
        int instanceIndex = anInstanceType.Index();
        Component currentCamera = m_singletonInstances[instanceIndex].Component;

        Debug.Assert(aSingletonInstance);

        if (currentCamera != null && currentCamera != aSingletonInstance)
            Debug.LogError("The " + typeof(SingletonType) + " singleton type " + instanceIndex.IndexToEnum<EnumType>() + " was already assigned to object " + currentCamera.name);

        var data = m_singletonInstances[instanceIndex];
        data.Component = aSingletonInstance;
        m_singletonInstances[instanceIndex] = data;

        if (aSetActive || m_queuedInstanceTypeIndex == instanceIndex)
            SetSingleton(instanceIndex.IndexToEnum<EnumType>());
        else if (instanceIndex != m_activeInstanceType.Index())
            aSingletonInstance.gameObject.SetActive(false);
    }

    public virtual void RegisterSingleton(SingletonType aSingletonInstance, EnumType[] someInstanceTypes)
    {
        foreach (EnumType type in someInstanceTypes)
        {
            RegisterSingleton(aSingletonInstance, type);
        }
    }

    public virtual void UnregisterSingleton(SingletonType aSingletonInstance, EnumType[] someInstanceTypes)
    {
        foreach (EnumType type in someInstanceTypes)
        {
            UnregisterSingleton(aSingletonInstance, type);
        }
    }

    public EnumType GetActiveInstanceType() { return m_activeInstanceType; }

    public SingletonType GetSingleton() { return m_singletonInstances[m_activeInstanceType.Index()].Component; }

    public SingletonType GetSingleton(int anInstanceIndex) { return m_singletonInstances[anInstanceIndex].Component; }

    public SingletonType GetSingleton(EnumType anInstanceType) { return GetSingleton(anInstanceType.Index()); }

    public void ValidateSingleton()
    {
        if (GetSingleton() == null)
        {
            int firstIndex = 0;
            SetSingleton(firstIndex.IndexToEnum<EnumType>());
        }
    }
}

public abstract class GfcMutableSingletonRegister<T, SingletonType, EnumType> : MonoBehaviour where T : GfcMutableSingleton<SingletonType, EnumType> where SingletonType : Component where EnumType : unmanaged, Enum
{
    [SerializeField] EnumType m_singletonType;

    [SerializeField] bool m_setAsMainOnEnable = false;

    protected void RegisterSingleton()
    {
        var instance = GfcMutableSingletonBase.GetInstance<T>();
        instance.RegisterSingleton(GetComponent<SingletonType>(), m_singletonType, m_setAsMainOnEnable);
    }

    protected void Start() { RegisterSingleton(); }

    protected void OnDestroy()
    {
        var instance = GfcMutableSingletonBase.GetInstance<T>();
        if (instance) instance.UnregisterSingleton(GetComponent<SingletonType>(), m_singletonType);
    }
}

public abstract class GfcMutableSingletonRegisterMulti<T, SingletonType, EnumType> : MonoBehaviour where T : GfcMutableSingleton<SingletonType, EnumType> where SingletonType : Component where EnumType : unmanaged, Enum
{
    [SerializeField] EnumType[] m_singletonTypes;

    protected void RegisterCamera()
    {
        var instance = GfcMutableSingletonBase.GetInstance<T>();
        instance.RegisterSingleton(GetComponent<SingletonType>(), m_singletonTypes);
    }

    protected void Start() { RegisterCamera(); }

    protected void OnDestroy()
    {
        var instance = GfcMutableSingletonBase.GetInstance<T>();
        if (instance) instance.UnregisterSingleton(GetComponent<SingletonType>(), m_singletonTypes);
    }
}