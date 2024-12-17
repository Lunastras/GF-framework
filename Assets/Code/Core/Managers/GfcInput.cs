using System;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using LMirman.RewiredGlyphs;

public class GfcInput : MonoBehaviour
{
    private static GfcInput Instance = null;

    [SerializeField] private GfcInputDisplayPromptParent m_displayedInputsParent;

    private List<GfcDisplayedInputData> m_displayedInputs = new(8);

    public const float AXIS_DEAD_ZONE = 0.0001f;

    private readonly List<ActionElementMap> m_actionsElementsBuffer = new(8);

    private int m_fixedUpdateCountOfUpdate = 1;

    private readonly GfcLockPriorityQueue m_inputLockHandle = new();

    public static GfcLockPriorityQueue InputLockHandle { get { return Instance.m_inputLockHandle; } }

    private const int FIXED_DELTAS_UNTIL_REMOVE_DISPLAY = 3;
    private const int FIXED_DELTAS_UNTIL_UPDATE_DISPLAY = 3;

    public static List<ActionElementMap> ActionsElementsBuffer
    {
        get
        {
            Debug.Assert(Instance.m_actionsElementsBuffer.Count == 0, "The actions elements buffer was not emptied after use.");
            return Instance.m_actionsElementsBuffer;
        }
    }

    private static void SetDisplayDirty()
    {
        if (Instance.m_fixedUpdateCountOfUpdate == -1)
            Instance.m_fixedUpdateCountOfUpdate = GfcPhysics.FixedUpdateCount + FIXED_DELTAS_UNTIL_UPDATE_DISPLAY;
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        Debug.Assert(m_displayedInputsParent);
    }

    void FixedUpdate()
    {
        if (InputLockHandle.Locked())
            Debug.Log("I AM LOCKED BY " + InputLockHandle.GetHeadCopy().ObjectHandle);

        int fixedUpdateCount = GfcPhysics.FixedUpdateCount;
        for (int i = 0; i < m_displayedInputs.Count; i++)
        {
            GfcDisplayedInputData data = m_displayedInputs[i];
            int numComponents = m_displayedInputs[i].Labels.Count;
            bool valid = false;

            for (int j = 0; j < numComponents; j++) //fixme
            {
                int fixedUpdatesSinceLastUpdate = fixedUpdateCount - data.Labels[j].LastUpdateFixedUpdateCount;
                if (data.Labels[j].LabelString != null && fixedUpdatesSinceLastUpdate >= FIXED_DELTAS_UNTIL_REMOVE_DISPLAY)
                {
                    data.Labels.RemoveAt(j);
                    data.UpdateLabelString();
                    SetDisplayDirty();
                    j--; //check this index again
                }
                else
                {
                    if (data.Labels[j].LabelString == null)
                        break;

                    valid = true;
                }
            }

            if (!valid)
            {
                m_displayedInputs.RemoveAt(i);
                SetDisplayDirty();
                i--; //check this index again
            }
            else
            {
                m_displayedInputs[i] = data;
            }
        }

        if (m_fixedUpdateCountOfUpdate != -1 && m_fixedUpdateCountOfUpdate <= fixedUpdateCount)
        {
            SortPrompts();
            m_displayedInputsParent.UpdatePrompts(m_displayedInputs);
            m_fixedUpdateCountOfUpdate = -1;
        }
    }

    void SortPrompts()
    {
        int count = m_displayedInputs.Count;
        for (int i = 0; i < count; i++)
        {
            GfcDisplayedInputData data = m_displayedInputs[i];
            m_displayedInputsParent.CalculateLength(ref data, 0);
            m_displayedInputs[i] = data;
        }

        m_displayedInputs.Sort((a, b) => b.CompareTo(a));
    }

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

    public static Sprite GetGlyph(ControllerType aControllerType, int anActionId, int aPlayerId = 0)
    {
        return InputGlyphs.GetGlyph(aControllerType, anActionId, Pole.Positive, out _, aPlayerId).GetSprite(AxisRange.Full);
    }

