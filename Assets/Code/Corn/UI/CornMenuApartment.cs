using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CornMenuApartment : MonoBehaviour
{
    public static CornMenuApartment Instance { get; protected set; } = null;

    [SerializeField] protected bool m_showConsumableSliders = false;
    public Transform CameraTarget;
    [SerializeField] protected Transform m_lightParent;
    [SerializeField] protected Light m_lightSun;
    [SerializeField] protected Light m_lightMoon;

    [SerializeField] protected TextMeshProUGUI m_textMoney;
    [SerializeField] protected TextMeshProUGUI m_textDateAndTime;
    [SerializeField] protected TextMeshProUGUI m_textSanity;
    [SerializeField] protected Transform m_parentConsumableSlider;
    [SerializeField] protected Transform m_parentButtonAction;
    [SerializeField] protected GameObject m_prefabConsumableSlider;
    [SerializeField] protected GameObject m_prefabButtonAction;
    [SerializeField] protected ResourceButtonInfo[] m_actionButtonsInfo;
    [SerializeField] protected Vector2 m_consumablesSlidersDistance;

    [SerializeField] protected Transform m_personalNeedsOptions;
    [SerializeField] protected Transform m_personalNeedsStudyOptions;
    [SerializeField] protected Transform m_shopItemsParent;

    private CornActionButton[] m_actionButtons;
    private GfxSliderText[] m_consumablesSliders = null;

    private Color m_sunColor;
    private Color m_moonColor;

    private LightShadows m_sunShadowMode;
    private LightShadows m_moonShadowMode;

    bool[] m_instantiatedShopItems;

    // Start is called before the first frame update

    void Awake()
    {
        if (Instance != this) Destroy(Instance);
        Instance = this;

        m_instantiatedShopItems = new bool[(int)CornShopItem.COUNT];
        for (int i = 0; i < m_actionButtonsInfo.Length; ++i)
            if (i != (int)m_actionButtonsInfo[i].Type) Debug.LogError("The type " + m_actionButtonsInfo[i].Type + " is at index " + i + ", it should be at index " + (int)m_actionButtonsInfo[i].Type);

        if (m_actionButtonsInfo.Length != (int)CornPlayerAction.COUNT) Debug.LogError("The list has " + m_actionButtonsInfo.Length + " elements, it should have " + (int)CornPlayerAction.COUNT);

        m_sunColor = m_lightSun.color;
        m_moonColor = m_lightMoon.color;

        m_sunShadowMode = m_lightSun.shadows;
        m_moonShadowMode = m_lightMoon.shadows;

        /*
        for (int i = 0; i < m_slidersConsumables.Length; ++i)
            Debug.Assert(i == (int)m_slidersConsumables[i].Type, "The type " + m_slidersConsumables[i].Type + " is at index " + i + ", it should be at index " + (int)m_slidersConsumables[i].Type);
        Debug.Assert(m_slidersConsumables.Length == (int)PlayerConsumables.COUNT - PlayerSaveData.NON_0_TO_100_CONSUMABLES, "The list only has " + m_slidersConsumables.Length + " elements, it should have " + (int)PlayerConsumables.COUNT);
*/
        GfcBase.InitializeGfBase();
    }

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;

        m_actionButtons = new CornActionButton[m_actionButtonsInfo.Length];
        if (m_showConsumableSliders)
            m_consumablesSliders = new GfxSliderText[GfgPlayerSaveData.COUNT_0_TO_100_CONSUMABLES];

        for (int i = 0; i < m_actionButtonsInfo.Length; ++i)
        {
            m_actionButtons[i] = Instantiate(m_prefabButtonAction, m_parentButtonAction, false).GetComponent<CornActionButton>();
            m_actionButtons[i].Initialize();

            m_actionButtons[i].WorldPointOfReference = m_actionButtonsInfo[i].WorldReferencePoint;
            m_actionButtons[i].SetPanelColor(m_actionButtonsInfo[i].ColorPanel, ColorBlendMode.MULTIPLY);
            m_actionButtons[i].SetContentColor(m_actionButtonsInfo[i].ColorContent, ColorBlendMode.MULTIPLY);
            m_actionButtons[i].SliderText.SetName(((CornPlayerAction)i).ToString());
            m_actionButtons[i].Button.Index = i;
            m_actionButtons[i].Button.OnButtonEventCallback += OnButtonEvent;
        }

        for (int i = 0; i < m_consumablesSliders.LengthSafe(); ++i)
        {
            m_consumablesSliders[i] = Instantiate(m_prefabConsumableSlider, m_parentConsumableSlider, false).GetComponent<GfxSliderText>();
            m_consumablesSliders[i].transform.localPosition = m_consumablesSlidersDistance * i;
            m_consumablesSliders[i].SetName(((CornPlayerConsumables)i).ToString());
        }

        if (!CornManagerEvents.ExecutingEvent) //done for the initial load of the apartment
            CornManagerPhone.LoadAvailableStoryScenes();

        GfxButton personalNeeds = m_actionButtons[(int)CornPlayerAction.PERSONAL_NEEDS].Button;
        personalNeeds.Submitable = false;
        personalNeeds.HideInteractablePrompt = true;

        m_personalNeedsOptions.SetParent(m_actionButtons[(int)CornPlayerAction.PERSONAL_NEEDS].Button.VisualsParent);
        m_personalNeedsOptions.localPosition = Vector3.zero;
        m_personalNeedsOptions.localScale = new(0.833333f, 0.833333f, 0.833333f); //hack, buttons have a scale of 1.2 when selected, 0.8333 offsets that and resets it to 1
        m_personalNeedsOptions.gameObject.SetActive(false);

        m_personalNeedsOptions.InitChildrenGfxButtons(OnPersonalNeedsEvent, (int)CornPersonalNeedsAction.COUNT);
        m_personalNeedsOptions.GetChild((int)CornPersonalNeedsAction.STUDY).GetComponent<GfxButton>().Submitable = false;

        m_personalNeedsStudyOptions.InitChildrenGfxButtons(OnStudyEvent, (int)CornPlayerSkillsStats.COMFORT);//comfort is the first skill stat that is not on the study list. which is why its used as the count
        m_personalNeedsStudyOptions.gameObject.SetActive(false);

        GfgManagerSaveData.GetActivePlayerSaveData().FinishedStartCutscene = true;
        UpdateGraphicsInternal();
    }

    public static void UpdateGraphics(bool anInstanceCanBeNull = false)
    {
        Debug.Assert(Instance || anInstanceCanBeNull);
        if (Instance) Instance.UpdateGraphicsInternal();
    }

    private void UpdateGraphicsInternal()
    {
        var saveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        float[] playerResources = saveData.Resources;
        float[] playerConsumables = saveData.Consumables;

        for (int i = 0; i < m_actionButtons.Length; ++i)
        {
            float value;
            if (i < (int)CornPlayerAction.SLEEP)
                value = playerResources[i];
            else
                value = playerConsumables[(int)CornPlayerConsumables.ENERGY];//HACK but who cares what is in the Corn project assembly, this will be thrown out after the game release.

            m_actionButtons[i].SliderText.SetSliderValue(value);
            m_actionButtons[i].Button.SetInteractable(CornManagerEvents.CanAffordMoney(new CornEvent((CornEventType)i)), "You do not have enough money for this action");
        }

        /*m_buttonSleep.Button.SetInteractable(saveData.CurrentHour >= CornManagerBalancing.EARLIEST_SLEEP_HOUR
                                             || saveData.CurrentHour < CornManagerBalancing.LATEST_WAKE_UP_HOUR, "It's too early to go sleep");*/ //should probably remove

        for (int i = 0; i < m_consumablesSliders.LengthSafe(); ++i)
            m_consumablesSliders[i].SetSliderValue(playerConsumables[i]);

        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;

        stringBuffer.Append(playerConsumables[(int)CornPlayerConsumables.MONEY], 0);
        stringBuffer.Append('$');

        m_textMoney.text = stringBuffer.GetStringCopy();
        m_textSanity.text = saveData.MentalSanity.ToString();
        stringBuffer.Clear();

        if (saveData.CurrentHour < 10)
            stringBuffer.Append('0');

        stringBuffer.Append(saveData.CurrentHour);
        stringBuffer.Append(":00   ");
        stringBuffer.Append(GfcLocalization.GetDateString(saveData.CurrentDay + 1, saveData.CurrentMonth));

        m_textDateAndTime.text = stringBuffer.GetStringCopy();

        stringBuffer.Clear();

        m_lightParent.rotation = Quaternion.AngleAxis(360.0f * (saveData.CurrentHour / 24.0f) - 90, Vector3.right);

        bool nightLight = saveData.CurrentHour >= 20 || saveData.CurrentHour < 6;

        if (nightLight)
        {
            m_lightSun.color = Color.black;
            m_lightSun.shadows = LightShadows.None;

            m_lightMoon.color = m_moonColor;
            m_lightMoon.shadows = m_moonShadowMode;
        }
        else
        {
            m_lightSun.color = m_sunColor;
            m_lightSun.shadows = m_sunShadowMode;

            m_lightMoon.color = Color.black;
            m_lightMoon.shadows = LightShadows.None;
        }

        foreach (CornShopItemPurchased purchasedData in saveData.PurchasedItems)
        {
            if (purchasedData.Arrived && false == m_instantiatedShopItems[(int)purchasedData.Item])
            {
                CornShopItemsData itemData = CornManagerBalancing.GetShopItemData(purchasedData.Item);
                if (itemData.Prefab)
                {
                    m_instantiatedShopItems[(int)purchasedData.Item] = true;
                    Instantiate(itemData.Prefab).transform.SetParent(m_shopItemsParent);
                    string prefabName = itemData.Prefab.name;
                    foreach (Transform child in m_shopItemsParent)
                    {
                        if (child.name == prefabName)
                        {
                            Destroy(child.gameObject);
                            break;
                        }
                    }
                }
                else
                    Debug.LogError("The prefab for shop item " + purchasedData.Item + " is null.");
            }
        }

        CornManagerShop.UpdateCanAffordButtons();
    }

    public static void EndStatsPreview() { Instance.EndStatsPreviewInternal(); }

    public void EndStatsPreviewInternal()
    {
        for (int i = 0; i < m_actionButtons.Length; ++i)
            m_actionButtons[i].SliderText.EndPreview();

        for (int i = 0; i < m_consumablesSliders.LengthSafe(); ++i)
            m_consumablesSliders[i].EndPreview();
    }

    public void PreviewChange(CornPlayerResources aModifier, float aChange)
    {
        m_actionButtons[(int)aModifier].SliderText.PreviewChange(aChange);
    }

    public void PreviewChange(CornPlayerConsumables aModifier, float aChange)
    {
        if (aModifier == CornPlayerConsumables.MONEY)
        {
            if ((int)aModifier < m_consumablesSliders.LengthSafe())
                m_consumablesSliders[(int)aModifier].PreviewChange(aChange);
        }
        else if (aModifier == CornPlayerConsumables.ENERGY)
        {
            m_actionButtons[(int)CornPlayerAction.SLEEP].SliderText.PreviewChange(aChange); //hack, but I will probably never change it and this is game specific code so idgaf
        }
        else Debug.LogError("Attempting to preview consumable modifier of type " + aModifier + ", but the current design only supports " + CornPlayerConsumables.MONEY + " and " + CornPlayerConsumables.ENERGY);
    }

    private void OnStudyEvent(GfxButtonCallbackType aType, GfxButton aButton, bool aState)
    {
        if (aType == GfxButtonCallbackType.SUBMIT)
            CornManagerEvents.ExecuteEvent(new(CornEventType.STUDY, (uint)aButton.Index));
    }

    private void OnPersonalNeedsEvent(GfxButtonCallbackType aType, GfxButton aButton, bool aState)
    {
        CornPersonalNeedsAction personalNeedsAction = (CornPersonalNeedsAction)aButton.Index;

        switch (aType)
        {
            case GfxButtonCallbackType.SELECT:

                if (personalNeedsAction != CornPersonalNeedsAction.SHOP)
                {
                    CornEventType eventType = CornEventType.PERSONAL_TIME;
                    if (personalNeedsAction == CornPersonalNeedsAction.STUDY)
                    {
                        eventType = CornEventType.STUDY;
                        m_personalNeedsStudyOptions.SetActiveGf(aState);
                    }

                    if (aState && aButton.Interactable())
                        CornManagerBalancing.GetEventCostAndRewardsRaw(eventType).Preview();

                    if (!aState)
                        EndStatsPreviewInternal();
                }

                break;

            case GfxButtonCallbackType.SUBMIT:

                switch (personalNeedsAction)
                {
                    case CornPersonalNeedsAction.REST:
                        CornManagerEvents.ExecuteEvent(new(CornEventType.PERSONAL_TIME));
                        break;

                    case CornPersonalNeedsAction.SHOP:
                        GfgManagerGame.SetGameState(GfcGameState.SHOP);
                        break;
                }

                aButton.SetSelected(false);
                aButton.SnapToDesiredState();
                break;
        }
    }

    private void OnButtonEvent(GfxButtonCallbackType aType, GfxButton aButton, bool aState)
    {
        CornEventType eventType = (CornEventType)aButton.Index;

        switch (aType)
        {
            case GfxButtonCallbackType.SELECT:
                if (eventType == CornEventType.PERSONAL_TIME)
                {
                    m_personalNeedsOptions.SetActiveGf(aState);
                    if (!aState)
                        m_personalNeedsStudyOptions.gameObject.SetActive(false);

                    m_actionButtons[(int)CornPlayerAction.PERSONAL_NEEDS].Button.DeselectOnEvenSystemDeselect = !aState;
                }
                else if (aState && aButton.Interactable())
                    CornManagerBalancing.GetEventCostAndRewardsRaw(eventType).Preview();

                if (!aState)
                    EndStatsPreviewInternal();

                break;

            case GfxButtonCallbackType.SUBMIT:
                switch (eventType)
                {
                    case CornEventType.SOCIAL:
                        CornManagerPhone.TogglePhone();
                        break;
                    default:
                        CornManagerEvents.ExecuteEvent(new(eventType));
                        break;
                }
                break;
        }
    }
}

[System.Serializable]
public struct ResourceButtonInfo
{
    public Transform WorldReferencePoint;
    public CornPlayerAction Type;

    public Color ColorPanel;

    public Color ColorContent;
}

public enum CornPersonalNeedsAction
{
    REST,
    STUDY,
    SHOP,
    COUNT
}