// LoadingScreenManager
// --------------------------------
// built by Martin Nerurkar (http://www.martin.nerurkar.de)
// for Nowhere Prophet (http://www.noprophet.com)
// Edited by Bucurescu Radu for the purpose of CSC384
//
// Licensed under GNU General Public License v3.0
// http://www.gnu.org/licenses/gpl-3.0.txt

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using MEC;

public class LoadingScreenManager : MonoBehaviour
{
    protected static LoadingScreenManager Instance = null;
    [SerializeField]
    private CanvasGroup m_loadingCanvasGroup = null;

    [SerializeField]
    private GameObject m_camera = null;

    [Header("Loading Visuals")]


    [Header("Timing Settings")]
    public float m_fadeDuration = 0.25f;

    [Header("Loading Settings")]
    public ThreadPriority m_loadThreadPriority;

    [Header("Other")]
    // If loading additive, link to the cameras audio listener, to avoid multiple active audio listeners
    public AudioListener audioListener;

    public static int GfBaseSceneIndex
    {
        get
        {
            return GetSceneBuildIndexByName("GfBaseScene");
        }
    }

    public static int LoadingSceneIndex
    {
        get
        {
            return GetSceneBuildIndexByName("LoadingScreen");
        }
    }

    public static bool CurrentlyLoading
    {
        get
        {
            return Instance && Instance.m_isLoading;
        }
    }

    protected static GameMultiplayerType GameTypeToLoad = GameMultiplayerType.NONE;

    public static int SceneBuildIndexToLoad = -1;

    protected static List<GameObject> m_rootGameObjects = new(4);

    protected static List<GameObject> m_rootGameObjectsOnStart = new(4);

    protected static HashSet<int> m_scenesToUnload = new(4);

    protected static float FadeDuration = 0.25f;

    private bool m_isLoading = true;

    public static ServerLoadingMode ServerLoadingMode { get; protected set; } = ServerLoadingMode.KEEP_SERVER;

    void Start()
    {
        Instance = this;
        FadeDuration = m_fadeDuration;
        m_loadingCanvasGroup.alpha = 0;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    protected void OnDestroy()
    {
        Instance = null;
    }

    private void OnSceneUnloaded(Scene unloadedScene)
    {
        if (m_scenesToUnload.Contains(unloadedScene.buildIndex))
        {
            m_scenesToUnload.Remove(unloadedScene.buildIndex);
        }
    }

    public static void LoadScene(int sceneBuildIndex, ServerLoadingMode loadingMode, GameMultiplayerType gameType)
    {
        Debug.Log("Starting to load scene with build index " + sceneBuildIndex);
        Application.backgroundLoadingPriority = ThreadPriority.High;
        Cursor.visible = false;
        ServerLoadingMode = sceneBuildIndex == 0 ? ServerLoadingMode.SHUTDOWN : loadingMode;
        int originalSceneIndexLoading = SceneBuildIndexToLoad;
        SceneBuildIndexToLoad = sceneBuildIndex;
        GameTypeToLoad = gameType;

        if (!CurrentlyLoading)
        {
            m_scenesToUnload.Clear();
            Timing.RunCoroutine(_LoadAsyncHost());
        }
        else if (sceneBuildIndex != originalSceneIndexLoading)
        {
            Debug.LogWarning("Already in the process of loading scene with build index " + SceneBuildIndexToLoad + ", there might be issues loading scene " + sceneBuildIndex);
        }
    }

    public static void LoadScene(string levelName, ServerLoadingMode loadingMode, GameMultiplayerType gameType)
    {
        LoadScene(GetSceneBuildIndexByName(levelName), loadingMode, gameType);
    }

    public static int GetSceneBuildIndexByName(string name)
    {
        int countScenes = SceneManager.sceneCountInBuildSettings;
        int index = 0; //return to main menu if there is no scene
        for (int i = 0; i < countScenes; ++i)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);

            if (scenePath.Contains(name))
            {
                index = i;
                break;
            }
        }

