// LoadingScreenManager
// --------------------------------
// built by Martin Nerurkar (http://www.martin.nerurkar.de)
// for Nowhere Prophet (http://www.noprophet.com)
// Edited by Bucurescu Radu for the purpose of CSC384
//
// Licensed under GNU General Public License v3.0
// http://www.gnu.org/licenses/gpl-3.0.txt

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;


public class LoadingScreenManager : MonoBehaviour
{

    [Header("Loading Visuals")]
    //public Image loadingIcon;
    //public Image progressBar;
    public Image m_fadeOverlay;

    [Header("Timing Settings")]
    public float m_waitOnLoadEnd = 0.25f;
    public float m_fadeDuration = 0.25f;

    [Header("Loading Settings")]
    public LoadSceneMode m_loadSceneMode = LoadSceneMode.Additive;
    public ThreadPriority m_loadThreadPriority;

    [Header("Other")]
    // If loading additive, link to the cameras audio listener, to avoid multiple active audio listeners
    public AudioListener audioListener;

    AsyncOperation m_operation;
    Scene m_currentScene;

    public static int SceneToLoad = -1;
    static int LoadingSceneIndex = 1;
    static int MainSceneIndex = 2;

    static int MainMenuIndex = 0;

    static int GameObjectsContainerSceneIndex = 4;
    public static bool currentlyLoading = false;

    public static void LoadScene(int levelNum)
    {
        Application.backgroundLoadingPriority = ThreadPriority.High;
        SceneToLoad = levelNum;
        SceneManager.LoadScene(LoadingSceneIndex);
    }

    void Start()
    {
        if (SceneToLoad < 0)
            return;

        //Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        m_fadeOverlay.gameObject.SetActive(true); // Making sure it's on so that we can crossfade Alpha
        m_currentScene = SceneManager.GetActiveScene();

        currentlyLoading = true;

        // Debug.Log("GOT HERE");
        StartCoroutine(LoadAsync(SceneToLoad));
    }

    private IEnumerator LoadAsync(int levelNum)
    {
        m_fadeOverlay.CrossFadeAlpha(1, 0, true);
        m_fadeOverlay.CrossFadeAlpha(0, m_fadeDuration, true);

        ShowLoadingVisuals();
        FadeOut();

        float lastProgress = 0f;

        yield return new WaitForSeconds(m_fadeDuration);
        StartSceneLoadingOperation(GameObjectsContainerSceneIndex);


        while (DoneLoading() == false)
        {
            yield return null;

            if (Mathf.Approximately(m_operation.progress, lastProgress) == false)
            {
                //progressBar.fillAmount = operation.progress;
                lastProgress = m_operation.progress;
            }
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(GameObjectsContainerSceneIndex));

        if (levelNum != MainMenuIndex)
        {
            StartSceneLoadingOperation(MainSceneIndex);
            // operation does not auto-activate scene, so it's stuck at 0.9
            while (DoneLoading() == false)
            {
                yield return null;

                if (Mathf.Approximately(m_operation.progress, lastProgress) == false)
                {
                    //progressBar.fillAmount = operation.progress;
                    lastProgress = m_operation.progress;
                }
            }
        }

        StartSceneLoadingOperation(levelNum);

        lastProgress = 0f;

        // operation does not auto-activate scene, so it's stuck at 0.9
        while (DoneLoading() == false)
        {
            yield return null;

            if (Mathf.Approximately(m_operation.progress, lastProgress) == false)
            {
                //progressBar.fillAmount = operation.progress;
                lastProgress = m_operation.progress;
            }
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelNum));

        FadeIn();
        yield return new WaitForSeconds(m_fadeDuration);

        //loads the camera, canvas and player

        ShowCompletionVisuals();

        Image imageFade = null;

        try
        {
            imageFade = GameObject.Find("LoadFade").GetComponent<Image>();
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }


        m_fadeOverlay.CrossFadeAlpha(1, m_fadeDuration, true);


        if (imageFade != null)
        {
            Color fixedColor = imageFade.color;
            fixedColor.a = 1;
            imageFade.color = fixedColor;

            imageFade.CrossFadeAlpha(1, m_fadeDuration, true);
            imageFade.CrossFadeAlpha(0, m_fadeDuration, true);
        }

        currentlyLoading = false;

        if (m_loadSceneMode == LoadSceneMode.Additive)
        {
            SceneManager.UnloadSceneAsync(m_currentScene.name);
        }
        else
        {
            m_operation.allowSceneActivation = true;
        }
    }




    private void StartSceneLoadingOperation(int buildIndex)
    {
        Application.backgroundLoadingPriority = m_loadThreadPriority;
        m_operation = SceneManager.LoadSceneAsync(buildIndex, m_loadSceneMode);
        m_operation.allowSceneActivation = true;
        //if (m_loadSceneMode == LoadSceneMode.Single)
        // m_operation.allowSceneActivation = false;
    }

    public void ImmediateLoadScene(int buildIndex, LoadSceneMode mode)
    {
        SceneManager.LoadScene(buildIndex, mode);
    }

    private bool DoneLoading()
    {
        return (m_loadSceneMode == LoadSceneMode.Additive && m_operation.isDone) || (m_loadSceneMode == LoadSceneMode.Single && m_operation.progress >= 0.9f);
    }

    void FadeIn()
    {
        m_fadeOverlay.CrossFadeAlpha(1, m_fadeDuration, true);
    }

    void FadeOut()
    {
        m_fadeOverlay.CrossFadeAlpha(0, m_fadeDuration, true);
    }

    void ShowLoadingVisuals()
    {
        //loadingIcon.gameObject.SetActive(true);
        //loadingDoneIcon.gameObject.SetActive(false);

        //progressBar.fillAmount = 0f;
    }

    void ShowCompletionVisuals()
    {
        //loadingIcon.gameObject.SetActive(false);
        //loadingDoneIcon.gameObject.SetActive(true);

        //progressBar.fillAmount = 1f;
    }

}