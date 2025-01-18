using UnityEngine;
using System.Collections.Generic;

public class CornMenuMain : MonoBehaviour
{
    [SerializeField] private EnumSingletons<GameObject, CornMenuMainScreens> m_screens;
    [SerializeField] private Transform m_buttonsParent;

    private CornMenuMainScreens m_currentlyShownScreen;

    private GfcInputTracker m_backTracker;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_screens.Initialize(CornMenuMainScreens.COUNT);
        Debug.Assert(m_screens[m_currentlyShownScreen].activeSelf, "The main menu needs to be active");
        m_buttonsParent.InitChildrenGfxButtons(OnButtonEvent);
        for (int i = 0; i < (int)CornMenuMainScreens.COUNT; i++)
            m_screens[i].SetActive(i == 0);

        m_backTracker = new(GfcInputType.BACK);
        m_backTracker.DisplayPromptString = new("Back to menu");
    }

    void Update()
    {
        if (m_currentlyShownScreen != CornMenuMainScreens.MAIN && m_backTracker.PressedSinceLastCheck())
            ShowMainScreen();
    }

    private void OnButtonEvent(GfxButtonCallbackType aCallbackType, GfxButton aButton, bool aState)
    {
        CornMenuMainButton buttonType = (CornMenuMainButton)aButton.Index;
        if (aCallbackType == GfxButtonCallbackType.SUBMIT)
        {
            switch (buttonType)
            {
                case CornMenuMainButton.START:
                    SetVisibleScreen(CornMenuMainScreens.PROFILE_SELECT);
                    break;
                case CornMenuMainButton.OPTIONS:
                    SetVisibleScreen(CornMenuMainScreens.OPTIONS);
                    break;
                case CornMenuMainButton.EXTRAS:
                    SetVisibleScreen(CornMenuMainScreens.EXTRAS);
                    break;
                case CornMenuMainButton.QUIT:
                    Application.Quit();
                    break;
            }
        }
    }

    public void SetVisibleScreen(CornMenuMainScreens aScreen)
    {
        if (aScreen != m_currentlyShownScreen)
        {
            m_screens[m_currentlyShownScreen].SetActiveGf(false);
            m_screens[aScreen].SetActiveGf(true);
            m_currentlyShownScreen = aScreen;
        }
    }

    public void ShowMainScreen() { SetVisibleScreen(CornMenuMainScreens.MAIN); }
}

public enum CornMenuMainButton
{
    START,
    OPTIONS,
    EXTRAS,
    QUIT
}

public enum CornMenuMainScreens
{
    MAIN,
    PROFILE_SELECT,
    OPTIONS,
    EXTRAS,
    COUNT
}