        return index;
    }

    public static void UnloadAllScenes()
    {
        int countScenes = SceneManager.sceneCount;
        for (int i = 0; i < countScenes; ++i)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            int buildIndex = scene.buildIndex;
            if (buildIndex != GfBaseSceneIndex && buildIndex != LoadingSceneIndex)
            {
                m_scenesToUnload.Add(scene.buildIndex);
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }

    private static IEnumerator<float> _LoadAsyncHost()
    {
        bool mustHaveGame = ServerLoadingMode != ServerLoadingMode.SHUTDOWN;

        FadeIn();
        yield return Timing.WaitForSeconds(FadeDuration);

        SceneManager.LoadScene(LoadingSceneIndex, LoadSceneMode.Additive);

        while (Instance == null)
            yield return Timing.WaitForSeconds(0.02f);

        Instance.m_isLoading = true;
        Instance.m_loadingCanvasGroup.alpha = 1;

        if (GfcManagerServer.HasInstance)
            GfcManagerServer.SetPlayerIsReady(false);

        FadeOut();
        yield return Timing.WaitForSeconds(FadeDuration);

    StartLoading:

        bool stopGame = ServerLoadingMode != ServerLoadingMode.KEEP_SERVER;
        mustHaveGame = ServerLoadingMode != ServerLoadingMode.SHUTDOWN;

        if (stopGame)
            GfcManagerGame.ShutdownServer();

        //UNLOAD SCENES SECTION START
        UnloadAllScenes();
        while (m_scenesToUnload.Count() != 0 || (NetworkManager.Singleton && NetworkManager.Singleton.ShutdownInProgress))
            yield return Timing.WaitForSeconds(0.5f);//wait for scenes to be unloaded and for the server to shutdown

        //UNLOAD SCENES SECTION END

        Scene loadingScreenScene = Instance.gameObject.scene;
        loadingScreenScene.GetRootGameObjects(m_rootGameObjectsOnStart);
        SceneManager.SetActiveScene(loadingScreenScene);

        float loadingProgress = 0f;
        int sceneToLoadIndex = SceneBuildIndexToLoad;
        Application.backgroundLoadingPriority = Instance.m_loadThreadPriority;
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoadIndex, LoadSceneMode.Additive);
        operation.allowSceneActivation = true;

        Instance.ShowLoadingVisuals();

        while (operation.isDone == false)
        {
            yield return Timing.WaitForSeconds(0.02f);
            loadingProgress = operation.progress;
        }

        Scene currentScene = SceneManager.GetSceneByBuildIndex(sceneToLoadIndex);
        SceneManager.SetActiveScene(currentScene);

        if (mustHaveGame)
        {
            GfcManagerGame.StartGame(GameTypeToLoad);
            while (!GfcManagerGame.GameInitialised || !(GfcManagerServer.HostReady || GfcManagerServer.HasAuthority))
                yield return Timing.WaitForSeconds(0.02f);
        }

        if (GfcManagerServer.HasAuthority || true)
        {
            loadingScreenScene.GetRootGameObjects(m_rootGameObjects);

            int objCount = m_rootGameObjects.Count;
            for (int i = 0; i < objCount; ++i)
            {
                if (false == m_rootGameObjectsOnStart.Contains(m_rootGameObjects[i]))
                {
                    SceneManager.MoveGameObjectToScene(m_rootGameObjects[i], currentScene);
                }
            }
        }

        //Check if we received a request to load a different map
        if (sceneToLoadIndex != SceneBuildIndexToLoad)
            goto StartLoading;

        ////START LOADING NEW SCENE END

        FadeIn();
        yield return Timing.WaitForSeconds(FadeDuration);

        Instance.m_camera.SetActive(false);
        Instance.m_loadingCanvasGroup.alpha = 0;
        Instance.m_isLoading = false;

        if (mustHaveGame)
            GfcManagerServer.SetPlayerIsReady(true);

        FadeOut();
        yield return Timing.WaitForSeconds(FadeDuration);

        SceneManager.UnloadSceneAsync(LoadingSceneIndex);
    }

    public void ImmediateLoadScene(int buildIndex, LoadSceneMode mode)
    {
        SceneManager.LoadScene(buildIndex, mode);
    }

    private static void FadeIn()
    {
        GfxUiTools.CrossFadeBlackAlpha(1, FadeDuration);
    }

    private static void FadeOut()
    {
        GfxUiTools.CrossFadeBlackAlpha(0, FadeDuration);
    }

    void ShowLoadingVisuals()
    {
        //loadingIcon.gameObject.SetActive(true);
        //loadingDoneIcon.gameObject.SetActive(false);

        //progressBar.fillAmount = 0f;
    }
}