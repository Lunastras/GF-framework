using UnityEngine;
using System;
using System.Collections.Generic;

public class MovementBasic : MovementGeneric
{
    // movement values

    [SerializeField]
    protected float speedAcc = 30;
    [SerializeField]
    protected float speedDeAcc = 40;
    [SerializeField]
    protected float midAirAccCoef = 0.6f;
    [SerializeField]
    protected float midAirDeAccCoef = 0.4f;

    [SerializeField]
    protected float terminalVelocity = 45;
    [SerializeField]
    protected float turnSpeed = 400;
    [SerializeField]
    protected float jumpForce = 13;
    [SerializeField]
    protected float jumpExtGravCoef = 0.65f;
    //time in miliseconds
    [SerializeField]
    protected float timingGroundJumpError = 0.2f;

    [SerializeField]
    protected float physCheckInterval = 0.1f;

    //Time values 
    protected float timeUntilUngrounded;

    protected float effectiveGravityAcc;
    protected float effectiveAcc;
    protected float effectiveDeAcc;

    protected float timeUntilPhysCheck = 0;
    //how much time is left of the ledge jump
    protected float timeLeftLedgeJump = 0;

    //groundcheck values   

    protected float groundCheckRadius;
    protected Transform groundCheck;

    //Movement variables

    public float turnAmount { get; protected set; } //clockwise turnAmount
    protected float targetYDeg;

    public bool jumpingFromLedge { get; protected set; }
    public bool isAttachedToWall { get; protected set; } = false;
    public bool canDoubleJump { get; protected set; }
    public bool canExtendJump { get; protected set; }

    protected bool checkPhysics;

    protected const float groundedInterval = 0.05f;

    protected const float jumpingFromLedgeInterval = 3.0f;

    protected override void InternalStart()
    {

    }

    //protected Vector2 input;
    public float targetSpeed { get; protected set; }

    protected void Initialize()
    {
        effectiveGravityAcc = mass;
        effectiveAcc = speedAcc;
        effectiveDeAcc = speedDeAcc;

        //  Time.timeScale = 0.5f;   

        Velocity = Vector3.zero;

        groundCheck = GameObject.Find("GroundCheck").transform;

        groundCheck.localPosition = Vector3.down * (Height() * 0.45f);
        groundCheckRadius = Radius() * 0.45f;



        // Debug.Log("height: " + (controller.height * 0.475f) + " groundCheckRadius: " + groundCheckRadius);
        //  Debug.Log("desired wall distance  " + desiredWallDst);
    }

    private void LateUpdate()
    {
        CalculateParentMovement();
    }

    protected override void MgOnCollision(MgCollisionStruct collision)
    {

    }

    protected void CalculateJump()
    {
        if (JumpTrigger)
        {
            JumpTrigger = false;
            if (jumpTriggerReleased)
            {
                jumpTriggerReleased = false;
                if (IsGrounded)
                {
                    IsGrounded = false;
                    canExtendJump = canDoubleJump = true;
                    DetachFromParent();
                    Velocity.y = jumpForce;
                }
                else if (canDoubleJump)
                {
                    canDoubleJump = canExtendJump = false;
                    Velocity.y = jumpForce;
                }
            }

        }
        else
        {
            canExtendJump = false;
            jumpTriggerReleased = true;
        }
    }

    protected void UpdateEffectiveValues()
    {
        effectiveDeAcc = speedDeAcc * (IsGrounded ? 1 : midAirDeAccCoef);
        effectiveAcc = speedAcc * (IsGrounded ? 1 : midAirAccCoef);
        effectiveGravityAcc = mass * (canExtendJump ? jumpExtGravCoef : 1) * Convert.ToInt32(!canFly);
    }

    /**
     * This function checks whether or not the player is grounded or sliding,
     * and applies gravity accordingly.
     */
    protected void GroundCheck()
    {
        Velocity.y = Mathf.Max(-terminalVelocity, Velocity.y - effectiveGravityAcc * Time.deltaTime);

        if (!IsGrounded && !jumpingFromLedge)
        {
            canExtendJump = canExtendJump && (Velocity.y > 0);
            DetachFromParent();
        }
    }

    private void FixedUpdate()
    {
        checkPhysics = 0 >= timeUntilPhysCheck;
        timeUntilPhysCheck = checkPhysics ? physCheckInterval : (timeUntilPhysCheck - Time.deltaTime);

        bool auxIsGrounded = timeUntilUngrounded > 0 || canFly;

        //check if previously grounded
        if(!auxIsGrounded && IsGrounded)
        {
            Debug.Log("Was previously grounded");
            Velocity.y = 0;
        }

        timeUntilUngrounded = Mathf.Max(-1, timeUntilUngrounded - Time.deltaTime);
    }

    public override void CalculateMovement(float speedMultiplier = 1)
    {
        UpdateEffectiveValues();

        if (!isAttachedToWall)
        {
            GroundCheck();
            CalculateJump();
            CalculateMovementVelocity();
        }

        Move((Velocity) * Time.deltaTime);
    }

