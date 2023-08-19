using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;
using MEC;

public class GfServerManager : NetworkBehaviour
{
    public static GfServerManager Instance { get; private set; } = null;

    protected NetworkVariable<short> m_sceneBuildIndex = new(-1);

    protected HashSet<NetworkClient> m_readyClients = new(16);

    public Action<ulong> OnPlayerReady = null;

    public Action<ulong> OnPlayerUnready = null;

    protected NetworkVariable<bool> m_hostReady = new(false);

    public static bool HostReady
    {
        get
        {
            return Instance && Instance.m_hostReady.Value;
        }
    }

    public static bool HasAuthority
    {
        get
        {
            return !NetworkManager.Singleton || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer || !GfGameManager.IsMultiplayer;
        }
    }
    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
        {
            Destroy(Instance);
        }

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (HasAuthority)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            ObjectVisibilityManager.AddExceptionObject(NetworkObject);
            if (!NetworkObject.IsSpawned)
                NetworkObject.Spawn();
        }

        Instance = this;
    }

    public static bool HasInstance
    {
        get
        {
            return Instance;

        }
    }

    public static void DestroyInstance()
    {
        if (Instance)
            Destroy(Instance.gameObject);
    }

    public static void OnClientDisconnectCallback(ulong clientId)
    {
        Instance.m_readyClients.Remove(NetworkManager.Singleton.ConnectedClients[clientId]);

        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Destroy(Instance.gameObject);
            Instance = null;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }

    public static void LoadScene(int sceneBuildIndex, ServerLoadingMode loadingMode)
    {
        if (HasAuthority)
        {
            LoadingScreenManager.LoadScene(sceneBuildIndex, loadingMode);
            Instance.LevelChangeClientRpc(sceneBuildIndex, ServerLoadingMode.RECONNECT);
        }
    }

    public static void LoadScene(string sceneName, ServerLoadingMode loadingMode)
    {
        LoadScene(LoadingScreenManager.GetSceneBuildIndexByName(sceneName), loadingMode);
    }

    [ClientRpc]
    protected virtual void LevelChangeClientRpc(int sceneBuildIndex, ServerLoadingMode loadingMode)
    {
        if (!HasAuthority)
            LoadingScreenManager.LoadScene(sceneBuildIndex, loadingMode);
    }

    protected new void OnDestroy()
    {
        Instance = null;
    }

    public static bool GetPlayerIsReady(NetworkClient player)
    {
        return Instance.m_readyClients.Contains(player);
    }

    public static void SetPlayerIsReady(bool ready)
    {
        Instance.SetPlayerIsReadyServerRpc(ready);
    }

    public static void SetPlayerIsReady(bool ready, ulong clientId)
    {
        if (HasAuthority)
        {
            NetworkClient client = NetworkManager.Singleton.ConnectedClients[clientId];

            //if is host
            if (client.ClientId == NetworkManager.Singleton.LocalClientId)
                Instance.m_hostReady.Value = ready;

            if (ready)
            {
                Instance.OnPlayerReady?.Invoke(clientId);
                Instance.m_readyClients.Add(client);
            }
            else
            {
                Instance.OnPlayerUnready?.Invoke(clientId);
                Instance.m_readyClients.Remove(client);
            }
        }
        else Debug.LogError("SetPlayerIsReady should only be called by the host or server.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerIsReadyServerRpc(bool ready, ServerRpcParams serverRpcParams = default)
    {
        SetPlayerIsReady(ready, serverRpcParams.Receive.SenderClientId);
    }

    public static short GetSceneBuildIndex()
    {
        if (Instance)
            return Instance.m_sceneBuildIndex.Value;
        else
            return -1;
    }

    public static void SetSceneBuildIndex(short index)
    {
        if (HasAuthority)
            Instance.m_sceneBuildIndex.Value = index;
    }


}
