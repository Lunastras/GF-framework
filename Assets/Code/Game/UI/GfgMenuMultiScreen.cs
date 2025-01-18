using UnityEngine;
using System;

public abstract class GfgMenuMultiScreen<ENUM> : MonoBehaviour where ENUM : unmanaged, Enum
{
    [SerializeField] protected EnumSingletons<GameObject, ENUM> m_screens;
    [SerializeField] protected Transform m_buttonsParent;
    protected ENUM m_currentlyShownScreen;

    public ENUM VisibleScreen { get { return m_currentlyShownScreen; } }
    public bool ShowingMainScreen { get { return 0 == m_currentlyShownScreen.Index(); } }

    protected void InitMultiScreenMenu(ENUM anEnumCount) { InitMultiScreenMenu(anEnumCount.Index()); }
    protected void InitMultiScreenMenu(int aCount)
    {
        m_screens.Initialize(aCount);
        m_buttonsParent.InitChildrenGfxButtons(OnButtonEvent);
        for (int i = 0; i < aCount; i++)
            m_screens[i].SetActive(i == 0);
    }

    public void SetVisibleScreen(int aScreenIndex)
    {
        if (aScreenIndex != m_currentlyShownScreen.Index())
        {
            m_screens[m_currentlyShownScreen].SetActiveGf(false);
            m_screens[aScreenIndex].SetActiveGf(true);
            m_screens[aScreenIndex].transform.SetAsLastSibling();
            m_currentlyShownScreen = aScreenIndex.IndexToEnum<ENUM>();
        }
    }

    protected abstract void OnButtonEvent(GfxButtonCallbackType aCallbackType, GfxButton aButton, bool aState);
    public void SetVisibleScreen(ENUM aScreen) { SetVisibleScreen(aScreen.Index()); }
    public void ShowMainScreen() { SetVisibleScreen(0); }
}