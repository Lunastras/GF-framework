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
using MEC;

public class GfgManagerSceneLoader : MonoBehaviour
{
    protected static GfgManagerSceneLoader Instance = null;

    [Header("Loading Visuals")]


    [Header("Timing Settings")]
    public float m_fadeDuration = 0.25f;

    [Header("Loading Settings")]
    public ThreadPriority m_loadThreadPriority;

    [Header("Other")]
    // If loading additive, link to the cameras audio listener, to avoid multiple active audio listeners
    public AudioListener audioListener;

    public static bool CurrentlyLoading { get { return Instance && Instance.m_isLoading; } }

    protected static GameMultiplayerType GameTypeToLoad = GameMultiplayerType.NONE;

    public static int SceneBuildIndexToLoad = -1;

    protected static List<GameObject> m_rootGameObjects = new(4);

    protected static List<GameObject> m_rootGameObjectsOnStart = new(4);

    protected static HashSet<int> m_scenesToUnload = new(4);

    protected static float FadeDuration = 0.25f;

    private bool m_isLoading = false;

    private bool m_fakeWait = false;

    private bool m_skipFinalFade = false;

    public static bool FakeWait
    {
        get
        {
            return Instance.m_fakeWait;
        }

        set
        {
            Instance.m_fakeWait = value;
        }
    }

    public static bool SkipFinalFade
    {
        get
        {
            return Instance.m_skipFinalFade;
        }

        set
        {
            Instance.m_skipFinalFade = value;
        }
    }

    private static CoroutineHandle ourLoadingRoutine = default;

    public static ServerLoadingMode ServerLoadingMode { get; protected set; } = ServerLoadingMode.KEEP_SERVER;

    void Awake()
    {
        this.SetSingleton(ref Instance);

        FadeDuration = m_fadeDuration;
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

    public static CoroutineHandle LoadScene(GfcScene aScene, bool aReloadIfLoaded = false, ServerLoadingMode aLoadingMode = ServerLoadingMode.KEEP_SERVER, GameMultiplayerType aGameType = GameMultiplayerType.SINGLEPLAYER)
    {
        return LoadScene((int)aScene, aReloadIfLoaded, aLoadingMode, aGameType);
    }

    public static CoroutineHandle LoadScene(int aSceneBuildIndex, bool aReloadIfLoaded = false, ServerLoadingMode aLoadingMode = ServerLoadingMode.KEEP_SERVER, GameMultiplayerType aGameType = GameMultiplayerType.SINGLEPLAYER)
    {
        //Debug.Log("Starting to load scene with build index " + aSceneBuildIndex);
        ServerLoadingMode = aSceneBuildIndex == 0 ? ServerLoadingMode.SHUTDOWN : aLoadingMode;
        int originalSceneIndexLoading = SceneBuildIndexToLoad;
        SceneBuildIndexToLoad = aSceneBuildIndex;
        GameTypeToLoad = aGameType;

        if (!CurrentlyLoading)
        {
            bool sceneLoaded = SceneManager.GetSceneByBuildIndex(SceneBuildIndexToLoad).isLoaded;
            if (aReloadIfLoaded || !sceneLoaded)
            {
                m_scenesToUnload.Clear();
                ourLoadingRoutine = Timing.RunCoroutine(_LoadAsyncHost());
            }
            else if (sceneLoaded)
            {
                Debug.Log("THe scene at build index " + aSceneBuildIndex + " was already loaded.");
            }
        }
        else if (aSceneBuildIndex != originalSceneIndexLoading)
        {
            Debug.LogWarning("Already in the process of loading scene with build index " + SceneBuildIndexToLoad + ", there might be issues loading scene " + aSceneBuildIndex);
        }

        return ourLoadingRoutine;
    }

    public static void LoadScene(string aLevelName, bool aReloadIfLoaded = false, ServerLoadingMode aLoadingMode = ServerLoadingMode.KEEP_SERVER, GameMultiplayerType aGameType = GameMultiplayerType.SINGLEPLAYER)
    {
        LoadScene(GetSceneBuildIndexByName(aLevelName), aReloadIfLoaded, aLoadingMode, aGameType);
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
            if (scene.buildIndex != (int)GfcScene.GF_BASE)
            {
                m_scenesToUnload.Add(scene.buildIndex);
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }

    private static IEnumerator<float> _LoadAsyncHost()
    {
        bool mustHaveNetworkGame = ServerLoadingMode != ServerLoadingMode.SHUTDOWN;

        FadeIn();
        yield return Timing.WaitForSeconds(FadeDuration);

        GfxLoadingScreenVisuals.Instance.SetGroupAlpha(1);

        Debug.Assert(!Instance.m_isLoading, "The routine is already running, you cannot have this coroutine called multiple times in parallel.");
        Instance.m_isLoading = true;

        if (GfcManagerServer.HasInstance)
            GfcManagerServer.SetPlayerIsReady(false);

        FadeOut();
        yield return Timing.WaitForSeconds(FadeDuration);

        var originalThreadPriority = Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority = Instance.m_loadThreadPriority;

    StartLoading:

        bool stopGame = ServerLoadingMode != ServerLoadingMode.KEEP_SERVER;
        mustHaveNetworkGame = NetworkManager.Singleton && ServerLoadingMode != ServerLoadingMode.SHUTDOWN;

        if (stopGame)
            GfcManagerGame.StopGame();

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

        if (mustHaveNetworkGame)
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

        Application.backgroundLoadingPriority = originalThreadPriority;

        if (!Instance.m_skipFinalFade)
        {
            FadeIn();
            yield return Timing.WaitForSeconds(FadeDuration);
        }

        Instance.m_isLoading = false;

        if (Instance.m_fakeWait)
            Timing.RunCoroutine(_FakeLoadWait());
        else
            GfxLoadingScreenVisuals.Instance.SetGroupAlpha(0);

        if (mustHaveNetworkGame)
            GfcManagerServer.SetPlayerIsReady(true);

        if (!Instance.m_skipFinalFade)
        {
            FadeOut();
            yield return Timing.WaitForSeconds(FadeDuration);
        }

        Instance.m_skipFinalFade = false;
        ourLoadingRoutine = default;
    }

    private static IEnumerator<float> _FakeLoadWait()
    {
        while (Instance.m_fakeWait)
            yield return Timing.WaitForSeconds(0.02f);

        Instance.m_fakeWait = false;
        GfxLoadingScreenVisuals.Instance.SetGroupAlpha(0);
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