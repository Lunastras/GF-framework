using System.Collections;
using UnityEngine;
using System;
using Unity.Netcode;
using System.Collections.Generic;
using MEC;
using System.Diagnostics;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance = null;

    [SerializeField] private bool m_canTriggerHardCheckpoints = false;

    [SerializeField] private GfMovementGeneric m_movementGeneric = null;

    [SerializeField] private StatsCharacter m_statsCharacter = null;

    private GfTriggerCheckpoint m_currentSoftCheckpoint = null;
    private GfTriggerCheckpoint m_currentHardCheckpoint = null;
    private Vector3 m_initialPos;

    private Transform m_transform = null;

    public static Action OnHardCheckpoint;

    private Vector3 m_respawnPoint;

    private List<CheckpointState> m_checkpointStates = new(64);

    protected bool m_isSoftReseting = false;

    public static void AddCheckpointState(CheckpointState checkpointState)
    {
        Instance.m_checkpointStates.Add(checkpointState);
    }

    private void Awake()
    {
        if (m_canTriggerHardCheckpoints)
        {
            Instance = this;
        }

        if (Instance != this && !GfcManagerGame.IsMultiplayer && m_canTriggerHardCheckpoints)
        {
            Destroy(Instance);
        }

        if (null == m_movementGeneric) m_movementGeneric = GetComponent<GfMovementGeneric>();
        if (null == m_statsCharacter) m_statsCharacter = GetComponent<StatsCharacter>();
        m_transform = m_statsCharacter.transform;

        GameObject respawnGameobject = GameObject.Find("Spawnpoint");
        if (respawnGameobject)
        {
            Transform respawnTransform = respawnGameobject.transform;
            Vector3 upVec = respawnTransform.up;

            m_movementGeneric.Initialize();
            m_movementGeneric.SetUpVec(upVec);
            m_movementGeneric.OrientToUpVecForced();
            m_transform.rotation = Quaternion.LookRotation(respawnTransform.forward, upVec);
            m_respawnPoint = respawnTransform.position;
        }
        else
        {
            UnityEngine.Debug.Log("No spawnpoint found.");
        }

        m_initialPos = m_respawnPoint;
        m_transform.position = m_respawnPoint;
    }

    private void Start()
    {
        m_checkpointStates.Clear();
        OnHardCheckpoint?.Invoke();
    }

    public void SetCheckpoint(GfTriggerCheckpoint checkpoint)
    {
        if (checkpoint != m_currentSoftCheckpoint)
        {
            m_currentSoftCheckpoint = checkpoint;
        }

        if (checkpoint != m_currentHardCheckpoint && m_canTriggerHardCheckpoints)//hard checkpoint
        {
            m_currentHardCheckpoint = checkpoint;
            if (null != OnHardCheckpoint && !GfcManagerGame.IsMultiplayer)
            {
                m_checkpointStates.Clear();
                OnHardCheckpoint();
            }
        }

        GfManagerLevel.OnCheckpointSet(this, !checkpoint.SoftCheckpoint);
    }

    public bool HasCheckpointRegistered(GfTriggerCheckpoint checkpoint)
    {
        return checkpoint == m_currentHardCheckpoint || checkpoint == m_currentSoftCheckpoint;
    }

    public void ResetToSoftCheckpoint(float damage = 0, bool canKill = false)
    {
        if (!m_isSoftReseting)
        {
            m_isSoftReseting = true;
            float delay = GfManagerLevel.OnCheckpointReset(this, false);

            if (delay > 0)
                Timing.RunCoroutine(_ResetToSoftCheckpoint(delay, damage, canKill));
            else
                InternalResetToSoftCheckpoint(damage, canKill);
        }
    }

    protected IEnumerator<float> _ResetToSoftCheckpoint(float delay, float damage, bool canKill)
    {
        yield return Timing.WaitForOneFrame;
        yield return Timing.WaitForSeconds(delay);
        InternalResetToSoftCheckpoint(damage, canKill);
    }

    protected void InternalResetToSoftCheckpoint(float damage, bool canKill)
    {
        if (damage != 0)
        {
            float currentHp = m_statsCharacter.GetCurrentHealth();
            if (!canKill && currentHp - damage <= 0) //if can't kill and the damage will kill character, set hp to 1
            {
                damage = currentHp - 1;
            }

            m_statsCharacter.Damage(new(damage, m_statsCharacter.transform.position, Vector3.zero));
        }

        if (m_currentSoftCheckpoint)
        {
            m_movementGeneric.SetPosition(m_currentSoftCheckpoint.Checkpoint.position);
        }
        else
        {
            m_movementGeneric.SetPosition(m_initialPos);
        }

        if (m_canTriggerHardCheckpoints)
        {
            CameraController.LookFowardInstance();
            CameraController.SnapToTargetInstance();
        }

        m_isSoftReseting = false;
        m_movementGeneric.SetVelocity(Vector3.zero);
    }

    protected void ExecuteCheckpointStates()
    {
        int stateCount = m_checkpointStates.Count;

        for (int i = 0; i < stateCount; ++i)
            m_checkpointStates[i].ExecuteCheckpointState();

        GfManagerLevel.CheckpointStatesExecuted(this);
    }

    public void ResetToHardCheckpoint()
    {
        if (m_canTriggerHardCheckpoints && !GfcManagerGame.IsMultiplayer)
        {
            GfManagerLevel.OnCheckpointReset(this, true);

            if (m_currentHardCheckpoint)
            {
                m_movementGeneric.SetPosition(m_currentHardCheckpoint.Checkpoint.position);

            }
            else
            {
                m_movementGeneric.SetPosition(m_initialPos);
            }

            GfcManagerCharacters.DestroyAllCharacters(false);
            //do not reset checkpoint states if we are playing multiplayer
            if (!GfcManagerGame.IsMultiplayer)
            {
                //start coroutine to execute checkpoint states in the next turn
                Timing.RunCoroutine(_ExecuteCheckpointsInNextFrame());
            }
        }
        else if (GfcManagerGame.IsMultiplayer)
        {
            ResetToSoftCheckpoint();
        }
    }

    private void OnDestroy()
    {
        Instance = null;
        OnHardCheckpoint = null;
    }


    private IEnumerator<float> _ExecuteCheckpointsInNextFrame()
    {
        yield return Timing.WaitForOneFrame;
        ExecuteCheckpointStates();
    }

    public GfTriggerCheckpoint GetSoftCheckpoint() { return m_currentSoftCheckpoint; }
    public GfTriggerCheckpoint GetHardCheckpoint() { return m_currentHardCheckpoint; }
}
