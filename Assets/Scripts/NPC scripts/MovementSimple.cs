﻿using UnityEngine;
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
    private float groundedCheckInterval = 0.1f;

    private float timeOfNextGroundCheck = 0;


    protected new void Initialize()
    {
        base.Initialize();

        velocity = Vector3.zero;
        isGrounded = false;
    }

    private void Start()
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

        velocity.y -= mass * Time.deltaTime;
        velocity.y = Mathf.Max(-terminalVelocity, velocity.y);

        if (isGrounded)
        {
            if (timeOfNextGroundCheck <= Time.time)
            {
                //if last grounded confirmation was 
                //longer than groundedCheckInterval
                jumpTrigger = true;
                // Debug.Log("I jumped I guess?");
            }
        }
    }

    private Vector3 refSmoothVelocity3;
    private Vector2 refSmoothVelocity2;
    private float refSmoothRot;

    protected void CalculateVelocity(float speedMultiplier = 1)
    {
        if (!canMove)
            return;

        float currentYVelocity = velocity.y;
        velocity.y = canFly ? velocity.y : 0;

        float effectiveSpeedSmoothing = speedSmoothing * (isGrounded || canFly ? 1.0f : 0.5f);

        if (movementDir != Vector3.zero)
        {
            float desiredYAngle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            float currentY = transform.rotation.eulerAngles.y;
            float newYRot = Mathf.SmoothDampAngle(currentY, desiredYAngle, ref refSmoothRot, effectiveSpeedSmoothing / Mathf.Max(speed, 0.01f));

            transform.rotation = Quaternion.Euler(0, newYRot, 0);
        }

        Vector3 desiredVelocity = movementDir;
        desiredVelocity *= speed * speedMultiplier;

        velocity = Vector3.SmoothDamp(velocity, desiredVelocity, ref refSmoothVelocity3, effectiveSpeedSmoothing);

        velocity.y = canFly ? velocity.y : currentYVelocity;
    }

    protected void CalculateJump()
    {
        if (jumpTrigger)
        {
            jumpTrigger = false;
            isGrounded = false;
            velocity.y = jumpPower;
            // Debug.Log("I jumped I guess?");
        }
    }

    public override void CalculateMovement(float speedMultiplier = 1.0f)
    {
        GroundCheck();
        CalculateJump();
        CalculateVelocity(speedMultiplier);
        Move(velocity * Time.deltaTime);
    }


    protected void OnControllerColliderHit(ControllerColliderHit collision)
    {
        CalculateCollisionHit(collision.normal, collision.transform);
    }

    protected void CalculateCollisionHit(Vector3 normal, Transform collisionTransform)
    {
        if (velocity.y > 5.0f)
            return;

        int angle = (int)Mathf.Round(Vector3.Angle(Vector3.up, normal));
        // Debug.Log("angle is: " + angle);

        Vector3 velocityChange = -Vector3.Dot(velocity, normal) * normal;

        Vector3 auxVelocity = velocity;
        bool auxGrounded = isGrounded;

        if (angle >= 0)
        {
            if (angle >= SlopeLimit())
            {
                // Debug.Log("over slope limit");

                if (isGrounded)
                {
                    // Debug.Log("jrumpsy");

                    jumpTrigger = true;
                }
                else if (angle > 91)
                {
                    velocity.y = -0.5f;
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

        velocity = auxVelocity;
        isGrounded = auxGrounded;
    }



}