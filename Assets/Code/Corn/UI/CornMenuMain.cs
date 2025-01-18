using UnityEngine;

public class CornMenuMain : GfgMenuMultiScreen<CornMenuMainScreens>
{
    private GfcInputTracker m_backTracker;

    void Start()
    {
        InitMultiScreenMenu(CornMenuMainScreens.COUNT);
        m_backTracker = new(GfcInputType.BACK) { DisplayPromptString = new("Back to menu") };
    }

    void Update() { if (m_currentlyShownScreen != CornMenuMainScreens.MAIN && m_backTracker.PressedSinceLastCheck()) ShowMainScreen(); }

    protected override void OnButtonEvent(GfxButtonCallbackType aCallbackType, GfxButton aButton, bool aState)
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