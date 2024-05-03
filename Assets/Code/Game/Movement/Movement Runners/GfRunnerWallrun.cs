using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;


using static System.MathF;

public class GfRunnerWallrun : GfRunnerSimple
{
    [SerializeField]
    private float m_moveFwdSecondsAfterWallDetach = 0.22f;

    [SerializeField]
    private float m_attachSecondsAfterWallDetach = 0.5f;

    [SerializeField]
    private float m_wallRunJumpForce = 27;

    [SerializeField]
    private float m_wallrunJumpAngle = 55;

    [SerializeField]
    private float m_wallRunDetachForce = 17;

    [SerializeField]
    private float m_maxWallRunSpeed = 17;

    [SerializeField]
    private float m_maxWallSlideSpeed = 10;

    [SerializeField]
    private float m_wallrunNormalMinDot = 0.8f;

    [SerializeField]
    private float m_wallrunSpeedRequired = 9;



    [SerializeField]
    private float m_maxWallRunDistance = 11;

    [SerializeField]
    private float m_wallrunJumpCooldownSeconds = 0.07f;

    [SerializeField]
    private float m_dashVelocity = 15f;

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

    protected void Start()
    {
        //if (m_useSimpleCollision) Debug.LogWarning("GfMovementWallrun does not support simple collision checks.");
    }

