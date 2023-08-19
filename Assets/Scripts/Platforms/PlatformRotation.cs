using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlatformRotation : MonoBehaviour
{
    public Vector3 Rotation;
    public float SpeedMultiplier = 1;
    public bool RotatesLocally = false;
    [SerializeField]
    protected bool PlayOnAwake = false;
    public bool Playing { get; protected set; } = false;

    private Rigidbody m_rb;
    private Transform m_transform;

    CheckpointStatePlatformRotation m_checkpointState = null;

    void Awake()
    {
        CheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
    }

    protected virtual void OnHardCheckpoint()
    {
        if (null == m_checkpointState) m_checkpointState = new();
        m_checkpointState.TransformRotation = m_transform.rotation;
        m_checkpointState.SpeedMultiplier = SpeedMultiplier;
        m_checkpointState.Rotation = Rotation;
        m_checkpointState.RotatesLocally = RotatesLocally;
        m_checkpointState.OriginalObject = this;

        CheckpointManager.AddCheckpointState(m_checkpointState);
    }

    public void SetCheckpointState(CheckpointStatePlatformRotation state)
    {
        m_checkpointState = state;
        Rotation = state.Rotation;
        SpeedMultiplier = state.SpeedMultiplier;
        RotatesLocally = state.RotatesLocally;
        m_transform.rotation = state.TransformRotation;
    }

    protected void OnDestroy()
    {
        CheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_rb = GetComponent<Rigidbody>();

        m_rb.isKinematic = true;
        m_rb.interpolation = RigidbodyInterpolation.Interpolate;
        m_transform = transform;
        Playing = PlayOnAwake;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Playing)
        {
            if (RotatesLocally)
                m_rb.MoveRotation(m_transform.rotation * Quaternion.Euler(Rotation * (SpeedMultiplier * Time.deltaTime)));

            else
                m_rb.MoveRotation(Quaternion.Euler(Rotation * (SpeedMultiplier * Time.deltaTime)) * m_transform.rotation);
        }
    }

    public void Play()
    {
        Playing = true;
    }

    public void Stop()
    {
        Playing = false;
    }
}
