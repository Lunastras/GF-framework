using UnityEngine;
using System;
using System.Collections.Generic;

public class AlphaPlayerController : MonoBehaviour
{
    //Player movement values
    public float maxRunSpeed = 10;
    public float speedAcc = 10;
    public float midAirAccCoef = 0.6f;
    public float speedDeAcc = 10;
    public float gravityAcc = 10;
    public float terminalVelocity = 90;
    public bool canMove = true;
    public float turnSpeedCoef = 1;
    public float jumpForce = 200;

    //groundcheck values
    private Transform groundCheck;
    public float groundCheckRadius = 0.4f;
    public bool isGrounded = false;
    private bool hasLanded = false;
    private bool isSliding = false;
    private Vector3 slippingDir;
    private float groundedVerticalSpeed;


    //Movement variables
    private CharacterController controller;
    //the following two are sepparated to more easily work with the data
    private Vector2 horizontalMovement;
    //private float verticalSpeed;
    private Vector2 movementDir;
    private Vector3 velocity; //not accounting movement
    private Vector2 input;
    private float slopeCheckRayDistance = 1.3f;

    //Wallrun values
    public float wallrunMaxDst = 50;
    public float wallrunCheckRadius = 0.4f;
    private float traversedWallDistance = 0;
    private Transform wallCheck;
    private Transform wallCheckWallAttached;
    private bool canWallrun = true;
    public bool isAttachedToWall = false;
    public bool isSlidingOffWall = false;
    private Vector3 wallRunDir;
    private float wallRunSpeed;


    //misc
    private Transform mainCamera;
    private Transform parentTransform;
    private float turnSmoothVelocity;
    private Transform modelTransform;
    private bool jumpButtonReleased = true;
    private Transform slipCheckPos;
    //layer 9 and 1
    private static readonly int LAYER_MASK_PHYS = 513;


    // Start is called before the first frame update
    void Start()
    {
        //Time.timeScale = 0.2f;
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main.transform;
        groundCheck = GameObject.Find("GroundCheck").transform;
        velocity = Vector3.zero;
        wallCheck = GameObject.Find("WallCheck").transform;
        wallCheckWallAttached = GameObject.Find("WallCheckDuringWallrun ").transform;
        modelTransform = GameObject.Find("PlayerModel").transform;

        groundCheck.localPosition = -Vector3.up * (controller.height / 2);
        groundCheckRadius = controller.radius * 0.8f;
        slopeCheckRayDistance = controller.radius * 1.3f;
        wallCheck.localPosition = new Vector3(0, 0, controller.radius);
        groundedVerticalSpeed = terminalVelocity * 1.5f;
    }

    // Update is called once per frame
    void Update()
    {
        CalculateWallRun();
        GroundCeilingCheck();

        if (!isAttachedToWall)
        {
            CalculateMovement(); //calculate horizontal movement
            CalculateJump();
        }

        Vector3 movement = (new Vector3(horizontalMovement.x, 0, horizontalMovement.y));
        controller.Move((movement + velocity) * Time.deltaTime);
    }

    /**
     * This function checks whether or not the player is grounded or sliding,
     * and applies gravity accordingly.
     */
    private void GroundCeilingCheck()
    {

        isSliding = false;
        slippingDir = Vector3.down; //direction of the sliding/slipping

        //Slide check
        if (velocity.y < 1) //check if player is jumping
        {
            Vector3 slopeNormal = GetSlopeNormal();

            if (slopeNormal != Vector3.zero)
            {
                isSliding = (Vector3.Angle(Vector3.up, slopeNormal) > controller.slopeLimit);
                if (isSliding)
                {
                    slippingDir = (Vector3.down + slopeNormal * Vector3.Dot(slopeNormal, Vector3.up)).normalized;
                    Debug.DrawRay(groundCheck.position, slippingDir * 1.5f, Color.blue, 4);
                }
            }
        }

        //Groundchek will check for BOTH the 9th layer and 0th layer
        Collider[] groundColliders = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, LAYER_MASK_PHYS);

        //another check taking into account sliding as well
        if (groundColliders.Length > 0 && !isSliding)
        {
            isGrounded = hasLanded = true;

            parentTransform = groundColliders[0].transform;

            if (velocity.y < 1)
            {
                velocity = Vector3.down * groundedVerticalSpeed;
            }
        }
        else if (!isAttachedToWall)
        {

            if (velocity.y < 1 && hasLanded)
            { //get rid of player's groundedVerticalSpeed
                velocity = Vector3.zero;
                hasLanded = false;
            }

            velocity += slippingDir * gravityAcc * Vector3.Dot(Vector3.down, slippingDir) * Time.deltaTime;

            if (velocity.magnitude > terminalVelocity)
            {
                velocity = velocity.normalized * terminalVelocity;
            }

            isGrounded = false;
            parentTransform = null;
        }

