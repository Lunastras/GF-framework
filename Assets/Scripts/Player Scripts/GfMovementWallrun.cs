using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using static System.MathF;

public class GfMovementWallrun : GfMovementSimple
{
    [SerializeField]
    private float m_attachSecondsAfterWallDetach = 0.2f;

    [SerializeField]
    private float m_wallRunJumpForce = 20;

    [SerializeField]
    private float m_wallRunDetachForce = 5;
    [SerializeField]
    private float m_maxWallRunSpeed = 17;

    [SerializeField]
    private float m_maxWallSlideSpeed = 10;

    [SerializeField]
    private float m_wallrunNormalMaxDot = 0.7f;

    [SerializeField]
    private float m_wallrunSpeedRequired = 6;

    [SerializeField]
    private float m_maxWallRunDistance = 11;

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

    protected override void InternalStart()
    {
        if (m_useSimpleCollision) Debug.LogWarning("GfMovementWallrun does not support simple collision checks.");
    }

    protected override void BeforePhysChecks(float deltaTime)
    {


        m_deltaTime = deltaTime;
        m_secondsUntilWallDetach = System.MathF.Max(-1, m_secondsUntilWallDetach - deltaTime); //prevent overflow
        m_touchedParent = m_jumpedThisFrame = m_touchedWallThisFrame = false;

        if (!m_isWallRunning)
        {
            Vector3 movDir = MovementDirComputed();

            //keep going forward for a bit after detaching from wall
            if (movDir.sqrMagnitude < 0.01f && 0 < m_secondsUntilWallDetach)
            {
                movDir = transform.forward;
            }

            CalculateEffectiveValues();
            CalculateVelocity(deltaTime, movDir);
            CalculateJump();
            CalculateRotation(deltaTime, movDir);
        }
        else
        {
            WallRunCalculations(deltaTime);
            CalculateJump(deltaTime);
        }
    }

    protected void CalculateJump(float deltaTime)
    {
        if (JumpTrigger)
        {
            JumpTrigger = false;
            if (m_isWallRunning)
            {
                Vector3 jumpPower = m_wallRunJumpForce * m_lastWallRunCollision.normal;
                DetachFromWall(true);
                Quaternion turnAround = Quaternion.AngleAxis(180, UPDIR);
                m_transform.rotation = turnAround * m_transform.rotation;
                GfTools.Add3(ref m_velocity, jumpPower);

            }
            else if (m_isGrounded)
            {
                m_velocity = m_velocity - m_upVec * Vector3.Dot(m_upVec, m_velocity);
                m_velocity = m_velocity + m_upVec * m_jumpForce;
                m_isGrounded = false;
                // Debug.Log("I HAVE JUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUMPED");
                DetachFromParentTransform();
            }
        }
    }

