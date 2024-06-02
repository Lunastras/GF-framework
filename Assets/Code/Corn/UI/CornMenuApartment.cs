using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CornMenuApartment : MonoBehaviour
{
    public static CornMenuApartment Instance { get; protected set; } = null;

    public Transform CameraTarget;

    [SerializeField] protected Transform m_light;

    [SerializeField] protected RectTransform m_optionsParent;

    [SerializeField] protected TextMeshProUGUI m_textMoney;

    [SerializeField] protected TextMeshProUGUI m_textDateAndTime;

    [SerializeField] protected TextMeshProUGUI m_textSanity;

    [SerializeField] protected Transform m_parentConsumableSlider;

    [SerializeField] protected Transform m_parentButtonAction;

    [SerializeField] protected GameObject m_prefabConsumableSlider;
    [SerializeField] protected GameObject m_prefabButtonAction;
    [SerializeField] protected ResourceButtonInfo[] m_actionButtonsInfo;

    // [SerializeField] protected ConsumablesSlidersLink[] m_slidersConsumables;

    [SerializeField] protected CornActionButton m_buttonSleep;

    [SerializeField] protected Vector2 m_consumablesSlidersDistance;

    private CornActionButton[] m_actionButtons;

    private GfxSliderText[] m_consumablesSliders;

    // Start is called before the first frame update

    void Awake()
    {
        if (Instance != this) Destroy(Instance);
        Instance = this;

        for (int i = 0; i < m_actionButtonsInfo.Length; ++i)
            Debug.Assert(i == (int)m_actionButtonsInfo[i].Type, "The type " + m_actionButtonsInfo[i].Type + " is at index " + i + ", it should be at index " + (int)m_actionButtonsInfo[i].Type);

        Debug.Assert(m_actionButtonsInfo.Length == (int)PlayerResources.COUNT, "The list only has " + m_actionButtonsInfo.Length + " elements, it should have " + (int)PlayerResources.COUNT);

        /*
        for (int i = 0; i < m_slidersConsumables.Length; ++i)
            Debug.Assert(i == (int)m_slidersConsumables[i].Type, "The type " + m_slidersConsumables[i].Type + " is at index " + i + ", it should be at index " + (int)m_slidersConsumables[i].Type);


        Debug.Assert(m_slidersConsumables.Length == (int)PlayerConsumables.COUNT - PlayerSaveData.NON_0_TO_100_CONSUMABLES, "The list only has " + m_slidersConsumables.Length + " elements, it should have " + (int)PlayerConsumables.COUNT);
*/
        GfcManagerGame.InitializeGfBase();
    }

    void Start()
    {
        m_actionButtons = new CornActionButton[PlayerSaveData.COUNT_RESOURCES];
        m_consumablesSliders = new GfxSliderText[PlayerSaveData.COUNT_0_TO_100_CONSUMABLES];

        for (int i = 0; i < PlayerSaveData.COUNT_RESOURCES; ++i)
        {
            m_actionButtons[i] = Instantiate(m_prefabButtonAction, m_parentButtonAction, false).GetComponent<CornActionButton>();
            m_actionButtons[i].Initialize();

            m_actionButtons[i].WorldPointOfReference = m_actionButtonsInfo[i].WorldReferencePoint;
            m_actionButtons[i].SetPanelColor(m_actionButtonsInfo[i].ColorPanel, ColorBlendMode.MULTIPLY);
            m_actionButtons[i].SetContentColor(m_actionButtonsInfo[i].ColorContent, ColorBlendMode.MULTIPLY);
            m_actionButtons[i].SliderText.SetName(((PlayerResources)i).ToString());
            m_actionButtons[i].Button.Index = i;
            m_actionButtons[i].Button.OnButtonEventCallback += OnButtonEvent;
        }

        m_buttonSleep.Initialize();

        m_buttonSleep.SliderText.SetName(CornEventType.SLEEP.ToString());
        m_buttonSleep.Button.Index = (int)CornEventType.SLEEP;
        m_buttonSleep.Button.OnButtonEventCallback += OnButtonEvent;

        for (int i = 0; i < PlayerSaveData.COUNT_0_TO_100_CONSUMABLES; ++i)
        {
            m_consumablesSliders[i] = Instantiate(m_prefabConsumableSlider, m_parentConsumableSlider, false).GetComponent<GfxSliderText>();
            m_consumablesSliders[i].transform.localPosition = m_consumablesSlidersDistance * i;
            m_consumablesSliders[i].SetName(((PlayerConsumables)i).ToString());
        }

        UpdateGraphicsInternal();
    }

    public static void UpdateGraphics() { Instance.UpdateGraphicsInternal(); }

    private void UpdateGraphicsInternal()
    {
        var saveData = GfgManagerSaveData.GetActivePlayerSaveData();
        float[] playerResources = saveData.Resources;
        float[] playerConsumables = saveData.Consumables;

        for (int i = 0; i < PlayerSaveData.COUNT_RESOURCES; ++i)
        {
            m_actionButtons[i].SliderText.SetSliderValue(playerResources[i]);
            m_actionButtons[i].Button.SetDisabled(!CornManagerEvents.CanAfford(new CornEvent((CornEventType)i)), "You do not have the requirements needed for this action");
        }

        for (int i = 0; i < PlayerSaveData.COUNT_0_TO_100_CONSUMABLES; ++i)
        {
            m_consumablesSliders[i].SetSliderValue(playerConsumables[i]);
        }

        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;

        stringBuffer.Concatenate(playerConsumables[(int)PlayerConsumables.MONEY], 0);
        stringBuffer.Concatenate('$');

        m_textMoney.text = stringBuffer.GetStringCopy();
        m_textSanity.text = saveData.MentalSanity.ToString();
        stringBuffer.Clear();

        if (saveData.CurrentHour < 10)
            stringBuffer.Concatenate('0');

        stringBuffer.Concatenate(saveData.CurrentHour);
        stringBuffer.Concatenate(":00   ");
        stringBuffer.Concatenate(GfcLocalization.GetDateString(saveData.CurrentDay, saveData.CurrentMonth));

        m_textDateAndTime.text = stringBuffer.GetStringCopy();

        stringBuffer.Clear();


        m_light.rotation = Quaternion.AngleAxis(360.0f * (saveData.CurrentHour / 24.0f) - 90, Vector3.right);
    }

    public static void EndStatsPreview() { Instance.EndStatsPreviewInternal(); }

    public void EndStatsPreviewInternal()
    {
        for (int i = 0; i < PlayerSaveData.COUNT_RESOURCES; ++i)
            m_actionButtons[i].SliderText.EndPreview();

        for (int i = 0; i < PlayerSaveData.COUNT_0_TO_100_CONSUMABLES; ++i)
            m_consumablesSliders[i].EndPreview();
    }

    public void PreviewChange(PlayerResourcesModifier aModifier, float aChange)
    {
        m_actionButtons[(int)aModifier.Type].SliderText.PreviewChange(aChange);
    }

    public void PreviewChange(PlayerConsumablesModifier aModifier, float aChange)
    {
        if ((int)aModifier.Type < m_consumablesSliders.Length)
            m_consumablesSliders[(int)aModifier.Type].PreviewChange(aChange);
    }

    private void OnButtonEvent(GfxButtonCallbackType aType, GfxButton aButton, bool aState)
    {
        CornEventType eventType = (CornEventType)aButton.Index;

        switch (aType)
        {
            case GfxButtonCallbackType.SELECT:
                if (aState && !aButton.IsDisabled())
                {
                    CornManagerBalancing.GetEventCostAndRewards(eventType).Preview();
                }
                else
                    EndStatsPreviewInternal();

                break;

            case GfxButtonCallbackType.SUBMIT:
                CornManagerEvents.ExecuteEvent(new(eventType));
                break;
        }
    }

    public void PressedWork()
    {
        CornManagerEvents.ExecuteEvent(new(CornEventType.WORK));
    }

    public void PressedSleep()
    {
        CornManagerEvents.ExecuteEvent(new(CornEventType.SLEEP));
    }

    public void PressedSocial()
    {
        CornManagerEvents.ExecuteEvent(new(CornEventType.SOCIAL));
    }

    public void PressedChores()
    {
        CornManagerEvents.ExecuteEvent(new(CornEventType.CHORES));
    }

    public void PressedPersonal()
    {
        CornManagerEvents.ExecuteEvent(new(CornEventType.PERSONAL_TIME));
    }
}

[System.Serializable]
public struct ResourceButtonInfo
{
    public Transform WorldReferencePoint;
    public PlayerResources Type;

    public Color ColorPanel;

    public Color ColorContent;
}

/*
[System.Serializable]
public struct ConsumablesSlidersLink
{
    public GfxSliderText SliderText;
    public PlayerConsumables Type;
}*/
