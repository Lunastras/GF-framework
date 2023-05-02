using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Networking;

public class EnemyKeySpawner : NetworkBehaviour
{
    [SerializeField]
    private GameObject m_enemy = null;

    private bool m_keyReleased = true;
    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.AddNetworkPrefab(m_enemy);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (m_keyReleased)
            {
                Transform enemyTransform = Instantiate(m_enemy).transform;
                enemyTransform.position = transform.position;
                enemyTransform.GetComponent<NetworkObject>().Spawn();
            }
        }
        else
        {
            m_keyReleased = true;
        }
    }
}
