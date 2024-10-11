using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class GfcInput : MonoBehaviour
{
    private static GfcInput Instance = null;

    [SerializeField] private bool m_printErrors = true;

    [SerializeField] private GameObject m_displayedInputPrefab;

    [SerializeField] private RectTransform m_displayedInputsParent;

    private List<DisplayedInputData> m_displayedInputs = new(8);

    public const float AXIS_DEAD_ZONE = 0.0001f;

    private bool m_dirtyDisplayedInput = true;

    private bool m_inputEnabled = true;

    public static bool TakeInput { get { return Instance.m_inputEnabled; } set { Instance.m_inputEnabled = value; } }

    private List<ActionElementMap> m_actionsElementsBuffer = new(8);

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        AddDisplayInput(GfcInputType.MOVEMENT_X);
        AddDisplayInput(GfcInputType.MOVEMENT_Y);
        AddDisplayInput(GfcInputType.JUMP);
        AddDisplayInput(GfcInputType.CROUCH);
        AddDisplayInput(GfcInputType.RUN);
    }

    /*
    void Update()
    {
        if (m_dirtyDisplayedInput)
        {
            for (int i = 0; i < m_displayedInputs.Count; ++i)
            {
                if (m_displayedInputs[i].Input != GfcInputType.NONE)
                {
                    GetPlayer().controllers.maps.GetElementMapsWithAction((int)m_displayedInputs[i].Input, true, m_actionsElementsBuffer);

                    string s = "The input " + m_displayedInputs[i].Input + " has the following " + m_actionsElementsBuffer.Count + " maps: ";

                    for (int j = 0; j < m_actionsElementsBuffer.Count; j++)
                    {
                        var keycode = m_actionsElementsBuffer[j].elementIdentifierName;
                        s += keycode + " ";
                    }

                    // Debug.Log(s);
                }
                else //Write the category
                {
                    //ReInput.mapping.ActionsInCategory((int)m_displayedInputs[i].Category, )
                }

            }

            m_dirtyDisplayedInput = false;
        }
    }*/

    private static string GetLocalizedLabel(GfcInputType aType)
    {
        //todo
        return aType.ToString();
    }

    private static string GetLocalizedLabel(GfcInputCategory aGroupType)
    {
        //todo
        return aGroupType.ToString();
    }

    public static void AddDisplayInput(GfcInputType aType, string aLabel = null)
    {
        if (aType != GfcInputType.NONE)
        {
            aLabel ??= GetLocalizedLabel(aType);

            int foundIndex = -1;
            //was initially thinking of using a dictionary instead of a list so the search isn't O(n), but we will have at most something like 16 inputs anyway and using a Dictionary with structs will cause boxing, so an array is better for this
            for (int i = 0; i < Instance.m_displayedInputs.Count; i++)
                if (aType == Instance.m_displayedInputs[i].Input)
                {
                    foundIndex = i;
                    break;
                }

            DisplayedInputData data;

            if (foundIndex < 0)
            {
                data = new()
                {
                    Input = aType,
                    Label = aLabel,
                };

                Instance.m_dirtyDisplayedInput = true;
                Instance.m_displayedInputs.Add(data);
            }
            else
            {
                data = Instance.m_displayedInputs[foundIndex];
                data.RegisterCount++;
                Instance.m_displayedInputs[foundIndex] = data;
            }
        }
    }

    public static void RemoveDisplayInput(GfcInputType aType)
    {
        if (aType != GfcInputType.NONE)
        {
            int foundIndex = -1;
            for (int i = 0; i < Instance.m_displayedInputs.Count; i++)
                if (aType == Instance.m_displayedInputs[i].Input)
                {
                    foundIndex = i;
                    break;
                }

            DisplayedInputData data;

            if (foundIndex > 0)
            {
                data = Instance.m_displayedInputs[foundIndex];
                data.RegisterCount--;
                Instance.m_displayedInputs[foundIndex] = data;

                if (data.RegisterCount == 0)
                    Instance.m_displayedInputs.RemoveAt(foundIndex);

                Instance.m_dirtyDisplayedInput = true;
            }
        }
    }

    public static void AddDisplayInput(GfcInputCategory aGroupType, string aLabel = null)
    {
        if (aGroupType != GfcInputCategory.NONE)
        {
            aLabel ??= GetLocalizedLabel(aGroupType);

            int foundIndex = -1;
            //was initially thinking of using a dictionary instead of a list so the search isn't O(n), but we will have at most something like 16 inputs anyway and using a Dictionary with structs will cause boxing, so an array is better for this
            for (int i = 0; i < Instance.m_displayedInputs.Count; i++)
                if (aGroupType == Instance.m_displayedInputs[i].Category)
                {
                    foundIndex = i;
                    break;
                }

            DisplayedInputData data;

            if (foundIndex < 0)
            {
                data = new()
                {
                    Category = aGroupType,
                    Label = aLabel,
                };

                Instance.m_dirtyDisplayedInput = true;
                Instance.m_displayedInputs.Add(data);
            }
            else
            {
                data = Instance.m_displayedInputs[foundIndex];
                data.RegisterCount++;
                Instance.m_displayedInputs[foundIndex] = data;
            }
        }
    }

    public static void RemoveDisplayInput(GfcInputCategory aGroupType)
    {
        if (aGroupType != GfcInputCategory.NONE)
        {
            int foundIndex = -1;
            for (int i = 0; i < Instance.m_displayedInputs.Count; i++)
                if (aGroupType == Instance.m_displayedInputs[i].Category)
                {
                    foundIndex = i;
                    break;
                }

            DisplayedInputData data;

            if (foundIndex > 0)
            {
                data = Instance.m_displayedInputs[foundIndex];
                data.RegisterCount--;
                Instance.m_displayedInputs[foundIndex] = data;

                if (data.RegisterCount == 0)
                    Instance.m_displayedInputs.RemoveAt(foundIndex);
            }
        }
    }

    public static Player GetPlayer(int aPlayerId = 0) { return ReInput.players.GetPlayer(aPlayerId); }
    //ignore input during transitions
    public static float GetAxis(GfcInputType aInputType, int aPlayerId = 0) { return aInputType == GfcInputType.NONE || !TakeInput ? 0 : GetPlayer(aPlayerId).GetAxis((int)aInputType); }
    public static float GetAxisRaw(GfcInputType aInputType, int aPlayerId = 0) { return aInputType == GfcInputType.NONE || !TakeInput ? 0 : GetPlayer(aPlayerId).GetAxisRaw((int)aInputType); }
    public static bool GetInput(GfcInputType aInputType, int aPlayerId = 0, float aError = AXIS_DEAD_ZONE) { return GetAxisRaw(aInputType, aPlayerId).Abs() > aError; }
}

