using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;
using MEC;

public class GfClientBehaviour : NetworkBehaviour
{
    [SerializeField]
    protected GameObject m_playerPrefab = null;

    protected NetworkVariable<bool> m_playerIsSpawned = new(false);

    protected NetworkVariable<ulong> m_spawnedPlayerObjectId = new();

    protected bool HasAuthority
    {
        get
        {
            return !NetworkManager.Singleton || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (HasAuthority)
        {
            GfServerManager.Instance.OnPlayerReady += OnPlayerReady;
            GfServerManager.Instance.OnPlayerUnready += OnPlayerUnready;
        }

        if (GfGameManager.StartGameAutomatically) GfServerManager.SetPlayerIsReady(true, OwnerClientId);

        DontDestroyOnLoad(gameObject);
    }

    public override void OnDestroy()
    {
    }

    protected void OnPlayerReady(ulong clientId)
    {
        if (clientId == OwnerClientId && !m_playerIsSpawned.Value)
        {
            NetworkObject networkObject = Instantiate(m_playerPrefab).GetComponent<NetworkObject>();
            networkObject.SpawnWithOwnership(OwnerClientId);
            m_spawnedPlayerObjectId.Value = networkObject.NetworkObjectId;
            m_playerIsSpawned.Value = true;
            networkObject.DestroyWithScene = true;
        }
    }

    protected void OnPlayerUnready(ulong clientId)
    {
        if (clientId == OwnerClientId && m_playerIsSpawned.Value)
        {
            m_spawnedPlayerObjectId.Value = 0;
            m_playerIsSpawned.Value = false;
        }
    }

    public ulong GetPlayerObjectId()
    {
        return m_spawnedPlayerObjectId.Value;
    }

    public NetworkObject GetPlayerObject()
    {
        return GfGameManager.GetNetworkObject(m_spawnedPlayerObjectId.Value);
    }
}