    protected void WallRunCalculations(float deltaTime)
    {
        Vector3 normal = m_lastWallRunCollision.normal;
        if (!m_previousWallRunNormal.Equals(normal))
        {
            m_previousWallRunNormal = normal;
            m_wallRunDir = (UpvecRotation() - normal * Vector3.Dot(normal, UpvecRotation())).normalized;
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
            acceleration = m_mass;
        }
        else
        {
            desiredSpeed = m_maxWallRunSpeed;
            acceleration = m_acceleration;
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

        float deaccMagn = Min(unwantedSpeed, m_deacceleration * deltaTime);

        GfTools.Mult3(ref unwantedVelocity, deaccMagn);
        GfTools.Minus3(ref m_velocity, unwantedVelocity);//add deacceleration
        GfTools.Add3(ref m_velocity, m_wallRunDir * (coef * acceleration));
    }

    private bool CheckWall(MgCollisionStruct collision)
    {
        Ray wallRay = new Ray(m_transform.position, -collision.normal);
        bool hitWall = collision.collider.Raycast(wallRay, out RaycastHit hitInfo, 2);
        float angle = 0;
        hitWall = hitWall
                && (angle = GfTools.AngleDeg(m_upVec, hitInfo.normal)) > m_slopeLimit //we do this to avoid using an if statement
                && angle <= MAX_WALL_ANGLE;

        return hitWall;
    }

    protected override void AfterPhysChecks(float deltaTime)
    {
        if (!m_touchedParent && null != m_parentTransform && 0 >= m_secondsUntilWallDetach)
        {
            DetachFromParentTransform();
            if (m_breakWhenUnparent) Debug.Break();
        }

        if (m_isWallRunning && !m_touchedWallThisFrame && !CheckWall(m_lastWallRunCollision))
        {
            Debug.Log("I haven't touched the wall in a while");
            DetachFromWall();
        }
    }

    protected bool NormalWallRunValid(Vector3 normal)
    {
        Vector3 testVector = Zero3;
        Vector3 movDir = MovementDirComputed();
        float movDirMag = movDir.magnitude;

        if (movDirMag > 0.5f)
        {
            testVector = movDir;
            GfTools.Div3(ref testVector, movDirMag);
        }
        else
        {
            float velMag = m_velocity.magnitude;

            if (velMag >= m_wallrunSpeedRequired)
            {
                //Debug.Break();
                testVector = m_velocity;
                GfTools.Div3(ref testVector, velMag);
            }
        }

        return -m_wallrunNormalMaxDot >= Vector3.Dot(testVector, normal); ;
    }

    protected bool WallCollisionCheck(MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;
        bool canWallRun = !m_isGrounded
                && (!m_isWallRunning ^ (collision.collider == m_lastWallRunCollision.collider || 0 < Vector3.Dot(collision.normal, m_lastWallRunCollision.normal))) //either we are not wall running or the colliders are different
                && !collision.overlap //overlaps return bad normals from time to time, ignore them
                && !collision.isGrounded
                && GfPhysics.LayerIsInMask(collisionTrans.gameObject.layer, GfPhysics.WallrunLayers())
                && collision.upVecAngle <= MAX_WALL_ANGLE
                && (m_isWallRunning || NormalWallRunValid(collision.normal));

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

        m_slidingOffWall |= m_isWallRunning && collision.upVecAngle >= m_upperSlopeLimit; ;

        return canWallRun;
    }

    protected override void MgOnCollision(MgCollisionStruct collision)
    {
        // Debug.Log("I am touching something: " + collision.collider.name + " the parent is: " + (m_parentTransform ? m_parentTransform.name : "null"));
        Transform collisionTrans = collision.collider.transform;
        bool canWallRun = WallCollisionCheck(collision);

        if ((canWallRun || collision.isGrounded) && collisionTrans != m_parentTransform)
            SetParentTransform(collisionTrans);

        m_touchedParent |= (canWallRun || collision.isGrounded) && m_parentTransform == collisionTrans;
    }

    private void DetachFromWall(bool wallJumped = false)
    {
        //GfTools.Minus3(ref m_velocity, m_previousWallRunNormal * Min(0, Vector3.Dot(m_velocity, m_previousWallRunNormal)));

        if (!m_slidingOffWall && !wallJumped)
        {
            GfTools.Add3(ref m_velocity, m_wallRunDetachForce * m_wallRunDir);
            m_secondsUntilWallDetach = m_attachSecondsAfterWallDetach;
        }

        DetachFromParentTransform();
        Debug.Log("DETACHING");
        //return;
        m_isWallRunning = false;
        m_slidingOffWall = false;
        Quaternion rotCorrection = Quaternion.FromToRotation(m_transform.up, m_upVec);
        m_transform.rotation = rotCorrection * m_transform.rotation;
        m_previousWallRunNormal = Zero3;
        m_lastWallRunCollision = default;
        m_touchedWallThisFrame = false;
    }

    private void InitiateWallRun(MgCollisionStruct collision)
    {
        m_secondsUntilWallDetach = -1;
        Debug.Log("ATTACHING");
        Vector3 normal = collision.normal;
        m_previousWallRunNormal = normal;
        m_wallRunDir = (UpvecRotation() - normal * Vector3.Dot(normal, UpvecRotation())).normalized;
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
