using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgPlayerController : MonoBehaviour
{
    [SerializeField] private GfMovementGeneric m_movement;

    [SerializeField] private GfgStatsCharacter m_statsCharacter;

    //misc
    [SerializeField] private GfgCameraController m_cameraController;

    //misc
    [SerializeField] private bool m_usesRigidBody = false;

    //misc
    [SerializeField] private bool m_fixedUpdatePhysics = true;

    //misc
    [SerializeField] private float m_timeBetweenPhysChecks = 0.02f;

    //misc
    public bool CanTakeInputs = true;

    public bool IgnoreRunner = false;

    protected GfRunnerTemplate m_runner;

    private float m_timeUntilPhysChecks = 0;

    //misc
    private Transform m_playerCamera;

    private Vector3 m_movDir = Vector3.zero;

    private bool m_flagJump = false;

    private bool m_flagDash = false;

    protected FireType m_lastFireType;

    // Start is called before the first frame update
    void Start()
    {
        if (null == m_movement)
        {
            m_movement = GetComponent<GfMovementGeneric>();
            Debug.Assert(m_movement, "ERROR: The gameobject does not have a MovementGeneric component! Please add on to the object");
        }

        if (null == m_statsCharacter)
            m_statsCharacter = GetComponent<GfgStatsCharacter>();

        m_cameraController = GfgCameraController.Instance;

        if (null == m_cameraController)
            Debug.LogError("ERROR: The main camera does not have a GfgCameraController component. Please add it to the main camera.");
        else
        {
            m_cameraController.SetMainTarget(m_movement.transform);
            m_playerCamera = m_cameraController.transform;
            m_cameraController.SnapToTarget();
        }

        m_runner = m_movement.GetRunner();
    }

    private Vector3 GetMovementInput(Vector3 upVec)
    {
        Vector2 input = new(GfgInput.GetAxisRaw(GfgInputType.MOVEMENT_X), GfgInput.GetAxisRaw(GfgInputType.MOVEMENT_Y));

        float movementDirMagnitude = input.magnitude;
        Vector3 movDir = Vector3.zero;
        movDir = input;//todo

        /*
        if (movementDirMagnitude > 0.001f)
        {
            //float effectiveMagnitude = System.MathF.Min(1.0f, System.MathF.Max(input.x, input.y));
            if (movementDirMagnitude > 1) GfcTools.Div2(ref input, movementDirMagnitude);

            Vector3 cameraForward = m_playerCamera.forward;
            GfcTools.Mult(ref cameraForward, input.y);

            Vector3 cameraRight = m_playerCamera.right;
            GfcTools.Mult(ref cameraRight, input.x);

            if (!m_runner.CanFly)
            {
                GfcTools.Minus(ref cameraForward, upVec * Vector3.Dot(upVec, cameraForward));
                GfcTools.Normalize(ref cameraForward);
            }

            movDir = cameraForward;
            GfcTools.Add(ref movDir, cameraRight);
        }*/

        return movDir;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (m_fixedUpdatePhysics && !m_usesRigidBody)
        {
            float physDelta = Time.fixedDeltaTime * (m_statsCharacter ? m_statsCharacter.GetDeltaTimeCoef() : 1);
            GetInputs(physDelta);

            m_movement.UpdatePhysics(physDelta, physDelta, false, IgnoreRunner); //actually the current deltatime   
            m_flagDash = m_flagJump = false;
        }
    }

    void GetInputs(float deltaTime)
    {
        bool auxFlagDash = false;
        bool auxFlagJump = false;
        Vector3 auxMovDir = Vector3.zero;

        if (!GfgManagerLevel.IsPaused() && CanTakeInputs) //get inputs
        {
            Vector3 upVec = m_movement.GetUpvecRotation();

            auxFlagDash = GfgInput.GetInput(GfgInputType.RUN);
            auxFlagJump = GfgInput.GetInput(GfgInputType.JUMP);
            auxMovDir = GetMovementInput(upVec);
        }

        m_flagDash |= auxFlagDash; // used the | operator to keep the flag true until the next phys update call
        m_flagJump |= auxFlagJump;
        m_movDir = auxMovDir;

        uint runnerFlags = m_runner.MyRunnerFlags;
        runnerFlags.SetBit((int)RunnerFlags.DASH, m_flagDash);
        runnerFlags.SetBit((int)RunnerFlags.JUMP, m_flagJump);
        m_runner.MyRunnerFlags = runnerFlags;

        m_runner.SetMovementDir(m_movDir);
    }

    void LateUpdate()
    {
        float deltaTime = Time.deltaTime * (m_statsCharacter ? m_statsCharacter.GetDeltaTimeCoef() : 1);

        GetInputs(deltaTime);

        m_movement.LateUpdateBehaviour(Time.deltaTime);

        if (!m_usesRigidBody && !m_fixedUpdatePhysics && (m_timeUntilPhysChecks -= deltaTime) <= 0)
        {
            float physDelta = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks - m_timeUntilPhysChecks);
            m_timeUntilPhysChecks += m_timeBetweenPhysChecks;
            float timeUntilNextUpdate = System.MathF.Max(deltaTime, m_timeUntilPhysChecks);
            m_movement.UpdatePhysics(physDelta, timeUntilNextUpdate, false, IgnoreRunner);

            m_flagDash = m_flagJump = false;
        }

        m_cameraController.Move(deltaTime);
    }
}