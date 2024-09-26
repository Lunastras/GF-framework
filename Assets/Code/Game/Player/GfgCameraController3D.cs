using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgCameraController3D : GfgCameraController
{
    [SerializeField]
    private Vector3 m_targetPositionOffset = Vector3.zero;

    [SerializeField]
    private float m_dstFromtarget = 6;

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

    [SerializeField]
    protected float m_physCheckInterval = 0.2f;

    [SerializeField]
    protected float m_collisionRadius = 1;

    protected PriorityValue<float> m_distanceMultiplier = new(1);

    private Vector3 m_currentTargetPos = default;
    private float m_currentTargetDst = 0;
    private float m_desiredDst = 0;

    private float m_timeUntilPhysCheck = 0;

    private bool m_collidingWithSmth = false;

    private RaycastHit m_raycastHit = default;

    //Internal script variables
    protected Vector3 m_refTargetPosVelocity = default;
    protected float m_refDistanceVel = 0;

    protected float m_yaw = 0;
    protected float m_pitch = 0;

    private static readonly Vector3 UPDIR = Vector3.up;

    private Quaternion m_lastAppliedRot = Quaternion.identity;
    private Quaternion m_previousDesiredRot = Quaternion.identity;

    private Quaternion m_rotVel = Quaternion.identity;

    private float m_currentSmoothTimeDistance;

    public Vector3 Upvec = Vector3.up;


    protected new void Awake()
    {
        base.Awake();
        m_currentTargetDst = m_dstFromtarget;
        m_desiredDst = m_dstFromtarget;
    }

    public override void SnapToTarget()
    {
        UpdateRotation(0, true);
        m_currentTargetPos = m_target.position;
        m_transform.position = m_currentTargetPos - m_transform.forward * m_currentTargetDst;
    }

    public override void RevertToDefault() { LookForward(); }

    //Look towards the target's forward
    public void LookForward(float pitchDegrees = 10, float yawOffset = 0)
    {
        UpdateRotation(0, false, false);
        Vector3 cameraForward = m_previousDesiredRot * Vector3.forward;
        GfcTools.RemoveAxis(ref cameraForward, Upvec);
        GfcTools.Normalize(ref cameraForward);

        Vector3 targetForward = m_target.forward;
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

    public override void Move(float deltaTime)
    {
        Quaternion upvecCorrection = Quaternion.FromToRotation(UPDIR, Upvec);

        Vector3 desiredTargetPos = m_target.position + upvecCorrection * m_targetPositionOffset;
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
            GfcTools.Add(ref desiredTargetPos, forward * -minDistanceOffset);

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
}
