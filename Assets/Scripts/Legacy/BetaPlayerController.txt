using UnityEngine;
using System;
using System.Collections.Generic;

public class BetaPlayerController : MonoBehaviour
{
    //Player movement values
    public float maxRunSpeed = 10;
    public float speedAcc = 49;
    public float midAirAccCoef = 0.6f;
    public float speedDeAcc = 60;
    public float midAirDeAccCoef = 0.5f;
    public float gravityAcc = 45;
    public float terminalVelocity = 60;
    public float turnSpeedCoef = 0.002f;
    public float jumpForce = 13;
    public float jumpExtGravCoef = 0.63f;
    public float wallrunMaxDst = 50;
    public float wallrunCheckRadius = 0.4f;
    public bool canMove = true;

    //jump values
    private bool canDoubleJump = true;
    private bool canExtendJump = true;

    //groundcheck values
    private Transform groundCheck;
    private float groundCheckRadius = 0.4f;
    private bool isGrounded = false;
    private bool isSliding = false;
    private bool isFloating;
    private bool isClimbingLedge = false;


    //Movement variables
    private CharacterController controller;
    private Vector2 movementDir;
    private Vector3 velocity;
    private Vector2 input;

    //Wallrun values 
    private float traversedWallDistance = 0;
    private Transform wallCheck;
    private Transform wallCheckWallAttached;
    private bool canWallrun = true;
    private bool isAttachedToWall = false;
    private bool isSlidingOffWall = false;
    private Vector3 wallRunDir;
    private float wallRunSpeed;

    //parent position movement
    private Transform parentTransform;
    private Vector3 parentLastPos;
    private bool initiatedParentPos = false;
    private Vector3 parentRotMov;
    private Vector3 parentPosMov;
    private float timeOfLastParentPosUpdate;
    private bool hasToDetachFromParent = false;

    //parent rotation 
    private Vector3 parentLastRot;
    private float timeOfLastParentRotUpdate;

    //misc
    private Transform playerCamera;

    private bool jumpedFromLedge = false;

    private float turnSmoothVelocity;
    private float desiredWallDst;
    private bool jumpButtonReleased = true;
    //layer 9 and 1
    private static readonly int LAYER_MASK_PHYS = 513;
    private static readonly int WALL_RUN_STICK_SPEED = 8;

    // Start is called before the first frame update
    void Start()
    {
        // Time.timeScale = 0.4f;   
        controller = GetComponent<CharacterController>();
        playerCamera = Camera.main.transform;
        velocity = Vector3.zero;
        wallCheck = GameObject.Find("WallCheck").transform;
        wallCheck.localPosition = new Vector3(0, controller.height * 0.1f, controller.radius);

        wallCheckWallAttached = GameObject.Find("WallCheckDuringWallrun ").transform;
        groundCheck = GameObject.Find("GroundCheck").transform;
        groundCheck.localPosition = Vector3.down * (controller.height * 0.325f);
        groundCheckRadius = controller.radius * 0.9f;
        desiredWallDst = controller.radius * 1.05f;

        Debug.Log("desired wall distance  " + desiredWallDst);
        Debug.Log("GroundCheck is " + groundCheckRadius);
    }

