using UnityEngine;
using System;
using System.Collections.Generic;

public class MovementAdvanced : MovementBasic
{
    //Player movement values
    public float wallrunMaxDst = 7;
    public float wallrunCheckRadius = 0.6f;

    //time in miliseconds

    public float timingWallrunJumpDelay = 0.2f;

    //Time values 
    protected float timeAttachedToWall;


    //protected Vector2 input;

    //Wallrun values 
    protected float traversedWallDistance = 0;
    protected bool canWallrun = true;

    public bool isSlidingOffWall { get; protected set; }
    protected Vector3 wallRunDir;
    protected float wallRunSpeed;

    //static readonly values
    protected static readonly int WALL_RUN_STICK_SPEED = 100;

    // Start is called before the first frame update
    void Start()
    {
        //  Time.timeScale = 0.5f;   
        Initialize();

    }

    private void FixedUpdate()
    {
        checkPhysics = 0 >= timeUntilPhysCheck;
        timeUntilPhysCheck = checkPhysics ? physCheckInterval : (timeUntilPhysCheck - Time.deltaTime);

        isGrounded = timeUntilUngrounded > 0 || canFly;
        timeUntilUngrounded = Mathf.Max(-1, timeUntilUngrounded - Time.deltaTime);

        jumpingFromLedge = timeLeftLedgeJump > 0 && velocity.y > -3.0f;
        timeLeftLedgeJump = Mathf.Max(-1, timeLeftLedgeJump - Time.deltaTime);
    }

    public override void CalculateMovement(float speedMultiplier = 1)
    {
        UpdateEffectiveValues();
        CalculateWallRun();

        if (!isAttachedToWall)
        {
            GroundCheck();
            CalculateJump();
            CalculateMovementVelocity();
        }

        Move((velocity) * Time.deltaTime);
    }

    RaycastHit wallRunHit;

