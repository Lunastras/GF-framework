using System;
using UnityEngine;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float sensitivity = 1;
    private Transform mainTarget;

    [SerializeField]
    private float dstFromtarget = 6;

    [SerializeField]
    private float aimDstFromPlayerCoef = 0.5f;

    [SerializeField]
    private float aimYPosOffset = 5f;

    [SerializeField]
    private float playerYPosOffset = 5f;

    [SerializeField]
    private float minDstFromtarget = 4;

    [SerializeField]
    private float pitchMin = -40;

    [SerializeField]
    private float pitchMax = 85;

    [SerializeField]
    private bool canMove = true;

    [SerializeField]
    private float rotationSmoothTime = 0.04f;

    public float targetChangeSpeed = 10;
    private Vector3 currentCameraSpeed;
    //Internal script variables

    private Vector3 rotationSmoothVelocity;
    private Vector3 currentRotation;
    private float distanceSmoothSpeed;
    private float currentDistance;
    private float desiredDst;

    private float yaw;
    private float pitch;
    private Camera cam;

    public float targetSearchRadius = 10;
    public int[] targetLayers;
    private int targetSearchLayerMask;
    private Vector3 currentTargetPos;

    private const float collisionError = 3;
    public int[] collisionLayers;
    private int collisionLayerMask;
    private Transform aimTarget;


    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        mainTarget = FindObjectOfType<MovementAdvanced>().transform;

        targetSearchLayerMask = 0;
        for (int i = 0; i < targetLayers.Length; i++)
        {
            targetSearchLayerMask += (int)Mathf.Pow(2, targetLayers[i]);
        }

        collisionLayerMask = 0;
        for (int i = 0; i < collisionLayers.Length; i++)
        {
            collisionLayerMask += (int)Mathf.Pow(2, collisionLayers[i]);
        }

        // Debug.Log("the mask is: lengths " + targetSearchLayerMask);

        aimTarget = null;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!canMove) return;

        yaw += Input.GetAxisRaw("Mouse X") * sensitivity * Time.deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredTargetPos = mainTarget.position + Vector3.up * playerYPosOffset;
        desiredDst = dstFromtarget;

        if (Input.GetAxisRaw("Aim") > 0.95f)
        {
            if (aimTarget != null || SearchTarget())
            {
                Vector3 mainToTarget = aimTarget.position - mainTarget.position;
                desiredDst = Mathf.Max(mainToTarget.magnitude, minDstFromtarget);
                desiredTargetPos = mainTarget.position + mainToTarget * aimDstFromPlayerCoef + Vector3.up * aimYPosOffset;
            }
            else
            {

                //still do some things;
            }
        }
        else
        {
            aimTarget = null;
        }

        if (currentTargetPos != desiredTargetPos)
        {
            currentTargetPos = Vector3.SmoothDamp(currentTargetPos, desiredTargetPos, ref currentCameraSpeed, targetChangeSpeed);
        }

        currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);
        transform.eulerAngles = currentRotation;

        RaycastHit hit;
        // Physics.Raycast(transform.position, transform.forward, out wallHit, controller.radius * 3, LAYER_MASK_PHYS))
        if (Physics.Raycast(currentTargetPos, -transform.forward, out hit, dstFromtarget, collisionLayerMask))
        {
            float hitError = currentDistance - hit.distance;
            if (hitError < collisionError)
            {
                desiredDst = hit.distance;
            }
        }

        if (desiredDst != currentDistance)
        {
            currentDistance = Mathf.SmoothDamp(currentDistance, desiredDst, ref distanceSmoothSpeed, targetChangeSpeed);
        }


        // Vector3 forwardVector = (Quaternion.Euler(pitch, yaw, 0) * Vector3.forward);
        Vector3 newPosition = (currentTargetPos) - transform.forward * currentDistance;

        transform.position = newPosition;
    }

    public void AddToCurrentTarget(Vector3 vecToAdd)
    {
        currentTargetPos += vecToAdd;
    }

    private bool SearchTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(mainTarget.position, targetSearchRadius, targetSearchLayerMask);
        Debug.Log(colliders.Length);

        if (colliders.Length > 0)
        {

            float shortestDstFromCenter = 99999999;
            int shortestIndex = 0;

            Vector2 normalizationValues = new Vector2(Screen.width, Screen.height) / 2.0f;

            for (int i = 0; i < colliders.Length; i++)
            {
                Vector2 screenCoords = (Vector2)cam.WorldToScreenPoint(colliders[i].transform.position) - normalizationValues;
                Debug.Log("Found " + colliders[i].gameObject.name + " with screen coords of " + screenCoords);
                float dstFromCenterOfScreen = screenCoords.magnitude;

                if (dstFromCenterOfScreen < shortestDstFromCenter)
                {
                    shortestDstFromCenter = dstFromCenterOfScreen;
                    shortestIndex = i;
                }
            }

            aimTarget = colliders[shortestIndex].transform;
            return true;
        }

        return false;
    }
}


