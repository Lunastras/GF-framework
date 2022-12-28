using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private WeaponFiring weaponFiring;

    [SerializeField]
    private MovementGeneric movement;

    [SerializeField]
    //misc
    private LoadoutManager loadoutManager;

    [SerializeField]
    //misc
    private Transform playerCamera;

    [SerializeField]
    //misc
    private CameraController cameraController;

    [SerializeField]
    //misc
    private bool invertedY = false;
    private bool releasedJumpButton = false;



    // Start is called before the first frame update
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main.transform;


        if (movement == null)
            movement = GetComponent<MovementGeneric>();


        if (weaponFiring == null)
            weaponFiring = GetComponent<WeaponFiring>();
    }


    private void GetMovementInput()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        float movementDirMagnitude = input.magnitude;

        if (movementDirMagnitude > 0.001f)
        {
            //float effectiveMagnitude = System.MathF.Min(1.0f, System.MathF.Max(input.x, input.y));
            if (movementDirMagnitude > 1) GfTools.Div2(ref input, movementDirMagnitude);

            Vector3 cameraForward = playerCamera.forward * input.y;
            Vector3 cameraRight = playerCamera.right * input.x;

            Vector3 upVec = movement.UpVec;

            if (!movement.CanFly)
            {
                GfTools.Minus3(ref cameraForward, upVec * Vector3.Dot(upVec, cameraForward));
                cameraForward.Normalize();
            }

            Vector3 movementDir = cameraForward + cameraRight;
            movement.SetMovementDir(movementDir, upVec);

            /*
            targetYDeg = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + playerCamera.eulerAngles.y;
            Vector2 movementDir2Norm = GfTools.Degree2Vector2(targetYDeg);


            float yDir = playerCamera.forward.y;
            yDir *= invertedY ? -1 : 1;

            movement.SetMovementDir(new Vector3(movementDir2Norm.x, yDir, movementDir2Norm.y));*/
        }
        else
        {
            movement.SetMovementDir(Vector3.zero);
        }


    }

    private void CalculateJump()
    {
        if (Input.GetAxisRaw("Jump") > 0.8f)
        {
            if (releasedJumpButton)
            {
                releasedJumpButton = false;
                movement.JumpTrigger = true;
            }
        }
        else
        {
            releasedJumpButton = true;
        }
    }

    private bool wasFiring = false;
    private void GetFireInput()
    {
        if (!weaponFiring)
            return;

        if (Input.GetAxisRaw("Fire1") > 0.2f)
        {
            wasFiring = true;
            weaponFiring.Fire();
        }
        else if (wasFiring)
        {
            wasFiring = false;
            weaponFiring.ReleaseFire();
        }
    }

    private void GetWeaponScrollInput()
    {
        if (loadoutManager == null)
            return;

        float wheelValue = Input.GetAxisRaw("Mouse ScrollWheel");
        if (wheelValue >= 0.1f)
        {
            Debug.Log("MOUSE UP");
            loadoutManager.NextLoadout();
        }
        else if (wheelValue <= -0.1f)
        {
            Debug.Log("MOUSE DOWN");
            loadoutManager.PreviousLoadout();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        GetFireInput();
        GetWeaponScrollInput();
    }

    void LateUpdate()
    {
        float deltaTime = Time.deltaTime;

        //only get input for movement if physics will be checked
        if (movement.PhysCheckGivenDelta(deltaTime))
        {
            CalculateJump();
            GetMovementInput();
        }

        cameraController.Upvec = movement.UpVecEstimated();

        movement.Move(deltaTime);
        cameraController.Move(deltaTime);
    }
}
