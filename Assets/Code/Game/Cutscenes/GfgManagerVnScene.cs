using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using System.Reflection;

public class GfgManagerVnScene : MonoBehaviour
{
    private static GfgManagerVnScene Instance;

    [SerializeField] private GfgVnSceneHandlerInstance[] m_handlers;

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);
    }

    public static GfgVnSceneHandler GetVnSceneHandler(GfgVnSceneHandlerType aType) { return Instance.m_handlers[(int)aType].Handler; }

    public static CoroutineHandle StartScene(Type aSceneType, GfgVnSceneHandlerType aType) { return GetVnSceneHandler(aType).StartScene(aSceneType); }

    public static CoroutineHandle StartScene<T>(GfgVnSceneHandlerType aType) where T : GfgVnScene { return StartScene(typeof(T), aType); }
}

public enum GfgVnSceneHandlerType
{
    DIALOGUE,
    MESSAGES,
    COUNT
}

[System.Serializable]
public struct GfgVnSceneHandlerInstance
{
    public GfgVnSceneHandlerType Type;
    public GfgVnSceneHandler Handler;
}
