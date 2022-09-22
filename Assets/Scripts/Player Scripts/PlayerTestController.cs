using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static System.MathF;

public class PlayerTestController : MovementGeneric
{
    [SerializeField]
    private float acceleration = 10;
    [SerializeField]
    private float deacceleration = 10;
    [SerializeField]
    private float midAirAcceleration = 10;
    [SerializeField]
    private float midAirDeacceleration = 10;
    [SerializeField]
    private float maxFallSpeed = 40;
    [SerializeField]
    private float jumpForce = 40;
    public float AccelerationCoef {get; set;} = 1;
    public float DeaccelerationCoef {get; set;} = 1;
    private float _effectiveDeacceleration;
    private float _effectiveAcceleration;
    public bool CanDoubleJump { get; protected set; }
    public bool CanExtendJump { get; protected set; }

    //whether we touched the current parent this frame or not
    private bool touchedParent;

    // Start is called before the first frame update
    protected override void InternalStart()
    {

    }

    public override void CalculateMovement(float speedMultiplier = 1) {

    }

    protected override void BeforeFixedUpdate()
    {
        touchedParent = false;
        //Debug.Log("CALC MOVEMENT WAS CAALLED, GROUNDED IS " + IsGrounded);
        if (SlopeNormal != Vector3.up) {
            Quaternion q = GfTools.RotationTo(Vector3.up, SlopeNormal);
            MovementDir = q * MovementDir;
        }
        CalculateEffectiveValues();
        CalculateVelocity();
        CalculateJump();
    }

    protected override void AfterFixedUpdate() {
        if(touchedParent && null != parentTransform) 
            DetachFromParent();
    }
    
    protected void CalculateEffectiveValues() {
        if(IsGrounded) {
            _effectiveAcceleration = acceleration;
            _effectiveDeacceleration = deacceleration;
        } else {
            _effectiveAcceleration = midAirAcceleration;
            _effectiveDeacceleration = midAirDeacceleration;
        }
        _effectiveAcceleration *= AccelerationCoef;
        _effectiveDeacceleration *= DeaccelerationCoef;
    }

        //

       // if(movementDirMagnitude > 0.0001f)
        //    Debug.Log("Speed in desired dir is: " + speedInDesiredDir + " and speed is: " + currentSpeed + " unwanted speed is: " + unwantedSpeed);
    // Update is called once per frame

    void CalculateVelocity()
    {
      //  Debug.Log("The vertical velocity is: " + (Velocity.magnitude * Vector3.Dot(SlopeNormal, Velocity.normalized)));
        //remove vertical factor from the velocity to calculate the horizontal plane velocity easier

        float verticalFallSpeed = (Velocity.magnitude * Vector3.Dot(SlopeNormal, Velocity.normalized));
        float fallMagn, fallMaxDiff = -verticalFallSpeed - maxFallSpeed;

        if(fallMaxDiff < 0) { //speed under maxFallSpeed
            fallMagn = Min(-fallMaxDiff, mass * Time.deltaTime);
        } else { //speed equal to maxFallSpeed or higher
            fallMagn = -Min(fallMaxDiff, _effectiveDeacceleration * Time.deltaTime);
        }

        Vector3 horizontalVelocity = Velocity - SlopeNormal * verticalFallSpeed;

        float currentSpeed = horizontalVelocity.magnitude;
        float dotMovementVelDir = Vector3.Dot(MovementDir, currentSpeed > 0.00001F ? horizontalVelocity / currentSpeed : Vector3.zero);

        float desiredSpeed = speed * movementDirMagnitude;
        float speedInDesiredDir = currentSpeed * Max(0, dotMovementVelDir);

        Vector3 unwantedVelocity = horizontalVelocity - MovementDir * Min(speedInDesiredDir, desiredSpeed);    
        float unwantedSpeed = unwantedVelocity.magnitude;
        unwantedVelocity = unwantedSpeed > 0.00001F ? unwantedVelocity / unwantedSpeed : Vector3.zero; //normalised

        Velocity +=   MovementDir * Min(Max(0, desiredSpeed - speedInDesiredDir), Time.deltaTime * _effectiveAcceleration) //acceleration force
                    - unwantedVelocity * Min(unwantedSpeed, _effectiveDeacceleration * Time.deltaTime)                     //deacceleration force
                    - SlopeNormal * fallMagn;                                                                              //fall force
    }

    void CalculateJump() 
    { 
        if(JumpTrigger) {
            JumpTrigger = false;
            if(jumpTriggerReleased) {
                jumpTriggerReleased = false;
                Velocity.y = jumpForce;
            }
        } else {
            jumpTriggerReleased = true;
        }
    }

    protected override void MgOnCollision(MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;
        bool auxGrounded = slopeLimit < collision.angle;
        if(auxGrounded && parentTransform == null) {
            touchedParent = true;
            SetParentTransform(collisionTrans);
        }

        touchedParent |= auxGrounded && parentTransform == collisionTrans;
    }

}