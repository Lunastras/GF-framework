using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraModifier : GfMovementTriggerable
{
    [SerializeField]
    private float m_targetDistanceMultiplier = 1;

    [SerializeField]
    private uint m_priority = 0;

    public override void MgOnTrigger(MgCollisionStruct collision, GfMovementGeneric movement)
    {
        if (GameManager.gameManager.GetPlayer() == movement.transform)
        {
            CameraController.m_currentCamera.SetDistanceMultiplier(m_targetDistanceMultiplier, m_priority);
        }
    }
}
