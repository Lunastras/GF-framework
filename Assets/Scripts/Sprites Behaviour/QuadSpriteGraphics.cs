using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class QuadSpriteGraphics : MonoBehaviour
{
    [SerializeField]
    public float m_xFollowFactor;

    [SerializeField]
    protected GfMovementGeneric m_movement;

    [SerializeField]
    protected Vector3 m_defaultUpvec = Vector3.up;

    [SerializeField]
    protected Vector3 m_defaultCameraUpvec = Vector3.up;

    private static CameraController m_cameraController;
    private static Transform m_cameraTransform;

    private Transform m_transform;

    // Start is called before the first frame update
    void Start()
    {
        m_cameraTransform = Camera.main.transform;
        m_cameraController = m_cameraTransform.GetComponent<CameraController>();
        if (null == m_cameraController) Debug.LogWarning("The main camera does not have the CameraController component. Default upvec of (0, 1, 0) for the camera has been set");
        m_transform = transform;
    }

    // Update is called once per frame

    private static readonly float THRESHOLD_CALCULATION = 0.99f;


    void Update()
    {
        Vector3 upVec = null != m_movement ? m_movement.UpvecRotation() : m_defaultUpvec;

        Vector3 dirToCamera = m_cameraTransform.position - m_transform.position;
        GfTools.Normalize(ref dirToCamera);

        float angle = GfTools.Angle(upVec, dirToCamera);
        float auxAngle = 90 + m_xFollowFactor * (angle - 90f);

        transform.rotation = Quaternion.LookRotation(dirToCamera, upVec) * Quaternion.AngleAxis(auxAngle - angle, Vector3.right);
    }
}

