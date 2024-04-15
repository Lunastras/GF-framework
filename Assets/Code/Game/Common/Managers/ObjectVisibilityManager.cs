using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//class originally made by Adam Cs on youtube, this class was modified and optimised for the Gf framework
public class ObjectVisibilityManager : MonoBehaviour
{
    private static ObjectVisibilityManager Instance = null;

    [SerializeField]
    private int m_minimumDistance = 100;

    private HashSet<NetworkObject> m_exceptionObjects = null;

    private void Awake()
    {
        if (!GfcManagerServer.HasAuthority)
            Destroy(this);
        if (this != Instance)
            Destroy(Instance);

        Instance = this;
    }

    public static void AddExceptionObject(NetworkObject obj)
    {
        if (GfcManagerServer.HasAuthority)
        {
            if (null == Instance.m_exceptionObjects) Instance.m_exceptionObjects = new(16);
            Instance.m_exceptionObjects.Add(obj);

            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (!obj.IsNetworkVisibleTo(client.Key))
                    obj.NetworkShow(client.Key);
            }
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void FixedUpdate()
    {
        if (GfcManagerServer.Instance && GfcManagerServer.HasAuthority)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (client.Key == NetworkManager.Singleton.LocalClientId) continue;

                foreach (var networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
                {
                    var clientTransform = client.Value.PlayerObject.transform;
                    bool clientIsReady = GfcManagerServer.GetPlayerIsReady(client.Value);
                    bool isVisible = clientIsReady && Vector3.Distance(clientTransform.position, networkObject.transform.position) < m_minimumDistance;

                    if ((null == m_exceptionObjects || !m_exceptionObjects.Contains(networkObject)) && networkObject.IsNetworkVisibleTo(client.Key) != isVisible)
                    {
                        if (isVisible)
                        {
                            networkObject.NetworkShow(client.Key);
                        }
                        else
                        {
                            networkObject.NetworkHide(client.Key);
                        }
                    }
                }
            }
        }
    }
}