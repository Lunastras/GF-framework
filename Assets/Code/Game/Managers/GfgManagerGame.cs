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
using Unity.Mathematics;

public class GfgManagerGame : MonoBehaviour
{
    public static GameObject GfNetworkGameManagerPrefab = null;

    protected static GfgManagerGame m_instance;

    public static GfgManagerGame Instance { get { return m_instance; } protected set { m_instance = value; } }

    public Action<GfcGameState, GfcGameState> OnGameStateChanged;

    [SerializeField] private GameObject m_serverManagerPrefab = null;

    protected float m_initialFixedDeltaTime = 0.02f;

    protected Camera m_camera = null;

    [SerializeField] private GfcGameState m_gameState = GfcGameState.INVALID;

    private GfcGameState m_previousGameState = GfcGameState.INVALID;

    private CoroutineHandle m_gameStateTransitionHandle;

    private CoroutineHandle m_queuedGameStateTransitionHandle;

    private GfcGameState m_gameStateAfterTransition = GfcGameState.INVALID;

    private int m_queuedGameStateTransitionPriority;

    private bool m_fadeOutGameStateTransition = false;

    private object m_gameStateLockHandle = null;
    private uint m_gameStateLockKey = 0;

    public static CoroutineHandle GetGameStateTransitionHandle() { return Instance.m_gameStateTransitionHandle; }

    public static GfcGameState GetGameState() { return Instance.m_gameState; }

    public static bool GameStateTransitioningFadeOutPhase() { return GameStateTransitioning() && Instance.m_fadeOutGameStateTransition; }

    public static bool GameStateTransitioningFadeInPhase() { return GameStateTransitioning() && !Instance.m_fadeOutGameStateTransition; }

    public static bool GameStateTransitioning() { return Instance.m_gameStateTransitionHandle.IsValid; }

    public static GfcGameState GetPreviousGameState() { return Instance.m_previousGameState; }

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

    private static void SetGameStateInternal(GfcGameState aState)
    {
        if (aState != Instance.m_gameState)
            Instance.m_previousGameState = Instance.m_gameState;

        Instance.m_gameState = aState;
        Instance.OnGameStateChanged?.Invoke(aState, Instance.m_previousGameState);
    }

    public static CoroutineHandle SetGameState(GfcGameState aState, bool aSmoothTransition = true, GfxTransitionType aTransition = GfxTransitionType.BLACK_FADE, int aPriorityInQueue = 0)
    {
        CoroutineHandle transitionHandle = default;

        if (!GameStateIsLocked())
        {
            if (aState == GfcGameState.INVALID)
            {
                Debug.LogError("The game state passed is invalid.");
            }
            else if (aSmoothTransition)
            {
                if (Instance.m_gameStateAfterTransition != aState && (!Instance.m_queuedGameStateTransitionHandle.IsValid || aPriorityInQueue > Instance.m_queuedGameStateTransitionPriority))
                {
                    Instance.m_queuedGameStateTransitionPriority = aPriorityInQueue;
                    Timing.KillCoroutines(Instance.m_queuedGameStateTransitionHandle);
                    GfcTransitionParent transition = GfxTransitions.Instance.GetSingleton(aTransition);
                    transitionHandle = Timing.RunCoroutine(Instance._TransitionGameState(aState, transition));
                    Instance.m_queuedGameStateTransitionHandle = transitionHandle;
                }
            }
            else
            {
                SetGameStateInternal(aState);
            }
        }

        return transitionHandle;
    }

    private IEnumerator<float> _TransitionGameState(GfcGameState aState, GfcTransitionParent aTransition)
    {
        yield return Timing.WaitForOneFrame; //don't execute immediately after RunCoroutine

        if (GameStateTransitioningFadeInPhase())
            yield return Timing.WaitUntilDone(m_gameStateTransitionHandle);

        Timing.KillCoroutines(m_gameStateTransitionHandle);

        m_gameStateTransitionHandle = m_queuedGameStateTransitionHandle; // make this the new main coroutine
        m_queuedGameStateTransitionHandle = default;
        m_queuedGameStateTransitionPriority = 0;
        m_gameStateAfterTransition = aState;

        GfcInput.InputEnabled = false;

        m_fadeOutGameStateTransition = false;
        yield return Timing.WaitUntilDone(aTransition.StartFadeIn());

        SetGameStateInternal(aState);

        GfcInput.InputEnabled = true;
        m_fadeOutGameStateTransition = true;
        m_gameStateAfterTransition = GfcGameState.INVALID;
        m_gameStateTransitionHandle = Timing.RunCoroutine(_TransitionGameStateFadeOut(aTransition));
    }

    private IEnumerator<float> _TransitionGameStateFadeOut(GfcTransitionParent aTransition)
    {
        yield return Timing.WaitUntilDone(aTransition.StartFadeOut());
        yield return Timing.WaitForOneFrame;

        m_fadeOutGameStateTransition = false;
        m_gameStateTransitionHandle = default;
    }

    public static uint LockGameState(object aLockHandle)
    {
        uint key = 0;
        if (Instance.m_gameStateLockHandle == null)
        {
            Instance.m_gameStateLockHandle = aLockHandle;
            Instance.m_gameStateLockKey = key = (uint)((double)UnityEngine.Random.Range(0.0f, 1.0f) * uint.MaxValue);
        }
        else
            Debug.LogError("Tried to lock the game state when its already locked by the object: (" + Instance.m_gameStateLockHandle + ").");

        return key;
    }

    public static bool UnlockGameState(uint aLockKey)
    {
        bool success = false;
        if (CanUnlockGameState(aLockKey))
        {
            Instance.m_gameStateLockHandle = null;
            success = true;
        }
        else
            Debug.LogError("The unlock key is invalid.");

        return success;
    }

    public static bool GameStateIsLocked() { return Instance.m_gameStateLockHandle != null; }

    public static bool CanUnlockGameState(uint aLockKey) { return Instance.m_gameStateLockHandle == null || aLockKey == Instance.m_gameStateLockKey; }

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