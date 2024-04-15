using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GfxMenuSelectMission : MonoBehaviour
{
    [SerializeField] protected RectTransform m_parentMissions;

    [SerializeField] protected int m_maxMissionsToDisplay = 1;

    [SerializeField] protected int m_pageDisplayCount = 10;

    protected int m_currentShownPage = -1;

    private Action<GfxPanelCallbackType, GfxPanel, bool> m_onEventMission;

    private List<GfxPanel> m_panelsMissions = new(10);

    private bool m_initialized = false;

    private void Initialize()
    {
        if (!m_initialized)
        {
            m_panelsMissions.Clear();
            m_onEventMission += OnEventMission;
            GfcPooling.DestroyChildren(m_parentMissions);
            m_initialized = true;
        }
    }

    // Start is called before the first frame update
    private void OnEnable()
    {
        Initialize();
        // DrawElements();
    }

    private void OnDisable()
    {
    }

    /*
    private void DrawElements()
    {
        DrawOwnedWeaponsAtPage(m_currentShownPage);
    }

    private void SetShownTier(int aTier)
    {
        aTier %= InvManagerWeapons.MAX_TIER + 1;
        m_shownTier = aTier;
        DrawOwnedWeaponsAtPage(0);
    }

    private void DrawEquippedWeapons(bool aSnapPanelsToDesiredState = false)
    {
        PlayerSaveData saveData = GfgManagerSaveData.GetActivePlayerSaveData();

        GfxPanelCreateData panelCreateData = GfUiTools.GetDefaultPanelCreateData();
        panelCreateData.OnEventCallback = m_onEventMission;

        GfUiTools.CreatePanelMatrix(m_parentMissions, panelCreateData, m_panelsEquippedList, Axis.VERTICAL, GfgManagerSaveData.MAX_EQUIPPED_WEAPONS, aSnapPanelsToDesiredState, true, AlignmentHorizontal.MIDDLE, AlignmentVertical.BOTTOM);

        for (int i = 0; i < GfgManagerSaveData.MAX_EQUIPPED_WEAPONS; ++i)
        {
            GfxPanel panel = m_panelsEquippedList[i];
            if (i >= equippedWeaponsIndeces.Count)
                panel.SetTextOnly(NO_WEAPON);
            else //i have a weapon at index i
                panel.SetWeaponData(ownedWeapons[equippedWeaponsIndeces[i]], false, true, false);
        }
    }*/

    private void OnEventMission(GfxPanelCallbackType aEventType, GfxPanel aPanel, bool aStatus)
    {
        switch (aEventType)
        {
            case GfxPanelCallbackType.SELECT:
                break;

            case GfxPanelCallbackType.PINNED:
                break;

            case GfxPanelCallbackType.SUBMIT:


                break;
        }
    }
}