    public static void UpdateDisplayInput(GfcInputCategory aCategoryType, GfcLockKey aKey = default, string aLabel = null) { UpdateDisplayInput(GfcInputType.NONE, aCategoryType, aKey, aLabel); }
    public static void UpdateDisplayInput(GfcInputType anInputType, GfcLockKey aKey = default, string aLabel = null) { UpdateDisplayInput(anInputType, GfcInputCategory.NONE, aKey, aLabel); }
    public static void UpdateDisplayInput(GfcInputType aType, GfcInputCategory aCategoryType, GfcLockKey aKey = default, string aLabel = null)
    {
        if (!InputLockHandle.AuthorityTest(aKey))
            return;

        Debug.Assert(aType != GfcInputType.NONE ^ aCategoryType != GfcInputCategory.NONE, "Only one of these can be none/valid.");

        int foundIndex = -1;
        //was initially thinking of using a dictionary instead of a list so the search isn't O(n), but we will have at most something like 16 inputs anyway and using a Dictionary with structs will cause boxing, so an array is better for this
        for (int i = 0; i < Instance.m_displayedInputs.Count; i++)
            if ((aType != GfcInputType.NONE && aType == Instance.m_displayedInputs[i].Input)
            || (aCategoryType != GfcInputCategory.NONE && aCategoryType == Instance.m_displayedInputs[i].Category))
            {
                foundIndex = i;
                break;
            }

        GfcDisplayedInputData data;

        aLabel ??= aType != GfcInputType.NONE ? GetLocalizedLabel(aType) : GetLocalizedLabel(aCategoryType);

        if (foundIndex < 0)
        {
            data = new()
            {
                Input = aType,
                Label = aLabel,
                Category = aCategoryType,
            };

            data.LabelUsed(aLabel);
            Instance.m_displayedInputs.Add(data);
            SetDisplayDirty();
        }
        else
        {
            data = Instance.m_displayedInputs[foundIndex];

            if (data.LabelUsed(aLabel))
                SetDisplayDirty();

            Instance.m_displayedInputs[foundIndex] = data;
        }
    }

    public static Player GetPlayer(int aPlayerId = 0) { return ReInput.players.GetPlayer(aPlayerId); }
    //ignore input during transitions
    public static float GetAxis(GfcInputType aInputType, int aPlayerId = 0, GfcLockKey aKey = default) { return aInputType == GfcInputType.NONE || !InputLockHandle.AuthorityTest(aKey) ? 0 : GetPlayer(aPlayerId).GetAxis((int)aInputType); }
    public static float GetAxisRaw(GfcInputType aInputType, int aPlayerId = 0, GfcLockKey aKey = default) { return aInputType == GfcInputType.NONE || !InputLockHandle.AuthorityTest(aKey) ? 0 : GetPlayer(aPlayerId).GetAxisRaw((int)aInputType); }
    public static bool GetInput(GfcInputType aInputType, int aPlayerId = 0, GfcLockKey aKey = default, float aError = AXIS_DEAD_ZONE) { return GetAxisRaw(aInputType, aPlayerId, aKey).Abs() > aError; }
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

public enum GfcInputLockPriority
{
    BASE = 0,
    UI1,
    UI2,
    UI3,
    UI4,
    GF_MASTER = 999999
}

[Serializable]
public struct GfcInputTracker : IGfcInputTracker
{
    [SerializeField] private GfcInputType m_inputType;

    public GfcInputCategory InputCategory;

    public float AxisError;

    public GameObject ParentGameObject;

    public int PlayerId;

    public GfcLockKey Key;

    public bool AllowFrameSkipping;

    public bool DisplayPrompt;

    [HideInInspector] public GfcLocalizedString DisplayPromptString;

    private int m_lastFrameOfUpdate;

    private int m_lastFrameOfCheck;

    private bool m_currentPressedState;

    private bool m_previousPressedState;

    private bool m_stateCheckedThisFrame;

    private bool m_waitingForReleaseAfterFrameSkip;

    public readonly GfcInputType InputType { get { return m_inputType; } }

    public GfcInputTracker(GfcInputType aType, GameObject aParentGameObject = null, int aPlayerId = 0, float aError = GfcInput.AXIS_DEAD_ZONE, bool anAllowFrameSkipping = false, GfcLocalizedString aDisplayPromptString = default, bool aDisplayPrompt = true, GfcInputCategory anInputCategory = GfcInputCategory.NONE)
    {
        InputCategory = anInputCategory;
        m_inputType = aType;
        AxisError = aError;
        PlayerId = aPlayerId;
        AllowFrameSkipping = anAllowFrameSkipping;
        m_waitingForReleaseAfterFrameSkip = false;
        ParentGameObject = aParentGameObject;
        m_stateCheckedThisFrame = false;
        DisplayPrompt = aDisplayPrompt;
        DisplayPromptString = aDisplayPromptString;
        m_lastFrameOfUpdate = m_lastFrameOfCheck = 0;
        m_previousPressedState = m_currentPressedState = false;
        Key = default;
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
        return pressed && !IgnoreInput();
    }

    public bool ReleasedSinceLastCheck(bool aUniqueThisFrame = true)
    {
        UpdateState();
        bool released = (!m_stateCheckedThisFrame || !aUniqueThisFrame) && m_previousPressedState && !m_currentPressedState;
        m_stateCheckedThisFrame |= aUniqueThisFrame;
        return released && !IgnoreInput();
    }

