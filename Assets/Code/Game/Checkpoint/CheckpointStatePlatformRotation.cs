using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;

public class CheckpointStatePlatformRotation : CheckpointState
{
    public float SpeedMultiplier = 1;

    public bool WaitOnPhaseEnd = false;

    public bool Loop = true;
    public bool RotatesLocally = false;

    public int CurrentPhase = 0;

    public Quaternion TransformRotation = Quaternion.identity;


    public float TimeUntilStart = 0;


    public float TimeUntilUnpause = 0;

    public float TimeUntilNextPhase = 0;

    public bool TimedPhase = false;


    public PlatformRotation OriginalObject;

    public override void ExecuteCheckpointState()
    {
        OriginalObject.SetCheckpointState(this);
    }
}