//used for generic inputs that should be compatible with a keyboard and a controller
public enum GfcInputType
{
    NONE = -1,

    MOVEMENT_X = 0,
    MOVEMENT_Y = 1,
    CAMERA_X = 2,
    CAMERA_Y = 3,

    FIRE1 = 4,
    FIRE2 = 5,

    SHOULDER_RIGHT = 6,
    SHOULDER_LEFT = 7,


    JUMP = 8,
    CROUCH = 9,
    RUN = 10,

    ACTION0 = 11,
    ACTION1 = 12,
    ACTION2 = 13,
    ACTION3 = 14,

    PAUSE = 15,
    SELECT = 16,
    SUBMIT = 17,
    BACK = 18,

    DIR_DOWN = 21,
    DIR_RIGHT = 22,
    DIR_LEFT = 23,
    DIR_UP = 24,
}

//used to category together inputs (e.g. MOVEMENT with GfcInputType.MOVEMENT_X and GfcInputType.MOVEMENT_Y)
public enum GfcInputCategory
{
    NONE,
    MOVEMENT,
    LOOK,
}

[System.Serializable]
public struct GfcInputTracker
{
    [SerializeField] private GfcInputType m_inputType;
    [SerializeField] private float m_axisError;
    public GameObject ParentGameObject;

    private int m_lastFrameOfUpdate;

    private int m_playerId;

    private bool m_currentPressedState;

    private bool m_previousPressedState;

    private bool m_stateCheckedThisFrame;

    public GfcInputType InputType { readonly get { return m_inputType; } set { m_inputType = value; UpdateState(); } }
    public float AxisError { readonly get { return m_axisError; } set { m_axisError = value; UpdateState(); } }

    public GfcInputTracker(GfcInputType aType, GameObject aParentGameObject = null, int aPlayerId = 0, float aError = GfcInput.AXIS_DEAD_ZONE)
    {
        m_inputType = aType;
        m_axisError = aError;
        m_playerId = aPlayerId;
        ParentGameObject = aParentGameObject;
        m_stateCheckedThisFrame = false;
        m_lastFrameOfUpdate = Time.frameCount;
        m_previousPressedState = m_currentPressedState = GfcInput.GetInput(m_inputType, aPlayerId, m_axisError);
    }

    public bool Pressed()
    {
        UpdateState();
        return m_currentPressedState && ParentValid();
    }

    public bool PressedSinceLastCheck(bool aUniqueThisFrame = true)
    {
        UpdateState();
        bool pressed = (!m_stateCheckedThisFrame || !aUniqueThisFrame) && !m_previousPressedState && m_currentPressedState;
        m_stateCheckedThisFrame |= aUniqueThisFrame;
        return pressed && ParentValid();
    }

    public bool ReleasedSinceLastCheck(bool aUniqueThisFrame = true)
    {
        UpdateState();
        bool released = (!m_stateCheckedThisFrame || !aUniqueThisFrame) && m_previousPressedState && !m_currentPressedState;
        m_stateCheckedThisFrame |= aUniqueThisFrame;
        return released && ParentValid();
    }

    private void UpdateState()
    {
        int currentFrame = Time.frameCount;
        if (currentFrame != m_lastFrameOfUpdate)
        {
            m_previousPressedState = m_currentPressedState;
            m_currentPressedState = GfcInput.GetInput(m_inputType, m_playerId, m_axisError);
            m_lastFrameOfUpdate = currentFrame;
            m_stateCheckedThisFrame = false;
        }
    }

    private readonly bool ParentValid() { return ParentGameObject == null || ParentGameObject.activeInHierarchy; }
}

//this is used mainly for coroutines where we need to share an input becaue async functions cannot have ref structs as arguments
[System.Serializable]
public class GfcInputTrackerShared
{
    [SerializeField] private GfcInputTracker m_inputTracker = new(GfcInputType.SUBMIT);

    public GfcInputTrackerShared(GfcInputType aType, GameObject aParentGameObject = null, int aPlayerId = 0, float aError = GfcInput.AXIS_DEAD_ZONE) { m_inputTracker = new(aType, aParentGameObject, aPlayerId, aError); }

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

public struct DisplayedInputData
{
    public string Label;

    public GfcInputType Input;
    public GfcInputCategory Category;

    public int RegisterCount;
}