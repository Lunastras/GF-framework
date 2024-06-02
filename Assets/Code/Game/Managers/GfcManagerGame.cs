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

public class GfcManagerGame : MonoBehaviour
{
    public static GameObject GfNetworkGameManagerPrefab = null;
    //current instance
    public static GfcManagerGame Instance;

    [SerializeField] private GameObject m_serverManagerPrefab = null;

    protected float m_initialFixedDeltaTime = 0.02f;
    protected Camera m_camera = null;

    public static float GetInitialFixedDeltaTime
    {
        get
        {
            return Instance.m_initialFixedDeltaTime;
        }
    }

    public static GameMultiplayerType GameType
    {
        get
        {
            GameMultiplayerType gameType = GameMultiplayerType.NONE;
            if (Instance)
                gameType = Instance.m_gameType;

            return gameType;
        }
    }

    protected GameObject m_gameAssetsInstance = null;

    public bool m_isMultiplayer { get; protected set; } = false;

    protected GameMultiplayerType m_gameType = GameMultiplayerType.NONE;

    public static bool IsMultiplayer { get { return Instance.m_isMultiplayer; } }

    private NetworkSpawnManager m_spawnManager;

    private float m_currentTimeScale;

    protected bool m_initialised = false;

    public static bool GameInitialised
    {
        get
        {
            return !NetworkManager.Singleton || GfcManagerServer.HasInstance;
        }
    }

    public void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        m_initialFixedDeltaTime = Time.fixedDeltaTime;
    }

    public static void InitializeGfBase()
    {
        int gfBaseSceneIndex = (int)GfcScene.GF_BASE;
        Scene sceneManager = SceneManager.GetSceneByBuildIndex(gfBaseSceneIndex);
        if (!sceneManager.isLoaded)
            SceneManager.LoadScene(gfBaseSceneIndex, LoadSceneMode.Additive);
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

    protected void ConnectionApprovalCallbackFunc(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        Debug.Log("The response was: " + response.Approved);
        if (!response.Approved)
        {
            QuitToMenu();
        }
    }

    public static void ValidateGameManager()
    {
        int sceneBuildIndex = (int)GfcScene.GF_BASE;
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
            Instance.m_gameType = GameMultiplayerType.NONE;
            Time.fixedDeltaTime = Instance.m_initialFixedDeltaTime;
            Time.timeScale = 1;
        }

        GfgCheckpointManager.OnHardCheckpoint = null;
    }

    public static void StartGame(GameMultiplayerType gameType)
    {
        if (!GameInitialised && Instance)
        {
            Instance.m_gameType = gameType;

            switch (GameType)
            {
                case (GameMultiplayerType.SINGLEPLAYER):
                    Debug.Log("Singleplayer started ");
                    Instance.m_isMultiplayer = false;
                    Instantiate(Instance.m_serverManagerPrefab);
                    NetworkManager.Singleton?.StartHost();
                    GfcManagerGame.SingleplayerStart();
                    break;

                case (GameMultiplayerType.SERVER):
                    Debug.Log("Server started ");
                    Instance.m_isMultiplayer = true;
                    Instantiate(Instance.m_serverManagerPrefab);
                    NetworkManager.Singleton.StartServer();
                    GfcManagerGame.MultiplayerStart();
                    break;

                case (GameMultiplayerType.HOST):
                    Debug.Log("Host started ");
                    Instance.m_isMultiplayer = true;
                    Instantiate(Instance.m_serverManagerPrefab);
                    NetworkManager.Singleton.StartHost();
                    GfcManagerGame.MultiplayerStart();
                    break;

                case (GameMultiplayerType.CLIENT):
                    Debug.Log("Client started ");
                    Instance.m_isMultiplayer = true;
                    NetworkManager.Singleton.StartClient();
                    GfcManagerGame.MultiplayerStart();
                    break;
            }
        }
    }

    public static Camera Camera { set { Instance.m_camera = value; } get { return Instance.m_camera; } } //todo

    public static bool Initialised()
    {
        return Instance.m_initialised;
    }

    protected GameMultiplayerType GetGameType() { return GameType; }

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

        GfgManagerSceneLoader.LoadScene(0, false, ServerLoadingMode.SHUTDOWN, GameMultiplayerType.NONE);
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
        if (Instance.m_spawnManager.SpawnedObjects.TryGetValue(objectNetworkId, out NetworkObject obj))
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

[System.Serializable]
public enum GameState
{
    LEVEL_IDLE,
    LEVEL_STARTED,
    LEVEL_ENDED
}