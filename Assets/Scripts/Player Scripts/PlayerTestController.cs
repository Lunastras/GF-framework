using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static System.MathF;

public class PlayerTestController : MovementGeneric
{
    [SerializeField]
    private float m_acceleration = 10;
    [SerializeField]
    private float m_deacceleration = 10;
    [SerializeField]
    private float m_midAirAcceleration = 10;
    [SerializeField]
    private float m_midAirDeacceleration = 10;
    [SerializeField]
    private float m_maxFallSpeed = 40;
    [SerializeField]
    private float m_jumpForce = 40;

    [SerializeField]
    private bool breakWhenUnparent = true;
    public float AccelerationCoef = 1;
    public float DeaccelerationCoef = 1;
    private float m_effectiveDeacceleration;
    private float m_effectiveAcceleration;
    public bool CanDoubleJump { get; protected set; }
    public bool CanExtendJump { get; protected set; }

    //whether we touched the current parent this frame or not
    private bool m_touchedParent;

    private Vector3 m_effectiveMovementDir;

    // Start is called before the first frame update
    protected override void InternalStart() { }
    protected override void BeforePhysChecks(float deltaTime)
    {
        m_touchedParent = false;
        m_effectiveMovementDir = MovementDir;
        // Debug.Log("CALC MOVEMENT WAS CAALLED, GROUNDED IS " + m_isGrounded);

        if (SlopeNormal != Vector3.up)
        {
            Quaternion q = GfTools.RotationTo(Vector3.up, SlopeNormal);
            m_effectiveMovementDir = q * MovementDir;
            //Debug.Log("movement Dir is: ")
        }

        CalculateEffectiveValues();
        CalculateVelocity(deltaTime);
        CalculateJump();
    }

    protected override void AfterPhysChecks(float deltaTime)
    {
        if (!m_touchedParent && null != m_parentTransform)
        {
            Debug.Log("I haven't touched " + m_parentTransform.name + " this frame");
            DetachFromParent();
            if (breakWhenUnparent) Debug.Break();
        }

    }

    protected void CalculateEffectiveValues()
    {
        if (m_isGrounded)
        {
            m_effectiveAcceleration = m_acceleration;
            m_effectiveDeacceleration = m_deacceleration;
        }
        else
        {
            m_effectiveAcceleration = m_midAirAcceleration;
            m_effectiveDeacceleration = m_midAirDeacceleration;
        }
        m_effectiveAcceleration *= AccelerationCoef;
        m_effectiveDeacceleration *= DeaccelerationCoef;
    }

    void CalculateVelocity(float deltaTime)
    {
        float verticalFallSpeed = Vector3.Dot(SlopeNormal, Velocity);

        float fallMagn, fallMaxDiff = -verticalFallSpeed - m_maxFallSpeed;

        if (fallMaxDiff < 0)
        { //speed under maxFallSpeed
            fallMagn = Min(-fallMaxDiff, m_mass * deltaTime);
        }
        else
        { //speed equal to maxFallSpeed or higher
            fallMagn = -Min(fallMaxDiff, m_effectiveDeacceleration * deltaTime);
        }

        //remove vertical factor from the velocity to calculate the horizontal plane velocity easier
        Vector3 horizontalVelocity = Velocity - SlopeNormal * verticalFallSpeed;

        float currentSpeed = horizontalVelocity.magnitude;
        float dotMovementVelDir = Vector3.Dot(m_effectiveMovementDir, currentSpeed > 0.00001F ? horizontalVelocity / currentSpeed : Zero3);

        float desiredSpeed = m_speed * m_movementDirMagnitude;
        float speedInDesiredDir = currentSpeed * Max(0, dotMovementVelDir);

        Vector3 unwantedVelocity = horizontalVelocity - m_effectiveMovementDir * Min(speedInDesiredDir, desiredSpeed);
        float unwantedSpeed = unwantedVelocity.magnitude;
        unwantedVelocity = unwantedSpeed > 0.00001F ? unwantedVelocity / unwantedSpeed : Zero3; //normalised

        float accMagn = Min(Max(0, desiredSpeed - speedInDesiredDir), deltaTime * m_effectiveAcceleration);
        float deaccMagn = Min(unwantedSpeed, m_effectiveDeacceleration * deltaTime);

        // Debug.Log("The Added deacc is: " + (unwantedVelocity));
        // Debug.Log("The Added acc is: " + (m_effectiveMovementDir * accMagn));
        //Debug.Log("The Added vertical is: " + (SlopeNormal * fallMagn));

        Velocity += m_effectiveMovementDir * accMagn - unwantedVelocity * deaccMagn - SlopeNormal * fallMagn;
    }

    void CalculateJump()
    {
        if (JumpTrigger)
        {
            JumpTrigger = false;
            Velocity = Velocity - UpVec * Vector3.Dot(UpVec, Velocity);
            Velocity = Velocity + UpVec * m_jumpForce;
            m_isGrounded = false;
            Debug.Log("I HAVE JUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUMPED");
            DetachFromParent();
        }

    }

    protected override void MgOnCollision(MgCollisionStruct collision)
    {
        //        Debug.Log("I came into collision WITH " + collision.collider.name);
        Transform collisionTrans = collision.collider.transform;
        bool auxGrounded = CheckGround(collision);

        if (auxGrounded && m_parentTransform == null && !collision.pushback)
        {
            // Debug.Break();
            SetParentTransform(collisionTrans);
        }

        m_touchedParent |= m_parentTransform == collisionTrans;
    }
}