using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using static System.MathF;

public class GfMovementWallrun : GfMovementSimple
{
    [SerializeField]
    private float m_moveFwdSecondsAfterWallDetach = 0.2f;

    [SerializeField]
    private float m_attachSecondsAfterWallDetach = 0.5f;

    [SerializeField]
    private float m_wallRunJumpForce = 20;

    [SerializeField]
    private float m_wallRunDetachForce = 5;
    [SerializeField]
    private float m_maxWallRunSpeed = 17;

    [SerializeField]
    private float m_maxWallSlideSpeed = 10;

    [SerializeField]
    private float m_wallrunNormalMinDot = 0.7f;

    [SerializeField]
    private float m_wallrunSpeedRequired = 6;

    [SerializeField]
    private float m_maxWallRunDistance = 11;

    [SerializeField]
    private float m_wallrunJumpCooldownSeconds = 0.2f;

    [SerializeField]
    private float m_dashVelocity = 10f;

    protected bool m_isWallRunning = false;

    protected bool m_touchedWallThisFrame = false;

    private MgCollisionStruct m_lastWallRunCollision;

    private Vector3 m_previousWallRunNormal;
    private Vector3 m_wallRunDir;

    private bool m_slidingOffWall;

    private float m_wallDistanceRan = 0;

    protected float m_deltaTime;

    const float MAX_WALL_ANGLE = 91f;

    private float m_secondsUntilWallDetach = -1;

    private float m_secondsUntilStopMovingFwd = -1;

    private float m_secondsUntilCanJump = 0;

    private bool m_dashFlagReleased;

    protected override void InternalStart()
    {
        if (m_useSimpleCollision) Debug.LogWarning("GfMovementWallrun does not support simple collision checks.");
    }

    protected override void BeforePhysChecks(float deltaTime)
    {
        m_deltaTime = deltaTime;
        m_secondsUntilWallDetach -= deltaTime;
        m_secondsUntilStopMovingFwd -= deltaTime;
        m_secondsUntilCanJump -= deltaTime;

        m_touchedParent = m_jumpedThisFrame = m_touchedWallThisFrame = false;

        if (!m_isWallRunning)
        {
            Vector3 movDir = MovementDirComputed();

            //keep going forward for a bit after detaching from wall
            if (0 < m_secondsUntilStopMovingFwd && movDir.sqrMagnitude < 0.01f)
                movDir = m_transform.forward;

            CalculateEffectiveValues();
            CalculateVelocity(deltaTime, movDir);
            CalculateRotation(deltaTime, movDir);
            CalculateDash(deltaTime, movDir);
        }
        else
        {
            WallRunCalculations(deltaTime);
        }

        CalculateJump();

    }

