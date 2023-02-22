using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static System.MathF;

public class GfMovementSimple : GfMovementGeneric
{
    [SerializeField]
    protected float m_acceleration = 45;
    [SerializeField]
    protected float m_deacceleration = 40;
    [SerializeField]
    protected float m_midAirAcceleration = 20;
    [SerializeField]
    protected float m_midAirDeacceleration = 15;
    [SerializeField]
    protected float m_maxFallSpeed = 40;
    [SerializeField]
    protected float m_jumpForce = 40;

    [SerializeField]
    protected float m_turnSpeed = 10;

    public float AccelerationCoef = 1;
    public float DeaccelerationCoef = 1;
    protected float m_effectiveDeacceleration;
    protected float m_effectiveAcceleration;
    public bool CanDoubleJump { get; protected set; }
    public bool CanExtendJump { get; protected set; }
    protected bool m_jumpedThisFrame = false;

    //whether we touched the current parent this frame or not
    protected bool m_touchedParent;

    protected float m_currentRotationSpeed = 0;
    protected float m_rotationSmoothRef = 0;
    

    [SerializeField]
    protected bool breakWhenUnparent = true;

    // Start is called before the first frame update
    protected override void InternalStart()
    {
    }

    protected override void BeforePhysChecks(float deltaTime)
    {
        m_touchedParent = m_jumpedThisFrame = false;
        Vector3 movDir = MovementDirComputed();

        CalculateEffectiveValues();
        CalculateVelocity(deltaTime, movDir);
        CalculateJump();
        CalculateRotation(deltaTime, movDir);
    }

    protected override void AfterPhysChecks(float deltaTime)
    {
        if (!m_touchedParent && null != m_parentTransform)
        {
            DetachFromParentTransform();
            if (breakWhenUnparent) Debug.Break();
        }

    }

    protected void CalculateEffectiveValues()
    {
        if (m_isGrounded || CanFly)
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

    protected virtual void CalculateRotation(float deltaTime, Vector3 movDir)
    {
        //ROTATION SECTION
        if (movDir != Vector3.zero)
        {
            Vector3 desiredForwardVec = GfTools.RemoveAxis(movDir, m_rotationUpVec);
            Vector3 forwardVec = GfTools.RemoveAxis(transform.forward, m_rotationUpVec);

            float turnAmount = m_turnSpeed * deltaTime;
            float angleDistance = -GfTools.SignedAngle(desiredForwardVec, forwardVec, m_rotationUpVec); //angle between the current and desired rotation
            float degreesMovement = Min(System.MathF.Abs(angleDistance), turnAmount);

            if (degreesMovement > 0.05f)
            {
                Quaternion angleAxis = Quaternion.AngleAxis(Sign(angleDistance) * degreesMovement, m_rotationUpVec);
                m_transform.rotation = angleAxis * m_transform.rotation;
            }
        }
    }

    protected virtual void CalculateVelocity(float deltaTime, Vector3 movDir)
    {
        Vector3 slope = m_slopeNormal;
        float movDirMagnitude = movDir.magnitude;
        if (movDirMagnitude > 0.000001f) GfTools.Div3(ref movDir, movDirMagnitude); //normalise

        float verticalFallSpeed = Vector3.Dot(slope, m_velocity);
        float fallMagn = 0, fallMaxDiff = -verticalFallSpeed - m_maxFallSpeed; //todo
        //remove vertical factor from the velocity to calculate the horizontal plane velocity easier
        Vector3 effectiveVelocity = m_velocity;

        if (!CanFly)
        {
            //remove vertical component of velocity if we can't fly
            effectiveVelocity.x -= slope.x * verticalFallSpeed;
            effectiveVelocity.y -= slope.y * verticalFallSpeed;
            effectiveVelocity.z -= slope.z * verticalFallSpeed;

            if (fallMaxDiff < 0)
                fallMagn = Min(-fallMaxDiff, m_mass * deltaTime); //speed under maxFallSpeed         
            else
                fallMagn = -Min(fallMaxDiff, m_effectiveDeacceleration * deltaTime);//speed equal to maxFallSpeed or higher
        }

        float currentSpeed = effectiveVelocity.magnitude;
        float dotMovementVelDir = 0;
        if (currentSpeed > 0.000001F)
        {
            Vector3 velDir = effectiveVelocity;
            GfTools.Div3(ref velDir, currentSpeed);
            dotMovementVelDir = Vector3.Dot(movDir, velDir);
        }

        float desiredSpeed = m_speed * movDirMagnitude;
        float speedInDesiredDir = currentSpeed * Max(0, dotMovementVelDir);

        float minAux = Min(speedInDesiredDir, desiredSpeed);
        Vector3 unwantedVelocity = effectiveVelocity;
        unwantedVelocity.x -= movDir.x * minAux;
        unwantedVelocity.y -= movDir.y * minAux;
        unwantedVelocity.z -= movDir.z * minAux;

        float unwantedSpeed = unwantedVelocity.magnitude;
        if (unwantedSpeed > 0.000001F) GfTools.Div3(ref unwantedVelocity, unwantedSpeed);

        float accMagn = Min(Max(0, desiredSpeed - speedInDesiredDir), deltaTime * m_effectiveAcceleration);
        float deaccMagn = Min(unwantedSpeed, m_effectiveDeacceleration * deltaTime);

        //GfTools.Mult3(ref movDir, accMagn);
        GfTools.Mult3(ref unwantedVelocity, deaccMagn);
        GfTools.Mult3(ref slope, fallMagn);

        GfTools.Add3(ref m_velocity, movDir * accMagn); //add acceleration
        GfTools.Minus3(ref m_velocity, unwantedVelocity);//add deacceleration
        GfTools.Minus3(ref m_velocity, slope); //add vertical speed change  
    }

    protected virtual void CalculateJump()
    {
        if (JumpTrigger)
        {
            JumpTrigger = false;
            m_velocity = m_velocity - m_upVec * Vector3.Dot(m_upVec, m_velocity);
            m_velocity = m_velocity + m_upVec * m_jumpForce;
            m_isGrounded = false;
            // Debug.Log("I HAVE JUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUMPED");
            DetachFromParentTransform();
        }

    }

    protected override void MgOnCollision(MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;

        if (collision.isGrounded && collisionTrans != m_parentTransform)
            SetParentTransform(collisionTrans);

        m_touchedParent |= collision.isGrounded && m_parentTransform == collisionTrans;
    }
}