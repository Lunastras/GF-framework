using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using System.Linq;

public class GfxWeaponsScreen : MonoBehaviour
{
    [SerializeField] protected RectTransform m_equippedWeaponsParent;

    [SerializeField] protected RectTransform m_ownedWeaponsParent;

    [SerializeField] protected RectTransform m_statsWeaponsParent;

    [SerializeField] protected int m_maxWeaponCountShown = 7;

    [SerializeField] protected TextMeshProUGUI m_tierText;

    [SerializeField] protected TextMeshProUGUI m_pagingText;

    [SerializeField] protected Vector2Int m_inventoryDisplayedColumnsAndRows = new(6, 5);

    protected const string NO_WEAPON = "NONE";

    protected const string REMOVE = "REMOVE";

    List<OwnedWeaponDisplayData> m_ownedWeaponsList = new(16);

    List<GfxPanel> m_panelsOwnedList = new(7);

    List<GfxPanel> m_panelsEquippedList = new(GfcManagerSaveData.MAX_EQUIPPED_WEAPONS);

    List<GfxPanel> m_panelsStatsList = new(2);

    protected int m_tierInitializedForWeaponsList = -1;

    //tier 0 are all tiers
    protected int m_shownTier = 0;

    protected int m_currentShownPage = -1;

    protected int m_selectedEquippedIndex = -1;

    protected int m_selectedOwnedIndex = -1;

    protected int m_startIndexOwnedWeapons = 0;

    private Action<GfxPanelCallbackType, GfxPanel, bool> m_onEventOwned;

    private Action<GfxPanelCallbackType, GfxPanel, bool> m_onEventEquipped;

    private bool m_initialized = false;

    private void Initialize()
    {
        if (!m_initialized)
        {
            m_onEventEquipped += OnEventEquipped;
            m_onEventOwned += OnEventOwned;

            GfcPooling.DestroyChildren(m_equippedWeaponsParent);
            GfcPooling.DestroyChildren(m_ownedWeaponsParent);
            GfcPooling.DestroyChildren(m_statsWeaponsParent);

            m_initialized = true;
        }
    }

    // Start is called before the first frame update
    private void OnEnable()
    {
        Initialize();
        DrawStatsWeapons();
        UnpinSelectedEquippedWeapon();
        UnpinSelectedOwnedWeapon();
        DrawElements();
    }

    private void OnDisable()
    {
        m_ownedWeaponsList.Clear();
        m_tierInitializedForWeaponsList = -1;
        m_currentShownPage = -1;
    }

    private void DrawElements()
    {
        DrawEquippedWeapons();
        DrawOwnedWeaponsAtPage(m_currentShownPage);
    }

    private void SetShownTier(int aTier)
    {
        aTier %= GfgManagerWeapons.MAX_TIER + 1;
        m_shownTier = aTier;
        DrawOwnedWeaponsAtPage(0);
    }

    private void UpdateWeaponsList()
    {
        m_ownedWeaponsList.Clear();
        bool allTiers = m_shownTier == 0;
        int tierIndex = m_shownTier - 1; //tiers for weapons start from 0
        PlayerSaveData saveData = GfcManagerSaveData.GetActivePlayerSaveData();
        List<WeaponOwnedData> ownedWeapons = saveData.GetOwnedWeapons();
        List<int> equippedWeaponsIndeces = saveData.GetEquippedWeapons();

        int countWeapons = ownedWeapons != null ? ownedWeapons.Count : 0;

        for (int i = 0; i < countWeapons; ++i)
        {
            if (ownedWeapons[i].Tier == tierIndex || allTiers)
            {
                WeaponOwnedData data = ownedWeapons[i];
                data.CountInInventory = saveData.CountAvailableOwnedWeaponAt(i);

                if (data.CountInInventory > 0)
                    m_ownedWeaponsList.Add(new(i, data));
            }
        }

        ownedWeapons.OrderBy(x => x.Tier);//.ThenBy(x => x.Blessed).ThenBy(x => x.GetEffectiveName());
        m_tierInitializedForWeaponsList = m_shownTier;
    }

