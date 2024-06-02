using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GfcInput : MonoBehaviour
{
    private static GfcInput Instance = null;

    [SerializeField] private GfcInputDefine[] m_inputDefines = new GfcInputDefine[(int)GfcInputType.COUNT];

    [SerializeField] private bool m_printErrors = true;

    public const float AXIS_DEAD_ZONE = 0.001f;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        if (m_printErrors)
        {
            for (int i = 0; i < m_inputDefines.Length; ++i)
                if (i != (int)m_inputDefines[i].Type)
                    Debug.LogError("The type " + m_inputDefines[i].Type + " is at index " + i + " it should be at index " + (int)m_inputDefines[i].Type);

            if ((int)GfcInputType.COUNT != m_inputDefines.Length)
                Debug.LogError("The input defines array is of size " + m_inputDefines.Length + ", but it should be of size " + (int)GfcInputType.COUNT);
        }
    }

    public static string GetAxisString(GfcInputType aInputType) { return Instance.m_inputDefines[(int)aInputType].AxisString; }
    public static float GetAxis(GfcInputType aInputType) { return aInputType == GfcInputType.NONE ? 0 : Input.GetAxis(GetAxisString(aInputType)); }
    public static float GetAxisRaw(GfcInputType aInputType) { return aInputType == GfcInputType.NONE ? 0 : Input.GetAxisRaw(GetAxisString(aInputType)); }
    public static bool GetAxisInput(GfcInputType aInputType, float aError = AXIS_DEAD_ZONE) { return GetAxisRaw(aInputType).Abs() > aError; }
}

//used for generic inputs that should be compatible with a keyboard and a controller
public enum GfcInputType
{
    NONE,
    PAUSE,
    SELECT,
    SUBMIT,
    BACK,
    ZOOM,

    MOVEMENT_X,
    MOVEMENT_Y,
    CAMERA_X,
    CAMERA_Y,
    GYRO_X,
    GYRO_Y,
    SCROLL,

    DIR_DOWN,
    DIR_RIGHT,
    DIR_LEFT,
    DIR_UP,

    UI_DOWN,
    UI_RIGHT,
    UI_LEFT,
    UI_UP,

    UI_OPTION1,
    UI_OPTION2,
    UI_OPTION3,
    UI_OPTION4,

    GAME_OPTION1,
    GAME_OPTION2,
    GAME_OPTION3,
    GAME_OPTION4,

    SHOULDER_RIGHT,
    SHOULDER_LEFT,

    JUMP,
    CROUCH,
    RUN,

    FIRE1,
    FIRE2,

    ACTION1,
    ACTION2,
    ACTION3,
    ACTION4,
    COUNT
}

[System.Serializable]
public struct GfcInputDefine
{
    public GfcInputType Type;
    public string AxisString;
}

[System.Serializable]
public struct GfcInputTracker
{
    [SerializeField] private GfcInputType m_inputType;
    [SerializeField] private float m_axisError;

    private int m_lastFrameOfUpdate;

    private bool m_currentPressedState;

    private bool m_previousPressedState;

    private bool m_stateCheckedThisFrame;

    public GfcInputType InputType { get { return m_inputType; } set { m_inputType = value; UpdateState(); } }
    public float AxisError { get { return m_axisError; } set { m_axisError = value; UpdateState(); } }

    public GfcInputTracker(GfcInputType aType, float aError = GfcInput.AXIS_DEAD_ZONE)
    {
        m_inputType = aType;
        m_axisError = aError;
        m_stateCheckedThisFrame = false;
        m_lastFrameOfUpdate = Time.frameCount;
        m_previousPressedState = m_currentPressedState = GfcInput.GetAxisInput(m_inputType, m_axisError);
    }

    private void UpdateState()
    {
        int currentFrame = Time.frameCount;
        if (currentFrame != m_lastFrameOfUpdate)
        {
            m_previousPressedState = m_currentPressedState;
            m_currentPressedState = GfcInput.GetAxisInput(m_inputType, m_axisError);
            m_lastFrameOfUpdate = currentFrame;
            m_stateCheckedThisFrame = false;
        }
    }

    public bool Pressed()
    {
        UpdateState();
        return m_currentPressedState;
    }

    public bool PressedSinceLastCheck(bool aUniqueThisFrame = true)
    {
        UpdateState();
        bool pressed = (!m_stateCheckedThisFrame || !aUniqueThisFrame) && !m_previousPressedState && m_currentPressedState;
        m_stateCheckedThisFrame |= aUniqueThisFrame;
        return pressed;
    }

    public bool ReleasedSinceLastCheck(bool aUniqueThisFrame = true)
    {
        UpdateState();
        bool released = (!m_stateCheckedThisFrame || !aUniqueThisFrame) && m_previousPressedState && !m_currentPressedState;
        m_stateCheckedThisFrame |= aUniqueThisFrame;
        return released;
    }
}

//this is used mainly for coroutines where we need to share an input becaue async functions cannot have ref structs as arguments
[System.Serializable]
public class GfcInputTrackerShared
{
    [SerializeField] private GfcInputTracker m_inputTracker = new GfcInputTracker(GfcInputType.SUBMIT);

    public GfcInputTrackerShared(GfcInputType aType, float aError = GfcInput.AXIS_DEAD_ZONE) { m_inputTracker = new(aType, aError); }

    public GfcInputTrackerShared(GfcInputTracker aTracker) { m_inputTracker = aTracker; }

    public bool Pressed() { return m_inputTracker.Pressed(); }

    public bool PressedSinceLastCheck(bool aUniqueThisFrame = true) { return m_inputTracker.PressedSinceLastCheck(aUniqueThisFrame); }

    public bool ReleasedSinceLastCheck(bool aUniqueThisFrame = true) { return m_inputTracker.ReleasedSinceLastCheck(aUniqueThisFrame); }
}

public static class GfcInputTrackerStatic
{
    public static bool PressedFallback(this GfcInputTrackerShared aTrackerInstance, ref GfcInputTracker aTrackerFallback)
    {
        return aTrackerInstance != null ? aTrackerInstance.Pressed() : aTrackerFallback.Pressed();
    }

    public static bool PressedSinceLastCheckFallback(this GfcInputTrackerShared aTrackerInstance, ref GfcInputTracker aTrackerFallback, bool aUniqueThisFrame = true)
    {
        return aTrackerInstance != null ? aTrackerInstance.PressedSinceLastCheck(aUniqueThisFrame) : aTrackerFallback.PressedSinceLastCheck(aUniqueThisFrame);
    }

    public static bool ReleasedSinceLastCheckFallback(this GfcInputTrackerShared aTrackerInstance, ref GfcInputTracker aTrackerFallback, bool aUniqueThisFrame = true)
    {
        return aTrackerInstance != null ? aTrackerInstance.ReleasedSinceLastCheck(aUniqueThisFrame) : aTrackerFallback.ReleasedSinceLastCheck(aUniqueThisFrame);
    }
}