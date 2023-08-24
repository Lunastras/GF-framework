using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;
using MEC;
using Unity.Netcode.Transports.UTP;

public class GfGameManager : MonoBehaviour
{
    public static GameObject GfNetworkGameManagerPrefab = null;
    //current instance
    public static GfGameManager Instance;

    [SerializeField] private GameObject m_serverManagerPrefab = null;

    [SerializeField] private GameObject m_gameAssetsPrefab = null;

    protected float m_initialFixedDeltaTime = 0.02f;

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

    public static bool IsMultiplayer
    {
        get
        {
            return Instance.m_isMultiplayer;
        }
    }

    private NetworkSpawnManager m_spawnManager;

    private float m_currentTimeScale;

    protected bool m_initialised = false;

    // Start is called before the first frame update

    public void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        m_initialFixedDeltaTime = Time.fixedDeltaTime;

        SpawnGameAssets();
    }

    private void Start()
    {
        //NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalCallbackFunc;
        NetworkManager.Singleton.OnServerStopped += OnServerStoppedFunc;

        m_currentTimeScale = Time.timeScale;



        DontDestroyOnLoad(gameObject);

        m_spawnManager = NetworkManager.Singleton.SpawnManager;
        m_initialised = true;
    }

    public static bool GameInitialised
    {
        get
        {
            return GfServerManager.HasInstance;
        }
    }

    protected void OnServerStoppedFunc(bool noIdea)
    {
        Debug.Log("The server was stopped");
        m_isMultiplayer = false;
        GfServerManager.DestroyInstance();
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

    public static void SetServerIp(string ip, ushort port = 7779)
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = ip;
        transport.ConnectionData.Port = port;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public static void ShutdownServer()
    {
        NetworkManager.Singleton.Shutdown();
        Instance.m_isMultiplayer = false;
        if (Instance.m_gameAssetsInstance)
            Destroy(Instance.m_gameAssetsInstance);
        Instance.m_gameAssetsInstance = null;
        Instance.m_gameType = GameMultiplayerType.NONE;
        CheckpointManager.OnHardCheckpoint = null;
        Time.fixedDeltaTime = Instance.m_initialFixedDeltaTime;
        Time.timeScale = 1;
    }

    public static void SpawnGameAssets()
    {
        if (null == Instance.m_gameAssetsInstance)
        {
            Instance.m_gameAssetsInstance = Instantiate(Instance.m_gameAssetsPrefab);
        }
    }

    public static void StartGame(GameMultiplayerType gameType)
    {
        if (!GameInitialised)
        {
            Instance.m_gameType = gameType;
            SpawnGameAssets();

            switch (GameType)
            {
                case (GameMultiplayerType.SINGLEPLAYER):
                    Debug.Log("Singleplayer started ");
                    Instance.m_isMultiplayer = false;
                    Instantiate(Instance.m_serverManagerPrefab);
                    NetworkManager.Singleton.StartHost();
                    GfGameManager.SingleplayerStart();
                    break;

                case (GameMultiplayerType.SERVER):
                    Debug.Log("Server started ");
                    Instance.m_isMultiplayer = true;
                    Instantiate(Instance.m_serverManagerPrefab);
                    NetworkManager.Singleton.StartServer();
                    GfGameManager.MultiplayerStart();
                    break;

                case (GameMultiplayerType.HOST):
                    Debug.Log("Host started ");
                    Instance.m_isMultiplayer = true;
                    Instantiate(Instance.m_serverManagerPrefab);
                    NetworkManager.Singleton.StartHost();
                    GfGameManager.MultiplayerStart();
                    break;

                case (GameMultiplayerType.CLIENT):
                    Debug.Log("Client started ");
                    Instance.m_isMultiplayer = true;
                    NetworkManager.Singleton.StartClient();
                    GfGameManager.MultiplayerStart();
                    break;
            }
        }
    }

    public static Camera Camera { get { return CameraController.Instance.Camera; } }

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
        GfLevelManager.Instance.CanPause = false;
        if (GfLevelManager.IsPaused())
            GfLevelManager.PauseToggle();

        LoadingScreenManager.LoadScene(0, ServerLoadingMode.SHUTDOWN, GameMultiplayerType.NONE);
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
        if (!GfLevelManager.IsPaused() || IsMultiplayer) Time.timeScale = timeScale;
    }

}

[System.Serializable]
public enum GameMultiplayerType
{
    SINGLEPLAYER,
    SERVER,
    HOST,
    CLIENT,
    NONE
}

[System.Serializable]
public enum GameState
{
    LEVEL_IDLE,
    LEVEL_STARTED,
    LEVEL_ENDED
}