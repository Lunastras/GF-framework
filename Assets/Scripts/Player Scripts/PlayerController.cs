using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    private FiringWeapons m_weaponFiring;

    [SerializeField]
    private GfMovementGeneric m_movement;

    [SerializeField]
    private StatsCharacter m_statsCharacter;

    [SerializeField]
    //misc
    private LoadoutManager m_loadoutManager;

    [SerializeField]
    //misc
    private CameraController m_cameraController;

    [SerializeField]
    //misc
    private bool m_usesRigidBody = false;

    [SerializeField]
    //misc
    private bool m_fixedUpdatePhysics = true;

    [SerializeField]
    //misc
    private float m_timeBetweenPhysChecks = 0.02f;

    [SerializeField]
    //misc
    private float m_scrollCooldown = 0.02f;

    private float m_timeUntillCanScroll = 0;

    private float m_timeUntilPhysChecks = 0;

    private bool m_fire1WasPressed = false;
    private bool m_fire2WasPressed = false;

    //misc
    private Transform m_playerCamera;

    private Vector3 m_movDir = Vector3.zero;
    private NetworkVariable<FireType> m_fireType = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private bool m_flagJump = false;
    private bool m_flagDash = false;

    protected FireType m_lastFireType;

    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            GfLevelManager.SetPlayer(transform);
            if (null == m_movement)
            {
                m_movement = GetComponent<GfMovementGeneric>();
                //  if (null == m_movement)
                // Debug.LogError("ERROR: The gameobject does not have a MovementGeneric component! Please add on to the object");
            }

            if (null == m_statsCharacter) m_statsCharacter = GetComponent<StatsCharacter>();

            if (null == m_cameraController)
            {
                m_playerCamera = Camera.main.transform;
                m_cameraController = m_playerCamera.GetComponent<CameraController>();
            }
            else m_playerCamera = m_cameraController.transform;

            m_cameraController = m_playerCamera.GetComponent<CameraController>();
            if (null == m_cameraController)
                Debug.LogError("ERROR: The main camera does not have a CameraController component. Please add it to the main camera.");
            else
                m_cameraController.SetMainTarget(m_movement.transform);

        }

        if (null == m_loadoutManager)
        {
            m_loadoutManager = GetComponent<LoadoutManager>();
            if (null == m_loadoutManager)
                Debug.LogWarning("PlayerControler: The loadout manager is null, please give it a value in the inspector. Object: " + gameObject.name);
        }

        if (null == m_weaponFiring)
        {
            m_weaponFiring = GetComponent<FiringWeapons>();
            if (null == m_weaponFiring)
                Debug.LogError("ERROR: The gameobject does not have a WeaponFiring component! Please add on to the object");
        }

        m_weaponFiring.SetAimTransform(m_playerCamera);
    }


    private Vector3 GetMovementInput(Vector3 upVec)
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        float movementDirMagnitude = input.magnitude;
        Vector3 movDir = Vector3.zero;

        if (movementDirMagnitude > 0.001f)
        {
            //float effectiveMagnitude = System.MathF.Min(1.0f, System.MathF.Max(input.x, input.y));
            if (movementDirMagnitude > 1) GfTools.Div2(ref input, movementDirMagnitude);

            Vector3 cameraForward = m_playerCamera.forward;
            GfTools.Mult3(ref cameraForward, input.y);

            Vector3 cameraRight = m_playerCamera.right;
            GfTools.Mult3(ref cameraRight, input.x);

            if (!m_movement.CanFly)
            {
                GfTools.Minus3(ref cameraForward, upVec * Vector3.Dot(upVec, cameraForward));
                GfTools.Normalize(ref cameraForward);
            }

            movDir = cameraForward;
            GfTools.Add3(ref movDir, cameraRight);
        }

        return movDir;
    }

    private void Fire()
    {
        if (m_weaponFiring)
        {
            if (m_fireType.Value != FireType.NONE)
            {
                m_weaponFiring.Fire(m_fireType.Value);
            }

            if (m_lastFireType != m_fireType.Value && m_lastFireType != FireType.NONE)
            {
                m_weaponFiring.ReleaseFire(m_lastFireType);
            }
        }

        m_lastFireType = m_fireType.Value;
    }

    private void GetWeaponScrollInput()
    {
        if (m_loadoutManager && m_timeUntillCanScroll <= 0)
        {
            float wheelValue = Input.GetAxisRaw("Mouse ScrollWheel");

            if (wheelValue >= 0.1f)
            {
                m_timeUntillCanScroll = m_scrollCooldown;
                m_loadoutManager.NextLoadout();
            }
            else if (wheelValue <= -0.1f)
            {
                m_timeUntillCanScroll = m_scrollCooldown;
                m_loadoutManager.PreviousLoadout();
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (IsOwner && m_fixedUpdatePhysics && !m_usesRigidBody)
        {
            float physDelta = Time.fixedDeltaTime * m_statsCharacter.GetDeltaTimeCoef();
            GetInputs(physDelta);

            m_movement.UpdatePhysics(physDelta, physDelta); //actually the current deltatime   
            m_flagDash = m_flagJump = false;
        }
    }

    void GetInputs(float deltaTime)
    {
        bool auxFlagFire1 = false;
        bool auxFlagFire2 = false;
        bool auxFlagDash = false;
        bool auxFlagJump = false;
        Vector3 auxMovDir = Vector3.zero;

        if (!GfLevelManager.IsPaused()) //get inputs
        {
            Vector3 upVec = m_movement.GetUpvecRotation();
            m_cameraController.m_upvec = m_movement.GetUpvecRotation();
            m_cameraController.UpdateRotation(deltaTime);

            auxFlagFire1 = Input.GetAxisRaw("Fire1") > 0.5f;
            auxFlagFire2 = Input.GetAxisRaw("Fire2") > 0.5f;
            auxFlagDash = Input.GetAxisRaw("Dash") > 0.8f;
            auxFlagJump = Input.GetAxisRaw("Jump") > 0.8f;
            auxMovDir = GetMovementInput(upVec);

            GetWeaponScrollInput();
        }

        FireType finalType = FireType.NONE;

        if (auxFlagFire2 && (!auxFlagFire1 || !m_fire2WasPressed))
        {
            finalType = FireType.SECONDARY;
        }

        if (auxFlagFire1 && (!auxFlagFire2 || !m_fire1WasPressed))
        {
            finalType = FireType.MAIN;
        }

        if (finalType == FireType.NONE && (auxFlagFire1 || auxFlagFire2))
            finalType = m_lastFireType;

        m_fireType.Value = finalType;

        m_flagDash |= auxFlagDash; // used the | operator to keep the flag true until the next phys update call
        m_flagJump |= auxFlagJump;
        m_movDir = auxMovDir;

        m_movement.FlagDash |= m_flagDash;
        m_movement.FlagJump |= m_flagJump;
        m_movement.SetMovementDir(m_movDir);

        m_fire1WasPressed = auxFlagFire1;
        m_fire2WasPressed = auxFlagFire2;
    }

    void LateUpdate()
    {
        if (IsOwner)
        {
            float deltaTime = Time.deltaTime * m_statsCharacter.GetDeltaTimeCoef();
            m_timeUntillCanScroll -= deltaTime;

            GetInputs(deltaTime);

            m_movement.Move(Time.deltaTime);

            if (!m_usesRigidBody && !m_fixedUpdatePhysics && (m_timeUntilPhysChecks -= deltaTime) <= 0)
            {
                float physDelta = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks - m_timeUntilPhysChecks);
                m_timeUntilPhysChecks += m_timeBetweenPhysChecks;
                float timeUntilNextUpdate = System.MathF.Max(deltaTime, m_timeUntilPhysChecks);
                m_movement.UpdatePhysics(physDelta, timeUntilNextUpdate);

                m_flagDash = m_flagJump = false;
            }

            m_cameraController.Move(deltaTime);
        }

        Fire();
    }

    /*

    [ClientRpc]
    private void FinishedMovementCalculationsClientRpc()
    {
        m_flagDash = false;
        m_flagJump = false;
    }


    [ClientRpc]
    private void FireWeaponClientRpc(bool fire)
    {
        m_flagFire = fire;
        Fire(fire);
    }

    [ServerRpc]
    private void SetFireServerRpc(bool fire)
    {
        m_flagFire = fire;
        Fire(fire);
        FireWeaponClientRpc(fire);
    }

    [ServerRpc]
    private void SetMovementDirServerRpc(Vector3 dir)
    {
        float sqrMagnitude = dir.sqrMagnitude;
        if (1 < sqrMagnitude) //make sure the magnitude isn't higher than 1
            GfTools.Div3(ref dir, System.MathF.Sqrt(sqrMagnitude)); //normalise vector

        m_movement.SetMovementDir(dir);
    }

    [ServerRpc]
    private void SetDashFlagServerRpc(bool dash)
    {
        m_flagDash = dash;
        m_movement.FlagDash = dash;
    }

    [ServerRpc]
    private void SetJumpFlagServerRpc(bool dash)
    {
        m_flagJump = dash;
        m_movement.FlagJump = dash;
    }*/
}
