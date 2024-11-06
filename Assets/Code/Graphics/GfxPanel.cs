using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GfxPanel : GfxButton2D
{
    [SerializeField] private RectTransform m_panelRectTransform;

    [SerializeField] private Image m_iconImage;

    [SerializeField] private TextMeshProUGUI m_textLeft;

    [SerializeField] private TextMeshProUGUI m_textRight;

    [SerializeField] private RectTransform m_contentsRectTransform;

    [SerializeField] private float m_contentInPadding = 12;

    [SerializeField] private Vector2 m_contentPadding = new(12, 12);

    protected bool m_iconActive = true;

    private RectTransform m_mainRectTransform;

    private RectTransform m_leftTextRectTransform;

    private RectTransform m_textParentRectTransform;

    private RectTransform m_rightTextRectTransform;

    private GfxPanelTransitionState m_stateAtTransitionStart = default;

    private RectTransform m_iconRectTransform;

    public Vector2Int IndecesColumnRow = new();

    private Image m_panelImage;

    protected bool m_leftTextHasPriority = true;

    //The length of the priority text relative to the text parent length
    protected float m_priorityTextLengthRatio = 1;

    private void Start() { Initialize(); }
    private bool m_initializedPanel = false;

    public override void Initialize()
    {
        if (!m_initializedPanel)
        {
            m_initializedPanel = true;
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

        IndecesColumnRow.x = IndecesColumnRow.y = 0;
        base.Initialize();
    }

    protected override void SetTransitionLerpState(float aTransitionPoint, GfxButtonHightlightState aDesiredState)
    {
        base.SetTransitionLerpState(aTransitionPoint, aDesiredState);

        if (TransitionCurve != null)
            aTransitionPoint = TransitionCurve.Evaluate(aTransitionPoint);

        m_panelImage.pixelsPerUnitMultiplier = Mathf.Lerp(m_stateAtTransitionStart.PixelsPerUnityMult, aDesiredState.PixelsPerUnit, aTransitionPoint);

        Vector2 currentSize = Vector2.Lerp(m_stateAtTransitionStart.Size, m_mainRectTransform.rect.size, aTransitionPoint);
        SetPanelLength(currentSize.x);
        SetPanelHeight(currentSize.y);
    }

    protected override void OnStartTransition()
    {
        base.OnStartTransition();
        m_stateAtTransitionStart.PixelsPerUnityMult = m_panelImage.pixelsPerUnitMultiplier;
        m_stateAtTransitionStart.Size = m_panelRectTransform.rect.size;
    }

    public TextMeshProUGUI GetLeftText() { return m_textLeft; }
    public TextMeshProUGUI GetRightText() { return m_textRight; }
    public Image GetIconImage() { return m_iconImage; }
    public Image GetPanelImage() { return m_panelImage; }
    public void SetIconSize(float aLength) { SetIconSize(aLength, aLength); }
    public RectTransform GetPanelRectTransform() { return m_panelRectTransform; }
    public RectTransform GetMainRectTransform() { return m_mainRectTransform; }
    public void SetContentPadding(float aPadding, bool aUpdateIconSize = true) { SetContentPadding(aPadding, aPadding, aUpdateIconSize); }
    public void SetContentPadding(float aPaddingX, float aPaddingY, bool aUpdateIconSize = true) { SetContentPadding(new Vector2(aPaddingX, aPaddingY), aUpdateIconSize); }
    public void SetIconSizeDefault() { SetIconSize(m_panelRectTransform.rect.height - m_contentPadding.y * 2); }

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
        SetPanelLength(aLength);
    }

    public void SetHeight(float aHeight)
    {
        m_mainRectTransform.SetSizeWithCurrentAnchorsY(aHeight);
        SetPanelHeight(aHeight);
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

    public void SetIconColor(Color aColor)
    {
        m_iconImage.color = aColor;
        UpdateOriginalColors();
    }

    public void SetTextOnly(string aString, HorizontalAlignmentOptions aXAlignment = HorizontalAlignmentOptions.Center, VerticalAlignmentOptions aYAlignment = VerticalAlignmentOptions.Middle)
    {
        SetLeftTextOnly(aString, aXAlignment, aYAlignment);
        SetIconActive(false);
    }

    public void SetCreateData(GfxPanelCreateData aPanelData, bool aSnapToDesiredState = false, bool aSetPositionAndIndex = true)
    {
        SetCreateData(aPanelData.ButtonCreateData, false, aSetPositionAndIndex);
        TransitionCurve = aPanelData.TransitionCurve;

        Initialize();
        SetLength(aPanelData.PanelSize.x);
        SetHeight(aPanelData.PanelSize.y);
        SetContentPadding(aPanelData.Padding);
        SetContentInPadding(aPanelData.ContentInPadding);

        if (aSetPositionAndIndex)
        {
            IndecesColumnRow = aPanelData.IndecesColumnRow;

            Vector2 spawnPosition = new();

            spawnPosition.x += IndecesColumnRow.x * (aPanelData.PanelSize.x + aPanelData.DistanceFromLastPanel.x);
            spawnPosition.y += IndecesColumnRow.y * (aPanelData.PanelSize.y + aPanelData.DistanceFromLastPanel.y);

            GfcTools.Mult(ref spawnPosition, aPanelData.SpawnAxisCoef);

            GfcTools.Add(ref spawnPosition, aPanelData.PositionOffset);

            RectTransform panelRectTransform = GetMainRectTransform();

            if (aPanelData.DistanceFromLastPanel.x <= -aPanelData.PanelSize.x
            || aPanelData.DistanceFromLastPanel.y <= -aPanelData.PanelSize.y)
                Debug.LogError("The distance from the panel is so low to the point where it goes against the spawn axis. If you wish to do this, please use the SpawnAxisCoef member of the panel create data.");

            panelRectTransform.localPosition = spawnPosition;
        }

        transform.localScale = new Vector3(1, 1, 1);

        if (aSnapToDesiredState)
            SnapToDesiredState();
    }
}

[Serializable]
public struct GfxButtonHightlightState
{
    public Vector3 Scale;
    public Vector3 PositionOffset;
    public Color ColorPanel;
    public Color ColorContent;

    public ColorBlendMode ColorPanelBlendMode;
    public ColorBlendMode ColorContentBlendMode;

    public float Opacity;
    public float PixelsPerUnit;
}

[Serializable]
public struct GfxPanelCreateData
{
    public GfxButtonCreateData ButtonCreateData;

    public Sprite PanelSprite;

    public AnimationCurve TransitionCurve;

    public Vector2 DistanceFromLastPanel;

    public Vector2Int SpawnAxisCoef;

    public Vector2 Padding;

    public Vector2 PanelSize;

    [HideInInspector] public Vector2 PositionOffset;

    [HideInInspector] public Vector2Int IndecesColumnRow;

    public float ContentInPadding;
}

internal struct GfxPanelTransitionState
{
    public Vector2 Size;
    public float PixelsPerUnityMult;
}