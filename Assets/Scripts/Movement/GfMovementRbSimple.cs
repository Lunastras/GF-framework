using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static System.MathF;

[RequireComponent(typeof(Rigidbody))]
public class GfMovementRbSimple : GfMovementSimple
{
    [SerializeField]
    protected Rigidbody m_rigidbody;
    // Start is called before the first frame update
    protected override void InternalStart()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.deltaTime;

        m_velocity = m_rigidbody.velocity;
        //after phys checks
        if (!m_touchedParent && null != m_parentTransform)
        {
            DetachFromParentTransform();
            if (m_breakWhenUnparent) Debug.Break();
        }

        m_touchedParent = m_jumpedThisFrame = false;
        Vector3 movDir = MovementDirComputed();

        CalculateEffectiveValues();
        CalculateVelocity(deltaTime, movDir);
        CalculateJump();
        CalculateRotation(deltaTime, movDir);

        m_rigidbody.velocity = m_velocity;
    }


    /*

    private void OnCollisionStay(Collision other)
    {
        ContactPoint contactPoint = other.GetContact(0);
        Vector3 normal = contactPoint.normal;

        m_velocity = m_rigidbody.velocity;
        GfTools.Minus3(ref m_velocity, (Vector3.Dot(m_velocity, normal)) * normal);
        m_rigidbody.velocity = m_velocity;
    }
    */
}
