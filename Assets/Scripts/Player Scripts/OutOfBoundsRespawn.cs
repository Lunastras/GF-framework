using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class OutOfBoundsRespawn : MonoBehaviour
{
    [SerializeField] private GfMovementGeneric m_movement;
    [SerializeField] private CheckpointManager m_checkpointManager;



    public float m_respawnYCoord = -10;
    public Transform m_respawnPoint;
    private Vector3 m_respawnPosition;

    // Start is called before the first frame update
    void Awake()
    {
        if (null == m_movement) m_movement = GetComponent<GfMovementGeneric>();
        if (null == m_checkpointManager) m_checkpointManager = GetComponent<CheckpointManager>();

        if (null == m_respawnPoint) m_respawnPoint = GameObject.Find("Spawnpoint").transform;

        if (m_respawnPoint)
        {
            transform.position = m_respawnPoint.position;
        }
        else
        {
            m_respawnPosition = Vector3.zero;
        }

        if (!NetworkManager.Singleton.IsServer) Destroy(this);
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (transform.position.y < m_respawnYCoord)
        {
            m_checkpointManager.ResetToSoftCheckpoint();
            if (m_movement != null)
            {
                m_movement.SetVelocity(Vector3.zero);
            }
        }
    }

    public void SetRespawn(Vector3 pos)
    {
        m_respawnPosition = pos;
    }
}
