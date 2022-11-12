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
    public float AccelerationCoef = 1;
    public float DeaccelerationCoef = 1;
    private float m_effectiveDeacceleration;
    private float m_effectiveAcceleration;
    public bool CanDoubleJump { get; protected set; }
    public bool CanExtendJump { get; protected set; }

    //whether we touched the current parent this frame or not
    private bool m_touchedParent;

    // Start is called before the first frame update
    protected override void InternalStart() { }
    protected override void BeforePhysChecks(float deltaTime)
    {
        m_touchedParent = false;
        //Debug.Log("CALC MOVEMENT WAS CAALLED, GROUNDED IS " + IsGrounded);
        if (SlopeNormal != Vector3.up) {
            Quaternion q = GfTools.RotationTo(Vector3.up, SlopeNormal);
            MovementDir = q * MovementDir;
        }
        
        CalculateEffectiveValues();
        CalculateVelocity(deltaTime);
        CalculateJump();
    }

    protected override void AfterPhysChecks(float deltaTime) {
        if(!m_touchedParent && null != m_parentTransform) {
            Debug.Log("I haven't touched the parent this frame");
            DetachFromParent();
        }
            
    }
    
    protected void CalculateEffectiveValues() {
        if(IsGrounded) {
            m_effectiveAcceleration = m_acceleration;
            m_effectiveDeacceleration = m_deacceleration;
        } else {
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

        if(fallMaxDiff < 0) { //speed under maxFallSpeed
            fallMagn = Min(-fallMaxDiff, m_mass * deltaTime);
        } else { //speed equal to maxFallSpeed or higher
            fallMagn = -Min(fallMaxDiff, m_effectiveDeacceleration * deltaTime);
        }

        //remove vertical factor from the velocity to calculate the horizontal plane velocity easier
        Vector3 horizontalVelocity = Velocity - SlopeNormal * verticalFallSpeed;

        float currentSpeed = horizontalVelocity.magnitude;
        float dotMovementVelDir = Vector3.Dot(MovementDir, currentSpeed > 0.00001F ? horizontalVelocity / currentSpeed : Zero3);

        float desiredSpeed = m_speed * m_movementDirMagnitude;
        float speedInDesiredDir = currentSpeed * Max(0, dotMovementVelDir);

        Vector3 unwantedVelocity = horizontalVelocity - MovementDir * Min(speedInDesiredDir, desiredSpeed);    
        float unwantedSpeed = unwantedVelocity.magnitude;
        unwantedVelocity = unwantedSpeed > 0.00001F ? unwantedVelocity / unwantedSpeed : Zero3; //normalised

        Velocity +=   MovementDir * Min(Max(0, desiredSpeed - speedInDesiredDir), deltaTime * m_effectiveAcceleration) //acceleration force
                    - unwantedVelocity * Min(unwantedSpeed, m_effectiveDeacceleration * deltaTime)                     //deacceleration force
                    - SlopeNormal * fallMagn;                                                                              //fall force
    }

    void CalculateJump() 
    { 
        if(JumpTrigger) {         
            JumpTrigger = false;
            Velocity = Velocity - UpVec * Vector3.Dot(UpVec, Velocity);
            Velocity = Velocity + UpVec * m_jumpForce;

            DetachFromParent();
        }

    }

    protected override void MgOnCollision(MgCollisionStruct collision)
    {
//        Debug.Log("I came into collision WITH " + collision.collider.name);
        Transform collisionTrans = collision.collider.transform;
        bool auxGrounded = m_slopeLimit >= collision.angle;

        if(auxGrounded && m_parentTransform == null) {
          //  Debug.Log("I am supposed to parent something");
            SetParentTransform(collisionTrans);
        }

        m_touchedParent |= auxGrounded && m_parentTransform == collisionTrans;
    }

}