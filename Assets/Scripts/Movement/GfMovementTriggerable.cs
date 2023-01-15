using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfMovementTriggerable : MonoBehaviour
{
    public abstract void MgOnTrigger(MgCollisionStruct collision, GfMovementGeneric movement);
}
