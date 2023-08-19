using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerRevertToCheckpoint : GfMovementTriggerable
{
    [SerializeField] GfTriggerCheckpoint m_triggerCheckpoint = null;

    public float Damage = 0;

    public bool CanKillOnRevert = false;

    void Start()
    {
        if (null == m_triggerCheckpoint) Debug.LogWarning("Warning! GfTriggerSoftKill requires a GfTriggerCheckpoint reference for its inspector element m_triggerCheckpoint.");
    }

    public override void MgOnTrigger(GfMovementGeneric player)
    {
        CheckpointManager checkpointManager = player.GetComponent<CheckpointManager>();
        if (checkpointManager && checkpointManager.HasCheckpointRegistered(m_triggerCheckpoint))
            checkpointManager.ResetToSoftCheckpoint(Damage, CanKillOnRevert);

    }
}
