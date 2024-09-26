using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;
using MEC;
using Unity.Netcode.Transports.UTP;
using log4net.Filter;

public class GfgManagerGame : MonoBehaviour
{
    public static GameObject GfNetworkGameManagerPrefab = null;

    protected static GfgManagerGame m_instance;

    public static GfgManagerGame Instance { get { return m_instance; } protected set { m_instance = value; } }

    [SerializeField] private GameObject m_serverManagerPrefab = null;

    protected float m_initialFixedDeltaTime = 0.02f;

    protected Camera m_camera = null;

    [SerializeField] private GfcGameState m_gameState = GfcGameState.INVALID;

    private GfcCoroutineHandle m_gameStateTransitionHandle;

    private GfcGameState m_currentTransitionState;

    private GfcGameState m_previousTransitionState;

    public Action<GfcGameState, GfcGameState> OnGameStateChanged;

    public static CoroutineHandle GetGameStateTransitionHandle() { return Instance.m_gameStateTransitionHandle; }

    public static GfcGameState GetGameState() { return Instance.m_gameState; }

    public static bool GameStateTransitioning() { return Instance.m_gameStateTransitionHandle.CoroutineHandle.IsValid; }

    public static GfcGameState GetPreviousGameState() { return Instance.m_previousTransitionState; }

    public static float GetInitialFixedDeltaTime { get { return Instance.m_initialFixedDeltaTime; } }

    public static GfcGameMultiplayerType GameType
    {
        get
        {
            GfcGameMultiplayerType gameType = GfcGameMultiplayerType.NONE;
            if (Instance)
                gameType = Instance.m_gameType;

            return gameType;
        }
    }

    protected GameObject m_gameAssetsInstance = null;

    public bool m_isMultiplayer { get; protected set; } = false;

    protected GfcGameMultiplayerType m_gameType = GfcGameMultiplayerType.NONE;

    public static bool IsMultiplayer { get { return NetworkManager.Singleton && Instance.m_isMultiplayer; } }

    private NetworkSpawnManager m_spawnManager;

    private float m_currentTimeScale;

    protected bool m_initialised = false;

    public static bool GameInitialised { get { return !NetworkManager.Singleton || GfcManagerServer.HasInstance; } }

    public void Awake()
    {
        this.SetSingleton(ref m_instance);
        m_initialFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Start()
    {
        m_spawnManager = null;

        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalCallbackFunc;
            NetworkManager.Singleton.OnServerStopped += OnServerStoppedFunc;
            m_spawnManager = NetworkManager.Singleton.SpawnManager;
        }

        m_currentTimeScale = Time.timeScale;
        DontDestroyOnLoad(gameObject);

        m_initialised = true;
    }

    protected void OnServerStoppedFunc(bool noIdea)
    {
        Debug.Log("The server was stopped");
        m_isMultiplayer = false;
        GfcManagerServer.DestroyInstance();
        QuitToMenu();
    }

    private static void SetGameStateInternal(GfcGameState aState, bool anAllowQueueIfNull = false)
    {
        if (aState != Instance.m_gameState)
            Instance.m_previousTransitionState = Instance.m_gameState;

        Instance.m_gameState = aState;
        Instance.OnGameStateChanged?.Invoke(aState, Instance.m_previousTransitionState);
    }

    public static CoroutineHandle SetGameState(GfcGameState aState, bool aSmoothTransition = true, bool anIgnoreInvalidGameState = false, bool anAllowQueueIfNull = false, bool aWaitForSmoothTransitionFadeOut = true)
    {
        CoroutineHandle transitionHandle = default;

        if (aState == GfcGameState.INVALID)
        {
            if (!anIgnoreInvalidGameState) Debug.LogError("The game state passed is invalid.");
        }
        else if (Instance.m_gameStateTransitionHandle.CoroutineIsRunning)
        {
            Debug.LogWarning("Already in the process of transitioning game state " + Instance.m_currentTransitionState + ", cannot transition to state: " + aState);
        }
        else if (aSmoothTransition)
        {
            Debug.Assert(!anAllowQueueIfNull, "Singleton queueing is not supported if we are using the smooth transition.");

            Instance.m_currentTransitionState = aState;
            transitionHandle = Instance.m_gameStateTransitionHandle.RunCoroutineIfNotRunning(Instance._TransitionGameState(aWaitForSmoothTransitionFadeOut));
        }
        else
        {
            SetGameStateInternal(aState, anAllowQueueIfNull);
        }

        return transitionHandle;
    }

    private IEnumerator<float> _TransitionGameStateFadeOut(bool aFinishRoutineOnEnd)
    {
        yield return Timing.WaitUntilDone(GfxUiTools.FadeOverlayAlpha(0, 0.3f));

        if (aFinishRoutineOnEnd)
        {
            Instance.m_gameStateTransitionHandle.Finished();
        }
    }

