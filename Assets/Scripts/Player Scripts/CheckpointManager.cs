using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using MEC;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance = null;

    [SerializeField] private bool m_canTriggerHardCheckpoints = true;

    [SerializeField] private GfMovementGeneric m_movementGeneric = null;

    [SerializeField] private StatsCharacter m_statsCharacter = null;

    private GfTriggerCheckpoint m_currentSoftCheckpoint = null;
    private GfTriggerCheckpoint m_currentHardCheckpoint = null;
    private Vector3 m_initialPos;

    private Transform m_transform = null;

    public static Action OnHardCheckpoint;

    private Vector3 m_respawnPoint;

    private List<CheckpointState> m_checkpointStates = new(64);

    public static void AddCheckpointState(CheckpointState checkpointState)
    {
        Instance.m_checkpointStates.Add(checkpointState);
    }

    private void Start()
    {
        if (Instance != null && !GameManager.IsMultiplayer && m_canTriggerHardCheckpoints)
        {
            Debug.LogWarning("Another CheckpointManager Instance that can trigger hard checkpoints was found (" + Instance.name + "), destroying it now...");
            Destroy(Instance);
        }

        m_respawnPoint = GameObject.Find("Spawnpoint").transform.position;

        if (m_canTriggerHardCheckpoints)
        {
            Instance = this;
            OnHardCheckpoint = null; //reset callbacks
        }

        if (null == m_movementGeneric) m_movementGeneric = GetComponent<GfMovementGeneric>();
        if (null == m_statsCharacter) m_statsCharacter = GetComponent<StatsCharacter>();
        m_transform = m_statsCharacter.transform;
        m_initialPos = m_respawnPoint;
        m_transform.position = m_respawnPoint;
    }

    public void SetCheckpoint(GfTriggerCheckpoint checkpoint)
    {
        if (checkpoint.SoftCheckpoint && checkpoint != m_currentSoftCheckpoint)
        {
            if (m_canTriggerHardCheckpoints)
                HudManager.TriggerSoftCheckpointVisuals();

            m_currentSoftCheckpoint = checkpoint;
        }
        else if (checkpoint != m_currentHardCheckpoint && m_canTriggerHardCheckpoints)//hard checkpoint
        {
            if (m_canTriggerHardCheckpoints)
                HudManager.TriggerHardCheckpointVisuals();

            m_currentHardCheckpoint = checkpoint;
            if (null != OnHardCheckpoint && !GameManager.IsMultiplayer)
            {
                m_checkpointStates.Clear();
                OnHardCheckpoint();
            }
        }
    }

    public bool HasCheckpointRegistered(GfTriggerCheckpoint checkpoint)
    {
        return checkpoint == m_currentHardCheckpoint || checkpoint == m_currentSoftCheckpoint;
    }

    public void ResetToSoftCheckpoint(float damage = 0, bool canKill = false)
    {
        if (m_canTriggerHardCheckpoints)
            HudManager.ResetSoftCheckpointVisuals();

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

    protected void ExecuteCheckpointStates()
    {
        int stateCount = m_checkpointStates.Count;

        for (int i = 0; i < stateCount; ++i)
            m_checkpointStates[i].ExecuteCheckpointState();
    }

    public void ResetToHardCheckpoint()
    {
        if (m_canTriggerHardCheckpoints)
            HudManager.ResetHardCheckpointVisuals();

        if (m_currentHardCheckpoint && m_canTriggerHardCheckpoints)
        {
            m_transform.position = m_currentHardCheckpoint.Checkpoint.position;
            HostilityManager.DestroyAllCharacters(false);
            //do not reset checkpoint states if we are playing multiplayer
            if (!GameManager.IsMultiplayer)
            {
                //start coroutine to execute checkpoint states in the next turn
                Timing.RunCoroutine(_ExecuteCheckpointsInNextFrame());
            }
        }
        else
        {
            m_transform.position = m_initialPos;
        }
    }


    private IEnumerator<float> _ExecuteCheckpointsInNextFrame()
    {
        yield return Timing.WaitForOneFrame;
        ExecuteCheckpointStates();
    }

    public GfTriggerCheckpoint GetSoftCheckpoint() { return m_currentSoftCheckpoint; }
    public GfTriggerCheckpoint GetHardCheckpoint() { return m_currentHardCheckpoint; }
}