using UnityEngine;
using UnityEngine.Rendering;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform m_mainTarget = null;

    [SerializeField]
    private float m_sensitivity = 1;

    [SerializeField]
    private float m_targetFov = 90;

    [SerializeField]
    private Vector3 m_positionOffset = Vector3.zero;

    [SerializeField]
    private float m_physCheckInterval = 0.2f;

    [SerializeField]
    private float m_dstFromtarget = 6;

    [SerializeField]
    private float m_collisionRadius = 1;

    [SerializeField]
    private float m_minDstFromtarget = 4;

    [SerializeField]
    private float m_pitchMin = -89;

    [SerializeField]
    private float m_pitchMax = 85;

    [SerializeField]
    private float m_rotationSmoothTime = 0.04f;

    [SerializeField]
    private float m_movementSmoothTime = 0.04f;

    [SerializeField]
    private float m_distanceSmoothTime = 0.04f;

    [SerializeField]
    private float m_minimumDistanceSmoothTime = 0.04f;

    [SerializeField]
    private bool m_invertedY = false;

    private PriorityValue<float> m_distanceMultiplier = new(1);

    private PriorityValue<float> m_fovMultiplier = new(1);

    //Internal script variables

    private Vector3 m_refTargetPosVelocity = default;
    private float m_refDistanceVel = 0;

    private float m_yaw = 0;
    private float m_pitch = 0;

    private Vector3 m_currentTargetPos = default;
    private float m_currentTargetDst = 0;
    private float m_desiredDst = 0;

    private float m_timeUntilPhysCheck = 0;

    private bool m_collidingWithSmth = false;

    private RaycastHit m_raycastHit = default;

    private Camera m_camera = null;

    public static CameraController Instance;

    public Camera Camera { get { return m_camera; } }

    public Vector3 Upvec = Vector3.up;

    private static readonly Vector3 UPDIR = Vector3.up;

    private Quaternion m_lastAppliedRot = Quaternion.identity;
    private Quaternion m_previousDesiredRot = Quaternion.identity;

    private Quaternion m_rotVel = Quaternion.identity;

    private float m_fovRefSpeed = 0;

    private Transform m_transform;

    private float m_fovSmoothTime = 1f;

    private float m_currentSmoothTimeDistance;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this) Destroy(Instance);
        Instance = this;

        m_transform = transform;
        m_camera = GetComponent<Camera>();

        m_currentTargetDst = m_dstFromtarget;
        m_desiredDst = m_dstFromtarget;
    }

    public static void SnapToTargetInstance()
    {
        Instance.SnapToTarget();
    }

    public static void LookFowardInstance(float pitchDegrees = 10, float yawOffset = 0)
    {
        Instance.LookForward(pitchDegrees, yawOffset);
    }

    public CameraController GetInstance() { return Instance; }

    public void SnapToTarget()
    {
        UpdateRotation(0, true);
        m_currentTargetPos = m_mainTarget.position;
        m_transform.position = m_currentTargetPos - m_transform.forward * m_currentTargetDst;
    }

    //Look towards the target's forward
    public void LookForward(float pitchDegrees = 10, float yawOffset = 0)
    {
        UpdateRotation(0, false, false);
        Vector3 cameraForward = m_previousDesiredRot * Vector3.forward;
        GfcTools.RemoveAxis(ref cameraForward, Upvec);
        GfcTools.Normalize(ref cameraForward);

        Vector3 targetForward = m_mainTarget.forward;
        GfcTools.RemoveAxis(ref targetForward, Upvec);
        GfcTools.Normalize(ref targetForward);

        float angle = GfcTools.SignedAngleDegNorm(targetForward, cameraForward, Upvec);

        m_yaw -= angle + yawOffset;
        m_pitch = pitchDegrees;
    }

    public void UpdateRotation(float deltaTime, bool instantUpdate = false, bool updateTransform = true)
    {
        float pitchCoef = 1;
        if (m_invertedY) pitchCoef = -1;

        m_yaw += Input.GetAxisRaw("Mouse X") * m_sensitivity;
        m_pitch += Input.GetAxisRaw("Mouse Y") * m_sensitivity * pitchCoef;
        m_pitch = Mathf.Clamp(m_pitch, m_pitchMin, m_pitchMax);

        Quaternion mouseRot = Quaternion.Euler(m_pitch, m_yaw, 0);
        mouseRot = Quaternion.identity;
        Quaternion desiredRot = Quaternion.Inverse(m_lastAppliedRot) * m_previousDesiredRot;
        desiredRot = Quaternion.FromToRotation(desiredRot * UPDIR, Upvec) * desiredRot;

        m_lastAppliedRot = Quaternion.AngleAxis(m_yaw, Upvec);
        m_lastAppliedRot = m_lastAppliedRot * Quaternion.AngleAxis(m_pitch, desiredRot * Vector3.right);

        desiredRot = m_lastAppliedRot * desiredRot;

        if (updateTransform)
            if (instantUpdate)
                m_transform.rotation = desiredRot;
            else
                m_transform.rotation = GfcTools.QuatSmoothDamp(m_transform.rotation, desiredRot, ref m_rotVel, m_rotationSmoothTime, deltaTime);

        m_previousDesiredRot = desiredRot;
    }

    // Update is called once per frame
    public void Move(float deltaTime)
    {
        Quaternion upvecCorrection = Quaternion.FromToRotation(UPDIR, Upvec);

        Vector3 desiredTargetPos = m_mainTarget.position + upvecCorrection * m_positionOffset;
        m_currentTargetPos = Vector3.SmoothDamp(m_currentTargetPos, desiredTargetPos, ref m_refTargetPosVelocity, m_movementSmoothTime, int.MaxValue, deltaTime);

        m_timeUntilPhysCheck -= deltaTime;

        Vector3 forward = m_transform.forward;

        if (m_timeUntilPhysCheck <= 0)
        {
            m_timeUntilPhysCheck = m_physCheckInterval;

            int layermask = GfcPhysics.NonCharacterCollisions();
            float currentDistance = (m_currentTargetPos - m_transform.position).magnitude;
            float previousDesiredDistance = m_desiredDst;
            m_desiredDst = m_dstFromtarget;

            RaycastHit[] raycastHits = GfcPhysics.GetRaycastHits();
            float minDistanceOffset = m_minDstFromtarget;
            GfcTools.Add3(ref desiredTargetPos, forward * -minDistanceOffset);

            m_collidingWithSmth = 0 < Physics.SphereCastNonAlloc(desiredTargetPos, m_collisionRadius, -forward, raycastHits, m_dstFromtarget, layermask, QueryTriggerInteraction.Ignore);

            if (m_collidingWithSmth)
            {
                m_raycastHit = raycastHits[0];
                m_desiredDst = System.MathF.Min(m_dstFromtarget, m_raycastHit.distance + minDistanceOffset);
            }

            if (System.MathF.Abs(previousDesiredDistance - m_desiredDst) > 0.1f)
                m_currentSmoothTimeDistance = System.MathF.Max(m_minimumDistanceSmoothTime, m_distanceSmoothTime * (System.MathF.Max(0, System.MathF.Abs(currentDistance - m_desiredDst) - 2f) / m_dstFromtarget));
        }

        m_currentTargetDst = Mathf.SmoothDamp(m_currentTargetDst, m_desiredDst * m_distanceMultiplier, ref m_refDistanceVel, m_currentSmoothTimeDistance, int.MaxValue, deltaTime);

        m_transform.position = m_currentTargetPos - forward * m_currentTargetDst;
        m_camera.fieldOfView = Mathf.SmoothDamp(m_camera.fieldOfView, m_fovMultiplier * m_targetFov, ref m_fovRefSpeed, m_fovSmoothTime, int.MaxValue, deltaTime);
    }

    public PriorityValue<float> GetDistanceMultiplier() { return m_distanceMultiplier; }

    public void SetDistanceMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        m_distanceMultiplier.SetValue(multiplier, priority, overridePriority);
    }

    public PriorityValue<float> GetFovMultiplier() { return m_fovMultiplier; }

    public void SetFovMultiplier(float multiplier, float fovSmoothTime = 1, uint priority = 0, bool overridePriority = false)
    {
        if (m_fovMultiplier.SetValue(multiplier, priority, overridePriority))
        {
            m_fovSmoothTime = fovSmoothTime;
        }
    }

    public void SetMainTarget(Transform target)
    {
        m_mainTarget = target;
    }
}


