using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform m_mainTarget;

    [SerializeField]
    private float m_sensitivity = 1;

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
    private bool m_invertedY = false;

    //Internal script variables

    private Vector3 m_refRotationVelocity;
    private Vector3 m_refTargetPosVelocity;
    private float m_refDistanceVel;

    private Vector3 m_currentRotation;
    private float m_distanceSmoothSpeed;

    private float m_yaw;
    private float m_pitch;

    private Vector3 m_currentTargetPos;
    private float m_currentTargetDst;
    private float m_currentDesiredDst;


    private float m_timeUntilPhysCheck;

    private Transform m_aimTarget;

    private bool m_collidingWithSmth = false;

    private RaycastHit m_raycastHit;

    private Camera m_camera;



    public Vector3 m_upvec = Vector3.up;

    private static readonly Vector3 UPDIR = Vector3.up;

    private Quaternion m_lastAppliedRot = Quaternion.identity;
    private Quaternion m_previousDesiredRot = Quaternion.identity;

    private Quaternion m_rotVel = Quaternion.identity;

    // Start is called before the first frame update
    void Start()
    {
        m_camera = GetComponent<Camera>();

        m_aimTarget = null;
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
        transform.rotation = GfTools.QuatSmoothDamp(transform.rotation, desiredRot, ref m_rotVel, m_rotationSmoothTime);

        m_previousDesiredRot = desiredRot;


        // transform.rotation = upvecCorrection * mouseRot;
    }

    // Update is called once per frame
    public void Move(float deltaTime)
    {
        Quaternion upvecCorrection = Quaternion.FromToRotation(UPDIR, m_upvec);

        Vector3 desiredTargetPos = m_mainTarget.position + upvecCorrection * m_offset;
        m_currentTargetPos = Vector3.SmoothDamp(m_currentTargetPos, desiredTargetPos, ref m_refTargetPosVelocity, m_movementSmoothTime);

        m_timeUntilPhysCheck -= deltaTime;

        Vector3 forward = transform.forward;

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

        m_currentTargetDst = Mathf.SmoothDamp(m_currentTargetDst, m_currentDesiredDst, ref m_refDistanceVel, m_distanceSmoothTime);

        // transform.position = currentTargetPos - forward * currentTargetDst;
        transform.position = m_currentTargetPos - forward * m_currentTargetDst;

    }

    public void SetMainTarget(Transform target)
    {
        m_mainTarget = target;
    }
}


