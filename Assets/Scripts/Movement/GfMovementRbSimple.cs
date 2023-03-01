using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static System.MathF;

[RequireComponent(typeof(Rigidbody))]
public class GfMovementRbSimple : GfMovementGeneric
{
    [SerializeField]
    protected float m_acceleration = 40;
    [SerializeField]
    protected bool m_touchedParent;
    [SerializeField]
    protected bool m_breakWhenUnparent;
    [SerializeField]
    protected Rigidbody m_rigidbody;
    // Start is called before the first frame update
    protected override void InternalStart()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate() {

       
        
        m_velocity = m_rigidbody.velocity;
        //after phys checks
        if (!m_touchedParent && null != m_parentTransform)
        {
            DetachFromParentTransform();
            if (m_breakWhenUnparent) Debug.Break();
        }

        float deltaTime = Time.deltaTime;
        //before phys updates
        m_touchedParent = false;
        Vector3 movDir = MovementDirComputed();

        Vector3 velChange = movDir * (deltaTime * m_acceleration);
        GfTools.Add3(ref velChange, m_upVec * (-m_mass * deltaTime));

        m_rigidbody.AddForce(velChange, ForceMode.VelocityChange);
       // CalculateEffectiveValues();
       // CalculateVelocity(deltaTime, movDir);
       // CalculateJump();
       // CalculateRotation(deltaTime, movDir);
    }


    private void OnCollisionEnter(Collision other) {
        //other.
    }
}
