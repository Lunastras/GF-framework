using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform m_mainTarget = null;

    [SerializeField]
    private float m_sensitivity = 1;

    [SerializeField]
    private float m_targetFov = 90;

    [SerializeField]
    private Vector3 m_offset = Vector3.zero;

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
    private float m_fovSmoothTime = 1f;

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
    private float m_currentDesiredDst = 0;

    private float m_timeUntilPhysCheck = 0;

    private bool m_collidingWithSmth = false;

    private RaycastHit m_raycastHit = default;

    private Camera m_camera = null;

    public static CameraController m_currentCamera;

    public Vector3 m_upvec = Vector3.up;

    private static readonly Vector3 UPDIR = Vector3.up;

    private Quaternion m_lastAppliedRot = Quaternion.identity;
    private Quaternion m_previousDesiredRot = Quaternion.identity;

    private Quaternion m_rotVel = Quaternion.identity;

    private float m_fovRefSpeed = 0;

    private Transform m_transform;

    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
        m_camera = GetComponent<Camera>();

        if (m_currentCamera) Destroy(m_currentCamera);
        m_currentCamera = this;

        m_currentTargetDst = m_dstFromtarget;
        m_currentDesiredDst = m_dstFromtarget;
    }

    public void UpdateRotation(float deltaTime)
    {
        float pitchCoef = 1;
        if (m_invertedY) pitchCoef = -1;

        m_yaw += Input.GetAxisRaw("Mouse X") * m_sensitivity * deltaTime;
        m_pitch += Input.GetAxisRaw("Mouse Y") * m_sensitivity * deltaTime * pitchCoef;
        m_pitch = Mathf.Clamp(m_pitch, m_pitchMin, m_pitchMax);

        Quaternion mouseRot = Quaternion.Euler(m_pitch, m_yaw, 0);
        mouseRot = Quaternion.identity;
        Quaternion desiredRot = Quaternion.Inverse(m_lastAppliedRot) * m_previousDesiredRot;
        desiredRot = Quaternion.FromToRotation(desiredRot * UPDIR, m_upvec) * desiredRot;


        m_lastAppliedRot = Quaternion.AngleAxis(m_yaw, m_upvec);
        m_lastAppliedRot = m_lastAppliedRot * Quaternion.AngleAxis(m_pitch, desiredRot * Vector3.right);

        desiredRot = m_lastAppliedRot * desiredRot;
        m_transform.rotation = GfTools.QuatSmoothDamp(m_transform.rotation, desiredRot, ref m_rotVel, m_rotationSmoothTime);

        m_previousDesiredRot = desiredRot;
    }

    // Update is called once per frame
    public void Move(float deltaTime)
    {
        Quaternion upvecCorrection = Quaternion.FromToRotation(UPDIR, m_upvec);

        Vector3 desiredTargetPos = m_mainTarget.position + upvecCorrection * m_offset;
        m_currentTargetPos = Vector3.SmoothDamp(m_currentTargetPos, desiredTargetPos, ref m_refTargetPosVelocity, m_movementSmoothTime);

        m_timeUntilPhysCheck -= deltaTime;

        Vector3 forward = m_transform.forward;

        if (m_timeUntilPhysCheck <= 0 && false)
        {
            m_timeUntilPhysCheck = m_physCheckInterval;

            int layermask = GfPhysics.NonCharacterCollisions();
            m_currentDesiredDst = m_dstFromtarget;

            RaycastHit[] raycastHits = GfPhysics.GetRaycastHits();
            m_collidingWithSmth = 0 < Physics.SphereCastNonAlloc(desiredTargetPos, m_collisionRadius, -forward, raycastHits, m_dstFromtarget, layermask);

            if (m_collidingWithSmth)
            {
                m_raycastHit = raycastHits[0];
                m_currentDesiredDst = Mathf.Max(m_raycastHit.distance, m_minDstFromtarget);
            }
        }

        m_currentTargetDst = Mathf.SmoothDamp(m_currentTargetDst, m_currentDesiredDst * m_distanceMultiplier, ref m_refDistanceVel, m_distanceSmoothTime);

        m_transform.position = m_currentTargetPos - forward * m_currentTargetDst;
        m_camera.fieldOfView = Mathf.SmoothDamp(m_camera.fieldOfView, m_fovMultiplier * m_targetFov, ref m_fovRefSpeed, m_fovSmoothTime);
    }

    public PriorityValue<float> GetDistanceMultiplier() { return m_distanceMultiplier; }

    public void SetDistanceMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        m_distanceMultiplier.SetValue(multiplier, priority, overridePriority);
    }

    public PriorityValue<float> GetFovMultiplier() { return m_fovMultiplier; }

    public void SetFovMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        m_fovMultiplier.SetValue(multiplier, priority, overridePriority);
    }

    public void SetMainTarget(Transform target)
    {
        m_mainTarget = target;
    }
}


