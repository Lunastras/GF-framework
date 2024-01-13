using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using UnityEditor;
using Unity.Mathematics;

[RequireComponent(typeof(NetworkObject))]
public class GfInSceneNetworkObject : NetworkBehaviour
{

    protected void OnServerStartedFunc()
    {
        if (!NetworkObject.IsSpawned)
            NetworkObject.Spawn();
    }

    [SerializeField]
    protected UInt32 m_globalObjectIdHash = 0;

    private bool m_subscribedToOnServerStarted = false;

    // Start is called before the first frame update
    void Start()
    {
        //GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(NetworkObject);
        NetworkManager.Singleton.AddNetworkPrefab(gameObject);
        NetworkManager.Singleton.PrefabHandler.AddHandler(m_globalObjectIdHash, new GfInScenePrefabHandler(NetworkObject, m_globalObjectIdHash));

        if (GfServerManager.HasAuthority)
        {
            if (GfServerManager.HasInstance)
            {
                if (!NetworkObject.IsSpawned)
                    NetworkObject.Spawn();
            }
            else
            {
                m_subscribedToOnServerStarted = true;
                NetworkManager.Singleton.OnServerStarted += OnServerStartedFunc;
            }
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(m_globalObjectIdHash);
            //NetworkManager.Singleton.PrefabHandler.RemoveNetworkPrefab(gameObject);

            if (m_subscribedToOnServerStarted)
                NetworkManager.Singleton.OnServerStarted -= OnServerStartedFunc;
        }
    }
}




public class GfInScenePrefabHandler : INetworkPrefabInstanceHandler
{
    NetworkObject m_networkObject;
    UInt32 m_globalObjectIdHash;

    public GfInScenePrefabHandler(NetworkObject networkObject, UInt32 hash)
    {
        m_networkObject = networkObject;
        m_globalObjectIdHash = hash;
    }

    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        Transform transform = m_networkObject.transform;
        transform.position = position;
        transform.rotation = rotation;
        m_networkObject.ChangeOwnership(ownerClientId);

        return m_networkObject;
    }

    public void Destroy(NetworkObject networkObject)
    {

    }
}
