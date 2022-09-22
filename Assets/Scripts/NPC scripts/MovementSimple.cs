using UnityEngine;
using System;
using System.Collections.Generic;

public class MovementSimple : MovementGeneric
{
    //Player movement values
    [SerializeField]
    private float jumpPower = 15.0f;
    [SerializeField]
    private float speedSmoothing = 1.0f;

    [SerializeField]
    private float terminalVelocity = 45;
    [SerializeField]
    private float acceleration = 2;

    [SerializeField]
    private float maxSpeed = 8;


    [SerializeField]
    private float groundedCheckInterval = 0.1f;

    private float timeOfNextGroundCheck = 0;

    private new Rigidbody rigidbody;


    protected void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();

        Velocity = Vector3.zero;
        IsGrounded = false;
    }

    protected override void InternalStart()
    {
        Initialize();
    }

    /**
     * This function checks whether or not the player is grounded or sliding,
     * and applies gravity accordingly.
     */
    protected void GroundCheck()
    {
        if (canFly)
            return;

        Velocity.y -= mass * Time.deltaTime;
        Velocity.y = Mathf.Max(-terminalVelocity, Velocity.y);
        JumpTrigger = timeOfNextGroundCheck <= Time.time && IsGrounded;
    }

    private Vector3 refSmoothVelocity3;
    private Vector2 refSmoothVelocity2;
    private float refSmoothRot;

    protected override void MgOnCollision(MgCollisionStruct collision)
    {

    }

    protected override void BeforeFixedUpdate()
    {
    }

    protected override void AfterFixedUpdate() {
    }
    protected void CalculateVelocity(float speedMultiplier = 1)
    {
        float currentYVelocity = Velocity.y;
        Velocity.y = canFly ? Velocity.y : 0;

        float effectiveSpeedSmoothing = speedSmoothing * (IsGrounded || canFly ? 1.0f : 0.5f);

        if (MovementDir != Vector3.zero)
        {
            float desiredYAngle = System.MathF.Atan2(Velocity.x, Velocity.z) * Mathf.Rad2Deg;
            float currentY = transform.rotation.eulerAngles.y;
            float newYRot = Mathf.SmoothDampAngle(currentY, desiredYAngle, ref refSmoothRot, effectiveSpeedSmoothing / System.MathF.Max(speed, 0.01f));

            transform.rotation = Quaternion.Euler(0, newYRot, 0);
        }

        Vector3 desiredVelocity = MovementDir;
        desiredVelocity *= speed * speedMultiplier;

        Velocity = Vector3.SmoothDamp(Velocity, desiredVelocity, ref refSmoothVelocity3, effectiveSpeedSmoothing);

        Velocity.y = canFly ? Velocity.y : currentYVelocity;
    }

    protected void CalculateJump()
    {
        if (JumpTrigger)
        {
            JumpTrigger = false;
            IsGrounded = false;
            Velocity.y = jumpPower;
            // Debug.Log("I jumped I guess?");
        }
    }

    private float redSmoothSpeed;

    public override void CalculateMovement(float speedMultiplier = 1.0f)
    {
       // GroundCheck();
       // CalculateJump();
       // CalculateVelocity(speedMultiplier);
        //transform.position += (velocity * Time.deltaTime);
        //rigidbody.velocity = velocity;
        Vector3 forceToAdd = Vector3.zero;

        float movementDirMagnitude = MovementDir.magnitude;
        Vector3 vel = MovementDir * acceleration * Time.deltaTime;
        rigidbody.AddForce(vel * speedMultiplier);

        float speedMagnitude = rigidbody.velocity.magnitude;
        if(speedMagnitude > maxSpeed) {
            //rigidbody
        }
        
        //Move(velocity * Time.deltaTime);
    }


    protected void OnControllerColliderHit(ControllerColliderHit collision)
    {
        CalculateCollisionHit(collision.normal, collision.transform);
    }

    void OnCollisionStay(Collision collision)
    {
        collision.GetContact(0);
       // CalculateCollisionHit(collision.normal, collision.transform);
    }

    protected void CalculateCollisionHit(Vector3 normal, Transform collisionTransform)
    {
        if (Velocity.y > 5.0f)
            return;

        int angle = (int)Mathf.Round(Vector3.Angle(Vector3.up, normal));
        // Debug.Log("angle is: " + angle);

        Vector3 velocityChange = -Vector3.Dot(Velocity, normal) * normal;

        Vector3 auxVelocity = Velocity;
        bool auxGrounded = IsGrounded;

        if (angle >= 0)
        {
            if (angle >= slopeLimit)
            {
                // Debug.Log("over slope limit");

                if (IsGrounded)
                {
                    // Debug.Log("jrumpsy");

                    JumpTrigger = true;
                }
                else if (angle > 91)
                {
                    Velocity.y = -0.5f;
                }
                else
                {
                    auxVelocity += velocityChange;
                }
            }
            else //grounded
            {
                auxGrounded = true;
            }
        }

        if (auxGrounded)
        {
            timeOfNextGroundCheck = Time.time + groundedCheckInterval;
            if (auxVelocity.y < 0.5f)
            {
                if (!GfPhysics.LayerIsInMask(collisionTransform.gameObject.layer, GfPhysics.IgnoreLayers()))
                {
                    //Debug.Log("parented " + collisionTransform.gameObject.name + " the layer is: " + collisionTransform.gameObject.layer);
                    SetParentTransform(collisionTransform);
                }
            }

            auxVelocity.y = Math.Min(velocityChange.y + auxVelocity.y, 0);
        }

        Velocity = auxVelocity;
        IsGrounded = auxGrounded;
    }



}
