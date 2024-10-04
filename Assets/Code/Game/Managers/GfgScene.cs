using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GfgScene : MonoBehaviour
{

    [System.Serializable]
    protected struct GfcGameStateScene
    {
        public GfcGameState GameState;
        public GfcSceneId Scene;
        public bool Static;
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

    protected GfcSceneId m_sceneToActivate = GfcSceneId.INVALID;

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);
        m_scenesData = new SceneData[(int)GfcSceneId.COUNT];

        for (int i = 0; i < m_persistentScenesOnStart.Length; i++)
        {
            LoadScene(m_persistentScenesOnStart[i], false, true);
        }

        SetScenePersistent(GfcSceneId.GF_BASE, true);

        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        GfgManagerGame.Instance.OnGameStateChanged += OnGameStateChanged;
        OnGameStateChanged(GfgManagerGame.GetGameState(), GfcGameState.INVALID);
    }

    void OnDestroy()
    {
        if (GfgManagerGame.Instance) GfgManagerGame.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    public static GfcSceneId GetActiveScene() { return (GfcSceneId)SceneManager.GetActiveScene().buildIndex; }

    public static GfcSceneId GetLastActiveScene() { return Instance.m_lastActiveScene; }

    public static GfcSceneActiveSetResult SetActiveScene(GfcSceneId aScene)
    {
        GfcSceneActiveSetResult result;

        GfcSceneId activeScene = GetActiveScene();
        if (activeScene != aScene)
        {
            if (SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex((int)aScene)))
            {
                Instance.m_lastActiveScene = activeScene;
                Instance.m_sceneToActivate = GfcSceneId.INVALID;
                result = GfcSceneActiveSetResult.SUCCESS;
            }
            else
            {
                Instance.m_sceneToActivate = aScene;
                result = GfcSceneActiveSetResult.ACTIVATE_QUEUED;
            }
        }
        else
        {
            result = GfcSceneActiveSetResult.SUCCESS;
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

        SceneData sceneData = Instance.m_scenesData[(int)aScene];
        sceneData.PersistentScene |= aPersistentScene;

        Scene scene = SceneManager.GetSceneByBuildIndex((int)aScene);
        if (!scene.isLoaded && sceneData.LoadState == GfcSceneLoadState.UNLOADED)
        {
            Debug.Log("Loading scene " + aScene);
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

    public static void OnGameStateChanged(GfcGameState aNewGameState, GfcGameState anOldGameState)
    {
        if (aNewGameState == GfcGameState.INVALID)
            return;

        int gameStateIndex = (int)GfgManagerGame.GetPreviousGameState();
        GfcGameStateScene[] gameStateScenes = Instance.m_gameStateScenes;
        if (!gameStateScenes[gameStateIndex].Static)
            gameStateScenes[gameStateIndex].Scene = GetActiveScene();

        if (Instance.m_gameStateScenes.Length > (int)aNewGameState)
        {
            GfcSceneId sceneId = Instance.m_gameStateScenes[(int)aNewGameState].Scene;

            if (sceneId != GfcSceneId.INVALID)
                LoadScene(sceneId, true);
        }
    }

    private void OnSceneUnloaded(Scene aScene)
    {
        m_scenesData[aScene.buildIndex] = default;

        GfcGameStateScene[] gameStateScenes = Instance.m_gameStateScenes;
        for (int i = 0; i < gameStateScenes.Length; i++)
            if (!gameStateScenes[i].Static && (int)gameStateScenes[i].Scene == aScene.buildIndex && !aScene.isLoaded)
                gameStateScenes[i].Scene = GfcSceneId.INVALID;
    }

    private void OnSceneLoaded(Scene aScene, LoadSceneMode aLoadSceneMode)
    {
        if (aScene.buildIndex == (int)m_sceneToActivate)
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