    private void DrawStatsWeapons()
    {
        GfxPanelCreateData panelCreateData = GfUiTools.GetDefaultPanelCreateData();
        panelCreateData.IsInteractable = false;

        GfUiTools.CreatePanelList(m_statsWeaponsParent, panelCreateData, m_panelsStatsList, Axis.VERTICAL, 2, false, true, AlignmentHorizontal.MIDDLE, AlignmentVertical.BOTTOM);
        for (int i = 0; i < m_panelsStatsList.Count; ++i)
            m_panelsStatsList[i].SetTextOnly(NO_WEAPON);
    }

    private void DrawEquippedWeapons(bool aSnapPanelsToDesiredState = false)
    {
        PlayerSaveData saveData = GfcManagerSaveData.GetActivePlayerSaveData();
        List<WeaponOwnedData> ownedWeapons = saveData.GetOwnedWeapons();
        List<int> equippedWeaponsIndeces = saveData.GetEquippedWeapons();
        UnpinSelectedEquippedWeapon();

        GfxPanelCreateData panelCreateData = GfUiTools.GetDefaultPanelCreateData();
        panelCreateData.OnEventCallback = m_onEventEquipped;

        GfUiTools.CreatePanelList(m_equippedWeaponsParent, panelCreateData, m_panelsEquippedList, Axis.VERTICAL, GfcManagerSaveData.MAX_EQUIPPED_WEAPONS, aSnapPanelsToDesiredState, true, AlignmentHorizontal.MIDDLE, AlignmentVertical.BOTTOM);

        for (int i = 0; i < GfcManagerSaveData.MAX_EQUIPPED_WEAPONS; ++i)
        {
            GfxPanel panel = m_panelsEquippedList[i];
            if (i >= equippedWeaponsIndeces.Count)
                panel.SetTextOnly(NO_WEAPON);
            else //i have a weapon at index i
                panel.SetWeaponData(ownedWeapons[equippedWeaponsIndeces[i]], false, true, false);
        }
    }

    private void DrawOwnedWeaponsAtPage(int aPage = -1, bool aSnapPanelsToDesiredState = false)
    {
        UpdateWeaponsList();
        UnpinSelectedOwnedWeapon();

        int numberOfPages = 1 + Math.Max(0, (m_ownedWeaponsList.Count - 1) / m_maxWeaponCountShown);
        m_currentShownPage = Math.Clamp(aPage, 0, numberOfPages - 1);
        m_startIndexOwnedWeapons = m_maxWeaponCountShown * m_currentShownPage;

        var stringBuffer = GfcPooling.GfcStringBuffer;
        stringBuffer.Concatenate(m_currentShownPage + 1);
        stringBuffer.Concatenate('/');
        stringBuffer.Concatenate(numberOfPages);
        m_pagingText.text = stringBuffer.GetStringClone();
        stringBuffer.Clear();

        GfxPanelCreateData panelCreateData = GfUiTools.GetDefaultPanelCreateData(MovementDisableOptions.REMOVE_ALL);
        panelCreateData.OnEventCallback = m_onEventOwned;
        panelCreateData.Parent = m_ownedWeaponsParent;

        GfxPanel removePanel = GfUiTools.CreatePanelWithCache(panelCreateData, m_panelsOwnedList, aSnapPanelsToDesiredState);
        removePanel.SetTextOnly(REMOVE);

        if (GfcManagerSaveData.GetActivePlayerSaveData().GetEquippedWeapons().Count == 1)
        {
            removePanel.SetDisabled(true, "Cannot remove the last weapon");
        }

        int weaponsToShow = Math.Min(m_ownedWeaponsList.Count - m_startIndexOwnedWeapons, m_maxWeaponCountShown);

        panelCreateData.PanelSize.x = panelCreateData.PanelSize.y;
        panelCreateData.PositionOffset = new(0, -(panelCreateData.PanelSize.y * 0.5f + panelCreateData.DistanceFromLastPanel.y));
        panelCreateData.Index = 1; //0 is already the remove panel, so we use an offset

        GfUiTools.CreatePanelMatrix(panelCreateData, m_panelsOwnedList, m_inventoryDisplayedColumnsAndRows, aSnapPanelsToDesiredState, true, AlignmentHorizontal.MIDDLE, AlignmentVertical.BOTTOM);

        for (int i = 1; i < m_panelsOwnedList.Count; ++i)
        {
            GfxPanel panel = m_panelsOwnedList[i];

            if (i <= weaponsToShow)
            {
                panel.SetWeaponData(m_ownedWeaponsList[i + m_startIndexOwnedWeapons - 1].WeaponOwnedData, true, false, false);
            }
            else
            {
                panel.SetTextOnly("");
                panel.SetDisabled(true);
            }
        }
    }

