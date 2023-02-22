using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using static System.MathF;

public class GfMovementWallrun : GfMovementSimple
{

    [SerializeField]
    private float m_wallRunDetachForce = 200; 
    [SerializeField]
    private float m_maxWallRunSpeed = 10;

    [SerializeField]
    private float m_maxWallSlideSpeed = 7;

    [SerializeField]
    private float m_wallrunNormalMaxDot = 0.3f;

    [SerializeField]
    private float m_wallrunSpeedRequired = 4;

    [SerializeField]
    private float m_maxWallRunDistance = 20;
    protected bool m_isWallRunning = false;

    protected bool m_touchedWallThisFrame = false;

    private MgCollisionStruct m_lastWallRunCollision;

    private Vector3 m_previousWallRunNormal;
    private Vector3 m_wallRunDir;

    private int m_framesSinceLastWall = 0;

    private bool m_slidingOffWall;

    private float m_wallDistanceRan = 0;

    protected float m_deltaTime;

    //if more frames than this value pass without any wall found, deatch from wall
    const int MAX_NO_WALLRUN_FRAMES = 4;

    protected override void InternalStart()
    {
        if(m_useSimpleCollision) Debug.LogWarning("GfMovementWallrun does not support simple collision checks.");
    }

    protected override void BeforePhysChecks(float deltaTime)
    {
        m_deltaTime = deltaTime;
        m_framesSinceLastWall = System.Math.Max(MAX_NO_WALLRUN_FRAMES, ++m_framesSinceLastWall); //cheese solution, but it works
        m_touchedParent = m_jumpedThisFrame = m_touchedWallThisFrame = false;

        if (!m_isWallRunning)
        {
            Vector3 movDir = MovementDirComputed();

            CalculateEffectiveValues();
            CalculateVelocity(deltaTime, movDir);
            CalculateJump();
            CalculateRotation(deltaTime, movDir);
        }
        else
        {
            WallRunCalculations(deltaTime);
            CalculateJump();
        }
    }

    protected override void CalculateJump()
    {
        if (JumpTrigger)
        {
            JumpTrigger = false;
            if(m_isWallRunning) {

            } else {
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

        //GfTools.Mult3(ref movDir, accMagn);
        GfTools.Mult3(ref unwantedVelocity, deaccMagn);
        GfTools.Minus3(ref m_velocity, (deltaTime * 500) * normal);
        GfTools.Minus3(ref m_velocity, unwantedVelocity);//add deacceleration
        GfTools.Add3(ref m_velocity, m_wallRunDir * (coef * acceleration));
    }

    protected override void AfterPhysChecks(float deltaTime)
    {
        if (!m_touchedParent && null != m_parentTransform)
        {
            DetachFromParentTransform();
            if (breakWhenUnparent) Debug.Break();
        }

        if (m_isWallRunning && m_framesSinceLastWall > MAX_NO_WALLRUN_FRAMES)
        {
            Debug.Log("I haven't touched the wall in a while");
            DetachFromWall(deltaTime);
        }

        //Debug.Log("WALL RUN IS: " + m_isWallRunning);

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
                && collision.upVecAngle <= 91.1f
                && (m_isWallRunning || NormalWallRunValid(collision.normal));

        m_touchedWallThisFrame |= canWallRun;        

        if (canWallRun)
        {
            m_framesSinceLastWall = 0;
            m_lastWallRunCollision = collision;
            if (!m_isWallRunning)
            {
                InitiateWallRun(collision);
            }
        }

        if(m_slidingOffWall && Vector3.Dot(m_lastWallRunCollision.normal, collision.normal) <= 0) {
            DetachFromWall(m_deltaTime);
            canWallRun = false;
        }

        m_slidingOffWall |= m_isWallRunning && collision.upVecAngle >= m_upperSlopeLimit;;
            
        return canWallRun;
    }

    protected override void MgOnCollision(MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;
        bool canWallRun = WallCollisionCheck(collision);

        if ((canWallRun || collision.isGrounded) && collisionTrans != m_parentTransform)
            SetParentTransform(collisionTrans);

        m_touchedParent |= (canWallRun || collision.isGrounded) && m_parentTransform == collisionTrans;
    }

    private void DetachFromWall(float deltaTime)
    {        
        GfTools.Minus3(ref m_velocity, m_previousWallRunNormal * Min(0, Vector3.Dot(m_velocity, m_previousWallRunNormal)));

        if (!m_slidingOffWall)
        {
            GfTools.Minus3(ref m_velocity, (deltaTime * m_wallRunDetachForce) * m_previousWallRunNormal);
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
        Debug.Log("ATTACHING");
        Vector3 normal = collision.normal;
        m_previousWallRunNormal = normal;
        m_wallRunDir = (UpvecRotation() - normal * Vector3.Dot(normal, UpvecRotation())).normalized;
        m_transform.rotation = Quaternion.LookRotation(-normal, m_wallRunDir);
        GfTools.RemoveAxis(ref m_velocity, normal);
        float speedInDesiredDir = Min(m_maxWallRunSpeed, Max(0, Vector3.Dot(m_velocity, m_wallRunDir)));

        m_wallDistanceRan = 0;
        m_slidingOffWall = false;
        m_velocity = m_wallRunDir * speedInDesiredDir;
        m_isWallRunning = true;
    }
}
