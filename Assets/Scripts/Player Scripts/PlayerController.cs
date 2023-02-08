using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private WeaponFiring m_weaponFiring;

    [SerializeField]
    private GfMovementGeneric m_movement;

    [SerializeField]
    //misc
    private LoadoutManager m_loadoutManager;

    [SerializeField]
    //misc
    private CameraController m_cameraController;

    [SerializeField]
    //misc
    private bool m_fixedUpdatePhysics = true;

    [SerializeField]
    //misc
    private float m_timeBetweenPhysChecks = 0.02f;

    private bool m_releasedJumpButton = false;

    private float m_timeUntilPhysChecks = 0;
    //misc
    private Transform m_playerCamera;

    // Start is called before the first frame update
    void Start()
    {
        if (m_movement == null)
        {
            m_movement = GetComponent<GfMovementGeneric>();
          //  if (null == m_movement)
               // Debug.LogError("ERROR: The gameobject does not have a MovementGeneric component! Please add on to the object");

        }

        if (m_cameraController == null)
        {
            m_playerCamera = Camera.main.transform;

        }
        else m_playerCamera = m_cameraController.transform;

        m_cameraController = m_playerCamera.GetComponent<CameraController>();
        if (null == m_cameraController)
            Debug.LogError("ERROR: The main camera does not have a CameraController component. Please add it to the main camera.");
        else
            m_cameraController.SetMainTarget(m_movement.transform);


        if (m_weaponFiring == null)
        {
            m_weaponFiring = GetComponent<WeaponFiring>();
            if (null == m_weaponFiring)
                Debug.LogError("ERROR: The gameobject does not have a WeaponFiring component! Please add on to the object");
        }

        //Physics.autoSyncTransforms |= !m_fixedUpdatePhysics;
    }


    private void GetMovementInput()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        float movementDirMagnitude = input.magnitude;

        if (movementDirMagnitude > 0.001f)
        {
            //float effectiveMagnitude = System.MathF.Min(1.0f, System.MathF.Max(input.x, input.y));
            if (movementDirMagnitude > 1) GfTools.Div2(ref input, movementDirMagnitude);

            Vector3 cameraForward = m_playerCamera.forward * input.y;
            Vector3 cameraRight = m_playerCamera.right * input.x;

            Vector3 upVec = m_movement.UpvecRotation();

            if (!m_movement.CanFly)
            {
                GfTools.Minus3(ref cameraForward, upVec * Vector3.Dot(upVec, cameraForward));
                cameraForward.Normalize();
            }

            Vector3 movementDir = cameraForward + cameraRight;
            m_movement.SetMovementDir(movementDir, upVec);
        }
        else
        {
            m_movement.SetMovementDir(Vector3.zero);
        }


    }

    private void CalculateJump()
    {
        if (Input.GetAxisRaw("Jump") > 0.8f)
        {
            if (m_releasedJumpButton || true)
            {
                m_releasedJumpButton = false;
                m_movement.JumpTrigger = true;
            }
        }
        else
        {
            m_releasedJumpButton = true;
        }
    }

    private bool wasFiring = false;
    private void GetFireInput()
    {
        if (!m_weaponFiring)
            return;

        if (Input.GetAxisRaw("Fire1") > 0.2f)
        {
            wasFiring = true;
            m_weaponFiring.Fire();
        }
        else if (wasFiring)
        {
            wasFiring = false;
            m_weaponFiring.ReleaseFire();
        }
    }

    private void GetWeaponScrollInput()
    {
        if (m_loadoutManager == null)
            return;

        float wheelValue = Input.GetAxisRaw("Mouse ScrollWheel");
        if (wheelValue >= 0.1f)
        {
            Debug.Log("MOUSE UP");
            m_loadoutManager.NextLoadout();
        }
        else if (wheelValue <= -0.1f)
        {
            Debug.Log("MOUSE DOWN");
            m_loadoutManager.PreviousLoadout();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (m_fixedUpdatePhysics)
        {
            float physDelta = Time.fixedDeltaTime;
            m_movement.UpdatePhysics(physDelta, true); //actually the current deltatime   
        }
    }

    private void PreMoveCalculations(float deltaTime)
    {
        m_cameraController.m_upvec = m_movement.UpvecRotation();
        m_cameraController.UpdateRotation(deltaTime);

        CalculateJump();
        GetMovementInput();

        m_movement.Move(deltaTime);
    }

    void LateUpdate()
    {
        GetFireInput();
        GetWeaponScrollInput();

        float deltaTime = Time.deltaTime;

        PreMoveCalculations(deltaTime);

        if (!m_fixedUpdatePhysics && (m_timeUntilPhysChecks -= deltaTime) <= 0)
        {
            float physDelta = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks - m_timeUntilPhysChecks);
            m_movement.UpdatePhysics(physDelta, false); //actually the current deltatime   
            m_timeUntilPhysChecks += m_timeBetweenPhysChecks;
        }

        m_cameraController.Move(deltaTime);
    }
}
