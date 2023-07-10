using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class OutOfBoundsRespawn : MonoBehaviour
{
    [SerializeField] private CheckpointManager m_checkpointManager;
    [SerializeField] private float m_respawnYCoord = -10;


    // Start is called before the first frame update
    void Awake()
    {
        if (null == m_checkpointManager) m_checkpointManager = GetComponent<CheckpointManager>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (transform.position.y < m_respawnYCoord)
        {
            m_checkpointManager.ResetToSoftCheckpoint();
        }
    }
}