        //Interact with anything BUT player entitties (8th layer)
        Collider[] ceilingColliders = Physics.OverlapSphere(transform.position - groundCheck.localPosition, groundCheckRadius, 2147483391);
        if (ceilingColliders.Length > 0)
        {
            //hit an object while jumping
            if (velocity.y > 0)
            {
                velocity = new Vector3(velocity.x, -1, velocity.z);
            }

            if (isAttachedToWall)
            {
                traversedWallDistance = wallrunMaxDst;
                isSlidingOffWall = true;
            }
        }
    }


    /**Checks if the player is sliding on a slope or is slipping from the 
     * edge of a platform
     */
    private Vector3 GetSlopeNormal()
    {
        RaycastHit slopeHit;
        float numRays = 4;
        float step = 2 * Mathf.PI / numRays;
        float rotationY = transform.eulerAngles.y * Mathf.Deg2Rad;

        Vector3 shortestNormal = Vector3.zero;
        float shortestDst = 999999;
        float rad;
        Vector3 rayDir;
        // Debug.Log("I am here checking slope 2");
        while (numRays > 0)
        {
            rad = step * numRays + rotationY;
            rayDir = new Vector3(Mathf.Sin(rad), (-2) * controller.height, Mathf.Cos(rad)).normalized;
            if (Physics.Raycast(transform.position - groundCheck.localPosition, rayDir, out slopeHit, controller.height * 1.5f, LAYER_MASK_PHYS))
            {
                float distance = slopeHit.distance;

                if (distance < shortestDst)
                {
                    shortestNormal = slopeHit.normal;
                    shortestDst = distance;
                }
            }

            //Debug.DrawRay(transform.position - groundCheck.localPosition, rayDir * controller.height * 1.5f, Color.red);

            numRays--;
        }

        return shortestNormal;
    }

    private void CalculateJump()
    {
        if (Input.GetAxisRaw("Jump") > 0.95f)
        {
            if (jumpButtonReleased)
            {
                jumpButtonReleased = false;
                if (isGrounded)
                {
                    velocity = new Vector3(velocity.x, jumpForce, velocity.z);
                }
            }
        }
        else
        {
            jumpButtonReleased = true;
        }
    }

    private void CalculateMovement()
    {

        bool isFooted = (isGrounded && !isSliding);
        float targetMaxSpeed = isFooted ? maxRunSpeed : terminalVelocity;
        Vector2 horizontalVel = new Vector2(velocity.x, velocity.z);

        if (canMove)
        {
            input = (new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));
            if (input.magnitude > 1)
            {
                input = input.normalized;
            }

            if (input != Vector2.zero)
            {
                float targetYDeg = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
                float eulerY = transform.eulerAngles.y;
                float angleDiff = Mathf.Abs(AngleDifference(eulerY, targetYDeg));
                float turnSpeed = turnSpeedCoef * angleDiff;

                transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(eulerY, targetYDeg, ref turnSmoothVelocity, turnSpeed);
                movementDir = Degree2Vector2(targetYDeg);

                float targetSpeed = maxRunSpeed * input.magnitude;

                float accelerationCoef;
                bool isFloating = (!isGrounded || isSliding);

                accelerationCoef = (horizontalVel.magnitude > targetSpeed * 0.9f) ? 3f : 1;
                accelerationCoef *= isFloating ? midAirAccCoef : 1;

                horizontalVel += movementDir * speedAcc * accelerationCoef * Time.deltaTime;

                if (horizontalVel.magnitude > targetMaxSpeed)
                {
                    horizontalVel = horizontalVel.normalized * targetMaxSpeed;
                }
            }
        }

        float inputMag = input.magnitude;

        if (!isFooted || inputMag < 0.01f)
        {
            float deAccCoef = isFooted ? 1 : midAirAccCoef;
            float targetMinSpeed = maxRunSpeed * inputMag;

            Vector2 deacceleration = horizontalVel.normalized * speedDeAcc * deAccCoef * Time.deltaTime;
            if (horizontalVel.magnitude - deacceleration.magnitude >= targetMinSpeed)
            {
                horizontalVel -= deacceleration;
            }
            else
            {
                horizontalVel = horizontalVel.normalized * targetMinSpeed;
            }
        }

        velocity = new Vector3(horizontalVel.x, velocity.y, horizontalVel.y);
    }

    private void CalculateWallRun()
    {
        if (isGrounded || !canMove || !canWallrun)
        {
            if (isAttachedToWall)
            {
                DetachFromWall();
            }
            return;
        }

        RaycastHit wallHit = new RaycastHit();
        bool foundWall = false;

        Vector3 wallSphereCheckPosition = isAttachedToWall ? wallCheckWallAttached.position : wallCheck.position;
        float checkRadius = wallrunCheckRadius * (isAttachedToWall ? 2 : 1);

        //check if the player is touching any walls
        if (Physics.OverlapSphere(wallSphereCheckPosition, wallrunCheckRadius, LAYER_MASK_PHYS).Length > 0)
        {
            // Debug.Log("Found wall");
            //check if player is heading towards wall and get wall's surface normal
            //ignore if player is already attached to wall
            if (isAttachedToWall || (Vector3.Dot(horizontalMovement, transform.forward) > -0.2f && horizontalMovement.magnitude > 0.2f))
            {
                // Debug.Log("Input is accurate");
                if (Physics.Raycast(wallCheck.position, transform.forward, out wallHit, 3, LAYER_MASK_PHYS))
                {
                    // Debug.Log("Raycast also hit a wall lmao");

                    float wallAngle = Vector3.Angle(wallHit.normal, Vector3.up);
                    foundWall = (wallAngle > controller.slopeLimit && wallAngle < 91); //check if the slope is good for wallrunning

                    if (foundWall)
                    {
                        if (isAttachedToWall)
                        {
                            // Debug.Log("Already attached");
                            SetWallRunRotation(wallHit);
                        }
                        else
                        {
                            //  Debug.Log("Attaching to wall");
                            InitiateWallRun(wallHit);
                        }
                    }
                }
                else
                {
                    Debug.DrawRay(wallCheck.position, transform.forward * 3, Color.cyan, 10f);
                    Debug.Log("No wall found");
                }
            }
            else Debug.Log("direction baddy");
        }
        else Debug.Log("no collider found");

        if (foundWall)
        {
            Debug.DrawRay(wallCheck.position, wallRunDir * 1.5f, Color.green, 2f);
            // transform.rotation = Quaternion.Lerp(transform.rotation, targetWallRotation, 0.5f);

            if (!isSlidingOffWall)
            {
                Debug.Log(velocity.magnitude);
                wallRunSpeed += speedAcc * Time.deltaTime;
                wallRunSpeed = Math.Min(wallRunSpeed, maxRunSpeed * 2);
                traversedWallDistance += wallRunSpeed * Time.deltaTime;
                float dstDifference = wallrunMaxDst - traversedWallDistance;

                if (dstDifference < 0) //start falling off wall
                {
                    isSlidingOffWall = true;
                    wallRunSpeed += dstDifference;
                    slippingDir = wallRunDir;
                }

                velocity = wallRunSpeed * wallRunDir;

                if (isSlidingOffWall)
                {
                    velocity = Vector3.zero;
                    wallRunSpeed = 0;
                }
            }
        }
        else if (isAttachedToWall)
        {

            Debug.Log("detaching from wall rn");

            DetachFromWall();

            if (!isSlidingOffWall)
            {
                velocity = (wallRunDir + transform.forward).normalized * jumpForce;
            }
        }

        if (isAttachedToWall)
        {
            if (Input.GetAxis("Jump") > 0.1f)
            {
                if (jumpButtonReleased)
                {
                    jumpButtonReleased = false;
                    wallRunSpeed = 0;
                    Vector3 jumpVelocity = (wallHit.normal + 0.5f * Vector3.up).normalized * jumpForce * 2f;
                    velocity = Vector3.up * velocity.y;
                    horizontalMovement = new Vector2(jumpVelocity.x, jumpVelocity.z);
                    DetachFromWall();
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
        //gameObject.layer = 8; //switch to player layer
        modelTransform.localPosition = Vector3.zero;
        isAttachedToWall = isSlidingOffWall = false;
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    /**
     * Start the wallrun sequence of the player
     * @param the slope's normal
     */
    private void InitiateWallRun(RaycastHit wallHit)
    {
        // gameObject.layer = 10; //switch to wallrun layer
        jumpButtonReleased = false;
        isAttachedToWall = true;
        velocity = horizontalMovement = Vector3.zero;
        traversedWallDistance = 0;
        wallRunSpeed = 4;

        SetWallRunRotation(wallHit);
        //Debug.DrawRay(transform.position, transform.forward, Color.red, 2f);
    }

    private void SetWallRunRotation(RaycastHit wallHit)
    {
        Vector3 slopeNormal = wallHit.normal;
        Quaternion characterRotation = Quaternion.LookRotation(-slopeNormal);
        transform.rotation = characterRotation;
        Vector3 transformForward = transform.forward;
        Vector3 playerForward = (new Vector3(transformForward.x, 0, transformForward.z)).normalized;
        wallRunDir = (Vector3.up - slopeNormal * Vector3.Dot(slopeNormal, Vector3.up)).normalized;
        //.DrawRay(groundCheck.position, wallRunDir * 1.5f, Color.blue, 0.5f);
        //Debug.Log(wallRunDir);

        // Debug.Log(wallHit.distance);

        if (wallHit.distance > 0.1f)
        {
            controller.Move(-slopeNormal * 5f * Time.deltaTime);
        }
    }

    private void CheckLedgeGrabbing()
    {

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
