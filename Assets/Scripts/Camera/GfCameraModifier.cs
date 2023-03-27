using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfCameraModifier : GfMovementTriggerable
{
    [SerializeField]
    private PriorityValueSetter<float> m_targetDistanceMultiplier = new(1);

    [SerializeField]
    private PriorityValueSetter<float> m_fovMultiplier = new(1);

    [SerializeField]
    private int m_fovSmoothTime = 1;

    public override void MgOnTrigger(MgCollisionStruct collision, GfMovementGeneric movement)
    {
        if (GameManager.gameManager.GetPlayer() == movement.transform)
        {
            if (!m_targetDistanceMultiplier.m_ignore)
                CameraController.m_currentCamera.SetDistanceMultiplier(m_targetDistanceMultiplier, m_targetDistanceMultiplier.m_priority, m_targetDistanceMultiplier.m_overridePriority);
            if (!m_fovMultiplier.m_ignore)
                CameraController.m_currentCamera.SetFovMultiplier(m_fovMultiplier, m_fovSmoothTime, m_fovMultiplier.m_priority, m_fovMultiplier.m_overridePriority);
        }
    }
}
