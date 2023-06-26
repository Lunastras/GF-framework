using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private GfMovementGeneric m_movementGeneric = null;

    [SerializeField] private StatsCharacter m_statsCharacter = null;

    private GfTriggerCheckpoint m_currentSoftCheckpoint = null;
    private GfTriggerCheckpoint m_currentHardCheckpoint = null;
    private Vector3 m_initialPos;

    private Transform m_transform = null;

    private void Start()
    {
        if (null == m_movementGeneric) m_movementGeneric = GetComponent<GfMovementGeneric>();
        if (null == m_statsCharacter) m_statsCharacter = GetComponent<StatsCharacter>();
        m_transform = m_statsCharacter.transform;
        m_initialPos = m_transform.position;
    }

    public void SetCheckpoint(GfTriggerCheckpoint checkpoint)
    {
        if (checkpoint.SoftCheckpoint && checkpoint != m_currentSoftCheckpoint)
        {
            m_currentSoftCheckpoint = checkpoint;
        }
        else if (checkpoint != m_currentHardCheckpoint)//hard checkpoint
        {
            m_currentHardCheckpoint = checkpoint;
        }
    }

    public bool HasCheckpointRegistered(GfTriggerCheckpoint checkpoint)
    {
        return checkpoint == m_currentHardCheckpoint || checkpoint == m_currentSoftCheckpoint;
    }

    public void ResetToSoftCheckpoint(float damage = 0, bool canKill = false)
    {
        if (damage != 0)
        {
            float currentHp = m_statsCharacter.GetCurrentHealth();
            if (!canKill && currentHp - damage <= 0) //if can't kill and the damage will kill character, set hp to 1
            {
                damage = currentHp - 1;
            }

            m_statsCharacter.Damage(damage);
        }

        if (m_currentSoftCheckpoint)
        {
            m_transform.position = m_currentSoftCheckpoint.Checkpoint.position;
        }
        else
        {
            m_transform.position = m_initialPos;
        }

        m_movementGeneric.SetVelocity(Vector3.zero);
    }

    public void ResetToHardCheckpoint()
    {
        if (m_currentSoftCheckpoint)
        {
            m_transform.position = m_currentSoftCheckpoint.Checkpoint.position;
        }
        else
        {
            m_transform.position = m_initialPos;
        }
    }

    public GfTriggerCheckpoint GetSoftCheckpoint() { return m_currentSoftCheckpoint; }
    public GfTriggerCheckpoint GetHardCheckpoint() { return m_currentHardCheckpoint; }
}
