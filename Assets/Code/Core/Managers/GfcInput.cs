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
    public static float GetAxis(GfcInputType aInputType) { return Input.GetAxis(GetAxisString(aInputType)); }
    public static float GetAxisRaw(GfcInputType aInputType) { return Input.GetAxisRaw(GetAxisString(aInputType)); }
    public static bool GetAxisInput(GfcInputType aInputType, float aError = AXIS_DEAD_ZONE) { return GetAxisRaw(aInputType).Abs() > aError; }
}

//used for generic inputs that should be compatible with a keyboard and a controller
public enum GfcInputType
{
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

public struct GfcInputTracker
{
    public float AxisError;

    public GfcInputType InputType { get; private set; }

    public int m_lastFrameOfUpdate;

    private bool m_currentPressedState;

    private bool m_previousPressedState;


    public GfcInputTracker(GfcInputType aType, float aError = GfcInput.AXIS_DEAD_ZONE)
    {
        InputType = aType;
        AxisError = aError;
        m_lastFrameOfUpdate = Time.frameCount;
        m_previousPressedState = m_currentPressedState = GfcInput.GetAxisInput(InputType, AxisError);
    }

    private void UpdateState()
    {
        int currentFrame = Time.frameCount;
        if (currentFrame != m_lastFrameOfUpdate)
        {
            m_previousPressedState = m_currentPressedState;
            m_currentPressedState = GfcInput.GetAxisInput(InputType, AxisError);
            m_lastFrameOfUpdate = currentFrame;
        }
    }

    public bool Pressed()
    {
        UpdateState();
        return m_currentPressedState;
    }

    public bool PressedSinceLastCheck()
    {
        UpdateState();
        return !m_previousPressedState && m_currentPressedState;
    }

    public bool ReleasedSinceLastCheck()
    {
        UpdateState();
        return m_previousPressedState && !m_currentPressedState;
    }
}