using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;

public class CheckpointStatePlatformRotation : CheckpointState
{
    public float SpeedMultiplier = 1;

    public bool RotatesLocally = false;

    public Vector3 Rotation;

    public Quaternion TransformRotation = Quaternion.identity;

    public PlatformRotation OriginalObject;

    public override void ExecuteCheckpointState()
    {
        OriginalObject.SetCheckpointState(this);
    }
}