    // Update is called once per frame
    void Update()
    {

        GroundCeilingCheck();
        CalculateMovement(); //calculate horizontal movement
        CalculateWallRun();

        if (!isAttachedToWall)
        {
            CalculateJump();
        }

        controller.Move((velocity) * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (hasToDetachFromParent && isFloating)
        {
            DetachFromParent();
        }

        if (parentTransform != null && initiatedParentPos)
        {


            Vector3 currentPos = parentTransform.position;
            if (parentLastPos != currentPos)
            {
                float currentTime = Time.time;
                parentPosMov = currentPos - parentLastPos;
                controller.Move(parentPosMov);

                float timeSinceLastFrame = currentTime - timeOfLastParentPosUpdate;

                parentPosMov = timeSinceLastFrame >= Time.deltaTime ? (parentPosMov /= timeSinceLastFrame) :
                                                                          Vector3.zero;


                timeOfLastParentPosUpdate = currentTime;
                parentLastPos = currentPos;
            }

            Vector3 currentRot = parentTransform.rotation.eulerAngles;
            if (parentLastRot != currentRot)
            {
                float currentTime = Time.time;
                Vector3 parentRotation = (currentRot - parentLastRot);
                float timeSinceLastFrame = currentTime - timeOfLastParentRotUpdate;

                Quaternion q = Quaternion.AngleAxis(parentRotation.y, Vector3.up);
                Vector3 dif = transform.position - parentTransform.position;
                dif = q * dif;
                parentRotMov = (parentTransform.position + dif) - transform.position;

                // Debug.DrawRay(transform.position, parentRotMov * 20f, Color.green, 0.1f);

                if (input == Vector2.zero)
                {
                    transform.Rotate(Vector3.up * parentRotation.y);
                }

                controller.Move(parentRotMov);
                parentRotMov = timeSinceLastFrame >= Time.deltaTime ? (parentRotMov /= timeSinceLastFrame) :
                                                                           Vector3.zero;

                // Debug.Log(parentRotMov + "with time since last frame of " + timeSinceLastFrame + " and a delta timeof " + Time.deltaTime);

                timeOfLastParentRotUpdate = currentTime;
                parentLastRot = currentRot;
            }
        }
    }

    private void SetParentTransform(Transform parent)
    {
        if (parent != parentTransform)
        {
            Debug.Log("set parent " + parent.name);
            timeOfLastParentPosUpdate = Time.time;
            parentRotMov = parentPosMov = Vector3.zero;
            parentTransform = parent;
            parentLastPos = parent.position;
            initiatedParentPos = true;
            hasToDetachFromParent = false;
            parentLastRot = parent.rotation.eulerAngles;
        }
    }

    private void DetachFromParent()
    {
        if (isClimbingLedge || isAttachedToWall || jumpedFromLedge)
        {
            //  Debug.Log("tried detaching but climbing ledge");
            return;
        }

        if (parentTransform != null && initiatedParentPos)
        {
            Vector3 parentMovement = parentPosMov + parentRotMov;
            initiatedParentPos = false;
            parentMovement.y = Mathf.Max(0, parentMovement.y);
            velocity += parentMovement;
            Debug.Log(parentMovement + " added to velocity at time frame ");
            parentTransform = null;
        }

        hasToDetachFromParent = false;
    }

    /**
     * This function checks whether or not the player is grounded or sliding,
     * and applies gravity accordingly.
     */
    private void GroundCeilingCheck()
    {
        //Groundchek will check for BOTH the 9th layer and 0th layer
        Collider[] groundColliders = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, LAYER_MASK_PHYS);

        isGrounded = groundColliders.Length > 0;

        if (isAttachedToWall && isGrounded)
        {
            Debug.Log("we are grounded bruh");
            DetachFromWall();
        }

        //Interact with anything BUT player entitties (8th layer) and the playerWallRun Layers (10th layer)
        Collider[] ceilingColliders = Physics.OverlapSphere(transform.position - groundCheck.localPosition, groundCheckRadius, ~1280);
        if (ceilingColliders.Length > 0)
        {
            if (isAttachedToWall)
            {
                if (wallRunSpeed > 0)
                {
                    // Debug.Log("found ceiling: " + ceilingColliders[0].name);

                    traversedWallDistance = wallrunMaxDst;
                    isSlidingOffWall = true;
                    wallRunSpeed = -1;
                }
            }
            else if (velocity.y > 0) //it hit an object while jumping

            {
                velocity = new Vector3(velocity.x, -1, velocity.z);
            }
        }

        if (!isAttachedToWall)
        {
            velocity.y -= gravityAcc * Time.deltaTime * (canExtendJump ? jumpExtGravCoef : 1);
            velocity.y = Mathf.Max(-terminalVelocity, velocity.y);

            isFloating = (!isGrounded || isSliding);
            //isClimbingLedge = isClimbingLedge && !(isGrounded || isSliding);

            isClimbingLedge = isClimbingLedge && velocity.y > 5;

            //another check taking into account sliding as well
            if (!isFloating && velocity.y < 0.5f)
            {
                SetParentTransform(groundColliders[0].transform);
                canDoubleJump = true;
                jumpedFromLedge = false;
            }
            else
            {
                canExtendJump = canExtendJump && (velocity.y > 0);
                isGrounded = false;
                hasToDetachFromParent = isFloating = true;
            }
        }
    }


    private void CalculateJump()
    {
        if (Input.GetAxisRaw("Jump") > 0.95f)
        {
            if (jumpButtonReleased)
            {
                jumpButtonReleased = false;
                if (!isFloating)
                {
                    canExtendJump = true;
                    velocity.y = jumpForce;
                }
                else if (canDoubleJump)
                {
                    canDoubleJump = canExtendJump = false;
                    velocity.y = jumpForce;
                }
            }
        }
        else
        {
            jumpButtonReleased = true;
            canExtendJump = false;
        }
    }

