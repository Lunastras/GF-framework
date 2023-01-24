using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;


using static Unity.Mathematics.math;

public class QuadFollowCamera : JobChild
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

    private NativeArray<Quaternion> m_rotation;

    private Transform m_transform;

    bool m_hasAJob;
    bool m_initialised;

    // Start is called before the first frame update
    void Start()
    {
        m_cameraTransform = Camera.main.transform;
        m_cameraController = m_cameraTransform.GetComponent<CameraController>();
        if (null == m_cameraController) Debug.LogWarning("The main camera does not have the CameraController component. Default upvec of (0, 1, 0) for the camera has been set");
        Init();
        m_transform = transform;
        m_initialised = true;
    }

    void OnEnable()
    {
        if (m_initialised)
            Init();
    }

    void OnDisable()
    {
        Deinit();
    }

    void OnDestroy()
    {
        Deinit();
    }

    public override bool ScheduleJob(out JobHandle handle, float deltaTime, int batchSize = 512)
    {
        Vector3 upVec = null != m_movement ? m_movement.UpvecRotation() : m_defaultUpvec;
        m_rotation = new(1, Allocator.TempJob);
        QuadFollowCameraJob jobStruct = new QuadFollowCameraJob(m_rotation, m_transform.position, m_cameraTransform.position, upVec, m_xFollowFactor);
        handle = jobStruct.Schedule();
        m_hasAJob = true;
        return true;
    }

    public override void OnJobFinished()
    {
        if (m_hasAJob)
        {
            transform.rotation = m_rotation[0];
            m_rotation.Dispose();
        }
    }

    public void SetUpVec(Vector3 upVec)
    {
        m_defaultUpvec = upVec;
    }
}

public struct QuadFollowCameraJob : IJob
{
    NativeArray<Quaternion> m_rotation;
    float3 m_currentPos;
    float3 m_cameraPos;
    float3 m_spriteUpVec;
    float m_xFollowFactor;
    //Note, if hasParent == true, then gravity3 MUST be normalised
    public QuadFollowCameraJob(NativeArray<Quaternion> rotation, float3 currentPos, float3 cameraPos, float3 spriteUpVec, float xFollowFactor)
    {
        m_rotation = rotation;
        m_currentPos = currentPos;
        m_cameraPos = cameraPos;
        m_xFollowFactor = xFollowFactor;
        m_spriteUpVec = spriteUpVec;
    }

    public void Execute()
    {
        float3 dirToCamera = normalize(m_cameraPos - m_currentPos);

        float angle = GfTools.Angle(m_spriteUpVec, dirToCamera);
        float auxAngle = 90f + m_xFollowFactor * (angle - 90f);

        m_rotation[0] = Quaternion.LookRotation(dirToCamera, m_spriteUpVec) * Quaternion.AngleAxis(auxAngle - angle, Vector3.right);
    }
}

