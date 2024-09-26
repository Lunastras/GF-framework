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

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        if (GfgManagerLevel.Player.transform == movement.transform)
        {
            GfgCameraController3D camera = GfgCameraController.Instance as GfgCameraController3D;

            if (!m_targetDistanceMultiplier.m_ignore)
                camera.SetDistanceMultiplier(m_targetDistanceMultiplier, m_targetDistanceMultiplier.m_priority, m_targetDistanceMultiplier.m_overridePriority);
            if (!m_fovMultiplier.m_ignore)
                camera.SetFovMultiplier(m_fovMultiplier, m_fovSmoothTime, m_fovMultiplier.m_priority, m_fovMultiplier.m_overridePriority);
        }
    }
}