    protected virtual void CalculateDash(float deltaTime, Vector3 movDir)
    {
        if (FlagDash)
        {
            FlagDash = false;
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
        m_velocity += m_dashVelocity * movDir.normalized;
    }

    protected override void PerformJump()
    {
        if (m_isWallRunning && 0 > m_secondsUntilCanJump)
        {
            Vector3 jumpPower = m_wallRunJumpForce * m_lastWallRunCollision.selfNormal;
            DetachFromWall(true);
            Quaternion turnAround = Quaternion.AngleAxis(180, m_rotationUpVec);
            m_transform.rotation = turnAround * m_transform.rotation;
            m_velocity = jumpPower;
        }
        else if (0 > m_secondsUntilCanJump)
        {
            DefaultJump();
        }
    }


    protected void WallRunCalculations(float deltaTime)
    {
        Vector3 normal = m_lastWallRunCollision.selfNormal;
        if (!m_previousWallRunNormal.Equals(normal))
        {
            m_previousWallRunNormal = normal;
            m_wallRunDir = (GetUpvecRotation() - normal * Vector3.Dot(normal, GetUpvecRotation())).normalized;
            m_transform.rotation = Quaternion.LookRotation(-normal, m_wallRunDir);
            GfTools.RemoveAxis(ref m_velocity, normal);
        }

        float desiredSpeed;
        float acceleration;
        float coef = 1;

        if (m_slidingOffWall)
        {
            coef = -1;
            desiredSpeed = m_maxWallSlideSpeed;
            acceleration = m_effectiveMass;
        }
        else
        {
            desiredSpeed = m_maxWallRunSpeed;
            acceleration = m_acceleration * m_accelerationMultiplier;
        }

        float speedInDesiredDir = Max(0, Vector3.Dot(m_velocity, coef * m_wallRunDir));
        acceleration = Min(desiredSpeed - speedInDesiredDir, deltaTime * acceleration);

        if (!m_slidingOffWall)
        {
            m_wallDistanceRan += deltaTime * (acceleration + speedInDesiredDir);
            m_slidingOffWall = m_maxWallRunDistance <= m_wallDistanceRan;
        }

        float minAux = Min(speedInDesiredDir, desiredSpeed) * coef;
        Vector3 unwantedVelocity = m_velocity;
        unwantedVelocity.x -= m_wallRunDir.x * minAux;
        unwantedVelocity.y -= m_wallRunDir.y * minAux;
        unwantedVelocity.z -= m_wallRunDir.z * minAux;

        float unwantedSpeed = unwantedVelocity.magnitude;
        if (unwantedSpeed > 0.000001F) GfTools.Div3(ref unwantedVelocity, unwantedSpeed);

        float deaccMagn = Min(unwantedSpeed, m_deacceleration * deltaTime * m_deaccelerationMultiplier);

        GfTools.Mult3(ref unwantedVelocity, deaccMagn);
        GfTools.Minus3(ref m_velocity, unwantedVelocity);//add deacceleration
        GfTools.Add3(ref m_velocity, m_wallRunDir * (coef * acceleration));
    }

    private bool CheckWall(MgCollisionStruct collision)
    {
        Ray wallRay = new Ray(m_transform.position, -collision.selfNormal);
        bool hitWall = collision.collider.Raycast(wallRay, out RaycastHit hitInfo, 2);
        float angle = 0;
        hitWall = hitWall
                && (angle = GfTools.AngleDeg(m_upVec, hitInfo.normal)) > m_slopeLimit //we do this to avoid using an if statement
                && angle <= MAX_WALL_ANGLE;

        return hitWall;
    }

    protected override void AfterPhysChecks(float deltaTime)
    {
        if (m_isWallRunning && (m_isGrounded || (!m_touchedWallThisFrame && !CheckWall(m_lastWallRunCollision))))
        {
            DetachFromWall();
        }

        if (!m_isWallRunning && !m_touchedParent && null != m_parentTransform.GetValue() && 0 >= m_secondsUntilWallDetach)
        {
            DetachFromParentTransform();
            if (m_breakWhenUnparent) Debug.Break();
        }
    }

    protected bool NormalWallRunValid(ref MgCollisionStruct collision)
    {
        Vector3 testVector = Zero3;
        Vector3 movDir = MovementDirComputed();
        float movDirMag = movDir.sqrMagnitude;

        if (movDirMag > 0.05f)
        {
            testVector = movDir;
            GfTools.Div3(ref testVector, Sqrt(movDirMag));
        }
        else
        {
            Vector3 horizontalVelocity = collision.selfVelocity;
            GfTools.RemoveAxis(ref horizontalVelocity, m_upVec);
            float velSqrMag = horizontalVelocity.sqrMagnitude;

            if (velSqrMag >= m_wallrunSpeedRequired * m_wallrunSpeedRequired)
            {
                testVector = horizontalVelocity;
                GfTools.Div3(ref testVector, System.MathF.Sqrt(velSqrMag));
            }
        }

        return m_wallrunNormalMinDot <= -Vector3.Dot(testVector, collision.selfNormal);
    }

    protected bool WallCollisionCheck(ref MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;

        bool canWallRun = !m_isGrounded
                && !collision.isStair
                && !m_isWallRunning ^ (collision.collider == m_lastWallRunCollision.collider || 0 < Vector3.Dot(collision.selfNormal, m_lastWallRunCollision.selfNormal)) //either we are not wall running or the colliders are different
                && !collision.isGrounded
                && GfPhysics.LayerIsInMask(collisionTrans.gameObject.layer, GfPhysics.WallrunLayers())
                && collision.selfUpVecAngle <= MAX_WALL_ANGLE
                && (m_isWallRunning || NormalWallRunValid(ref collision));

        m_touchedWallThisFrame |= canWallRun;

        if (canWallRun)
        {
            m_lastWallRunCollision = collision;
            if (!m_isWallRunning)
            {
                InitiateWallRun(collision);
            }
        }

        if (m_slidingOffWall && Vector3.Dot(m_lastWallRunCollision.selfNormal, collision.selfNormal) <= 0)
        {
            DetachFromWall();
            canWallRun = false;
        }

        m_slidingOffWall |= m_isWallRunning && collision.selfUpVecAngle >= m_upperSlopeLimit; ;

        return canWallRun;
    }

    protected override void MgOnCollision(ref MgCollisionStruct collision)
    {
        // Debug.Log("I am touching something: " + collision.collider.name + " the parent is: " + (m_parentTransform ? m_parentTransform.name : "null"));
        Transform collisionTrans = collision.collider.transform;
        bool canWallRun = WallCollisionCheck(ref collision);

        if (collision.isGrounded) m_currentJumpsCount = 0;

        if ((canWallRun || collision.isGrounded) && collisionTrans != m_parentTransform)
            SetParentTransform(collisionTrans);

        m_isExtendingJump &= !collision.isGrounded && collision.selfUpVecAngle <= m_upperSlopeLimit;
        m_touchedParent |= (canWallRun || collision.isGrounded) && m_parentTransform == collisionTrans;
    }

    private void DetachFromWall(bool wallJumped = false)
    {
        //GfTools.Minus3(ref m_velocity, m_previousWallRunNormal * Min(0, Vector3.Dot(m_velocity, m_previousWallRunNormal)));
        if (!wallJumped && (!m_slidingOffWall || 0.1f < Vector3.Dot(m_upVec, m_velocity)))
        {
            //Debug.Log("lmao am sarit, sliding is: " + m_slidingOffWall);
            //GfTools.Add3(ref m_velocity, m_wallRunDetachForce * m_wallRunDir);
            m_velocity = m_wallRunDetachForce * m_upVec;
            m_secondsUntilWallDetach = m_attachSecondsAfterWallDetach;
            m_secondsUntilStopMovingFwd = m_moveFwdSecondsAfterWallDetach;
        }

        m_secondsUntilCanJump = -1;
        m_currentJumpsCount = 1;
        //DetachFromParentTransform();
        //Debug.Log("DETACHING");
        //return;
        m_isWallRunning = false;
        m_slidingOffWall = false;
        Quaternion rotCorrection = Quaternion.FromToRotation(m_transform.up, m_rotationUpVec);
        m_transform.rotation = rotCorrection * m_transform.rotation;
        m_previousWallRunNormal = Zero3;
        m_lastWallRunCollision = default;
        m_touchedWallThisFrame = false;
    }

    private void InitiateWallRun(MgCollisionStruct collision)
    {
        m_secondsUntilCanJump = m_wallrunJumpCooldownSeconds;
        //m_secondsUntilWallDetach = -1;
        //Debug.Log("ATTACHING");
        Vector3 normal = collision.selfNormal;
        m_previousWallRunNormal = normal;
        m_wallRunDir = (GetUpvecRotation() - normal * Vector3.Dot(normal, GetUpvecRotation())).normalized;
        m_transform.rotation = Quaternion.LookRotation(-normal, m_wallRunDir);
        GfTools.RemoveAxis(ref m_velocity, normal);
        float speedInDesiredDir = Min(m_maxWallRunSpeed, Max(0, Vector3.Dot(m_velocity, m_wallRunDir)));
        GfTools.Minus3(ref m_velocity, normal);
        m_wallDistanceRan = 0;
        m_slidingOffWall = false;
        m_velocity = m_wallRunDir * speedInDesiredDir;
        m_isWallRunning = true;
    }


}
