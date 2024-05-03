using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static System.MathF;

public class GfRunnerSimple : GfRunnerTemplate
{
    [SerializeField]
    protected float m_acceleration = 35;
    [SerializeField]
    protected float m_deacceleration = 40;
    [SerializeField]
    protected float m_midAirAcceleration = 20;
    [SerializeField]
    protected float m_midAirDeacceleration = 10;
    [SerializeField]
    protected float m_maxFallSpeed = 50;
    [SerializeField]
    protected float m_jumpForce = 14;
    [SerializeField]
    protected float m_jumpExtensionMassCoef = 0.6f;

    [SerializeField]
    protected float m_turnSpeed = 400;

    [SerializeField]
    protected bool m_requireJumpRelease = true;

    [SerializeField]
    protected int m_maxJumps = 2;

    protected int m_currentJumpsCount = 0;

    protected float m_effectiveDeacceleration;
    protected float m_effectiveAcceleration;
    public bool CanDoubleJump { get; protected set; }
    public bool CanExtendJump { get; protected set; }
    protected bool m_jumpedThisFrame = false;

    //whether we touched the current parent this frame or not
    protected bool m_touchedParent;

    protected float m_currentRotationSpeed = 0;
    protected float m_rotationSmoothRef = 0;

    protected bool m_jumpFlagReleased = true;

    protected bool m_isExtendingJump = false;

    protected float m_effectiveMass = 0;

    protected float m_effectiveSpeed;

    protected PriorityValue<float> m_accelerationMultiplier = new(1);
    protected PriorityValue<float> m_deaccelerationMultiplier = new(1);


    [SerializeField]
    protected bool m_breakWhenUnparent = false;

    public override void BeforePhysChecks(float deltaTime)
    {
        m_touchedParent = m_jumpedThisFrame = false;
        Vector3 movDir = m_mov.MovementDirComputed(MovementDirRaw, CanFly);

        CalculateEffectiveValues();
        CalculateVelocity(deltaTime, movDir);
        CalculateJump();
        CalculateRotation(deltaTime, movDir);
    }

    public override void AfterPhysChecks(float deltaTime)
    {
        if (!m_touchedParent && null != m_mov.GetParentTransform())
        {
            m_mov.DetachFromParentTransform();
            if (m_breakWhenUnparent) Debug.Break();
        }
    }

    protected void CalculateEffectiveValues()
    {
        m_isExtendingJump &= 0 < Vector3.Dot(m_mov.GetVelocity(), m_mov.GetUpvecRotation()); //if falling, stop jump extending

        if (m_mov.GetIsGrounded() || CanFly)
        {
            m_effectiveAcceleration = m_acceleration;
            m_effectiveDeacceleration = m_deacceleration;
        }
        else
        {
            m_effectiveAcceleration = m_midAirAcceleration;
            m_effectiveDeacceleration = m_midAirDeacceleration;
        }

        m_effectiveAcceleration *= m_accelerationMultiplier;
        m_effectiveDeacceleration *= m_deaccelerationMultiplier;

        m_effectiveSpeed = m_speed * m_speedMultiplier;
        m_effectiveMass = m_mass * m_massMultiplier;
        if (m_isExtendingJump) m_effectiveMass *= m_jumpExtensionMassCoef;
    }

    protected virtual void CalculateRotation(float deltaTime, Vector3 movDir)
    {
        Vector3 m_rotationUpVec = m_mov.GetUpvecRotation();

        //ROTATION SECTION
        if (movDir != Vector3.zero)
        {
            Vector3 desiredForwardVec = GfcTools.RemoveAxis(movDir, m_rotationUpVec);
            Vector3 forwardVec = GfcTools.RemoveAxis(transform.forward, m_rotationUpVec);

            float turnAmount = m_turnSpeed * deltaTime;
            float angleDistance = -GfcTools.SignedAngleDeg(desiredForwardVec, forwardVec, m_rotationUpVec); //angle between the current and desired rotation
            float degreesMovement = Min(System.MathF.Abs(angleDistance), turnAmount);

            if (degreesMovement > 0.05f)
            {
                Quaternion angleAxis = Quaternion.AngleAxis(Sign(angleDistance) * degreesMovement, m_rotationUpVec);
                m_mov.SetRotation(angleAxis * m_transform.rotation);
            }
        }
    }

