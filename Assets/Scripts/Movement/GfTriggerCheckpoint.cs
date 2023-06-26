using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerCheckpoint : GfMovementTriggerable
{
    public Transform Checkpoint = null;

    public bool SoftCheckpoint = false;

    void Start()
    {
        if (null == Checkpoint) Checkpoint = transform;
    }

    public override void MgOnTrigger(ref MgCollisionStruct collisionData, GfMovementGeneric player)
    {
        CheckpointManager checkpointManager = player.GetComponent<CheckpointManager>();
        if (checkpointManager)
            checkpointManager.SetCheckpoint(this);
    }
}
