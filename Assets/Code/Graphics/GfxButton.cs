using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using System.Reflection;
using UnityEngine.UI;

public abstract class GfxButton : GfcInteractable, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GfcUnityEvent OnSubmit = new();

    [SerializeField] protected GfxButtonHightlightState m_highlightStateDefault = new() { Scale = new(1, 1, 1), Opacity = 1, ColorContent = Color.white, ColorPanel = Color.white, PixelsPerUnit = 1 };

    [SerializeField] protected GfxButtonHightlightState m_highlightStateDisabled = new() { Scale = new(1, 1, 1), Opacity = 0.6f, ColorContent = Color.white, ColorPanel = Color.white, PixelsPerUnit = 1 };

    [SerializeField] protected GfxButtonHightlightState m_highlightStateSelected = new() { Scale = new(1, 1, 1), Opacity = 1, ColorContent = Color.white, ColorPanel = new(0.75f, 0.75f, 0.75f, 1), PixelsPerUnit = 1 };

    [SerializeField] protected GfxButtonHightlightState m_highlightStateSubmit = new() { Scale = new(1, 1, 1), Opacity = 1, ColorContent = Color.white, ColorPanel = new(0.75f, 0.75f, 0.75f, 1), PixelsPerUnit = 1 };

    [SerializeField] protected GfxButtonHightlightState m_highlightStatePinned = new() { Scale = new(1, 1, 1), Opacity = 1, ColorContent = Color.white, ColorPanel = new(0.5f, 0.5f, 0.5f, 1), PixelsPerUnit = 1 };

    public int Index = 0;

    public float TransitionTime = 0.07f;

    protected Transform m_transform;

    public Action<GfxButtonCallbackType, GfxButton, bool> OnButtonEventCallback;

    private Action<GfxButtonCallbackType, GfxButton, bool> OnEventCallbackPrivate;

    private float m_transitionTimeEffective = 0.07f;

    protected bool m_isSelected = false;

    protected bool m_isPinned = false;

    protected bool m_isDisabled = false;

    protected int m_frameOfEnable = -1;

    protected bool m_isInteractable = true;

    protected bool m_deselectOnPointerExit = false;

    private float m_timeSinceTransitionStart = 0;

    protected string m_disabledReason = null;

    protected GfcSound m_soundSelect;

    protected GfcSound m_soundDeselect;

    protected GfcSound m_soundPinned;

    protected GfcSound m_soundUnpinned;

    protected GfcSound m_soundSubmit;

    //the bool tells if the panel was selected or not. False means it was deselected

    protected const string EMPTY = "";

    protected string m_inactiveReason = EMPTY;

    protected bool m_isActive = false;

    public void OnSelect(BaseEventData aEventData) { SetSelected(true); }

    public void OnPointerEnter(PointerEventData aEventData) { SetSelected(true); }

    public void OnDeselect(BaseEventData aEventData) { SetSelected(false); }

    public void OnPointerExit(PointerEventData aEventData) { if (m_deselectOnPointerExit || true) SetSelected(false); }

    public bool IsSelectable() { return m_isInteractable && (!m_isDisabled || !m_disabledReason.IsEmpty()); }

    public override void Interact(GfcCursorRayhit aHit) { Submit(); }

    private bool m_initialisedButton = false;

    private void Start() { Initialize(); }

    const string EVENT_FUNCTION_NAME = "OnButtonEvent";

    public virtual void Initialize()
    {
        if (!m_initialisedButton)
        {
            m_transform = transform;
            m_initialisedButton = true;

            const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            List<Component> componentsBuffer = GfcPooling.ComponentsBuffer;
            gameObject.GetComponents(componentsBuffer);

            foreach (Component component in componentsBuffer)
            {
                MethodInfo eventCallback = component.GetType().GetMethod(EVENT_FUNCTION_NAME, FLAGS);
                if (eventCallback != null)
                {
                    OnEventCallbackPrivate += (Action<GfxButtonCallbackType, GfxButton, bool>)Delegate.CreateDelegate(typeof(Action<GfxButtonCallbackType, GfxButton, bool>), component, eventCallback);
                }
            }

            componentsBuffer.Clear();

            Reset();
        }
    }

    public virtual void Reset()
    {
        m_transitionTimeEffective = TransitionTime;
        m_timeSinceTransitionStart = TransitionTime;

        Index = 0;
        m_deselectOnPointerExit = false;
        SetInteractable(true);
        SetDisabled(false);
        SetSelected(false);
        SetPinned(false);
        StartTransitionToNewState();
    }

    private void OnEventCallbackInternal(GfxButtonCallbackType aCallbackType, bool aState)
    {
        OnButtonEventCallback?.Invoke(aCallbackType, this, aState);
        OnEventCallbackPrivate?.Invoke(aCallbackType, this, aState);
    }

    public void Submit()
    {
        if (!m_isDisabled && m_isInteractable)
        {
            OnSubmit.Invoke();
            OnEventCallbackInternal(GfxButtonCallbackType.SUBMIT, true);
            m_soundSubmit?.PlaySingleInstance();
        }
    }

    protected void Update()
    {
        if (m_timeSinceTransitionStart < m_transitionTimeEffective && m_initialisedButton)
        {
            m_timeSinceTransitionStart += Time.deltaTime;
            m_timeSinceTransitionStart.MinSelf(m_transitionTimeEffective);
            SetTransitionLerpState(m_timeSinceTransitionStart / m_transitionTimeEffective, GetFinalHighlightState());
        }
    }

    protected abstract void SetTransitionLerpState(float aTransitionPoint, GfxButtonHightlightState aDesiredState);

    public void SetSelected(bool aSelect, bool aIgnoreInteractable = false)
    {
        if (IsSelectable() && aSelect != m_isSelected && (aIgnoreInteractable || m_isInteractable))
        {

            m_isSelected = aSelect;
            StartTransitionToNewState(aSelect);
            OnEventCallbackInternal(GfxButtonCallbackType.SELECT, aSelect);

            if (aSelect)
            {
                m_soundSelect?.PlaySingleInstance();
                if (m_isDisabled) GfxUiTools.WriteDisableReason(this);
            }
            else //deselected
            {
                m_soundDeselect?.PlaySingleInstance();
                if (m_isDisabled) GfxUiTools.EraseDisableReason(this);
            }
        }
    }

    public void SetPinned(bool aIsPinned, bool aForceEnable = false, bool aIgnoreInteractable = false)
    {
        if (aIgnoreInteractable || m_isInteractable)
        {
            if (aForceEnable) SetDisabled(false);
            aIsPinned &= !m_isDisabled;

            if (aIsPinned != m_isPinned)
            {
                m_isPinned = aIsPinned;
                StartTransitionToNewState(aIsPinned);
                OnEventCallbackInternal(GfxButtonCallbackType.PINNED, aIsPinned);

                if (aIsPinned)
                    m_soundPinned?.PlaySingleInstance();
                else
                    m_soundUnpinned?.PlaySingleInstance();
            }
        }
    }

    public void SetDisabled(bool aIsDisabled, string aDisableReason = null, bool aIgnoreInteractable = false)
    {
        if (aIgnoreInteractable || m_isInteractable)
        {
            m_disabledReason = aDisableReason;
            if (m_isDisabled != aIsDisabled)
            {
                m_isDisabled = aIsDisabled;
                StartTransitionToNewState(true);
                SetPinned(m_isPinned && !m_isDisabled);
                OnEventCallbackInternal(GfxButtonCallbackType.DISABLED, aIsDisabled);
            }
        }
    }

    public void SetInteractable(bool aIsInteractable, bool aOverrideStates = true)
    {
        bool newState = aIsInteractable || !aOverrideStates;
        m_isInteractable = aIsInteractable;
        SetPinned(m_isPinned && newState, false, true);
        SetSelected(m_isSelected && newState, true);
        SetDisabled(m_isDisabled && newState, EMPTY, true);
    }

    public bool GetIsInteractable() { return m_isInteractable; }

    protected static GfxButtonHightlightState ComposeHighlightState(GfxButtonHightlightState aBase, GfxButtonHightlightState aHighlight)
    {
        return new()
        {
            Scale = aHighlight.Scale.Mult(aBase.Scale),
            PositionOffset = aHighlight.PositionOffset + aBase.PositionOffset,

            ColorPanel = GfxUiTools.BlendColors(aBase.ColorPanel, aHighlight.ColorPanel, aHighlight.ColorPanelBlendMode),
            ColorContent = GfxUiTools.BlendColors(aBase.ColorContent, aHighlight.ColorContent, aHighlight.ColorContentBlendMode),

            ColorPanelBlendMode = aHighlight.ColorPanelBlendMode,
            ColorContentBlendMode = aHighlight.ColorContentBlendMode,

            Opacity = aHighlight.Opacity * aBase.Opacity,
            PixelsPerUnit = aHighlight.PixelsPerUnit,
        };
    }

    protected abstract void OnStartTransition();

    public void StartTransitionToNewState(bool aRestartTransitionTime = true)
    {
        if (WasEnabledThisFrame())
        {
            SnapToDesiredState();
        }
        else
        {
            /*
                   if (aRestartTransitionTime || m_stateTransitionProgress > 0.9999f)
                       m_transitionTimeEffective = TransitionTime;
                   else
                       m_transitionTimeEffective = TransitionTime * (1.0f - m_stateTransitionProgress);
                   */

            m_timeSinceTransitionStart = 0;
            m_transitionTimeEffective = TransitionTime;
            OnStartTransition();
        }
    }

    protected bool WasEnabledThisFrame() { return m_frameOfEnable == -1 || m_frameOfEnable == Time.frameCount; }

    protected void OnEnable() { m_frameOfEnable = Time.frameCount; }

    protected GfxButtonHightlightState GetHighlightedDefaultState(GfxButtonHightlightState aHighlight) { return ComposeHighlightState(m_highlightStateDefault, aHighlight); }

    protected GfxButtonHightlightState GetDefaultHighlightState()
    {
        if (m_isSelected)
        {
            if (m_isPinned) return GetHighlightedDefaultState(m_highlightStatePinned);
            if (m_isDisabled) return GetHighlightedDefaultState(m_highlightStateDisabled);
        }

        return m_highlightStateDefault;
    }

    protected GfxButtonHightlightState GetDesiredHighlightState()
    {
        if (m_isSelected) return m_highlightStateSelected;
        if (m_isDisabled) return m_highlightStateDisabled;
        if (m_isPinned) return m_highlightStatePinned;
        return m_highlightStateDefault;
    }

    protected GfxButtonHightlightState GetFinalHighlightState()
    {
        if (!m_isPinned && !m_isSelected && !m_isDisabled)
            return m_highlightStateDefault;

        GfxButtonHightlightState defaultState = GetDefaultHighlightState();
        GfxButtonHightlightState desiredState = GetDesiredHighlightState();

        return ComposeHighlightState(defaultState, desiredState);
    }

    public virtual void SetCreateData(GfxButtonCreateData aPanelData, bool aSnapToDesiredState = false, bool aSetPositionAndIndex = true)
    {
        Initialize();
        Reset();

        TransitionTime = aPanelData.TransitionTime;

        m_highlightStateDefault = aPanelData.DefaultHighlightState;
        m_highlightStateDisabled = aPanelData.DisabledHighlightState;
        m_highlightStateSelected = aPanelData.SelectHighlightState;
        m_highlightStateSubmit = aPanelData.SubmitHighlightState;
        m_highlightStatePinned = aPanelData.PinnedHighlightState;

        OnButtonEventCallback = aPanelData.OnEventCallback;

        m_soundDeselect = aPanelData.SoundDeselect;
        m_soundPinned = aPanelData.SoundPinned;
        m_soundSelect = aPanelData.SoundSelect;
        m_soundSubmit = aPanelData.SoundSubmit;
        m_soundUnpinned = aPanelData.SoundUnpinned;

        if (aSetPositionAndIndex)
        {
            Index = aPanelData.Index;
            m_deselectOnPointerExit = aPanelData.DeselectOnPointerExit;

            if (aPanelData.Parent == null)
                Debug.LogError("The parent of the GfxPanelCreateData is null, this is most likely a mistake.");

            m_transform.SetParent(aPanelData.Parent);
        }

        SetInteractable(aPanelData.IsInteractable);

        if (aSnapToDesiredState)
            SnapToDesiredState();
    }

    public void SnapToDesiredState() { SetTransitionLerpState(1, GetFinalHighlightState()); }

    public string GetInactiveReason() { return m_inactiveReason; }

    public string GetDisabledReason() { return m_disabledReason; }

    public void SetHightlightStateDefault(GfxButtonHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStateDefault = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }
    public void SetHightlightStateDisabled(GfxButtonHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStateDisabled = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }
    public void SetHightlightStatePinned(GfxButtonHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStatePinned = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }
    public void SetHightlightStateSelected(GfxButtonHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStateSelected = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }
    public void SetHightlightStateSubmit(GfxButtonHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStateSubmit = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }

    public bool IsSelected() { return m_isSelected; }
    public GfxButtonHightlightState GetHightlightStateDefault() { return m_highlightStateDefault; }
    public GfxButtonHightlightState GetHightlightStateDisabled() { return m_highlightStateDisabled; }
    public GfxButtonHightlightState GetHightlightStateSelected() { return m_highlightStateSelected; }
    public GfxButtonHightlightState GetHightlightStateSubmit() { return m_highlightStateSubmit; }
    public GfxButtonHightlightState GetHightlightStatePinned() { return m_highlightStatePinned; }
    public bool IsPinned() { return m_isPinned; }

    public bool IsDisabled() { return m_isDisabled; }

    public bool IsSelectedPinned() { return m_isPinned && m_isSelected; }
}

public enum GfxButtonCallbackType
{
    SELECT,
    PINNED,
    DISABLED,
    SUBMIT
}


[Serializable]
public struct GfxButtonCreateData
{
    public GfcSound SoundSelect;

    public GfcSound SoundDeselect;

    public GfcSound SoundPinned;

    public GfcSound SoundUnpinned;

    public GfcSound SoundSubmit;

    public Action<GfxButtonCallbackType, GfxButton, bool> OnEventCallback;

    public GfxButtonHightlightState DefaultHighlightState;

    public GfxButtonHightlightState DisabledHighlightState;

    public GfxButtonHightlightState SelectHighlightState;

    public GfxButtonHightlightState SubmitHighlightState;

    public GfxButtonHightlightState PinnedHighlightState;

    [HideInInspector] public Transform Parent;

    [HideInInspector] public int Index;

    public float TransitionTime;

    public bool IsInteractable;

    public bool DeselectOnPointerExit;
}
