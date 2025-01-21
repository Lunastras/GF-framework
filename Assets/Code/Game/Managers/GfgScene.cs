using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GfgScene : MonoBehaviour
{

    [Serializable]
    protected struct GfcGameStateScene
    {
        public GfcGameState GameState;
        public GfcSceneId Scene;
        [HideInInspector][NonSerialized] public bool Static;
    }

    protected struct SceneData
    {
        public AsyncOperation Operation;
        public GfcSceneLoadState LoadState;

        public bool PersistentScene;
    }

    protected static GfgScene Instance;
    protected SceneData[] m_scenesData;

    [SerializeField] protected GfcSceneId[] m_persistentScenesOnStart;

    [SerializeField] protected GfcGameStateScene[] m_gameStateScenes;

    protected GfcSceneId m_lastActiveScene;

    protected GfcSceneId m_queuedSceneToActivate = GfcSceneId.INVALID;
    protected bool m_registeredGameStateTask = false;

    public static GfcSceneId QueuedSceneToActivate { get { return Instance.m_queuedSceneToActivate; } }
    public static bool WaitingForSceneToActivate { get { return Instance.m_queuedSceneToActivate != GfcSceneId.INVALID; } }

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);
        m_scenesData = new SceneData[(int)GfcSceneId.COUNT];

        for (int i = 0; i < m_gameStateScenes.Length; i++)
        {
            var sceneData = m_gameStateScenes[i];
            sceneData.Static = sceneData.Scene != GfcSceneId.INVALID;
            m_gameStateScenes[i] = sceneData;

            if (i != (int)m_gameStateScenes[i].GameState)
                Debug.LogError("Scene at index " + i + " should be at index " + (int)m_gameStateScenes[i].GameState);
        }

        int diffLength = (int)GfcGameState.COUNT - m_gameStateScenes.Length;
        if (m_gameStateScenes.Length != (int)GfcGameState.COUNT)
            Array.Resize(ref m_gameStateScenes, (int)GfcGameState.COUNT);

        for (int i = 0; i < diffLength; i++)
        {
            int index = m_gameStateScenes.Length - 1 - i;
            m_gameStateScenes[m_gameStateScenes.Length - 1 - i] = new()
            {
                GameState = (GfcGameState)index,
                Scene = GfcSceneId.INVALID
            };
        }

        for (int i = 0; i < m_persistentScenesOnStart.Length; i++)
        {
            LoadScene(m_persistentScenesOnStart[i], false, true);
        }

        SetScenePersistent(GfcSceneId.GF_BASE, true);

        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        for (int i = 0; i < m_scenesData.Length; i++)
        {
            if (SceneManager.GetSceneByBuildIndex(i).isLoaded)
            {
                SceneData sceneData = m_scenesData[i];
                sceneData.LoadState = GfcSceneLoadState.LOADED;
                m_scenesData[i] = sceneData;
            }
        }
    }

    void Start()
    {
        GfgManagerGame.Instance.OnGameStateChanged += OnGameStateChanged;
        OnGameStateChangedInternal(GfgManagerGame.GetGameState(), GfcGameState.INVALID, true, true);
    }

    void OnDestroy()
    {
        if (GfgManagerGame.Instance) GfgManagerGame.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    public static GfcSceneLoadState GetSceneState(GfcSceneId aScene) { return Instance.m_scenesData[(int)aScene].LoadState; }
    public static GfcSceneId GetActiveSceneId()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GfcSceneId sceneId = (GfcSceneId)activeScene.buildIndex;
#if UNITY_EDITOR
        if ((int)sceneId >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("The active scene '" + activeScene.name + "' is not in the build settings, cannot fetch its scene id.");
            sceneId = GfcSceneId.INVALID;
        }
#endif //UNITY_EDITOR
        return sceneId;
    }

    public static GfcSceneId GetLastActiveScene() { return Instance.m_lastActiveScene; }

    public static GfcSceneActiveSetResult SetActiveScene(GfcSceneId aScene)
    {
        GfcSceneActiveSetResult result;

        GfcSceneId activeScene = GetActiveSceneId();
        if (activeScene != aScene)
        {
            Scene sceneToActivate = SceneManager.GetSceneByBuildIndex((int)aScene);
            if (sceneToActivate.isLoaded && SceneManager.SetActiveScene(sceneToActivate))
            {
                Instance.m_lastActiveScene = activeScene;
                Instance.m_queuedSceneToActivate = GfcSceneId.INVALID;
                result = GfcSceneActiveSetResult.SUCCESS;
            }
            else
            {
                Instance.m_queuedSceneToActivate = aScene;
                result = GfcSceneActiveSetResult.ACTIVATE_QUEUED;
            }
        }
        else
        {
            result = GfcSceneActiveSetResult.SUCCESS;
        }

        if (result == GfcSceneActiveSetResult.SUCCESS && Instance.m_registeredGameStateTask)
        {
            Instance.m_registeredGameStateTask = false;
            GfgManagerGame.UnregisterGameStateTask();
        }

        return result;
    }

    public static bool GetScenePersistent(GfcSceneId aScene)
    {
        Debug.Assert(aScene != GfcSceneId.INVALID);
        return Instance.m_scenesData[(int)aScene].PersistentScene;
    }

    public static void SetScenePersistent(GfcSceneId aScene, bool aPersistentScene)
    {
        Debug.Assert(aScene != GfcSceneId.INVALID);

        SceneData sceneData = Instance.m_scenesData[(int)aScene];
        sceneData.PersistentScene = aPersistentScene;
        Instance.m_scenesData[(int)aScene] = sceneData;
    }

    public static void LoadScene(GfcSceneId aScene, bool aSceneActivation = false, bool aPersistentScene = false)
    {
        Debug.Assert(aScene != GfcSceneId.INVALID);
        Debug.Assert(aScene != GfcSceneId.COUNT);

        SceneData sceneData = Instance.m_scenesData[(int)aScene];
        sceneData.PersistentScene |= aPersistentScene;

        Scene scene = SceneManager.GetSceneByBuildIndex((int)aScene);
        if (!scene.isLoaded && sceneData.LoadState == GfcSceneLoadState.UNLOADED)
        {
            sceneData.LoadState = GfcSceneLoadState.LOADING;
            SceneManager.LoadScene((int)aScene, LoadSceneMode.Additive);
        }

        if (aSceneActivation)
            SetActiveScene(aScene);

        Instance.m_scenesData[(int)aScene] = sceneData;
    }

    public static AsyncOperation LoadSceneAsync(GfcSceneId aScene, bool aSceneActivation = false, bool aPersistentScene = false)
    {
        Debug.Assert(aScene != GfcSceneId.INVALID);

        SceneData sceneData = Instance.m_scenesData[(int)aScene];
        sceneData.PersistentScene |= aPersistentScene;
        AsyncOperation operation = sceneData.Operation;

        if (operation == null && !SceneManager.GetSceneByBuildIndex((int)aScene).isLoaded)
        {
            operation = SceneManager.LoadSceneAsync((int)aScene, LoadSceneMode.Additive);
            operation.allowSceneActivation = aSceneActivation;
            sceneData.Operation = operation;
            sceneData.LoadState = GfcSceneLoadState.LOADING;
        }

        Instance.m_scenesData[(int)aScene] = sceneData;

        return operation;
    }

    protected static void OnGameStateChanged(GfcGameState aNewGameState, GfcGameState anOldGameState, bool anInstant)
    {
        OnGameStateChangedInternal(aNewGameState, anOldGameState, anInstant, false);
    }

    protected static void OnGameStateChangedInternal(GfcGameState aNewGameState, GfcGameState anOldGameState, bool anInstant, bool aStartCall)
    {
        if (aNewGameState == GfcGameState.INVALID)
            return;

        int gameStateIndex = (int)GfgManagerGame.GetPreviousGameState();
        GfcGameStateScene[] gameStateScenes = Instance.m_gameStateScenes;
        if (!gameStateScenes[gameStateIndex].Static)
        {
            gameStateScenes[gameStateIndex].Scene = GetActiveSceneId();

        }

        if (Instance.m_gameStateScenes.Length > (int)aNewGameState)
        {
            GfcSceneId sceneId = Instance.m_gameStateScenes[(int)aNewGameState].Scene;

            if (sceneId != GfcSceneId.INVALID)
            {
                Debug.Assert(!Instance.m_registeredGameStateTask, "Trying to register task, but it was already registered and unfinished.");

                if (!anInstant)
                {
                    Instance.m_registeredGameStateTask = true;
                    GfgManagerGame.RegisterGameStateTask();
                }

                LoadScene(sceneId, true);
            }
        }
    }

    private void OnSceneUnloaded(Scene aScene)
    {
        m_scenesData[aScene.buildIndex] = default;

        GfcGameStateScene[] gameStateScenes = Instance.m_gameStateScenes;
        for (int i = 0; i < gameStateScenes.Length; i++)
            if ((int)gameStateScenes[i].Scene == aScene.buildIndex && !gameStateScenes[i].Static)
                gameStateScenes[i].Scene = GfcSceneId.INVALID;
    }

    private void OnSceneLoaded(Scene aScene, LoadSceneMode aLoadSceneMode)
    {
        if (aScene.buildIndex == (int)m_queuedSceneToActivate)
            Debug.Assert(SetActiveScene((GfcSceneId)aScene.buildIndex) == GfcSceneActiveSetResult.SUCCESS);

        SceneData sceneData = m_scenesData[aScene.buildIndex];
        sceneData.LoadState = GfcSceneLoadState.LOADED;
        sceneData.Operation = null;
        m_scenesData[aScene.buildIndex] = sceneData;
    }
}

public enum GfcSceneActiveSetResult
{
    SUCCESS,
    ACTIVATE_QUEUED,
    MAP_INVALID
}

public enum GfcSceneLoadState
{
    UNLOADED,
    LOADING,
    LOADED
}