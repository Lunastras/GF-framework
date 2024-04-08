using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfcMovementExecuter : MonoBehaviour
{
    GfMovementGenericRb m_mov;

    protected void Awake()
    {
        m_mov = GetComponent<GfMovementGenericRb>();
    }

    protected void FixedUpdate()
    {
        m_mov.UpdatePhysics(Time.fixedDeltaTime, Time.fixedDeltaTime);
    }
}
