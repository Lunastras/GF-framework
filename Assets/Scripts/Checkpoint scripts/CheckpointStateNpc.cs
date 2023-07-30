using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointStateNpc : CheckpointState
{
    public Vector3 Position = Vector3.zero;
    public Quaternion Rotation = Quaternion.identity;
    public Vector3 Scale = new Vector3(1, 1, 1);
    public float CurrentHp = 100;
    public GameObject Prefab = null;

    public Transform MovementParent = null;

    public uint MovementParentPriority = 0;

    public Transform MovementParentSpherical = null;
    public uint MovementGravityPriority = 0;

    public Vector3 Velocity;

    public Vector3 UpVec;

    public bool WasFollowingPlayer = false;

    public override void ExecuteCheckpointState()
    {
        StatsNpc statsNpc = GfPooling.Instantiate(Prefab).GetComponent<StatsNpc>();
        statsNpc.SetCheckpointState(this);
    }

    public void CopyState(CheckpointStateNpc state)
    {
        Position = state.Position;
        Rotation = state.Rotation;
        Scale = state.Scale;
        CurrentHp = state.CurrentHp;
        Prefab = state.Prefab;
        Velocity = state.Velocity;
        MovementParent = state.MovementParent;
        MovementParentPriority = state.MovementParentPriority;
        MovementParentSpherical = state.MovementParentSpherical;
        MovementGravityPriority = state.MovementGravityPriority;
        UpVec = state.UpVec;
    }
}
