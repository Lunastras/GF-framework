using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GfgPathFindingNamespace;

public class GfTriggerPathfinding : GfMovementTriggerable
{
    [SerializeField]
    private GfgPathfinding m_pathfinding;

    private void Start()
    {
        if (null == m_pathfinding) m_pathfinding = GetComponent<GfgPathfinding>();
    }

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        NpcController npcController = movement.GetComponent<NpcController>();
        if (npcController) npcController.SetPathfindingManager(m_pathfinding);
    }
}