    private void PinSelectedEquippedWeapon(int aPanelIndex)
    {
        m_selectedEquippedIndex = aPanelIndex;
        m_panelsEquippedList[aPanelIndex].SetPinned(true);
    }

    private void PinSelectedOwnedWeapon(int aPanelIndex)
    {
        m_selectedOwnedIndex = aPanelIndex;
        m_panelsOwnedList[aPanelIndex].SetPinned(true);
    }

    private void UnpinSelectedEquippedWeapon()
    {
        if (m_selectedEquippedIndex >= 0 && m_selectedEquippedIndex < m_panelsEquippedList.Count)
            m_panelsEquippedList[m_selectedEquippedIndex].SetPinned(false);
        m_selectedEquippedIndex = -1;
    }

    private void UnpinSelectedOwnedWeapon()
    {
        if (m_selectedOwnedIndex >= 0 && m_selectedOwnedIndex < m_panelsOwnedList.Count)
            m_panelsOwnedList[m_selectedOwnedIndex].SetPinned(false);
        m_selectedOwnedIndex = -1;
    }

    private void OnEventEquipped(GfxPanelCallbackType aEventType, GfxPanel aPanel, bool aStatus)
    {
        switch (aEventType)
        {
            case GfxPanelCallbackType.SELECT:
                ChangeStatsPanelState(0, aStatus, aPanel.Index, false);
                break;

            case GfxPanelCallbackType.PINNED:
                ChangeStatsPanelState(1, aStatus, aPanel.Index, false);
                break;

            case GfxPanelCallbackType.SUBMIT:
                int equippedWeaponPanelIndex = aPanel.Index;
                if (m_selectedEquippedIndex != -1 && m_selectedEquippedIndex != equippedWeaponPanelIndex) //flip weapons
                {
                    PlayerSaveData saveData = GfcManagerSaveData.GetActivePlayerSaveData();
                    var equippedWeapons = saveData.GetEquippedWeapons();
                    if (equippedWeapons.Count > m_selectedEquippedIndex && equippedWeapons.Count > equippedWeaponPanelIndex) //can't flip a weapon with a 'none' panel
                    {
                        int otherWeaponIndex = equippedWeapons[m_selectedEquippedIndex];
                        saveData.EquipWeapon(m_selectedEquippedIndex, equippedWeapons[equippedWeaponPanelIndex]);
                        saveData.EquipWeapon(equippedWeaponPanelIndex, otherWeaponIndex);

                        DrawEquippedWeapons();
                    }
                    else
                    {
                        UnpinSelectedEquippedWeapon();
                        PinSelectedEquippedWeapon(equippedWeaponPanelIndex);
                    }
                }
                else if (m_selectedEquippedIndex != equippedWeaponPanelIndex)
                {
                    PinSelectedEquippedWeapon(equippedWeaponPanelIndex);
                    ResolveWeaponEquip();
                }
                else
                {
                    UnpinSelectedEquippedWeapon();
                }
                break;
        }
    }

