using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlatformRotation : MonoBehaviour
{
    [SerializeField]
    protected RotationPhase[] RotationPhases;
    public float SpeedMultiplier = 1;

    public bool WaitOnPhaseEnd = false;

    public bool Loop = true;
    public bool RotatesLocally = false;
    [SerializeField]
    protected bool PlayOnAwake = true;
    public bool Playing
    {
        get
        {
            return m_timeUntilUnpause <= 0;
        }
    }

    private Rigidbody m_rb;
    private Transform m_transform;
    CheckpointStatePlatformRotation m_checkpointState = null;


    protected int m_currentPhase = 0;

    protected float m_timeUntilStart = 0;


    protected float m_timeUntilUnpause = 0;

    protected float m_timeUntilNextPhase = 0;

    protected bool m_timedPhase = false;


    void Awake()
    {
        CheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_rb = GetComponent<Rigidbody>();

        m_rb.isKinematic = true;
        m_rb.interpolation = RigidbodyInterpolation.Interpolate;
        m_transform = transform;
    }

    protected virtual void OnHardCheckpoint()
    {
        if (null == m_checkpointState) m_checkpointState = new();
        m_checkpointState.OriginalObject = this;

        m_checkpointState.TransformRotation = m_transform.rotation;
        m_checkpointState.SpeedMultiplier = SpeedMultiplier;
        m_checkpointState.RotatesLocally = RotatesLocally;

        m_checkpointState.CurrentPhase = m_currentPhase;
        m_checkpointState.TimeUntilStart = m_timeUntilStart;
        m_checkpointState.TimeUntilNextPhase = m_timeUntilNextPhase;
        m_checkpointState.TimeUntilUnpause = m_timeUntilUnpause;

        CheckpointManager.AddCheckpointState(m_checkpointState);
    }

    public void SetCheckpointState(CheckpointStatePlatformRotation state)
    {
        m_checkpointState = state;
        SpeedMultiplier = state.SpeedMultiplier;
        RotatesLocally = state.RotatesLocally;
        m_transform.rotation = state.TransformRotation;

        m_currentPhase = state.CurrentPhase;
        m_timeUntilStart = m_checkpointState.TimeUntilStart;
        m_timeUntilNextPhase = m_checkpointState.TimeUntilNextPhase;
        m_timeUntilUnpause = m_checkpointState.TimeUntilUnpause;
    }

    protected void OnDestroy()
    {
        CheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float deltaTime = Time.deltaTime;
        m_timeUntilUnpause -= deltaTime;

        if (m_timeUntilUnpause <= 0)
        {
            m_timeUntilStart -= deltaTime;
            if (m_timeUntilStart <= 0)
            {
                m_timeUntilNextPhase -= deltaTime;

                if (m_timedPhase && m_timeUntilNextPhase <= 0)
                {
                    PlayNextPhase();
                    if (WaitOnPhaseEnd)
                        m_timeUntilUnpause = float.MaxValue;
                }
                else
                {
                    Vector3 rotation = RotationPhases[m_currentPhase].Rotation;

                    if (RotatesLocally)
                        m_rb.MoveRotation(m_transform.rotation * Quaternion.Euler(rotation * (SpeedMultiplier * Time.deltaTime)));
                    else
                        m_rb.MoveRotation(Quaternion.Euler(rotation * (SpeedMultiplier * Time.deltaTime)) * m_transform.rotation);
                }
            }
        }
    }

    public void PlayPhase(int phase)
    {
        m_currentPhase = phase;
        m_timeUntilUnpause = 0;
        m_timeUntilNextPhase = RotationPhases[m_currentPhase].Duration;
        m_timedPhase = 0 <= m_timeUntilNextPhase; //if negative, play infinitely until commanded otherwise
        m_timeUntilStart = RotationPhases[m_currentPhase].Delay;
    }

    public void PlayNextPhase()
    {
        m_currentPhase++;
        if (m_currentPhase < RotationPhases.Length)
        {
            PlayPhase(m_currentPhase);
        }
        else if (Loop)
        {
            PlayPhase(0);
        }
        else
        {
            PlayPhase(0);
            m_timeUntilUnpause = float.MaxValue;
        }
    }

    public void Play()
    {
        m_timeUntilUnpause = 0;
    }

    public void Pause(float timeUntilUnpause = float.MaxValue)
    {
        m_timeUntilUnpause = timeUntilUnpause;
    }
}

public class RotationPhase
{
    public Vector3 Rotation;
    public float Duration = -1;
    public float Delay = 0;
}

public enum CurrentState
{
    PAUSED,

    DELAY,

    PLAYING
}
