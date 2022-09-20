using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTestController : MovementGeneric
{
    [SerializeField]
    private float acceleration = 10;

    [SerializeField]
    private float deacceleration = 10;

    [SerializeField]
    private float maxFallSpeed = 40;

    [SerializeField]
    private float jumpForce = 40;

    // Start is called before the first frame update
    void Start()
    {
    }

    protected override void MgCollisionEnter(RaycastHit hitObject)
    {

    }

    /*
     Implementation from quat.rotationTo function from toji/gl-matrix on github, found in quat.js
     */
    private Quaternion RotationTo(Vector3 initial, Vector3 final)
    {
        float dot = Vector3.Dot(initial, final);

        if (dot < -0.999999)
        {
            Vector3 cross = Vector3.Cross(Vector3.right, final);
            if (cross.magnitude < 0.000001)
                cross = Vector3.Cross(Vector3.up, initial);

            return Quaternion.AngleAxis(180, cross.normalized);
        }
        else if (dot < 0.999999)
        {
            Vector3 cross = Vector3.Cross(initial, final);
            float w = 1 + dot;
            return new Quaternion(cross.x, cross.y, cross.z, w).normalized;
        }
        else //vectors are identical, dot = 1
        {
            return Quaternion.identity;
        }
    }

    public override void CalculateMovement(float speedMultiplier = 1)
    {
        //Debug.Log("CALC MOVEMENT WAS CAALLED, GROUNDED IS " + IsGrounded);

        if (SlopeNormal != Vector3.up) {
            Quaternion q = RotationTo(Vector3.up, SlopeNormal);
            movementDir = q * movementDir;
        }
        
        CalculateVelocity();
        CalculateGravity();
        CalculateJump();

        PM_FlyMove();
    }

    void CalculateGravity() 
    {    
        Velocity -= SlopeNormal * (mass * Time.deltaTime);


        //clip fall speed to maxFallSpeed
        float verticalFallSpeed = Velocity.magnitude * System.MathF.Max(0, -Vector3.Dot(SlopeNormal, Velocity.normalized));
        //Debug.Log("Vertical fall speed is " + verticalFallSpeed);

       // Velocity += SlopeNormal * System.MathF.Max(0, verticalFallSpeed - maxFallSpeed);  
    }

    void CalculateJump() 
    { 
        if(jumpTrigger) {
            jumpTrigger = false;

            if(jumpTriggerReleased) {
                jumpTriggerReleased = false;
                Velocity.y = jumpForce;
            }

        } else {
            jumpTriggerReleased = true;
        }
    }

    // Update is called once per frame
    void CalculateVelocity()
    {
        Vector3 horizontalVelocity = Velocity;

        //remove vertical factor from the velocity to calculate the horizontal plane velocity easier
        horizontalVelocity -= SlopeNormal * (horizontalVelocity.magnitude * Vector3.Dot(SlopeNormal, horizontalVelocity.normalized));

        float currentSpeed = horizontalVelocity.magnitude;
        Vector3 velDir = horizontalVelocity.normalized;

        float desiredSpeed = speed * movementDirMagnitude;

        //make sure it does not affect vertical velocity
        float deaccCoef = 1;

        float speedMaxDiff = currentSpeed - desiredSpeed;

        float speedToMax = System.MathF.Max(0, speedMaxDiff);
        if (0 == speedToMax)
        {
            deaccCoef *= System.MathF.Min(1, 1 - Vector3.Dot(movementDir, velDir));         
        }

        //make sure it doesn't reduce the speed to less than 0 when not moving
        //+ make sure it doesn't reduce the speed past the desired speed
        float deaccMagn = System.MathF.Min(speedToMax, System.MathF.Min(currentSpeed, deacceleration * deaccCoef * Time.deltaTime));

        Vector3 deaccForce = -velDir * deaccMagn;

        //ACCELERATION CALCULATION

        float speedInDesiredDir = currentSpeed * System.MathF.Max(0, Vector3.Dot(movementDir, velDir));
        float maxAccCoef = System.MathF.Max(0, desiredSpeed - speedInDesiredDir);

        float accMagn = System.MathF.Min(maxAccCoef, Time.deltaTime * acceleration);
        Vector3 accForce = movementDir * accMagn;

        Debug.Log("Speed in desired dir is: " + speedInDesiredDir * currentSpeed + " and speed is: " + currentSpeed);


        Vector3 vel = deaccForce + accForce;

        //Debug.Log("horizontalVelocity " + horizontalVelocity + " and the deacc magn is " + deaccMagn);

        Velocity += vel;
    }

    void OnCollisionStay(Collision collision)
    {
        int contactCount = collision.contactCount;
        // collision.GetContact(0).normal;
        // Debug.DrawRay(collision.GetContact(0).point, collision.GetContact(0).normal, Color.red, 0.2f);

    }
}
