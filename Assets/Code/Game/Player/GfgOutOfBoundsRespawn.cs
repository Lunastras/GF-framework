using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class GfgOutOfBoundsRespawn : MonoBehaviour
{
    [SerializeField] private GfgCheckpointManager m_CheckpointManager;
    [SerializeField] private float m_respawnYCoord = -10;


    // Start is called before the first frame update
    void Awake()
    {
        if (null == m_CheckpointManager) m_CheckpointManager = GetComponent<GfgCheckpointManager>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (transform.position.y < m_respawnYCoord)
        {
            m_CheckpointManager.ResetToSoftCheckpoint();
        }
    }
}
