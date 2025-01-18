using UnityEngine;

public class GfgMenuPause : GfgMenuMultiScreen<GfgMenuPauseScreens>
{
    public static GfgMenuPause Instance { get; protected set; }

    private GfcInputTracker m_backTracker;

    void Start()
    {
        Instance = this;
        InitMultiScreenMenu(GfgMenuPauseScreens.COUNT);
        m_backTracker = new(GfcInputType.BACK) { DisplayPromptString = new("Back to menu"), Key = new(GfcInputLockPriority.PAUSE) };
    }

    void Update() { if (m_currentlyShownScreen != GfgMenuPauseScreens.MAIN && m_backTracker.PressedSinceLastCheck()) ShowMainScreen(); }

    protected override void OnButtonEvent(GfxButtonCallbackType aCallbackType, GfxButton aButton, bool aState)
    {
        GfgMenuPauseButtons buttonType = (GfgMenuPauseButtons)aButton.Index;
        if (aCallbackType == GfxButtonCallbackType.SUBMIT)
        {
            switch (buttonType)
            {
                case GfgMenuPauseButtons.CONTINUE:
                    GfgPauseToggle.SetPause(false);
                    break;
                case GfgMenuPauseButtons.OPTIONS:
                    SetVisibleScreen(GfgMenuPauseScreens.OPTIONS);
                    break;
                case GfgMenuPauseButtons.QUIT:
                    GfgManagerSceneLoader.LoadScene(GfcSceneId.MAIN_MENU);
                    break;
            }
        }
    }
}

public enum GfgMenuPauseScreens
{
    MAIN,
    OPTIONS,
    COUNT,
}

public enum GfgMenuPauseButtons
{
    CONTINUE,
    OPTIONS,
    QUIT,
    COUNT,
}