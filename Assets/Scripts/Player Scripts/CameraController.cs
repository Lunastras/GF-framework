using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform mainTarget;

    [SerializeField]
    private float sensitivity = 1;

    [SerializeField]
    private Vector3 offset;

    [SerializeField]
    private float physCheckInterval = 0.2f;

    [SerializeField]
    private float dstFromtarget = 6;

    [SerializeField]
    private float collisionRadius = 1;

    [SerializeField]
    private float minDstFromtarget = 4;

    [SerializeField]
    private float pitchMin = -40;

    [SerializeField]
    private float pitchMax = 85;

    [SerializeField]
    private float rotationSmoothTime = 0.04f;

    [SerializeField]
    private float movementSmoothTime = 0.04f;

    [SerializeField]
    private float distanceSmoothTime = 0.04f;

    //Internal script variables

    private Vector3 refRotationVelocity;
    private Vector3 currentRotation;
    private float distanceSmoothSpeed;

    private float yaw;
    private float pitch;
    private Camera cam;

    private Vector3 currentTargetPos;
    private float currentTargetDst;
    private float currentDesiredDst;


    private float timeUntilPhysCheck;

    private Transform aimTarget;

    private bool collidingWithSmth = false;

    private RaycastHit raycastHit;

    private Vector3 refTargetPosVelocity;
    private float refDistanceVel;

    private Vector3 desiredPosition;

    public Vector3 Upvec = Vector3.up;

    private Vector3 previousUp = Vector3.up;

    private static readonly Vector3 UPDIR = Vector3.up;

    private Quaternion m_lastAppliedRot = Quaternion.identity;

    private float m_oldUpvecDotSign;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();

        aimTarget = null;
    }

    public void UpdateRotation(float deltaTime)
    {
        yaw += Input.GetAxisRaw("Mouse X") * sensitivity * deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity * deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        /*

        Quaternion upvecCorrection;
        float currentSign = Mathf.Sign(Vector3.Dot(UPDIR, Upvec));

        if (Vector3.Dot(UPDIR, Upvec) < 0)
            upvecCorrection = Quaternion.FromToRotation(-UPDIR, Upvec) * Quaternion.FromToRotation(UPDIR, -UPDIR);
        else
            upvecCorrection = Quaternion.FromToRotation(UPDIR, Upvec);

        if (m_oldUpvecDotSign != currentSign)
        {
            Vector3 horizontalVector = GfTools.RemoveAxis(Upvec, UPDIR);
            horizontalVector.Normalize();
            float angle = 2 * GfTools.SignedAngle(Vector3.forward, horizontalVector, UPDIR);
            yaw += currentSign * angle;
        }

        m_oldUpvecDotSign = currentSign;
        */

        Quaternion mouseRot = Quaternion.Euler(pitch, yaw, 0);
        mouseRot = Quaternion.identity;
        Quaternion desiredRot = Quaternion.Inverse(m_lastAppliedRot) * transform.rotation;
        desiredRot = Quaternion.FromToRotation(desiredRot * UPDIR, Upvec) * desiredRot;


        m_lastAppliedRot = Quaternion.AngleAxis(yaw, Upvec);
        m_lastAppliedRot = m_lastAppliedRot * Quaternion.AngleAxis(pitch, desiredRot * Vector3.right);

        desiredRot = m_lastAppliedRot * desiredRot;
        transform.rotation = desiredRot;


        // transform.rotation = upvecCorrection * mouseRot;
    }

    // Update is called once per frame
    public void Move(float deltaTime)
    {
        Quaternion upvecCorrection = Quaternion.FromToRotation(UPDIR, Upvec);

        Vector3 desiredTargetPos = mainTarget.position + upvecCorrection * offset;
        currentTargetPos = Vector3.SmoothDamp(currentTargetPos, desiredTargetPos, ref refTargetPosVelocity, movementSmoothTime);

        timeUntilPhysCheck -= deltaTime;

        Vector3 forward = transform.forward;

        if (timeUntilPhysCheck <= 0)
        {
            timeUntilPhysCheck = physCheckInterval;

            int layermask = GfPhysics.NonCharacterCollisions();
            Collider[] colliders = GfPhysics.GetCollidersArray();
            currentDesiredDst = dstFromtarget;

            if (collidingWithSmth || 0 < Physics.OverlapSphereNonAlloc(transform.position, 2.0f * collisionRadius, colliders, layermask))
            {
                RaycastHit[] raycastHits = GfPhysics.GetRaycastHits();
                collidingWithSmth = 0 < Physics.SphereCastNonAlloc(desiredTargetPos, collisionRadius, -forward, raycastHits, dstFromtarget, layermask);

                if (collidingWithSmth)
                {
                    raycastHit = raycastHits[0];
                    currentDesiredDst = Mathf.Max(raycastHit.distance, minDstFromtarget);
                }
            }
        }

        currentTargetDst = Mathf.SmoothDamp(currentTargetDst, currentDesiredDst, ref refDistanceVel, distanceSmoothTime);

        // transform.position = currentTargetPos - forward * currentTargetDst;
        transform.position = desiredTargetPos - forward * dstFromtarget;

    }
}


