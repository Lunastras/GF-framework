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


    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();

        aimTarget = null;
    }

    // Update is called once per frame
    public void Move(float deltaTime)
    {
        yaw += Input.GetAxisRaw("Mouse X") * sensitivity * Time.deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref refRotationVelocity, rotationSmoothTime);
        transform.eulerAngles = currentRotation;

        Vector3 desiredTargetPos = mainTarget.position + offset;
        currentTargetPos = Vector3.SmoothDamp(currentTargetPos, desiredTargetPos, ref refTargetPosVelocity, movementSmoothTime);

        timeUntilPhysCheck -= Time.deltaTime;

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

        transform.position = currentTargetPos - forward * currentTargetDst;
        //transform.position = currentTargetPos - forward * dstFromtarget;

    }
}