    private void UpdateState()
    {
        int currentFrame = Time.frameCount;
        if (currentFrame != m_lastFrameOfCheck)
        {
            if (GfcInput.InputLockHandle.AuthorityTest(Key) && ParentValid()) //skip the frame in case the authority test fails
            {
                m_previousPressedState = m_currentPressedState;
                m_currentPressedState = GfcInput.GetInput(m_inputType, PlayerId, Key, AxisError);
                m_waitingForReleaseAfterFrameSkip = !AllowFrameSkipping && m_currentPressedState && (m_waitingForReleaseAfterFrameSkip || currentFrame != m_lastFrameOfUpdate + 1);

                if (!IgnoreInput() && DisplayPrompt)
                    GfcInput.UpdateDisplayInput(m_inputType, InputCategory, Key, DisplayPromptString);

                m_lastFrameOfUpdate = currentFrame;
                m_stateCheckedThisFrame = false;
            }
            else
            {
                m_previousPressedState = m_stateCheckedThisFrame = m_currentPressedState = false;
            }

            m_lastFrameOfCheck = currentFrame;
        }
    }

    private readonly bool IgnoreInput() { return m_waitingForReleaseAfterFrameSkip; }
    private readonly bool ParentValid() { return ParentGameObject == null || ParentGameObject.ActiveInHierarchyGf(); }
}

//this is used mainly for coroutines where we need to share an input becaue async functions cannot have ref structs as arguments
[Serializable]
public class GfcInputTrackerShared : IGfcInputTracker
{
    [SerializeField] public GfcInputTracker m_inputTracker = new(GfcInputType.SUBMIT);

    public GfcInputTrackerShared(GfcInputType aType, GameObject aParentGameObject = null, int aPlayerId = 0, float aError = GfcInput.AXIS_DEAD_ZONE, bool anAllowFrameSkipping = false, GfcLocalizedString aDisplayPromptString = default, bool aDisplayPrompt = true, GfcInputCategory anInputCategory = GfcInputCategory.NONE)
    {
        m_inputTracker = new(aType, aParentGameObject, aPlayerId, aError, anAllowFrameSkipping, aDisplayPromptString, aDisplayPrompt, anInputCategory);
    }

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

public interface IGfcInputTracker
{
    public bool Pressed();
    public bool PressedSinceLastCheck(bool aUniqueThisFrame = true);
    public bool ReleasedSinceLastCheck(bool aUniqueThisFrame = true);
}

public struct GfcDisplayedInputDataLabel : IComparable<GfcDisplayedInputDataLabel>, IEquatable<GfcDisplayedInputDataLabel>
{
    public string LabelString;
    public int LastUpdateFixedUpdateCount;

    public int CompareTo(GfcDisplayedInputDataLabel aLabel)
    {
        if (LabelString == null || aLabel.LabelString == null)
        {
            if (aLabel.LabelString == LabelString) return 0;
            if (LabelString == null && aLabel.LabelString != null) return 1;
            return -1; //aLabel.LabelString is null and LabelString isn't
        }

        return LabelString.CompareTo(aLabel.LabelString);
    }

    public bool Equals(GfcDisplayedInputDataLabel aLabel)
    {
        return CompareTo(aLabel) == 0;
    }
}

public struct GfcDisplayedInputData : IComparable<GfcDisplayedInputData>
{
    public string Label;
    public Vector4<GfcDisplayedInputDataLabel> Labels;
    public GfcInputType Input;
    public GfcInputCategory Category;
    public float PromptLength;

    public readonly int CompareTo(GfcDisplayedInputData aData)
    {
        /*
        int ret = 0;
        if (aData.Input != Input || aData.Category != Category)
        {
            ret = 1;
            if (aData.Category > Category || (aData.Input > Input && Category == GfcInputCategory.NONE))
                ret = -1;
        }*/

        return (PromptLength - aData.PromptLength).Sign();
    }

    bool m_threwWarning;

    public void UpdateLabelString()
    {
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
        int numComponents = Labels.Count;
        string label;

        for (int i = 0; i < numComponents; i++)
        {
            label = Labels[i].LabelString;
            if (label != null)
            {
                if (i > 0) stringBuffer.Append('/');
                stringBuffer.Append(Labels[i].LabelString);
            }
            else
            {
                break;
            }
        }

        Label = stringBuffer.GetStringCopy();
        stringBuffer.Clear();
    }

    public bool LabelUsed(string aLabel)
    {
        Debug.Assert(aLabel != null);

        int labelIndex = 0;
        int numComponents = Labels.Count;
        for (; labelIndex < numComponents && Labels[labelIndex].LabelString != null && aLabel != Labels[labelIndex].LabelString; ++labelIndex) ;

        bool validIndex = labelIndex < numComponents;
        bool addNewLabel = validIndex && Labels[labelIndex].LabelString == null;

        if (validIndex)
        {
            GfcDisplayedInputDataLabel label = new() { LabelString = aLabel, LastUpdateFixedUpdateCount = GfcPhysics.FixedUpdateCount };
            if (addNewLabel)
            {
                Labels.Insert(Labels.GetSortedIndex(label), label);
            }
            else
            {
                Labels[labelIndex] = label;
            }

            UpdateLabelString();
        }
        else if (!m_threwWarning)
        {
            Debug.LogWarning("There are over 4 prompts for the input of type " + Input + "/" + Category + ", cannot register prompt " + aLabel);
            m_threwWarning = true;
        }

        return addNewLabel;
    }
}