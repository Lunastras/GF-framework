using System.Collections.Generic;
using UnityEngine;
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

    [SerializeField] protected bool m_enableLogs = false;

    public static bool CurrentlyLoading { get { return Instance && Instance.m_loadingHandle.CoroutineIsRunning; } }

    protected static GfcGameState GameStateAfterLoad = GfcGameState.INVALID;

    protected static GfcGameMultiplayerType GameTypeToLoad = GfcGameMultiplayerType.NONE;

    public static int SceneBuildIndexToLoad = -1;

    protected static List<GameObject> m_rootGameObjects = new(4);

    protected static List<GameObject> m_rootGameObjectsOnStart = new(4);

    protected HashSet<int> m_scenesToUnload = new(4);

    protected static float FadeDuration = 0.25f;

    private bool m_fakeWait = false;

    private GfcLockKey m_gameStateKey;

    private bool m_softGameStateLock = false;

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

    private GfcCoroutineHandle m_loadingHandle = default;
    private GfcCoroutineHandle m_fakeWaitHandle = default;

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

    public static void RequestGameStateAfterLoad(GfcGameState aNewGameState, bool aSmoothTransition = true, GfxTransitionType aTransition = GfxTransitionType.BLACK_FADE)
    {
        if (CurrentlyLoading || Instance.m_fakeWaitHandle.CoroutineIsRunning)
        {
            if (Instance.m_softGameStateLock)
                GameStateAfterLoad = aNewGameState;
        }
        else
            GfgManagerGame.SetGameState(aNewGameState, aSmoothTransition, aTransition);
    }

    public static CoroutineHandle LoadScene(GfcSceneId aScene, GfcGameState aNewGameState = GfcGameState.INVALID, bool aReloadIfLoaded = false, GfcGameMultiplayerType aGameType = GfcGameMultiplayerType.SINGLEPLAYER, ServerLoadingMode aLoadingMode = ServerLoadingMode.KEEP_SERVER)
    {
        return LoadScene((int)aScene, aNewGameState, aReloadIfLoaded, aGameType, aLoadingMode);
    }

    public static CoroutineHandle LoadScene(int aSceneBuildIndex, GfcGameState aNewGameState = GfcGameState.INVALID, bool aReloadIfLoaded = false, GfcGameMultiplayerType aGameType = GfcGameMultiplayerType.SINGLEPLAYER, ServerLoadingMode aLoadingMode = ServerLoadingMode.KEEP_SERVER)
    {
        ServerLoadingMode = aSceneBuildIndex == 0 ? ServerLoadingMode.SHUTDOWN : aLoadingMode;
        int originalSceneIndexLoading = SceneBuildIndexToLoad;
        SceneBuildIndexToLoad = aSceneBuildIndex;
        GameTypeToLoad = aGameType;
        Instance.m_softGameStateLock = aNewGameState == GfcGameState.INVALID;
        GameStateAfterLoad = aNewGameState == GfcGameState.INVALID ? GfgManagerGame.GetGameState() : aNewGameState;

        if (!CurrentlyLoading)
        {
            bool sceneLoaded = SceneManager.GetSceneByBuildIndex(SceneBuildIndexToLoad).isLoaded;
            if (aReloadIfLoaded || !sceneLoaded)
            {
                Debug.Assert(!CurrentlyLoading, "The routine is already running, you cannot have this coroutine called multiple times in parallel.");
                if (Instance.m_enableLogs) Debug.Log("Starting to load scene with build index " + aSceneBuildIndex);

                Instance.m_scenesToUnload.Clear();
                Instance.m_gameStateKey = GfgManagerGame.GameStateLockHandle.Lock(Instance);
                Instance.m_loadingHandle.RunCoroutine(_LoadAsyncHost());
            }
            else
            {
                if (Instance.m_enableLogs)
                    Debug.Log("The scene at build index " + aSceneBuildIndex + " was already loaded.");
                return GfgManagerGame.SetGameState(GameStateAfterLoad);
            }
        }
        else if (aSceneBuildIndex != originalSceneIndexLoading)
        {
            Debug.LogWarning("Already in the process of loading scene with build index " + SceneBuildIndexToLoad + ", there might be issues loading scene " + aSceneBuildIndex);
        }

        return Instance.m_loadingHandle;
    }

    public static void LoadScene(string aLevelName, GfcGameState aNewGameState = GfcGameState.INVALID, bool aReloadIfLoaded = false, GfcGameMultiplayerType aGameType = GfcGameMultiplayerType.SINGLEPLAYER, ServerLoadingMode aLoadingMode = ServerLoadingMode.KEEP_SERVER)
    {
        LoadScene(GetSceneBuildIndexByName(aLevelName), aNewGameState, aReloadIfLoaded, aGameType, aLoadingMode);
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
            if (!GfgScene.GetScenePersistent((GfcSceneId)scene.buildIndex))
            {
                Instance.m_scenesToUnload.Add(scene.buildIndex);
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }

    private static IEnumerator<float> _LoadAsyncHost()
    {

        GfgManagerGame.GameStateLockHandle.Unlock(ref Instance.m_gameStateKey);
        yield return Timing.WaitUntilDone(GfgManagerGame.SetGameState(GfcGameState.LOADING, true));

        Instance.m_gameStateKey = GfgManagerGame.GameStateLockHandle.Lock(Instance);

        if (GfcManagerServer.HasInstance)
            GfcManagerServer.SetPlayerIsReady(false);

        var originalThreadPriority = Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority = Instance.m_loadThreadPriority;

    StartLoading:

        bool stopGame = ServerLoadingMode != ServerLoadingMode.KEEP_SERVER;
        bool mustHaveNetworkGame = NetworkManager.Singleton && ServerLoadingMode != ServerLoadingMode.SHUTDOWN;

        if (stopGame)
            GfgManagerGame.StopGame();

        //UNLOAD SCENES SECTION START
        UnloadAllScenes();
        while (Instance.m_scenesToUnload.Count != 0 || (NetworkManager.Singleton && NetworkManager.Singleton.ShutdownInProgress))
            yield return Timing.WaitForSeconds(0.1f);//wait for scenes to be unloaded and for the server to shutdown

        GfcPooling.ClearNonPersistentPools();

        //UNLOAD SCENES SECTION END
        Scene loadingScreenScene = SceneManager.GetSceneByBuildIndex((int)GfcSceneId.GF_BASE);
        loadingScreenScene.GetRootGameObjects(m_rootGameObjectsOnStart);
        SceneManager.SetActiveScene(loadingScreenScene);

        int sceneToLoadIndex = SceneBuildIndexToLoad;
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoadIndex, LoadSceneMode.Additive);
        operation.allowSceneActivation = true;

        while (operation.isDone == false)
        {
            yield return Timing.WaitForSeconds(0.02f);
        }

        Scene currentScene = SceneManager.GetSceneByBuildIndex(sceneToLoadIndex);
        SceneManager.SetActiveScene(currentScene);

        if (mustHaveNetworkGame)
        {
            GfgManagerGame.StartGame(GameTypeToLoad);
            while (!GfgManagerGame.GameInitialised || !(GfcManagerServer.HostReady || GfcManagerServer.HasAuthority))
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

        CoroutineHandle transitionHandle = GfgManagerGame.GetGameStateTransitionHandle();
        if (transitionHandle.IsValid) yield return Timing.WaitUntilDone(transitionHandle); //make sure the fade out of the first transition is done

        if (!Instance.m_fakeWait)
        {
            GfgManagerGame.GameStateLockHandle.Unlock(ref Instance.m_gameStateKey);
            yield return Timing.WaitUntilDone(GfgManagerGame.SetGameState(GameStateAfterLoad, true));
        }

        if (mustHaveNetworkGame) GfcManagerServer.SetPlayerIsReady(true);

        if (Instance.m_fakeWait)
            Instance.m_fakeWaitHandle.RunCoroutine(_FakeLoadWait());
        else
        {
            transitionHandle = GfgManagerGame.GetGameStateTransitionHandle();
            if (transitionHandle.IsValid) yield return Timing.WaitUntilDone(transitionHandle); //make sure the fade out of the first transition is done
        }

        Instance.m_loadingHandle.Finished();
    }

    private static IEnumerator<float> _FakeLoadWait()
    {
        while (Instance.m_fakeWait)
            yield return Timing.WaitForSeconds(0.02f);

        Instance.m_fakeWait = false;
        GfgManagerGame.GameStateLockHandle.Unlock(ref Instance.m_gameStateKey);
        GfgManagerGame.SetGameState(GameStateAfterLoad, false);
        Instance.m_fakeWaitHandle.Finished();
    }
}