using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerLevelStart : GfMovementTriggerable
{
    public Transform Checkpoint = null;

    public bool SoftCheckpoint = false;

    void Start()
    {
        if (null == Checkpoint) Checkpoint = transform;
    }

    public override void MgOnTrigger(GfMovementGeneric character)
    {
        if (character.transform == GfgManagerLevel.Player.transform)
            GfgManagerLevel.StartLevel();
    }
}
