using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerRevertToSoftCheckpoint : GfMovementTriggerable
{
    [SerializeField] GfTriggerCheckpoint m_triggerCheckpoint = null;

    [SerializeField] bool m_onlyForPlayer = true;


    public float Damage = 0;

    public bool CanKillOnRevert = false;

    void Start()
    {
        if (null == m_triggerCheckpoint) Debug.LogWarning("Warning! GfTriggerSoftKill requires a GfTriggerCheckpoint reference for its inspector element m_triggerCheckpoint.");
    }

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        GfgCheckpointManager GfgCheckpointManager = movement.GetComponent<GfgCheckpointManager>();
        if ((!m_onlyForPlayer || GfgManagerLevel.Player.transform == movement.transform)
            && GfgCheckpointManager && (GfgCheckpointManager.HasCheckpointRegistered(m_triggerCheckpoint) || null == m_triggerCheckpoint))
            GfgCheckpointManager.ResetToSoftCheckpoint(Damage, CanKillOnRevert);
    }
}
