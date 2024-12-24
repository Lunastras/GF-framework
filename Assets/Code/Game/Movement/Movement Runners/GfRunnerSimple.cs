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
    protected float m_accelerationMidAir = 20;
    [SerializeField]
    protected float m_deaccelerationMidAir = 10;

    [SerializeField]
    protected float m_deacelerationDashCoef = 5;

    [SerializeField]
    protected float m_maxFallSpeed = 50;

    [SerializeField]
    private float m_dashVelocity = 20f;

    [SerializeField]
    protected float m_jumpForce = 14;
    [SerializeField]
    protected float m_jumpExtensionMassCoef = 0.6f;

    [SerializeField]
    protected float m_turnSpeed = 400;

    [SerializeField]
    protected int m_maxJumps = 2;

    [SerializeField]
    protected float m_dashStatusDuration = 0.6f;

    [SerializeField]
    protected int m_maxDashes = 2;

    [SerializeField]
    protected bool m_enableRotation = true;

    protected int m_currentJumpsCount = 0;

    protected int m_currentDashesCount = 0;

    public bool Dashing { get; private set; } = false;

    protected float m_dashDurationLeft = 0;

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

    protected bool m_dashFlagReleased = true;

    protected bool m_isExtendingJump = false;

    protected float m_effectiveMass = 0;

    protected float m_effectiveSpeed;

    protected PriorityValue<float> m_accelerationMultiplier = new(1);
    protected PriorityValue<float> m_deaccelerationMultiplier = new(1);


    [SerializeField]
    protected bool m_breakWhenUnparent = false;

    public override void BeforePhysChecks(float deltaTime)
    {
        m_dashDurationLeft -= deltaTime;
        Dashing = m_dashDurationLeft > 0;

        m_touchedParent = m_jumpedThisFrame = false;
        Vector3 movDir = m_movement.MovementDirComputed(MovementDirRaw, CanFly);

        CalculateEffectiveValues();
        CalculateVelocity(deltaTime, movDir);
        CalculateJump();
        CalculateDash(deltaTime, MovementDirRaw);
        if (m_enableRotation) CalculateRotation(deltaTime, movDir);
    }

    public override void AfterPhysChecks(float deltaTime)
    {
        if (!m_touchedParent && null != m_movement.GetParentTransform())
        {
            m_movement.DetachFromParentTransform();
            if (m_breakWhenUnparent) Debug.Break();
        }
    }

    protected void CalculateEffectiveValues()
    {
        m_isExtendingJump &= !Dashing && 0 < Vector3.Dot(m_movement.GetVelocity(), m_movement.GetUpvecRotation()); //if falling, stop jump extending

        if (m_movement.GetGrounded() || CanFly)
        {
            m_effectiveAcceleration = m_acceleration;
            m_effectiveDeacceleration = m_deacceleration;
        }
        else
        {
            m_effectiveAcceleration = m_accelerationMidAir;
            m_effectiveDeacceleration = m_deaccelerationMidAir;
        }

        m_effectiveAcceleration *= m_accelerationMultiplier;
        m_effectiveDeacceleration *= m_deaccelerationMultiplier;

        m_effectiveSpeed = m_speed * m_speedMultiplier;
        m_effectiveMass = m_mass * m_massMultiplier;

        if (Dashing)
        {
            m_effectiveDeacceleration *= m_deacelerationDashCoef;

        }

        if (m_isExtendingJump) m_effectiveMass *= m_jumpExtensionMassCoef;
    }

    protected virtual void CalculateRotation(float deltaTime, Vector3 movDir)
    {
        Vector3 m_rotationUpVec = m_movement.GetUpvecRotation();
        GfcTools.RemoveAxisKeepMagnitude(ref movDir, m_movement.GetUpVecRaw());

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
                m_movement.SetRotation(angleAxis * m_transform.rotation);
            }
        }
    }

    protected virtual void CalculateVelocity(float deltaTime, Vector3 movDir)
    {
        Vector3 slope = m_movement.GetSlope();
        Vector3 velocity = m_movement.GetVelocity();

        float movDirMagnitude = movDir.magnitude;
        if (movDirMagnitude > 0.000001f) GfcTools.Div(ref movDir, movDirMagnitude); //normalise

        float verticalFallSpeed = Vector3.Dot(slope, velocity);
        float fallMagn = 0, fallMaxDiff = -verticalFallSpeed - m_maxFallSpeed;
        //remove vertical factor from the velocity to calculate the horizontal plane velocity easier
        Vector3 effectiveVelocity = velocity;

        if (!CanFly && !Dashing)
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
        deacceleration.x -= movDir.x * minAux;
        deacceleration.y -= movDir.y * minAux;
        deacceleration.z -= movDir.z * minAux;

        if (desiredSpeed > speedInDesiredDir) GfcTools.RemoveAxis(ref deacceleration, movDir);

        float unwantedSpeed = deacceleration.magnitude;
        if (unwantedSpeed > 0.000001F) GfcTools.Div(ref deacceleration, unwantedSpeed); //normalize deacceleration

        float accMagn = Min(Max(0, desiredSpeed - speedInDesiredDir), deltaTime * m_effectiveAcceleration);
        float deaccMagn = Min(unwantedSpeed, m_effectiveDeacceleration * deltaTime);

        //GfTools.Mult3(ref movDir, accMagn);
        GfcTools.Mult(ref deacceleration, deaccMagn);
        GfcTools.Mult(ref movDir, accMagn); //acceleration
        GfcTools.Mult(ref slope, fallMagn);

        GfcTools.Add(ref velocity, movDir); //add acceleration
        GfcTools.Minus(ref velocity, deacceleration);//add deacceleration
        GfcTools.Minus(ref velocity, slope); //add vertical speed change  

        m_movement.SetVelocity(velocity);
    }

    protected virtual void CalculateDash(float deltaTime, Vector3 movDir)
    {
        if (MyRunnerFlags.GetAndUnsetBit((int)RunnerFlags.DASH))
        {
            if (m_dashFlagReleased)
            {
                m_dashFlagReleased = false;
                PerformDash(deltaTime, movDir);
            }
        }
        else
        {
            m_dashFlagReleased = true;
        }
    }

    protected virtual void PerformDash(float deltaTime, Vector3 movDir)
    {
        if (m_maxDashes > m_currentDashesCount && movDir.sqrMagnitude > 0.001f)
        {
            m_dashDurationLeft = m_dashStatusDuration;
            m_currentDashesCount++;

            m_movement.SetVelocity(m_dashVelocity * movDir.normalized);
            m_movement.SetIsGrounded(false);
            m_movement.DetachFromParentTransform();
        }
    }

    protected virtual void CalculateJump()
    {
        if (MyRunnerFlags.GetAndUnsetBit((int)RunnerFlags.JUMP))
        {
            if (m_jumpFlagReleased)
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
            Vector3 velocity = m_movement.GetVelocity();
            Vector3 upVecRotation = m_movement.GetUpvecRotation();
            GfcTools.RemoveAxis(ref velocity, upVecRotation);
            GfcTools.Add(ref velocity, upVecRotation * m_jumpForce);

            m_movement.SetVelocity(velocity);
            m_movement.SetIsGrounded(false);
            m_movement.DetachFromParentTransform();
        }
    }

    public override void MgOnCollision(ref MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;
        if (collision.isGrounded)
        {
            m_currentJumpsCount = 0;
            m_currentDashesCount = 0;
        }

        if (collision.isGrounded && collisionTrans != m_movement.GetParentTransform())
            m_movement.SetParentTransform(collisionTrans);

        m_isExtendingJump &= !collision.isGrounded && collision.selfUpVecAngle <= m_movement.GetUpperSlopeLimitDeg();
        m_touchedParent |= collision.isGrounded && m_movement.GetParentTransform() == collisionTrans;
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