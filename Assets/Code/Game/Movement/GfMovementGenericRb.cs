using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GfMovementGenericRb : GfMovementGeneric
{
    protected Rigidbody m_rigidbody;

    protected bool m_isGroundedAux;

    protected bool m_ignoreRunner = false;

    public override void Initialize()
    {
        if (!m_initialisedMovementGeneric)
        {
            m_runner = GetComponent<GfRunnerTemplate>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_initialisedMovementGeneric = true;
            m_transform = transform;
            m_lastRotation = m_transform.rotation;
            if (null == m_statsCharacter) m_statsCharacter = GetComponent<GfgStatsCharacter>();
        }
    }

    public override void LateUpdateBehaviour(float deltaTime) { }

    public override bool UpdatePhysics(float deltaTime, float timeUntilNextUpdate, bool ignorePhysics = false, bool anIgnoreRunner = false)
    {
        double currentTime = Time.timeAsDouble;
        ApplyParentMovement(m_transform.position, deltaTime, currentTime);
        m_isGrounded = m_isGroundedAux;
        if (!m_isGrounded) m_slopeNormal = m_upVec;
        //because physics aren't actually checked here, we first call AfterPhysicsChecks because we did the calculations in the previous rigidbody cycle
        if (!anIgnoreRunner)
        {
            m_runner.AfterPhysChecks(deltaTime);
            m_runner.BeforePhysChecks(deltaTime);
        }

        m_ignoreRunner = anIgnoreRunner;

        UpdateSphericalOrientation(true, deltaTime);

        m_isGroundedAux = false;
        return false; //rigidbody simulation cannot know in advance if we will collide with an object or not
    }

    protected override TransformDelta ApplyParentMovement(Vector3 position, float deltaTime, double currentTime)
    {
        TransformDelta parentMovement = GetParentMovement(position, deltaTime, currentTime);
        m_rigidbody.MovePosition(m_transform.position + parentMovement.DeltaMovement);

        if (!parentMovement.DeltaRotation.Equals(IDENTITY_QUAT))
        {
            m_rigidbody.MoveRotation(parentMovement.DeltaRotation * m_rigidbody.rotation);
        }

        return parentMovement;
    }

    public override Vector3 GetVelocity()
    {
        return m_rigidbody.linearVelocity;
    }

    public override void SetVelocity(Vector3 velocity)
    {
        m_rigidbody.linearVelocity = velocity;
    }

    public override bool UsesRigidbody() { return true; }

    public override Vector3 AddVelocity(Vector3 force)
    {
        m_rigidbody.linearVelocity += force;
        return m_rigidbody.linearVelocity;
    }

    public override Quaternion GetTransformRotation() { return m_rigidbody.rotation; }

    public override void SetPosition(Vector3 position, bool local) { m_rigidbody.position = position; }

    public override void SetRotation(Quaternion rotation, bool local) { m_rigidbody.rotation = rotation; }

    private void OnTriggerEnter(Collider other)
    {
        CallTriggerableEvents(other.gameObject, ZERO3, ZERO3);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contactPoint = collision.GetContact(0);
        CallTriggerableEvents(collision.collider.gameObject, contactPoint.normal, contactPoint.point);

        MgCollisionStruct collisionData = new(contactPoint.normal, m_upVec, collision.collider, contactPoint.point, true, null, false, m_transform.position);
        collisionData.isGrounded = CheckGround(ref collisionData);
        m_isGroundedAux |= collisionData.isGrounded;
        if (m_isGroundedAux)
            m_slopeNormal = contactPoint.normal;

        if (!m_ignoreRunner) m_runner.MgOnCollision(ref collisionData);
    }
}
