using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GfPathFindingNamespace;

public class GfTriggerPathfinding : GfMovementTriggerable
{
    [SerializeField]
    private GfPathfinding m_pathfinding;

    private void Start()
    {
        if (null == m_pathfinding) m_pathfinding = GetComponent<GfPathfinding>();
    }

    public override void MgOnTrigger(ref MgCollisionStruct collision, GfMovementGeneric movement)
    {
        NpcController npcController = movement.GetComponent<NpcController>();
        if (npcController) npcController.SetPathfindingManager(m_pathfinding);
    }
}
