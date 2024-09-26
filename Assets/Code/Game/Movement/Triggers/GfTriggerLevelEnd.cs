using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerLevelEnd : GfMovementTriggerable
{
    public override void MgOnTrigger(GfMovementGeneric character)
    {
        if (character.transform == GfgManagerLevel.Player.transform)
            GfgManagerLevel.EndLevel();
    }
}