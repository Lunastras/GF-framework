using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static System.MathF;

[RequireComponent(typeof(GfMovementGenericRb))]
public class GfRunnerRbGravity : GfRunnerTemplate
{
    [SerializeField]
    public float m_maxFallSpeed = 40;

    //whether we touched the current parent this frame or not
    protected bool m_touchedParent;

    protected Rigidbody m_rigidBody;

    new protected void Awake()
    {
        base.Awake();
        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.useGravity = false;
    }

    public override void BeforePhysChecks(float deltaTime)
    {
        m_touchedParent = false;
        Vector3 upVec = m_mov.GetUpVecRaw();
        GfcTools.Mult3(ref upVec, m_mass * deltaTime * -1f);
        m_rigidBody.AddForce(upVec, ForceMode.VelocityChange);

        Vector3 currentVelocity = m_rigidBody.velocity;
        float sqrSpeed = m_rigidBody.velocity.sqrMagnitude;
        if (sqrSpeed > m_maxFallSpeed * m_maxFallSpeed)
        {
            GfcTools.Div3(ref currentVelocity, System.MathF.Sqrt(sqrSpeed)); //normalize the speed
            GfcTools.Mult3(ref currentVelocity, m_maxFallSpeed);
            m_rigidBody.velocity = currentVelocity;
        }
    }

    public override void AfterPhysChecks(float deltaTime)
    {
        if (!m_touchedParent && null != m_mov.GetParentTransform())
            m_mov.DetachFromParentTransform();
    }

    public override void MgOnCollision(ref MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;

        if (collision.isGrounded && collisionTrans != m_mov.GetParentTransform())
            m_mov.SetParentTransform(collisionTrans);

        m_touchedParent |= collision.isGrounded && m_mov.GetParentTransform() == collisionTrans;
    }
}