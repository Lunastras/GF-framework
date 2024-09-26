using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgCameraController2D : GfgCameraController
{
    [SerializeField] protected Vector2 m_targetOffset;

    [SerializeField] protected float m_movementSmoothTime = 0.05f;

    private Vector3 m_movementSmoothRef;

    public Vector3 GetDesiredPosition()
    {
        Vector3 pos = m_transform.position;

        if (m_target)
        {
            Vector3 targetPos = m_target.position;
            pos.x = targetPos.x + m_targetOffset.x;
            pos.y = targetPos.y + m_targetOffset.y;
        }

        return pos;
    }

    public override void SnapToTarget()
    {
        m_transform.position = GetDesiredPosition();
    }

    public override void Move(float deltaTime)
    {
        m_transform.position = Vector3.SmoothDamp(m_transform.position, GetDesiredPosition(), ref m_movementSmoothRef, m_movementSmoothTime);
    }
}