    protected virtual void CalculateVelocity(float deltaTime, Vector3 movDir)
    {
        Vector3 slope = m_mov.GetSlope();
        Vector3 velocity = m_mov.GetVelocity();

        float movDirMagnitude = movDir.magnitude;
        if (movDirMagnitude > 0.000001f) GfcTools.Div(ref movDir, movDirMagnitude); //normalise

        float verticalFallSpeed = Vector3.Dot(slope, velocity);
        float fallMagn = 0, fallMaxDiff = -verticalFallSpeed - m_maxFallSpeed; //todo
        //remove vertical factor from the velocity to calculate the horizontal plane velocity easier
        Vector3 effectiveVelocity = velocity;

        if (!CanFly)
        {
            //remove vertical component of velocity if we can't fly
            effectiveVelocity.x -= slope.x * verticalFallSpeed;
            effectiveVelocity.y -= slope.y * verticalFallSpeed;
            effectiveVelocity.z -= slope.z * verticalFallSpeed;

            if (fallMaxDiff < 0)
                fallMagn = Min(-fallMaxDiff, m_effectiveMass * deltaTime); //speed under maxFallSpeed         
            else
                fallMagn = -Min(fallMaxDiff, m_effectiveDeacceleration * deltaTime);//speed equal to maxFallSpeed or higher
        }

        float currentSpeed = effectiveVelocity.magnitude;
        float dotMovementVelDir = 0;
        if (currentSpeed > 0.000001F)
        {
            Vector3 velDir = effectiveVelocity;
            GfcTools.Div(ref velDir, currentSpeed);
            dotMovementVelDir = Vector3.Dot(movDir, velDir);
        }

        float desiredSpeed = m_effectiveSpeed * movDirMagnitude;
        float speedInDesiredDir = currentSpeed * Max(0, dotMovementVelDir);

        float minAux = Min(speedInDesiredDir, desiredSpeed);
        Vector3 deacceleration = effectiveVelocity;

        deacceleration = effectiveVelocity;
        deacceleration.x -= movDir.x * minAux;
        deacceleration.y -= movDir.y * minAux;
        deacceleration.z -= movDir.z * minAux;

        if (desiredSpeed > speedInDesiredDir) GfcTools.RemoveAxis(ref deacceleration, movDir);

        float unwantedSpeed = deacceleration.magnitude;
        if (unwantedSpeed > 0.000001F) GfcTools.Div(ref deacceleration, unwantedSpeed);

        float accMagn = Min(Max(0, desiredSpeed - speedInDesiredDir), deltaTime * m_effectiveAcceleration);
        float deaccMagn = Min(unwantedSpeed, m_effectiveDeacceleration * deltaTime);

        //GfTools.Mult3(ref movDir, accMagn);
        GfcTools.Mult(ref deacceleration, deaccMagn);
        GfcTools.Mult(ref slope, fallMagn);

        GfcTools.Mult(ref movDir, accMagn); //acceleration

        GfcTools.Add(ref velocity, movDir); //add acceleration
        GfcTools.Minus(ref velocity, deacceleration);//add deacceleration
        GfcTools.Minus(ref velocity, slope); //add vertical speed change  

        m_mov.SetVelocity(velocity);
    }

    protected virtual void CalculateJump()
    {
        if (FlagJump)
        {
            FlagJump = false;
            if (m_jumpFlagReleased || !m_requireJumpRelease)
            {
                m_jumpFlagReleased = false;
                PerformJump();
            }
        }
        else
        {
            m_jumpFlagReleased = true;
            m_isExtendingJump = false;
        }
    }

    protected virtual void PerformJump()
    {
        DefaultJump();
    }

    protected virtual void DefaultJump()
    {
        if (m_maxJumps > m_currentJumpsCount)
        {
            m_isExtendingJump = m_currentJumpsCount == 0;
            m_currentJumpsCount++;

            //we use the rotation upVec because it feels more natural when the player's rotation is still changing
            Vector3 m_velocity = m_mov.GetVelocity();
            Vector3 upVecRotation = m_mov.GetUpvecRotation();
            GfcTools.RemoveAxis(ref m_velocity, upVecRotation);
            GfcTools.Add(ref m_velocity, upVecRotation * m_jumpForce);
            m_mov.SetVelocity(m_velocity);
            m_mov.SetIsGrounded(false);

            m_mov.DetachFromParentTransform();
        }
    }

    public override void MgOnCollision(ref MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;
        if (collision.isGrounded) m_currentJumpsCount = 0;

        if (collision.isGrounded && collisionTrans != m_mov.GetParentTransform())
            m_mov.SetParentTransform(collisionTrans);

        m_isExtendingJump &= !collision.isGrounded && collision.selfUpVecAngle <= m_mov.GetUpperSlopeLimitDeg();
        m_touchedParent |= collision.isGrounded && m_mov.GetParentTransform() == collisionTrans;
    }

    public PriorityValue<float> GetAccelerationMultiplier()
    {
        return m_accelerationMultiplier;
    }

    public void SetAccelerationMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        m_accelerationMultiplier.SetValue(multiplier, priority, overridePriority);
    }

    public PriorityValue<float> GetDeaccelerationMultiplier()
    {
        return m_deaccelerationMultiplier;
    }

    public void SetDeaccelerationMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        m_deaccelerationMultiplier.SetValue(multiplier, priority, overridePriority);
    }
}