    public override void BeforePhysChecks(float deltaTime)
    {
        m_deltaTime = deltaTime;
        m_secondsUntilWallDetach -= deltaTime;
        m_secondsUntilStopMovingFwd -= deltaTime;
        m_secondsUntilCanJump -= deltaTime;

        m_touchedParent = m_jumpedThisFrame = m_touchedWallThisFrame = false;
        CalculateEffectiveValues();

        if (!m_isWallRunning)
        {
            Vector3 movDir = m_mov.MovementDirComputed(MovementDirRaw, CanFly);

            //keep going forward for a bit after detaching from wall
            if (0 < m_secondsUntilStopMovingFwd && movDir.sqrMagnitude < 0.01f)
                movDir = m_transform.forward;

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

    public override void AfterPhysChecks(float deltaTime)
    {
        if (m_isWallRunning && (m_mov.GetIsGrounded() || (!m_touchedWallThisFrame && !CheckWall(m_lastWallRunCollision))))
        {
            DetachFromWall();
        }

        if (!m_isWallRunning && !m_touchedParent && null != m_mov.GetParentTransform() && 0 >= m_secondsUntilWallDetach)
        {
            m_mov.DetachFromParentTransform();
            if (m_breakWhenUnparent) Debug.Break();
        }
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
        m_mov.AddVelocity(m_dashVelocity * movDir.normalized);
    }

    protected override void PerformJump()
    {
        if (m_isWallRunning && 0 > m_secondsUntilCanJump) //perform wall jump
        {
            Vector3 rotationUpVec = m_mov.GetUpvecRotation();
            Vector3 horizontalAxis = m_previousWallRunNormal;
            GfcTools.RemoveAxis(ref horizontalAxis, rotationUpVec);
            GfcTools.Normalize(ref horizontalAxis);

            Vector3 rotationAxis = Vector3.Cross(rotationUpVec, horizontalAxis);
            GfcTools.Normalize(ref rotationAxis);

            Vector3 jumpDir = Quaternion.AngleAxis(-m_wallrunJumpAngle, rotationAxis) * horizontalAxis;
            GfcTools.Mult(ref jumpDir, m_wallRunJumpForce);
            m_mov.SetVelocity(jumpDir);

            DetachFromWall(true);

            Quaternion turnAround = Quaternion.AngleAxis(180, rotationUpVec);
            m_transform.rotation = turnAround * m_transform.rotation;
        }
        else if (0 > m_secondsUntilCanJump)
        {
            DefaultJump();
        }
    }


    protected void WallRunCalculations(float deltaTime)
    {
        Vector3 normal = m_lastWallRunCollision.normal;
        Vector3 velocity = m_mov.GetVelocity();
        if (!m_previousWallRunNormal.Equals(normal))
        {
            Vector3 rotationUpVec = m_mov.GetUpvecRotation();
            m_previousWallRunNormal = normal;
            m_wallRunDir = (rotationUpVec - normal * Vector3.Dot(normal, rotationUpVec)).normalized;
            m_transform.rotation = Quaternion.LookRotation(-normal, m_wallRunDir);
            GfcTools.RemoveAxis(ref velocity, normal);
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

        float speedInDesiredDir = Max(0, Vector3.Dot(velocity, coef * m_wallRunDir));
        acceleration = Min(desiredSpeed - speedInDesiredDir, deltaTime * acceleration);

        if (!m_slidingOffWall)
        {
            m_wallDistanceRan += deltaTime * (acceleration + speedInDesiredDir);
            m_slidingOffWall = m_maxWallRunDistance <= m_wallDistanceRan;
        }

        float minAux = Min(speedInDesiredDir, desiredSpeed) * coef;
        Vector3 unwantedVelocity = velocity;
        unwantedVelocity.x -= m_wallRunDir.x * minAux;
        unwantedVelocity.y -= m_wallRunDir.y * minAux;
        unwantedVelocity.z -= m_wallRunDir.z * minAux;

        float unwantedSpeed = unwantedVelocity.magnitude;
        if (unwantedSpeed > 0.000001F) GfcTools.Div(ref unwantedVelocity, unwantedSpeed);

        float deaccMagn = Min(unwantedSpeed, m_deacceleration * deltaTime * m_deaccelerationMultiplier);

        GfcTools.Mult(ref unwantedVelocity, deaccMagn);
        GfcTools.Minus(ref velocity, unwantedVelocity);//add deacceleration
        GfcTools.Add(ref velocity, m_wallRunDir * (coef * acceleration));

        m_mov.SetVelocity(velocity);
    }

    private bool CheckWall(MgCollisionStruct collision)
    {
        Ray wallRay = new Ray(m_transform.position, -collision.normal);
        bool hitWall = collision.collider.Raycast(wallRay, out RaycastHit hitInfo, 2);
        float angle = 0;
        hitWall = hitWall
                && (angle = GfcTools.AngleDegNorm(m_mov.GetUpVecRaw(), hitInfo.normal)) > m_mov.GetSlopeLimitDeg() //we do this to avoid using an if statement
                && angle <= MAX_WALL_ANGLE;

        return hitWall;
    }

    protected bool NormalWallRunValid(ref MgCollisionStruct collision)
    {
        Vector3 upVec = m_mov.GetUpVecRaw();
        Vector3 testVector = Vector3.zero;
        Vector3 movDir = MovementDirRaw;
        float movDirMag = movDir.sqrMagnitude;
        Vector3 horizontalNormal = collision.normal;
        GfcTools.RemoveAxis(ref horizontalNormal, upVec);
        GfcTools.Normalize(ref horizontalNormal);
        bool normalValid = false;

        if (movDirMag > 0.05f)
        {
            testVector = movDir;
            GfcTools.Div(ref testVector, Sqrt(movDirMag));
            normalValid = m_wallrunNormalMinDot <= -Vector3.Dot(testVector, horizontalNormal);
        }
        else
        {
            Vector3 horizontalVelocity = collision.selfVelocity;
            GfcTools.RemoveAxis(ref horizontalVelocity, upVec);
            float velSqrMag = horizontalVelocity.sqrMagnitude;
            float sqrdSpeedRequired = m_wallrunSpeedRequired * m_wallrunSpeedRequired;

            if (velSqrMag >= sqrdSpeedRequired)
            {
                testVector = horizontalVelocity;
                GfcTools.Div(ref testVector, Sqrt(velSqrMag));

                Vector3 forwardDir = m_transform.forward;
                GfcTools.RemoveAxis(ref forwardDir, upVec);
                GfcTools.Normalize(ref forwardDir);

                normalValid = m_wallrunNormalMinDot <= -Vector3.Dot(testVector, horizontalNormal)
                            && m_wallrunNormalMinDot <= -Vector3.Dot(forwardDir, horizontalNormal);
            }
        }

        return normalValid;
    }

    protected bool WallCollisionCheck(ref MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;

        bool canWallRun = !m_mov.GetIsGrounded()
                && !collision.isStair
                && !m_isWallRunning ^ (collision.collider == m_lastWallRunCollision.collider || 0 < Vector3.Dot(collision.normal, m_lastWallRunCollision.normal)) //either we are not wall running or the colliders are different
                && !collision.isGrounded
                && GfcPhysics.LayerIsInMask(collisionTrans.gameObject.layer, GfcPhysics.WallrunLayers())
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

        if (m_slidingOffWall && Vector3.Dot(m_lastWallRunCollision.normal, collision.normal) <= 0)
        {
            DetachFromWall();
            canWallRun = false;
        }

        m_slidingOffWall |= m_isWallRunning && collision.selfUpVecAngle >= m_mov.GetUpperSlopeLimitDeg();

        return canWallRun;
    }

    public override void MgOnCollision(ref MgCollisionStruct collision)
    {
        // Debug.Log("I am touching something: " + collision.collider.name + " the parent is: " + (m_parentTransform ? m_parentTransform.name : "null"));
        Transform collisionTrans = collision.collider.transform;
        bool canWallRun = WallCollisionCheck(ref collision);

        if (collision.isGrounded) m_currentJumpsCount = 0;

        if ((canWallRun || collision.isGrounded) && collisionTrans != m_mov.GetParentTransform())
        {
            m_mov.SetParentTransform(collisionTrans);
        }

        m_isExtendingJump &= !collision.isGrounded && collision.selfUpVecAngle <= m_mov.GetUpperSlopeLimitDeg();
        m_touchedParent |= (canWallRun || collision.isGrounded) && m_mov.GetParentTransform() == collisionTrans;
    }

    private void DetachFromWall(bool wallJumped = false)
    {
        Vector3 upVec = m_mov.GetUpVecRaw();
        //GfTools.Minus3(ref m_velocity, m_previousWallRunNormal * Min(0, Vector3.Dot(m_velocity, m_previousWallRunNormal)));
        if (!wallJumped && (!m_slidingOffWall || 0.1f < Vector3.Dot(upVec, m_mov.GetVelocity())))
        {
            m_mov.SetVelocity(m_wallRunDetachForce * upVec);
            m_secondsUntilWallDetach = m_attachSecondsAfterWallDetach;
            m_secondsUntilStopMovingFwd = m_moveFwdSecondsAfterWallDetach;
        }

        m_secondsUntilCanJump = -1;
        m_currentJumpsCount = 1;
        m_isWallRunning = false;
        m_slidingOffWall = false;
        Quaternion rotCorrection = Quaternion.FromToRotation(m_transform.up, m_mov.GetUpvecRotation());
        m_transform.rotation = rotCorrection * m_transform.rotation;
        m_previousWallRunNormal = Vector3.zero;
        m_lastWallRunCollision = default;
        m_touchedWallThisFrame = false;
        m_mov.DetachFromParentTransform();
    }

    private void InitiateWallRun(MgCollisionStruct collision)
    {
        Vector3 rotationUpVec = m_mov.GetUpvecRotation();
        Vector3 velocity = m_mov.GetVelocity();
        m_secondsUntilCanJump = m_wallrunJumpCooldownSeconds;
        //m_secondsUntilWallDetach = -1;
        //Debug.Log("ATTACHING");
        Vector3 normal = collision.normal;
        m_previousWallRunNormal = normal;
        m_wallRunDir = (rotationUpVec - normal * Vector3.Dot(normal, rotationUpVec)).normalized;
        m_transform.rotation = Quaternion.LookRotation(-normal, m_wallRunDir);
        GfcTools.RemoveAxis(ref velocity, normal);
        float speedInDesiredDir = Min(m_maxWallRunSpeed, Max(0, Vector3.Dot(velocity, m_wallRunDir)));
        GfcTools.Minus(ref velocity, normal);
        m_wallDistanceRan = 0;
        m_slidingOffWall = false;
        velocity = m_wallRunDir * speedInDesiredDir;
        m_isWallRunning = true;
        m_mov.SetVelocity(velocity);
    }


}
