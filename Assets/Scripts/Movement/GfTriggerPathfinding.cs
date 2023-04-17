using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerPathfinding : GfMovementTriggerable
{
    [SerializeField]
    private GfPathfinding m_pathfinding;

    private void Start()
    {
        if (null == m_pathfinding) m_pathfinding = GetComponent<GfPathfinding>();
    }

    public override void MgOnTrigger(MgCollisionStruct collision, GfMovementGeneric movement)
    {
        NpcController npcController = movement.GetComponent<NpcController>();
        if (npcController) npcController.SetPathfindingManager(m_pathfinding);
    }
}
