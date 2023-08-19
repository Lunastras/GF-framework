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

public class LoadingScreenManager : NetworkBehaviour
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

    const int LoadingSceneIndex = 1;

    const int ContainerScene = 4;

    public static bool CurrentlyLoading
    {
        get
        {
            return Instance && Instance.m_isLoading;
        }
    }

    public static int SceneBuildIndexToLoad = -1;

    public static List<GameObject> RootGameObjects = new(4);

    public static List<GameObject> RootGameObjectsOnStart = new(4);

    public static HashSet<int> ScenesToUnload = new(4);

    public static float FadeDuration = 0.25f;

    private bool m_isLoading = true;

    public static ServerLoadingMode ServerLoadingMode { get; protected set; } = ServerLoadingMode.KEEP_SERVER;

    protected bool HasAuthority
    {
        get
        {
            return !NetworkManager.Singleton || NetworkManager.Singleton.IsHost;
        }

    }

    void Start()
    {
        Instance = this;
        FadeDuration = m_fadeDuration;
        m_loadingCanvasGroup.alpha = 0;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public override void OnDestroy()
    {
        Instance = null;
    }

    private void OnSceneUnloaded(Scene unloadedScene)
    {
        if (ScenesToUnload.Contains(unloadedScene.buildIndex))
        {
            ScenesToUnload.Remove(unloadedScene.buildIndex);
        }
    }

    public static void LoadScene(int sceneBuildIndex, ServerLoadingMode loadingMode)
    {
        Debug.Log("Starting to load scene with build index " + sceneBuildIndex);
        Application.backgroundLoadingPriority = ThreadPriority.High;
        GfGameManager.SetAllowGameTypeOverride(false);
        GfGameManager.SetStartGameAutomatically(false);
        Cursor.visible = false;
        ServerLoadingMode = loadingMode;
        int originalSceneIndexLoading = SceneBuildIndexToLoad;
        SceneBuildIndexToLoad = sceneBuildIndex;

        if (!CurrentlyLoading)
        {
            ScenesToUnload.Clear();
            Timing.RunCoroutine(_LoadAsyncHost());
        }
        else if (sceneBuildIndex != originalSceneIndexLoading)
        {
            Debug.LogError("Already in the process of loading scene with build index " + SceneBuildIndexToLoad + ", cannot load scene index " + sceneBuildIndex);
        }
    }

    public static void LoadScene(string levelName, ServerLoadingMode loadingMode)
    {
        LoadScene(GetSceneBuildIndexByName(levelName), loadingMode);
    }

    public static int GetSceneBuildIndexByName(string name)
    {
        int countScenes = SceneManager.sceneCountInBuildSettings;
        int index = 0; //return first index if no scene is found
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
            if (buildIndex != ContainerScene && buildIndex != LoadingSceneIndex)
            {
                ScenesToUnload.Add(scene.buildIndex);
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

        if (GfServerManager.HasInstance)
            GfServerManager.SetPlayerIsReady(false);

        FadeOut();
        yield return Timing.WaitForSeconds(FadeDuration);

    StartLoading:

        bool stopGame = ServerLoadingMode != ServerLoadingMode.KEEP_SERVER;
        mustHaveGame = ServerLoadingMode != ServerLoadingMode.SHUTDOWN;

        if (stopGame)
        {
            GfGameManager.ShutdownServer();
            while (NetworkManager.Singleton.ShutdownInProgress)
                yield return Timing.WaitForSeconds(0.02f);//wait for scenes to be unloaded and for the server to shutdown
        }

        //UNLOAD SCENES SECTION START
        UnloadAllScenes();
        while (ScenesToUnload.Count() != 0)
            yield return Timing.WaitForSeconds(0.02f);//wait for scenes to be unloaded and for the server to shutdown

        //UNLOAD SCENES SECTION END

        Scene loadingScreenScene = Instance.gameObject.scene;
        loadingScreenScene.GetRootGameObjects(RootGameObjectsOnStart);
        SceneManager.SetActiveScene(loadingScreenScene);

        if (mustHaveGame)
            GfGameManager.SpawnGameAssets();

        float loadingProgress = 0f;
        int sceneToLoadIndex = SceneBuildIndexToLoad;
        Application.backgroundLoadingPriority = Instance.m_loadThreadPriority;
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoadIndex, LoadSceneMode.Additive);
        operation.allowSceneActivation = true;

        Instance.ShowLoadingVisuals();

        // operation does not auto-activate scene, so it's stuck at 0.9
        while (operation.isDone == false)
        {
            yield return Timing.WaitForSeconds(0.02f);
            loadingProgress = operation.progress;
        }

        Scene currentScene = SceneManager.GetSceneByBuildIndex(SceneBuildIndexToLoad);
        SceneManager.SetActiveScene(currentScene);

        if (mustHaveGame)
        {
            GfGameManager.StartGame();
            while (!GfGameManager.GameInitialised || !(GfServerManager.HostReady || GfServerManager.HasAuthority))
                yield return Timing.WaitForSeconds(0.02f);
        }

        if (GfServerManager.HasAuthority || true)
        {
            loadingScreenScene.GetRootGameObjects(RootGameObjects);

            int objCount = RootGameObjects.Count;
            for (int i = 0; i < objCount; ++i)
            {
                if (false == RootGameObjectsOnStart.Contains(RootGameObjects[i]))
                {
                    SceneManager.MoveGameObjectToScene(RootGameObjects[i], currentScene);
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
            GfServerManager.SetPlayerIsReady(true);

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
        GfUiTools.CrossFadeAlpha(1, FadeDuration);
    }

    private static void FadeOut()
    {
        GfUiTools.CrossFadeAlpha(0, FadeDuration);
    }

    void ShowLoadingVisuals()
    {
        //loadingIcon.gameObject.SetActive(true);
        //loadingDoneIcon.gameObject.SetActive(false);

        //progressBar.fillAmount = 0f;
    }
}

public enum ServerLoadingMode
{
    KEEP_SERVER,
    SHUTDOWN,
    RECONNECT
}