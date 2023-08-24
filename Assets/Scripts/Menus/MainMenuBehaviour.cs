using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public class MainMenuBehaviour : MonoBehaviour
{
    [SerializeField]
    protected GameObject m_mainScreen = null;

    [SerializeField]
    protected GameObject m_multiplayerScreen = null;

    [SerializeField]
    protected TMP_InputField m_ipInputField = null;

    [SerializeField]
    protected TMP_InputField m_portInputField = null;


    public float m_fadeDelay = 1.0f;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        int mainSceneIndex = LoadingScreenManager.GfGameManagerSceneIndex;
        if (!SceneManager.GetSceneByBuildIndex(mainSceneIndex).isLoaded)
        {
            SceneManager.LoadScene(mainSceneIndex, LoadSceneMode.Additive);
        }
    }

    private void Start()
    {
        //m_fadeOverlay.CrossFadeAlpha(1f, 0f, true);
        GfUiTools.SetOverlayColor(Color.black);
        GfUiTools.CrossFadeAlpha(1f, 0);

        m_multiplayerScreen.SetActive(false);
        m_mainScreen.SetActive(true);

        GfUiTools.CrossFadeAlpha(0f, m_fadeDelay);
        Time.timeScale = 1;
    }

    public void ToggleMultiplayerScreen()
    {
        m_multiplayerScreen.SetActive(!m_multiplayerScreen.activeSelf);
        m_mainScreen.SetActive(!m_multiplayerScreen.activeSelf);
    }

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
            GfGameManager.SetServerIp("127.0.0.1");
        }
        else
        {
            GfGameManager.SetServerIp(m_ipInputField.text, ushort.Parse(m_portInputField.text));
        }

        LoadingScreenManager.LoadScene(levelIndex, ServerLoadingMode.KEEP_SERVER, gameType);
    }

    private IEnumerator StartMenu()
    {
        yield return new WaitForSeconds(m_fadeDelay);

        GfUiTools.CrossFadeAlpha(0f, m_fadeDelay);

        yield return new WaitForSeconds(m_fadeDelay);
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}