    public override void SetMovementDir(Vector3 dir)
    {
        if (!canFly)
            dir.y = 0;

        movementDirMagnitude = dir.magnitude;
        MovementDir = dir;
    }

    protected void CalculateMovementVelocity()
    {
        Vector3 auxVelocity = Velocity;
        auxVelocity.y = canFly ? auxVelocity.y : 0;
        Vector3 frameAccerelartion = Vector3.zero;

        turnAmount = 0;
        targetSpeed = 0;

        if (canMove && MovementDir != Vector3.zero)
        {
            targetYDeg = Mathf.Atan2(MovementDir.x, MovementDir.z) * Mathf.Rad2Deg;
            float eulerY = transform.eulerAngles.y;
            turnAmount = GfTools.AngleDifference(eulerY, targetYDeg);
            float absTurnAmount = Math.Abs(turnAmount);


            if (absTurnAmount > 0.1f)
            {
                transform.Rotate(Vector3.down * Math.Min(turnSpeed * Time.deltaTime, absTurnAmount) * Math.Sign(turnAmount));
            }
            else
            {
                turnAmount = 0;
            }

            targetSpeed = speed * movementDirMagnitude;

            frameAccerelartion = MovementDir * effectiveAcc * Time.deltaTime;
        }

        Vector3 velocityDir = auxVelocity.normalized;

        float movementDeaccDot = 1;

        if (auxVelocity.magnitude <= targetSpeed || !IsGrounded)
        {
            movementDeaccDot = Vector3.Dot(MovementDir, velocityDir);
            if (movementDeaccDot >= 0)
            {
                movementDeaccDot = 1 - movementDeaccDot;
            }
            else
            {
                movementDeaccDot = 1;
            }
        }

        float deAccMagn = Time.deltaTime * movementDeaccDot * effectiveDeAcc;

        Vector3 deacceleration = velocityDir * deAccMagn;

        if (auxVelocity.magnitude - deAccMagn >= 0)
        {
            auxVelocity -= deacceleration;
        }
        else
        {
            auxVelocity = Vector2.zero;
        }

        if (auxVelocity.magnitude < targetSpeed)
        {
            auxVelocity += frameAccerelartion;
            if (auxVelocity.magnitude > targetSpeed)
            {
                auxVelocity = auxVelocity.normalized * targetSpeed;
            }
        }

        Velocity = new Vector3(auxVelocity.x, canFly ? auxVelocity.y : Velocity.y, auxVelocity.z);
    }


    void OnCollisionStay(Collision collision)
    {
        Debug.Log("I HIT SOMETHING");
        // collision.GetContact(0).normal;
        // Debug.DrawRay(collision.GetContact(0).point, collision.GetContact(0).normal, Color.red, 0.2f);
        CalculateCollisionHit(collision.GetContact(0).normal, collision.transform);
    }

    protected void CalculateCollisionHit(Vector3 normal, Transform collision)
    {
        if (isAttachedToWall || canFly) return;

        int angle = (int)Mathf.Round(Vector3.Angle(Vector3.up, normal));
        //Debug.Log("angle is: " + angle);

        Vector3 velocityChange = (-Vector3.Dot(Velocity, normal) + 0.5f) * normal;
        Vector3 auxVelocity = Velocity;
        bool collisionIsGround = GfPhysics.LayerIsInMask(collision.gameObject.layer, GfPhysics.GroundLayers());
        bool auxGrounded = angle < SlopeLimit() && collisionIsGround;

        //check if hitting ceiling
        if (!auxGrounded && angle >= 110)
        {
            // Debug.Log("Hit ceiling");
            canExtendJump = false;
            auxVelocity.y = Mathf.Min(auxVelocity.y, 0);
        }
        else if (!auxGrounded)//check if sliding
        {
            bool foundGroundCollisions = 0 < Physics.OverlapSphereNonAlloc(groundCheck.position, groundCheckRadius, GfPhysics.GetCollidersArray(), GfPhysics.GroundLayers());
            if (foundGroundCollisions) //is most likely still grounded
            {
                // Debug.Log("sliding but found ground");
                auxGrounded = true;
            }
            else
            { //isSliding

                //Debug.Log("I am sliding rn i guess with an angle of ");
                auxGrounded = false;
                //check if previously grounded
                if (IsGrounded)
                {
                    //Debug.Log("Was previously GROUNDED SIDDIING");
                    auxVelocity.y = Velocity.y = 0;
                }
                else
                {
                    auxVelocity += velocityChange;
                }
            }
        }



        if (auxGrounded)
        {
            timeUntilUngrounded = groundedInterval;
            SetParentTransform(collision);
            auxVelocity.y = Math.Max(-terminalVelocity, auxVelocity.y);
        }

        IsGrounded = auxGrounded;

        if (!jumpingFromLedge)
        {
            Velocity = auxVelocity;
        }
        else
        {
            //  Debug.Log("I am still jumping from ledge");
        }
    }

    protected void OnControllerColliderHit(ControllerColliderHit collision)
    {
        CalculateCollisionHit(collision.normal, collision.transform);
    }
}
