using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GfxPanel : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform m_panelRectTransform;

    [SerializeField] private CanvasGroup m_canvasGroup;

    [SerializeField] private Image m_iconImage;

    [SerializeField] private TextMeshProUGUI m_textLeft;

    [SerializeField] private TextMeshProUGUI m_textRight;

    [SerializeField] private ParticleSystem m_blessedParticleSystem;

    [SerializeField] private RectTransform m_contentsRectTransform;

    [SerializeField] private float m_contentInPadding = 12;

    [SerializeField] private Vector2 m_contentPadding = new Vector2(12, 12);

    [SerializeField] protected GfxPanelHightlightState m_highlightStateDefault = new() { PanelSizeCoef = new(1, 1), GroupAlpha = 1, };

    [SerializeField] protected GfxPanelHightlightState m_highlightStateDisabled = new() { PanelSizeCoef = new(1, 1), GroupAlpha = 0.6f, };

    [SerializeField] protected GfxPanelHightlightState m_highlightStateSelected = new() { PanelSizeCoef = new(1, 1), GroupAlpha = 1, };

    [SerializeField] protected GfxPanelHightlightState m_highlightStateSubmit = new() { PanelSizeCoef = new(1, 1), GroupAlpha = 1, };

    [SerializeField] protected GfxPanelHightlightState m_highlightStatePinned = new() { PanelSizeCoef = new(1, 1), GroupAlpha = 1, };

    protected bool m_iconActive = true;

    public Vector2Int IndecesColumnRow = new();

    public int Index = 0;

    public float TransitionTime = 0.07f;

    protected float m_transitionTimeEffective = 0.07f;

    private RectTransform m_mainRectTransform;

    private RectTransform m_leftTextRectTransform;

    private RectTransform m_textParentRectTransform;

    private RectTransform m_rightTextRectTransform;

    private RectTransform m_iconRectTransform;

    private Image m_panelImage;

    //the bool tells if the panel was selected or not. False means it was deselected
    public Action<GfxPanelCallbackType, GfxPanel, bool> OnEventCallback;

    protected const string EMPTY = "";

    protected string m_inactiveReason = EMPTY;

    protected bool m_isActive = false;

    protected bool m_leftTextHasPriority = true;

    //The length of the priority text relative to the text parent length
    protected float m_priorityTextLengthRatio = 1;

    private bool m_initialized = false;

    private bool m_isSelected = false;

    private bool m_isPinned = false;

    private bool m_isDisabled = false;

    private int m_frameOfEnable = -1;

    private bool m_isInteractable = true;

    private bool m_deselectOnPointerExit = false;

    private float m_timeSinceTransitionStart = 0;

    private string m_disabledReason = null;

    private GfcSound m_soundSelect;

    private GfcSound m_soundDeselect;

    private GfcSound m_soundPinned;

    private GfcSound m_soundUnpinned;

    private GfcSound m_soundSubmit;

    private AnimationCurve m_transitionCurve;

    private GfxPanelTransitionState m_stateAtTransitionStart = default;

    private bool WasEnabledThisFrame() { return m_frameOfEnable == -1 || m_frameOfEnable == Time.frameCount; }

    private void OnEnable() { m_frameOfEnable = Time.frameCount; }

    private void Awake() { InitializeHard(); }

    public void InitializeHard()
    {
        if (!m_initialized)
        {
            m_initialized = true;
            m_mainRectTransform = GetComponent<RectTransform>();
            m_panelImage = m_panelRectTransform.GetComponent<Image>();
            m_iconRectTransform = m_iconImage.GetComponent<RectTransform>();
            m_leftTextRectTransform = m_textLeft.GetComponent<RectTransform>();
            m_rightTextRectTransform = m_textRight.GetComponent<RectTransform>();
            m_textParentRectTransform = m_rightTextRectTransform.parent.GetComponent<RectTransform>();

            float priorityTextLength = m_leftTextHasPriority ? m_leftTextRectTransform.rect.width : m_rightTextRectTransform.rect.width;
            m_priorityTextLengthRatio = priorityTextLength / m_textParentRectTransform.rect.width;

            transform.localScale = new Vector3(1, 1, 1);
            RefreshPadding();
            if (m_panelImage.pixelsPerUnit != 1)
                Debug.LogError("The default pixelsPerUnit value set in " + gameObject.name + " was not 1, this will cause issues.");
        }

        m_transitionTimeEffective = TransitionTime;
        m_timeSinceTransitionStart = TransitionTime;

        IndecesColumnRow.x = IndecesColumnRow.y = Index = 0;
        m_deselectOnPointerExit = false;
        SetInteractable(true);
        SetDisabled(false);
        SetSelected(false);
        SetPinned(false);
        StartTransitionToNewState();
    }

    protected void Update()
    {
        if (m_timeSinceTransitionStart < m_transitionTimeEffective && m_initialized)
        {
            m_timeSinceTransitionStart += Time.deltaTime;
            SetTransitionLerpState(m_timeSinceTransitionStart / m_transitionTimeEffective);
        }

        Color c = m_panelImage.color;
        c.a = 1;
        m_panelImage.color = c;
    }

    protected void SetTransitionLerpState(float aTransitionPoint)
    {
        GfxPanelHightlightState finalState = GetFinalHighlightState();
        m_timeSinceTransitionStart = aTransitionPoint * m_transitionTimeEffective;

        if (m_transitionCurve != null)
            aTransitionPoint = m_transitionCurve.Evaluate(aTransitionPoint);

        float effectiveLerpValue = MathF.Min(1.0f, aTransitionPoint);
        m_panelRectTransform.localPosition = Vector2.Lerp(m_stateAtTransitionStart.LocalPosition, finalState.PositionOffset, effectiveLerpValue);
        m_panelImage.color = Color.Lerp(m_stateAtTransitionStart.ColorPanel, finalState.ColorPanel, effectiveLerpValue);

        Color effectiveColor = GfUiTools.BlendColors(finalState.ColorLeftText, finalState.ColorContent, finalState.ColorContentSelfBlendMode);
        m_textLeft.color = Color.Lerp(m_stateAtTransitionStart.ColorTextLeft, effectiveColor, effectiveLerpValue);

        effectiveColor = GfUiTools.BlendColors(finalState.ColorRightText, finalState.ColorContent, finalState.ColorContentSelfBlendMode);
        m_textRight.color = Color.Lerp(m_stateAtTransitionStart.ColorTextRight, effectiveColor, effectiveLerpValue);

        if (m_iconActive)
        {
            effectiveColor = GfUiTools.BlendColors(finalState.ColorIcon, finalState.ColorContent, finalState.ColorContentSelfBlendMode);
            m_iconImage.color = Color.Lerp(m_stateAtTransitionStart.ColorIcon, effectiveColor, effectiveLerpValue);
        }

        m_panelImage.pixelsPerUnitMultiplier = Mathf.Lerp(m_stateAtTransitionStart.PixelsPerUnityMult, finalState.PixelsPerUnit, effectiveLerpValue);

        Vector2 currentSize = Vector2.Lerp(m_stateAtTransitionStart.Size, finalState.PanelSizeCoef * m_mainRectTransform.rect.size, effectiveLerpValue);
        SetPanelLength(currentSize.x);
        SetPanelHeight(currentSize.y);

        m_canvasGroup.alpha = Mathf.Lerp(m_stateAtTransitionStart.GroupAlpha, finalState.GroupAlpha, effectiveLerpValue);
    }

    private static GfxPanelHightlightState ComposeHighlightState(GfxPanelHightlightState aBase, GfxPanelHightlightState aHighlight)
    {
        return new()
        {
            PanelSizeCoef = aHighlight.PanelSizeCoef * aBase.PanelSizeCoef,
            PositionOffset = aHighlight.PositionOffset + aBase.PositionOffset,

            ColorPanel = GfUiTools.BlendColors(aBase.ColorPanel, aHighlight.ColorPanel, aHighlight.ColorPanelBlendMode),
            ColorContent = GfUiTools.BlendColors(aBase.ColorContent, aHighlight.ColorContent, aHighlight.ColorContentBlendMode),

            ColorLeftText = GfUiTools.BlendColors(aBase.ColorLeftText, aHighlight.ColorLeftText, aHighlight.ColorLeftTextBlendMode),
            ColorRightText = GfUiTools.BlendColors(aBase.ColorRightText, aHighlight.ColorRightText, aHighlight.ColorRightTextBlendMode),
            ColorIcon = GfUiTools.BlendColors(aBase.ColorIcon, aHighlight.ColorIcon, aHighlight.ColorIconBlendMode),

            ColorPanelBlendMode = aHighlight.ColorPanelBlendMode,
            ColorContentBlendMode = aHighlight.ColorContentBlendMode,
            ColorContentSelfBlendMode = aHighlight.ColorContentSelfBlendMode,
            ColorLeftTextBlendMode = aHighlight.ColorLeftTextBlendMode,
            ColorRightTextBlendMode = aHighlight.ColorRightTextBlendMode,
            ColorIconBlendMode = aHighlight.ColorIconBlendMode,

            GroupAlpha = aHighlight.GroupAlpha * aBase.GroupAlpha,
            PixelsPerUnit = aHighlight.PixelsPerUnit,
        };
    }

    private void SaveCurrentTransitionState()
    {
        m_stateAtTransitionStart.ColorPanel = m_panelImage.color;
        m_stateAtTransitionStart.LocalPosition = m_panelRectTransform.localPosition;
        m_stateAtTransitionStart.ColorTextLeft = m_textLeft.color;
        m_stateAtTransitionStart.ColorTextRight = m_textRight.color;
        m_stateAtTransitionStart.ColorIcon = m_iconImage.color;
        m_stateAtTransitionStart.PixelsPerUnityMult = m_panelImage.pixelsPerUnitMultiplier;
        m_stateAtTransitionStart.GroupAlpha = m_canvasGroup.alpha;
        m_stateAtTransitionStart.Size = m_panelRectTransform.rect.size;
    }

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
            SaveCurrentTransitionState();
        }
    }

    private GfxPanelHightlightState GetHighlightedDefaultState(GfxPanelHightlightState aHighlight) { return ComposeHighlightState(m_highlightStateDefault, aHighlight); }

    private GfxPanelHightlightState GetDefaultHighlightState()
    {
        if (m_isSelected)
        {
            if (m_isPinned) return GetHighlightedDefaultState(m_highlightStatePinned);
            if (m_isDisabled) return GetHighlightedDefaultState(m_highlightStateDisabled);
        }

        return m_highlightStateDefault;
    }

    private GfxPanelHightlightState GetDesiredHighlightState()
    {
        if (m_isSelected) return m_highlightStateSelected;
        if (m_isDisabled) return m_highlightStateDisabled;
        if (m_isPinned) return m_highlightStatePinned;
        return m_highlightStateDefault;
    }

    protected GfxPanelHightlightState GetFinalHighlightState()
    {
        if (!m_isPinned && !m_isSelected && !m_isDisabled)
            return m_highlightStateDefault;

        GfxPanelHightlightState defaultState = GetDefaultHighlightState();
        GfxPanelHightlightState desiredState = GetDesiredHighlightState();

        return ComposeHighlightState(defaultState, desiredState);
    }

    public void SetHightlightStateDefault(GfxPanelHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStateDefault = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }
    public void SetHightlightStateDisabled(GfxPanelHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStateDisabled = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }
    public void SetHightlightStatePinned(GfxPanelHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStatePinned = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }
    public void SetHightlightStateSelected(GfxPanelHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStateSelected = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }
    public void SetHightlightStateSubmit(GfxPanelHightlightState aState, bool aSnapToDesiredState = false) { m_highlightStateSubmit = aState; if (aSnapToDesiredState) SnapToDesiredState(); else StartTransitionToNewState(); }

    public bool IsSelected() { return m_isSelected; }
    public GfxPanelHightlightState GetHightlightStateDefault() { return m_highlightStateDefault; }
    public GfxPanelHightlightState GetHightlightStateDisabled() { return m_highlightStateDisabled; }
    public GfxPanelHightlightState GetHightlightStateSelected() { return m_highlightStateSelected; }
    public GfxPanelHightlightState GetHightlightStateSubmit() { return m_highlightStateSubmit; }
    public GfxPanelHightlightState GetHightlightStatePinned() { return m_highlightStatePinned; }
    public bool IsPinned() { return m_isPinned; }
    public bool IsSelectedPinned() { return m_isPinned && m_isSelected; }
    public TextMeshProUGUI GetLeftText() { return m_textLeft; }
    public TextMeshProUGUI GetRightText() { return m_textRight; }
    public Image GetIconImage() { return m_iconImage; }
    public string GetDisabledReason() { return m_disabledReason; }
    public void SnapToDesiredState() { SetTransitionLerpState(1); }
    public Image GetPanelImage() { return m_panelImage; }
    public void SetIconSize(float aLength) { SetIconSize(aLength, aLength); }
    public RectTransform GetPanelRectTransform() { return m_panelRectTransform; }
    public RectTransform GetMainRectTransform() { return m_mainRectTransform; }
    public void SetContentPadding(float aPadding, bool aUpdateIconSize = true) { SetContentPadding(aPadding, aPadding, aUpdateIconSize); }
    public void SetContentPadding(float aPaddingX, float aPaddingY, bool aUpdateIconSize = true) { SetContentPadding(new Vector2(aPaddingX, aPaddingY), aUpdateIconSize); }
    public void SetIconSizeDefault() { SetIconSize(m_panelRectTransform.rect.height - m_contentPadding.y * 2); }
    public string GetInactiveReason() { return m_inactiveReason; }

    public void SetContentPadding(Vector2 aPadding, bool aUpdateIconSize = true)
    {
        m_contentPadding = aPadding;
        m_contentsRectTransform.SetOffsets(aPadding);
        if (aUpdateIconSize) SetIconSizeDefault();
        UpdatePriorityTextLength();
    }

    public void SetLength(float aLength)
    {
        m_mainRectTransform.SetSizeWithCurrentAnchorsX(aLength);
        SetPanelLength(aLength * GetFinalHighlightState().PanelSizeCoef.x);
    }

    public void SetHeight(float aHeight)
    {
        m_mainRectTransform.SetSizeWithCurrentAnchorsY(aHeight);
        SetPanelHeight(aHeight * GetFinalHighlightState().PanelSizeCoef.y);
    }

    public void SetPanelLength(float aLength)
    {
        m_panelRectTransform.SetSizeWithCurrentAnchorsX(aLength);
        UpdatePriorityTextLength();
    }

    public void SetPanelHeight(float aHeight)
    {
        m_panelRectTransform.SetSizeWithCurrentAnchorsY(aHeight);
    }

    public void UpdatePriorityTextLength()
    {
        float priorityTextFinalLength = m_priorityTextLengthRatio * m_textParentRectTransform.rect.width;
        if (m_leftTextHasPriority)
            SetLeftTextLength(priorityTextFinalLength);
        else
            SetRightTextLength(priorityTextFinalLength);
    }

    public void SetLeftTextOnly(string aString, HorizontalAlignmentOptions aXAlignment = HorizontalAlignmentOptions.Left, VerticalAlignmentOptions aYAlignment = VerticalAlignmentOptions.Middle, bool aFadeInText = true)
    {
        SetRightText(EMPTY);
        SetLeftText(aString, aXAlignment, aYAlignment, aFadeInText);
        SetLeftTextLength(m_textParentRectTransform.rect.width);
    }

    public void SetRightTextOnly(string aString, HorizontalAlignmentOptions aXAlignment = HorizontalAlignmentOptions.Right, VerticalAlignmentOptions aYAlignment = VerticalAlignmentOptions.Middle, bool aFadeInText = true)
    {
        SetLeftText(EMPTY);
        SetRightText(aString, aXAlignment, aYAlignment, aFadeInText);
        SetRightTextLength(m_textParentRectTransform.rect.width);
    }

    public void SetLeftText(string aString, HorizontalAlignmentOptions aXAlignment = HorizontalAlignmentOptions.Left, VerticalAlignmentOptions aYAlignment = VerticalAlignmentOptions.Middle, bool aFadeInText = true)
    {
        if (!m_textLeft.text.Equals(aString))
        {
            if (aFadeInText)
            {
                var color = m_textLeft.color;
                color.a = 0;
                m_textLeft.color = color;
                StartTransitionToNewState();
            }

            m_textLeft.text = aString;
        }

        m_textLeft.horizontalAlignment = aXAlignment;
        m_textLeft.verticalAlignment = aYAlignment;
    }

    public void SetRightText(string aString, HorizontalAlignmentOptions aXAlignment = HorizontalAlignmentOptions.Right, VerticalAlignmentOptions aYAlignment = VerticalAlignmentOptions.Middle, bool aFadeInText = true)
    {
        if (!m_textRight.text.Equals(aString))
        {
            if (aFadeInText)
            {
                var color = m_textRight.color;
                color.a = 0;
                m_textRight.color = color;
                StartTransitionToNewState();
            }

            m_textRight.text = aString;
        }

        m_textRight.horizontalAlignment = aXAlignment;
        m_textRight.verticalAlignment = aYAlignment;
    }

    public void RefreshPadding()
    {
        SetContentPadding(m_contentPadding);
        SetContentInPadding(m_contentInPadding);
    }

    public void SetLeftTextLengthRatio(float aRatio, bool aChangePriorityAxis = true, bool aResizeTextRight = true) { SetLeftTextLength(m_textParentRectTransform.rect.width * aRatio, aChangePriorityAxis, aResizeTextRight); }

    public void SetRightTextLengthRatio(float aRatio, bool aChangePriorityAxis = true, bool aResizeTextRight = true) { SetRightTextLength(m_textParentRectTransform.rect.width * aRatio, aChangePriorityAxis, aResizeTextRight); }

    //man I am bad at making UI code, I should ask Andrada to help me design this 
    public void SetLeftTextLength(float aLength, bool aChangeTextPriority = true, bool aResizeTextRight = true)
    {
        if (aChangeTextPriority)
        {
            m_leftTextHasPriority = true;
            m_priorityTextLengthRatio = aLength / m_textParentRectTransform.rect.width;
        }

        m_leftTextRectTransform.SetPosX(aLength * 0.5f);
        m_leftTextRectTransform.SetSizeWithCurrentAnchorsX(aLength);
        if (aResizeTextRight) SetRightTextLength(m_textParentRectTransform.rect.width - aLength - m_contentInPadding, false, false);
    }

    public void SetRightTextLength(float aLength, bool aChangePriorityAxis = true, bool aResizeTextLeft = true)
    {
        if (aChangePriorityAxis)
        {
            m_leftTextHasPriority = false;
            m_priorityTextLengthRatio = aLength / m_textParentRectTransform.rect.width;
        }

        m_rightTextRectTransform.SetPosX(-aLength * 0.5f);
        m_rightTextRectTransform.SetSizeWithCurrentAnchorsX(aLength);
        if (aResizeTextLeft) SetLeftTextLength(m_textParentRectTransform.rect.width - aLength - m_contentInPadding, false, false);
    }

    public void SetIconActive(bool aActive, bool aForceSet = false)
    {
        if (aActive != m_iconActive || aForceSet)
        {
            if (aActive)
            {
                m_textParentRectTransform.SetLeft(m_iconRectTransform.rect.width + m_contentInPadding);
            }
            else
            {
                SetIcon(null);
                SetIconColor(new Color(0, 0, 0, 0));
                m_textParentRectTransform.SetLeft(0);
            }

            UpdatePriorityTextLength();
            m_iconActive = aActive;
        }
    }

    public void SetContentInPadding(float aPadding)
    {
        m_contentInPadding = aPadding;
        UpdatePriorityTextLength();
    }

    public void SetAllPadding(float aPadding, bool aUpdateIconSize = true)
    {
        SetContentInPadding(aPadding);
        SetContentPadding(aPadding, aPadding, aUpdateIconSize);
    }

    //todo
    public void SetButtonActive(bool aActive, string aInactiveReason = EMPTY)
    {
        m_isActive = aActive;
        m_inactiveReason = aInactiveReason;
    }

    public void SetIconSize(float aLength, float aHeight, bool aActivateIcon = false)
    {
        if (aActivateIcon)
            SetIconActive(true);

        m_iconRectTransform.SetSizeWithCurrentAnchors(aLength, aHeight);
        m_iconRectTransform.SetPosX(aLength * 0.5f);
    }

    public void SetIcon(Sprite aIcon, bool aActivateIcon = false)
    {
        if (aActivateIcon)
            SetIconActive(true);
        m_iconImage.sprite = aIcon;
    }

    protected void SetIconColor(Color aColor, bool aActivateIcon = false)
    {
        if (aActivateIcon)
            SetIconActive(true);

        m_iconImage.color = aColor;
    }

    public void SetBlessed(bool aBlessed)
    {
        return;
        if (aBlessed)
            m_blessedParticleSystem.Play();
        else
            m_blessedParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetTextOnly(string aString, HorizontalAlignmentOptions aXAlignment = HorizontalAlignmentOptions.Center, VerticalAlignmentOptions aYAlignment = VerticalAlignmentOptions.Middle)
    {
        SetLeftTextOnly(aString, aXAlignment, aYAlignment);
        SetIconActive(false);
    }

    public void OnSelect(BaseEventData aEventData) { SetSelected(true); }

    public void OnPointerEnter(PointerEventData aEventData) { SetSelected(true); }

    public void OnDeselect(BaseEventData aEventData) { SetSelected(false); }

    public void OnPointerExit(PointerEventData aEventData) { if (m_deselectOnPointerExit || true) SetSelected(false); }

    public bool IsSelectable() { return m_isInteractable && (!m_isDisabled || !m_disabledReason.IsEmpty()); }
    public void Submit()
    {
        if (!m_isDisabled && m_isInteractable)
        {
            OnEventCallback?.Invoke(GfxPanelCallbackType.SUBMIT, this, true);
            m_soundSubmit?.PlaySingleInstance();
        }
    }

    public void SetSelected(bool aSelect, bool aIgnoreInteractable = false)
    {
        if (IsSelectable() && aSelect != m_isSelected && (aIgnoreInteractable || m_isInteractable))
        {
            m_isSelected = aSelect;
            StartTransitionToNewState(aSelect);
            OnEventCallback?.Invoke(GfxPanelCallbackType.SELECT, this, aSelect);

            if (aSelect)
            {
                //  EventSystem.current.SetSelectedGameObject(gameObject);
                m_soundSelect?.PlaySingleInstance();
                if (m_isDisabled) GfUiTools.WriteDisableReason(this);
            }
            else //deselected
            {
                // if (EventSystem.current.currentSelectedGameObject == gameObject)
                // EventSystem.current.
                //EventSystem.current.SetSelectedGameObject(null);

                m_soundDeselect?.PlaySingleInstance();
                if (m_isDisabled) GfUiTools.EraseDisableReason(this);
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
                OnEventCallback?.Invoke(GfxPanelCallbackType.PINNED, this, aIsPinned);

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
                OnEventCallback?.Invoke(GfxPanelCallbackType.DISABLED, this, aIsDisabled);
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

    public void SetCreateData(GfxPanelCreateData aPanelData, bool aSnapToDesiredState = false, bool aSetPositionAndIndex = true)
    {
        TransitionTime = aPanelData.TransitionTime;
        m_transitionCurve = aPanelData.TransitionCurve;

        InitializeHard();
        SetLength(aPanelData.PanelSize.x);
        SetHeight(aPanelData.PanelSize.y);
        SetContentPadding(aPanelData.Padding);
        SetContentInPadding(aPanelData.ContentInPadding);

        m_highlightStateDefault = aPanelData.DefaultHighlightState;
        m_highlightStateDisabled = aPanelData.DisabledHighlightState;
        m_highlightStateSelected = aPanelData.SelectHighlightState;
        m_highlightStateSubmit = aPanelData.SubmitHighlightState;
        m_highlightStatePinned = aPanelData.PinnedHighlightState;

        OnEventCallback = aPanelData.OnEventCallback;

        m_soundDeselect = aPanelData.SoundDeselect;
        m_soundPinned = aPanelData.SoundPinned;
        m_soundSelect = aPanelData.SoundSelect;
        m_soundSubmit = aPanelData.SoundSubmit;
        m_soundUnpinned = aPanelData.SoundUnpinned;

        if (aSetPositionAndIndex)
        {
            IndecesColumnRow = aPanelData.IndecesColumnRow;
            Index = aPanelData.Index;
            m_deselectOnPointerExit = aPanelData.DeselectOnPointerExit;

            Vector2 spawnPosition = new();

            spawnPosition.x += IndecesColumnRow.x * (aPanelData.PanelSize.x + aPanelData.DistanceFromLastPanel.x);
            spawnPosition.y += IndecesColumnRow.y * (aPanelData.PanelSize.y + aPanelData.DistanceFromLastPanel.y);

            GfcTools.Mult2(ref spawnPosition, aPanelData.SpawnAxisCoef);

            GfcTools.Add2(ref spawnPosition, aPanelData.PositionOffset);

            RectTransform panelRectTransform = GetMainRectTransform();
            if (aPanelData.Parent == null)
                Debug.LogError("The parent of the GfxPanelCreateData is null, this is most likely a mistake.");

            if (aPanelData.DistanceFromLastPanel.x <= -aPanelData.PanelSize.x
            || aPanelData.DistanceFromLastPanel.y <= -aPanelData.PanelSize.y)
                Debug.LogError("The distance from the panel is so low to the point where it goes against the spawn axis. If you wish to do this, please use the SpawnAxisCoef member of the panel create data.");

            panelRectTransform.SetParent(aPanelData.Parent);
            panelRectTransform.localPosition = spawnPosition;
        }

        transform.localScale = new Vector3(1, 1, 1);
        SetInteractable(aPanelData.IsInteractable);

        if (aSnapToDesiredState)
            SnapToDesiredState();
    }
}

[Serializable]
public struct GfxPanelHightlightState
{
    public Vector2 PanelSizeCoef;
    public Vector2 PositionOffset;
    public Color ColorPanel;
    public Color ColorContent;
    public Color ColorLeftText;
    public Color ColorRightText;
    public Color ColorIcon;
    public ColorBlendMode ColorPanelBlendMode;
    public ColorBlendMode ColorContentBlendMode;
    public ColorBlendMode ColorContentSelfBlendMode;
    public ColorBlendMode ColorLeftTextBlendMode;
    public ColorBlendMode ColorRightTextBlendMode;
    public ColorBlendMode ColorIconBlendMode;
    public float GroupAlpha;
    public float PixelsPerUnit;
}

[Serializable]
public struct GfxPanelCreateData
{
    public Sprite PanelSprite;

    public AnimationCurve TransitionCurve;

    public GfcSound SoundSelect;

    public GfcSound SoundDeselect;

    public GfcSound SoundPinned;

    public GfcSound SoundUnpinned;

    public GfcSound SoundSubmit;

    public Action<GfxPanelCallbackType, GfxPanel, bool> OnEventCallback;

    public Action<GfxPanel, bool> OnSelectEvent;

    public Action<GfxPanel> OnSubmitEvent;

    //the bool is the same as the one in OnSelectEvent
    public Action<GfxPanel, bool> OnPinnedEvent;

    public GfxPanelHightlightState DefaultHighlightState;

    public GfxPanelHightlightState DisabledHighlightState;

    public GfxPanelHightlightState SelectHighlightState;

    public GfxPanelHightlightState SubmitHighlightState;

    public GfxPanelHightlightState PinnedHighlightState;

    public Vector2 DistanceFromLastPanel;

    public Vector2Int SpawnAxisCoef;

    public Vector2 Padding;

    public Vector2 PanelSize;

    [HideInInspector] public Vector2 PositionOffset;

    [HideInInspector] public RectTransform Parent;

    [HideInInspector] public Vector2Int IndecesColumnRow;

    [HideInInspector] public int Index;

    public float ContentInPadding;

    public float TransitionTime;

    public bool IsInteractable;

    public bool DeselectOnPointerExit;
}

internal struct GfxPanelTransitionState
{
    public Vector2 LocalPosition;
    public Vector2 Size;
    public Color ColorPanel;
    public Color ColorTextLeft;
    public Color ColorTextRight;
    public Color ColorIcon;
    public float PixelsPerUnityMult;
    public float GroupAlpha;
}

public enum GfxPanelCallbackType
{
    SELECT,
    PINNED,
    DISABLED,
    SUBMIT
}