    private unsafe void CalculateWallRun()
    {
        if (!canWallrun || (isGrounded && !jumpTrigger))
        {
            if (isAttachedToWall)
            {
                DetachFromWall();
            }
            return;
        }

        Vector2 movementDir2Norm = new Vector2(movementDir.x, movementDir.z);

        bool foundWall = !checkPhysics && isAttachedToWall;
        float currentTime = Time.time;

        Vector3 wallSphereCheckPosition = transform.position + transform.forward * Radius();

        //check if the player is touching any walls
        if (checkPhysics && 0 < Physics.OverlapSphereNonAlloc(wallSphereCheckPosition, Radius(), GfPhysics.GetCollidersArray(), GfPhysics.WallrunLayers()))
        {
            if (isAttachedToWall
                || (new Vector2(velocity.x, velocity.z).magnitude > 0.3f && Vector3.Dot(velocity, transform.forward) > 0.1f))
            {
                Vector3 offset = new Vector3(0, Height() / 4.0f, 0);
                //Vector3 dirToWall = (GfPhysics.GetCollidersArray()[0].transform.position - transform.position).normalized;

                float radius = Radius() * 1.5f;

                Ray ray1 = new Ray(transform.position + transform.forward * Radius(), transform.forward);
                Ray ray2 = new Ray(transform.position + transform.forward * Radius() + transform.up * Height() / 2.0f, transform.forward);

                // Debug.Log("Input is accurate");
                //tried spherecast, not working
                if (0 < Physics.RaycastNonAlloc(ray1, GfPhysics.GetRaycastHits(), Radius(), GfPhysics.WallrunLayers()) ||
                    0 < Physics.RaycastNonAlloc(ray2, GfPhysics.GetRaycastHits(), Radius(), GfPhysics.WallrunLayers()))
                {
                    // Debug.Log("Raycast also hit a wall lmao");

                    RaycastHit auxHit = GfPhysics.GetRaycastHits()[0];

                    float wallAngle = Vector3.Angle(auxHit.normal, Vector3.up);
                    foundWall = (wallAngle > SlopeLimit() && wallAngle < 91 && wallAngle > 0); //check if the slope is good for wallrunning

                    isGrounded = false;

                    if (foundWall)
                    {
                        wallRunHit = auxHit;

                        if (isAttachedToWall)
                        {
                            //Debug.Log("Already attached");
                            SetWallRunRotation(wallRunHit);
                        }
                        else
                        {
                            //Debug.Log("Attaching to wall");
                            timeAttachedToWall = Time.time;
                            InitiateWallRun(wallRunHit);
                        }                     
                    } //else Debug.Log("object found between player and wall");
                } //else Debug.Log("No wall found with raycasts");
            }  //else Debug.Log("no velocity all wack");
        }
        

        if (foundWall)
        {
            SetParentTransform(wallRunHit.transform);

            if (!isSlidingOffWall)
            {
                wallRunSpeed += speedAcc * Time.deltaTime;
                wallRunSpeed = Math.Min(wallRunSpeed, speed * 1.5f);
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
                wallRunSpeed -= mass * Time.deltaTime;
                wallRunSpeed = Math.Max(wallRunSpeed, -terminalVelocity / 4f);
            }

            if (checkPhysics)

                if (wallRunSpeed > 0)
                {                    
                    if (0 < Physics.OverlapSphereNonAlloc(transform.position - groundCheck.localPosition, groundCheckRadius, GfPhysics.GetCollidersArray(), ~GfPhysics.IgnoreLayers())) //ceiling check
                    {
                        traversedWallDistance = wallrunMaxDst;
                        isSlidingOffWall = true;
                        //  Debug.Log("Found a ceiling while running up");

                        wallRunSpeed = -0.5f;
                    }
                }
                else
                {
                    if (0 < Physics.OverlapSphereNonAlloc(groundCheck.position, groundCheckRadius, GfPhysics.GetCollidersArray(), GfPhysics.GroundLayers())) //ground check
                    {
                        //Debug.Log("Detaching from wall 1");
                        DetachFromWall();
                    }
                }

            velocity = wallRunSpeed * wallRunDir;
        }
        else if (isAttachedToWall)
        {

            //Debug.Log("Detaching from wall 2");
            DetachFromWall();
            // Debug.Log("I am here for some reason;");
            if (wallRunSpeed > 0)
            {
                // Debug.Log("Climbing ledge");
                jumpingFromLedge = true;
                timeLeftLedgeJump = jumpingFromLedgeInterval;

                wallRunDir = 1.5f * wallRunDir + 1.0f * transform.forward;
                velocity = wallRunDir.normalized * jumpForce;

            }
        }

        if (isAttachedToWall)
        {
            if (jumpTrigger)
            {
                jumpTrigger = false;
                if ((Time.time - timeAttachedToWall) > timingWallrunJumpDelay && jumpTriggerReleased)
                {
                    Vector3 wallNormal = wallRunHit.normal;
                    wallRunSpeed = 0;
                    jumpTriggerReleased = false;
                    Vector3 jumpDir;
                    if (movementDir2Norm != Vector2.zero && Vector2.Dot(new Vector2(wallNormal.x, wallNormal.z), movementDir2Norm) > 0.3f)
                    {
                        jumpDir = new Vector3(movementDir2Norm.x, wallNormal.y + 0.6f, movementDir2Norm.y).normalized;
                    }
                    else
                    {
                        jumpDir = (wallNormal + 0.5f * Vector3.up).normalized;
                    }

                    canDoubleJump = true;
                    velocity = jumpDir * jumpForce * 1.5f;
                    // Debug.Log("Detaching from wall 3");
                    DetachFromWall();
                    //  Debug.Log("Wait, JUMPY??");
                    transform.Rotate(Vector3.up * 180);
                }
            }
            else
            {
                jumpTriggerReleased = true;
            }
        }
    }

    private void DetachFromWall()
    {
        gameObject.layer = 8; //switch to player layer
        isAttachedToWall = isSlidingOffWall = false;

        if (!jumpingFromLedge)
            DetachFromParent();

        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    /**
     * Start the wallrun sequence of the player
     * @param the slope's normal
     */
    private void InitiateWallRun(RaycastHit wallHit)
    {
        gameObject.layer = 10; //switch to wallrun layer
        jumpTriggerReleased = false;
        isAttachedToWall = true;
        canDoubleJump = false;

        traversedWallDistance = 0;
        wallRunSpeed = 4;
        SetWallRunRotation(wallHit);

        if (Vector3.Dot(velocity, wallRunDir) > 0)
        {
            wallRunSpeed = Mathf.Clamp(velocity.magnitude, 4, speed);
        }
        else wallRunSpeed = 4;

        velocity = Vector3.zero;
    }

    private void SetWallRunRotation(RaycastHit wallHit)
    {
        transform.rotation = Quaternion.LookRotation(-wallHit.normal);
        wallRunDir = transform.up;

        const float wallDst = 0.05f;

        Vector3 rayDir = (wallHit.point + wallHit.normal * wallDst) - (transform.position + transform.forward * Radius());

        Move(rayDir * Time.deltaTime * 5.0f);
    }
}