    private IEnumerator<float> _TransitionGameState(bool aWaitForSmoothTransitionFadeOut)
    {
        yield return Timing.WaitUntilDone(GfxUiTools.FadeOverlayAlpha(1, 0.3f));
        SetGameStateInternal(m_currentTransitionState, false);
        CoroutineHandle fadeOutRoutine = Timing.RunCoroutine(_TransitionGameStateFadeOut(!aWaitForSmoothTransitionFadeOut));

        if (aWaitForSmoothTransitionFadeOut)
        {
            yield return Timing.WaitUntilDone(fadeOutRoutine);
            m_gameStateTransitionHandle.Finished();
        }
        else
        {
            Instance.m_gameStateTransitionHandle.CoroutineHandle = fadeOutRoutine;
        }
    }

    protected void ConnectionApprovalCallbackFunc(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        Debug.Log("The response was: " + response.Approved);
        if (!response.Approved)
        {
            QuitToMenu();
        }
    }

    public static void InitializeGfBase()
    {
        int sceneBuildIndex = (int)GfcSceneId.GF_BASE;
        if (!SceneManager.GetSceneByBuildIndex(sceneBuildIndex).isLoaded)
        {
            SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Additive);
            GfgScene.SetScenePersistent(GfcSceneId.GF_BASE, true);
        }
    }

    public static void SetServerIp(string ip, ushort port = 7779)
    {
        UnityTransport transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = ip;
        transport.ConnectionData.Port = port;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public static void StopGame()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.Shutdown();
            Instance.m_isMultiplayer = false;
        }

        if (Instance)
        {
            if (Instance.m_gameAssetsInstance)
                Destroy(Instance.m_gameAssetsInstance);
            Instance.m_gameAssetsInstance = null;
            Instance.m_gameType = GfcGameMultiplayerType.NONE;
            Time.fixedDeltaTime = Instance.m_initialFixedDeltaTime;
            Time.timeScale = 1;
        }

        GfgCheckpointManager.OnHardCheckpoint = null;
    }

    public static void StartGame(GfcGameMultiplayerType gameType)
    {
        if (!GameInitialised && Instance)
        {
            Instance.m_gameType = gameType;

            switch (GameType)
            {
                case (GfcGameMultiplayerType.SINGLEPLAYER):
                    Debug.Log("Singleplayer started ");
                    Instance.m_isMultiplayer = false;
                    Instantiate(Instance.m_serverManagerPrefab);
                    NetworkManager.Singleton?.StartHost();
                    GfgManagerGame.SingleplayerStart();
                    break;

                case (GfcGameMultiplayerType.SERVER):
                    Debug.Log("Server started ");
                    Instance.m_isMultiplayer = true;
                    Instantiate(Instance.m_serverManagerPrefab);
                    NetworkManager.Singleton.StartServer();
                    GfgManagerGame.MultiplayerStart();
                    break;

                case (GfcGameMultiplayerType.HOST):
                    Debug.Log("Host started ");
                    Instance.m_isMultiplayer = true;
                    Instantiate(Instance.m_serverManagerPrefab);
                    NetworkManager.Singleton.StartHost();
                    GfgManagerGame.MultiplayerStart();
                    break;

                case (GfcGameMultiplayerType.CLIENT):
                    Debug.Log("Client started ");
                    Instance.m_isMultiplayer = true;
                    NetworkManager.Singleton.StartClient();
                    GfgManagerGame.MultiplayerStart();
                    break;
            }
        }
    }

    public static bool Initialised()
    {
        return Instance.m_initialised;
    }

    protected GfcGameMultiplayerType GetGameType() { return GameType; }

    public static void MultiplayerStart()
    {
        Time.timeScale = 1;
        Instance.m_isMultiplayer = true;
        Instance.m_spawnManager = NetworkManager.Singleton.SpawnManager;
    }

    public static void SingleplayerStart()
    {
        Instance.m_spawnManager = NetworkManager.Singleton.SpawnManager;
    }

    public static void QuitToMenu()
    {
        GfgManagerLevel.Instance.CanPause = false;
        if (GfgManagerLevel.IsPaused())
            GfgManagerLevel.PauseToggle();

        GfgManagerSceneLoader.LoadScene(0, GfcGameState.MAIN_MENU, false, GfcGameMultiplayerType.NONE, ServerLoadingMode.SHUTDOWN);
    }

    public static void QuitGame()
    {
        Application.Quit();
    }

    public static NetworkObject GetNetworkObject(ulong objectNetworkId)
    {
        Instance.m_spawnManager.SpawnedObjects.TryGetValue(objectNetworkId, out NetworkObject obj);
        return obj;
    }

    public static T GetComponentFromNetworkObject<T>(ulong objectNetworkId) where T : Component
    {
        T component = null;
        if (Instance && Instance.m_spawnManager != null && Instance.m_spawnManager.SpawnedObjects.TryGetValue(objectNetworkId, out NetworkObject obj))
            component = obj.GetComponent<T>();

        return component;
    }

    public static Dictionary<ulong, NetworkObject> GetNetworkObjects()
    {
        return Instance.m_spawnManager.SpawnedObjects;
    }

    public static float GetTimeScale() { return Instance.m_currentTimeScale; }

    public static void SetTimeScale(float timeScale)
    {
        Instance.m_currentTimeScale = timeScale;
        if (!GfgManagerLevel.IsPaused() || IsMultiplayer) Time.timeScale = timeScale;
    }
}