    private void CalculateMovement()
    {

        input = Vector2.zero;
        input = (new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));
        float inputMagnitude = input.magnitude;
        if (inputMagnitude > 1)
        {
            inputMagnitude = 1;
            input = input.normalized;
        }

        float targetYDeg = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + playerCamera.eulerAngles.y;
        movementDir = Degree2Vector2(targetYDeg);

        if (!isAttachedToWall)
        {
            Vector2 horizontalVel = new Vector2(velocity.x, velocity.z);
            Vector2 frameAccerelartion = Vector2.zero;
            float targetSpeed = 0;

            if (canMove && input != Vector2.zero && !isClimbingLedge)
            {
                float eulerY = transform.eulerAngles.y;
                float angleDiff = Mathf.Abs(AngleDifference(eulerY, targetYDeg));
                float turnSpeed = turnSpeedCoef * angleDiff;

                transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(eulerY, targetYDeg, ref turnSmoothVelocity, turnSpeed);

                targetSpeed = maxRunSpeed * inputMagnitude;

                float accelerationCoef = (isFloating ? midAirAccCoef : 1);

                frameAccerelartion = movementDir * speedAcc * accelerationCoef * Time.deltaTime;
                Vector2 newVel = horizontalVel + frameAccerelartion;

                horizontalVel = newVel;
            }

            float deAccCoef = isFloating ? midAirDeAccCoef : 1;

            Vector2 deacceleration = horizontalVel.normalized * speedDeAcc * deAccCoef * Time.deltaTime;
            if (horizontalVel.magnitude - deacceleration.magnitude >= 0)
            {
                horizontalVel -= deacceleration;
            }
            else
            {
                horizontalVel = Vector2.zero;
            }

            float hozitontalMagnitude = horizontalVel.magnitude;
            if (hozitontalMagnitude < targetSpeed)
            {
                horizontalVel += frameAccerelartion;
                hozitontalMagnitude = horizontalVel.magnitude;
                if (horizontalVel.magnitude > targetSpeed)
                {
                    horizontalVel = horizontalVel.normalized * targetSpeed;
                    hozitontalMagnitude = horizontalVel.magnitude;
                }
            }

            if (!isFloating && hozitontalMagnitude > maxRunSpeed)
            {
                horizontalVel = horizontalVel.normalized * maxRunSpeed;
            }

            velocity = new Vector3(horizontalVel.x, velocity.y, horizontalVel.y);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit collision)
    {
        if (isAttachedToWall || isClimbingLedge) return;

        Vector3 normal = collision.normal;

        if (collision.collider.tag != "Destroyable")
        {
            float angle = Vector3.Angle(Vector3.up, normal);
            isSliding = Vector3.Angle(Vector3.up, normal) > controller.slopeLimit;
            Vector3 velocityChange = -Vector3.Dot(velocity, normal) * normal;
            //Debug.Log("angle is: " + (int)angle);

            if (isSliding)
            {
                velocity += velocityChange;
            }
            else
            {
                velocity.y += velocityChange.y;
            }
        }
    }

