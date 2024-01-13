using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerCheckpoint : GfMovementTriggerable
{
    public Transform Checkpoint = null;

    public bool OnlyOnce = false;

    public bool SoftCheckpoint = false;

    private bool m_activated = false;

    void Start()
    {
        if (null == Checkpoint) Checkpoint = transform;
    }

    public override void MgOnTrigger(GfMovementGeneric player)
    {
        if (!m_activated || !OnlyOnce)
        {
            CheckpointManager checkpointManager = player.GetComponent<CheckpointManager>();
            if (checkpointManager)
            {
                m_activated = true;
                checkpointManager.SetCheckpoint(this);
            }
        }
    }
}