    private void OnEventOwned(GfxPanelCallbackType aEventType, GfxPanel aPanel, bool aStatus)
    {
        switch (aEventType)
        {
            case GfxPanelCallbackType.SELECT:
                ChangeStatsPanelState(0, aStatus, aPanel.Index, true);
                break;

            case GfxPanelCallbackType.PINNED:
                ChangeStatsPanelState(1, aStatus, aPanel.Index, true);
                break;

            case GfxPanelCallbackType.SUBMIT:
                int ownedWeaponPanelIndex = aPanel.Index;
                int previousSelectedWeapon = m_selectedOwnedIndex;

                UnpinSelectedOwnedWeapon();

                if (previousSelectedWeapon != ownedWeaponPanelIndex)
                    PinSelectedOwnedWeapon(ownedWeaponPanelIndex);

                ResolveWeaponEquip();
                break;
        }
    }

    private void ResolveWeaponEquip()
    {
        if (m_selectedOwnedIndex != -1 && m_selectedEquippedIndex != -1)
        {
            PlayerSaveData saveData = GfcManagerSaveData.GetActivePlayerSaveData();

            if (m_selectedOwnedIndex != 0)
            {
                int weaponIndexInOurList = m_startIndexOwnedWeapons + m_selectedOwnedIndex - 1;
                int ownedWeaponIndex = m_ownedWeaponsList[weaponIndexInOurList].Index;
                saveData.EquipWeapon(m_selectedEquippedIndex, ownedWeaponIndex);
            }
            else if (saveData.GetEquippedWeapons().Count > 1)
            {
                saveData.UnequipWeapon(m_selectedEquippedIndex);
            }

            DrawElements();
        }
    }

    private void SetDefaultStatsPanel(GfxPanel aPanel, int aIndex)
    {
        GfxPanelCreateData panelCreateData = GfUiTools.GetDefaultPanelCreateData();
        panelCreateData.Parent = m_statsWeaponsParent;
        panelCreateData.IsInteractable = false;
        panelCreateData.Index = panelCreateData.IndecesColumnRow.y = aIndex;

        aPanel.SetCreateData(panelCreateData, false, false);
        aPanel.SetTextOnly(NO_WEAPON);
    }

    private void ChangeStatsPanelState(int aStatsPanelIndex, bool aSelectedOrPinned, int aItemPanelIndex, bool aOwnedWeapon)
    {
        if (aSelectedOrPinned && (!aOwnedWeapon || aItemPanelIndex > 0))
        {
            if (aOwnedWeapon)
            {
                m_panelsStatsList[aStatsPanelIndex].SetWeaponData(m_ownedWeaponsList[aItemPanelIndex - 1].WeaponOwnedData, true, true);
            }
            else //index is from equipped weapon
            {
                PlayerSaveData saveData = GfcManagerSaveData.GetActivePlayerSaveData();
                List<int> equippedWeapons = saveData.GetEquippedWeapons();
                if (equippedWeapons.Count > aItemPanelIndex)
                {
                    int selectedWeaponIndex = equippedWeapons[aItemPanelIndex];
                    GfxPanel panel = m_panelsStatsList[aStatsPanelIndex];
                    panel.SetWeaponData(saveData.GetOwnedWeapons()[selectedWeaponIndex], false, true);
                }
            }
        }
        else
        {
            SetDefaultStatsPanel(m_panelsStatsList[aStatsPanelIndex], aStatsPanelIndex);
        }
    }
}

public struct OwnedWeaponDisplayData
{
    public OwnedWeaponDisplayData(int aIndex, WeaponOwnedData aWeaponData)
    {
        Index = aIndex;
        WeaponOwnedData = aWeaponData;
    }

    public int Index;
    public WeaponOwnedData WeaponOwnedData;
}