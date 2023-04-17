using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class MainMenuBehaviour : MonoBehaviour
{
    public Image m_fadeOverlay;
    public float m_fadeDelay = 1.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        Color fixedColor = m_fadeOverlay.color;
        fixedColor.a = 1;
        m_fadeOverlay.color = fixedColor;
        m_fadeOverlay.CrossFadeAlpha(1f, 0f, true);

        if (!SceneManager.GetSceneByBuildIndex(4).isLoaded)
        {
            SceneManager.LoadScene(4, LoadSceneMode.Additive);
        }
        Time.timeScale = 1;


        //replayScreen.SetActive(false);
        //mainScreen.SetActive(true);

        Debug.Log("lmao i am here");
        StartCoroutine(StartMenu());
    }


    private IEnumerator StartMenu()
    {
        yield return new WaitForSeconds(m_fadeDelay);

        m_fadeOverlay.CrossFadeAlpha(0f, m_fadeDelay, true);

        yield return new WaitForSeconds(m_fadeDelay);

        m_fadeOverlay.gameObject.SetActive(false);
    }

    public void SwitchScreen()
    {
        //AudioManager.instance.Play("Selection");
        // mainScreen.SetActive(!mainScreen.activeSelf);
        // replayScreen.SetActive(!replayScreen.activeSelf);
    }

    public void StartGame()
    {
        Debug.Log("game started");
        StartCoroutine(ChoiceMade(3));
    }

    public void StartReplay(int replayIndex)
    {

        StartCoroutine(ChoiceMade(3));
    }

    public void Tutorial()
    {

        StartCoroutine(ChoiceMade(4));
    }


    private IEnumerator ChoiceMade(int levelIndex)
    {
        Debug.Log("i am in the coroutine");
        m_fadeOverlay.gameObject.SetActive(true);

        Color fixedColor = m_fadeOverlay.color;
        fixedColor.a = 1;
        m_fadeOverlay.color = fixedColor;
        m_fadeOverlay.CrossFadeAlpha(0f, 0f, true);

        m_fadeOverlay.CrossFadeAlpha(1f, m_fadeDelay, true);

        yield return new WaitForSeconds(m_fadeDelay);

        LoadingScreenManager.LoadScene(levelIndex);
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}