    private void CalculateWallRun()
    {
        if (isGrounded || !canMove || !canWallrun || isClimbingLedge)
        {
            if (isAttachedToWall)
            {
                Debug.Log("one of these is true??? why");
                DetachFromWall();
            }
            return;
        }

        RaycastHit wallHit = new RaycastHit();
        bool foundWall = false;

        Vector3 wallSphereCheckPosition = isAttachedToWall ? wallCheckWallAttached.position : wallCheck.position;

        //check if the player is touching any walls
        if (Physics.OverlapSphere(wallSphereCheckPosition, wallrunCheckRadius, LAYER_MASK_PHYS).Length > 0)
        {
            //Debug.Log("Found wall");
            //check if player is heading towards wall and get wall's surface normal
            //ignore if player is already attached to wall
            if (isAttachedToWall || (new Vector2(velocity.x, velocity.z).magnitude > 0.2f && Vector3.Dot(velocity, transform.forward) > 0.1f))
            {
                // Debug.Log("Input is accurate");
                if (Physics.Raycast(transform.position, transform.forward, out wallHit, controller.radius * 2, LAYER_MASK_PHYS))
                {
                    // Debug.Log("Raycast also hit a wall lmao");

                    float wallAngle = Vector3.Angle(wallHit.normal, Vector3.up);
                    foundWall = (wallAngle > controller.slopeLimit && wallAngle < 91 && wallAngle > 0); //check if the slope is good for wallrunning
                    //Debug.Log(wallAngle);

                    if (foundWall)
                    {
                        if (isAttachedToWall)
                        {
                            // Debug.Log("Already attached");
                            SetWallRunRotation(wallHit);
                        }
                        else
                        {
                            //Debug.Log("Attaching to wall");
                            InitiateWallRun(wallHit);
                        }
                    }
                }
            }
        }

        if (foundWall)
        {
            SetParentTransform(wallHit.transform);
            Debug.DrawRay(wallCheck.position, wallRunDir * 1.5f, Color.green, 2f);

            if (!isSlidingOffWall)
            {
                wallRunSpeed += speedAcc * Time.deltaTime;
                wallRunSpeed = Math.Min(wallRunSpeed, maxRunSpeed * 1.5f);
                traversedWallDistance += wallRunSpeed * Time.deltaTime;
                float dstDifference = wallrunMaxDst - traversedWallDistance;

                if (dstDifference < 0) //start falling off wall
                {
                    isSlidingOffWall = true;
                    wallRunSpeed += dstDifference;
                }

            }
            else
            {
                wallRunSpeed -= gravityAcc * Time.deltaTime;
                wallRunSpeed = Math.Max(wallRunSpeed, -terminalVelocity / 4f);
            }

            velocity = wallRunSpeed * wallRunDir;
        }
        else if (isAttachedToWall)
        {

            DetachFromWall();
            Debug.Log("I am here for some reason;");
            if (wallRunSpeed > 0)
            {
                Debug.Log("Climbing ledge");
                isClimbingLedge = jumpedFromLedge = true;
                velocity = (wallRunDir * 1.4f + transform.forward).normalized * jumpForce * 1.2f;
            }
        }

        if (isAttachedToWall)
        {
            if (Input.GetAxis("Jump") > 0.1f)
            {
                if (jumpButtonReleased)
                {
                    Vector3 wallNormal = wallHit.normal;
                    jumpButtonReleased = false;
                    wallRunSpeed = 0;
                    Vector3 jumpDir;
                    if (input != Vector2.zero && Vector2.Dot(new Vector2(wallNormal.x, wallNormal.z), movementDir) > 0.1f)
                    {
                        jumpDir = (new Vector3(movementDir.x, wallNormal.y, movementDir.y) + 0.5f * Vector3.up).normalized;
                    }
                    else
                    {
                        jumpDir = (wallNormal + 0.5f * Vector3.up).normalized;
                    }

                    velocity = jumpDir * jumpForce * 1.5f;
                    DetachFromWall();
                    Debug.Log("Wait, JUMPY??");
                    transform.Rotate(Vector3.up * 180);
                }
            }
            else
            {
                jumpButtonReleased = true;
            }
        }
    }

    private void DetachFromWall()
    {
        Debug.Log("Detaching from wall");
        gameObject.layer = 8; //switch to player layer
        isAttachedToWall = isSlidingOffWall = false;
        hasToDetachFromParent = true;
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    /**
     * Start the wallrun sequence of the player
     * @param the slope's normal
     */
    private void InitiateWallRun(RaycastHit wallHit)
    {
        gameObject.layer = 10; //switch to wallrun layer
        jumpButtonReleased = false;
        isAttachedToWall = true;
        canDoubleJump = false;
        velocity = Vector3.zero;
        traversedWallDistance = 0;
        wallRunSpeed = 4;
        SetWallRunRotation(wallHit);
    }

    private void SetWallRunRotation(RaycastHit wallHit)
    {
        Vector3 slopeNormal = wallHit.normal;
        transform.rotation = Quaternion.LookRotation(-slopeNormal);
        wallRunDir = (Vector3.up - slopeNormal * Vector3.Dot(slopeNormal, Vector3.up)).normalized;

        float distanceError = desiredWallDst - wallHit.distance;
        if (distanceError <= -0.05f || distanceError >= 0.05f)
        {
            float movement = Mathf.Sign(distanceError) * WALL_RUN_STICK_SPEED * Time.deltaTime;
            if (Math.Sign(distanceError - movement) != Math.Sign(distanceError))
            {
                movement = distanceError;
            }
            controller.Move(slopeNormal * movement);
        }
    }

    private float AngleDifference(float deg1, float deg2)
    {
        float diff = (deg1 - deg2 + 180) % 360 - 180;
        return ((diff < -180 ? diff + 360 : diff));
    }

    private Vector2 Degree2Vector2(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
    }
}
