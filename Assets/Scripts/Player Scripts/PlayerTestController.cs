using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTestController : MovementGeneric
{
    [SerializeField]
    private float acceleration = 10;

    [SerializeField]
    private float maxSpeed = 10;

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
        Debug.Log("CALC MOVEMENT WAS CAALLED, GROUNDED IS " + IsGrounded);

        Vector3 oldDir = movementDir;
        if (SlopeNormal != Vector3.up) {
            Quaternion q = RotationTo(Vector3.up, SlopeNormal);
            movementDir = q * movementDir;
        }

        //Debug.Log("The original dir: " + oldDir + " reprojected dir: " + movementDir + " for the slope " + movement.SlopeNormal);

        
        CalculateVelocity();
        CalculateGravity();
        CalculateJump();

        PM_FlyMove();
    }

    void CalculateGravity() 
    {
        if(!IsGrounded)
            Velocity -= UpVec * (mass * Time.deltaTime);
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

        //make sure it does not affect vertical velocity
        float deaccCoef = 1;

        float speedMaxDiff = currentSpeed - maxSpeed * movementDirMagnitude;

        float speedToMax = System.MathF.Max(0, speedMaxDiff);
        if (0 == speedToMax)
        {
            deaccCoef *= System.MathF.Min(1, 1 - Vector3.Dot(movementDir, velDir));         
        }

        //make sure it doesn't reduce the speed to less than 0 when not moving
        //+ make sure it doesn't reduce the speed past the desired speed
        float deaccMagn = System.MathF.Min(speedToMax, System.MathF.Min(currentSpeed, deacceleration * deaccCoef * Time.deltaTime));

        Vector3 deaccForce = -velDir * deaccMagn;

        float accMagn = Time.deltaTime * acceleration;
        Vector3 accForce = movementDir * accMagn;

        Vector3 vel = deaccForce + accForce;

        Debug.Log("horizontalVelocity " + horizontalVelocity + " and the deacc magn is " + deaccMagn);

        Velocity += vel;
    }

    void OnCollisionStay(Collision collision)
    {
        int contactCount = collision.contactCount;
        // collision.GetContact(0).normal;
        // Debug.DrawRay(collision.GetContact(0).point, collision.GetContact(0).normal, Color.red, 0.2f);

    }
}
