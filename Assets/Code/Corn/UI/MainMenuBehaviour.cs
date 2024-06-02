using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public class MainMenuBehaviour : MonoBehaviour
{
    //The screens must be in the same order as found in the MenuScreens enum
    [SerializeField] protected GameObject[] m_screens;

    [SerializeField] protected TMP_InputField m_ipInputField = null;

    [SerializeField] protected TMP_InputField m_portInputField = null;

    public float m_fadeDelay = 1.0f;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;


    }

    private void Start()
    {
        //m_fadeOverlay.CrossFadeAlpha(1f, 0f, true);
        GfxUiTools.SetOverlayColor(Color.black);
        GfxUiTools.CrossFadeBlackAlpha(1f, 0);
        EnableScreen(MenuScreens.MAIN);
        GfxUiTools.CrossFadeBlackAlpha(0f, m_fadeDelay);
        Time.timeScale = 1;
    }

    public void EnableScreen(MenuScreens aMenuScreen)
    {
        int menuScreenIndex = (int)aMenuScreen;
        for (int i = 0; i < m_screens.Length; ++i)
            m_screens[i].SetActive(i == menuScreenIndex);
    }

    public void EnableMainMenuScreen() { EnableScreen(MenuScreens.MAIN); }

    public void EnableMultiplayerScreen() { EnableScreen(MenuScreens.MULTIPLAYER); }

    public void EnableWeaponsScreen() { EnableScreen(MenuScreens.WEAPONS); }

    public void EnableCharmsScreen() { EnableScreen(MenuScreens.CHARMS); }

    public void EnableOptionsScreen() { EnableScreen(MenuScreens.OPTIONS); }

    public void StartClientGame(int levelIndex)
    {
        StartGame(levelIndex, GameMultiplayerType.CLIENT);
    }

    public void StartSingleplayerGame(int levelIndex)
    {
        StartGame(levelIndex, GameMultiplayerType.SINGLEPLAYER);
    }

    public void StartHostGame(int levelIndex)
    {
        StartGame(levelIndex, GameMultiplayerType.HOST);
    }

    public void StartServerGame(int levelIndex)
    {
        StartGame(levelIndex, GameMultiplayerType.SERVER);
    }

    protected void StartGame(int levelIndex, GameMultiplayerType gameType)
    {
        if (gameType == GameMultiplayerType.SINGLEPLAYER)
        {
            GfcManagerGame.SetServerIp("127.0.0.1");
        }
        else
        {
            GfcManagerGame.SetServerIp(m_ipInputField.text, ushort.Parse(m_portInputField.text));
        }

        GfgManagerSceneLoader.LoadScene(levelIndex, false, ServerLoadingMode.KEEP_SERVER, gameType);
    }

    private IEnumerator StartMenu()
    {
        yield return new WaitForSeconds(m_fadeDelay);

        GfxUiTools.CrossFadeBlackAlpha(0f, m_fadeDelay);

        yield return new WaitForSeconds(m_fadeDelay);
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}

public enum MenuScreens
{
    MAIN,
    MULTIPLAYER,
    WEAPONS,
    OPTIONS,
    CHARMS,
}
