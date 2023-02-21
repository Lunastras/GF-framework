using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;

using static Unity.Mathematics.math;

public class QuadFollowCamera : MonoBehaviour
{
    [SerializeField]
    public float m_xFollowFactor;

    [SerializeField]
    protected Transform m_transformCharacter; //couldn't come up with a better name, doo doo fart

    [SerializeField]
    protected Vector3 m_defaultUpvec = Vector3.up;

    private static Transform m_cameraTransform;

    private Transform m_transform;

    private static readonly Vector3 RIGHT3 = Vector3.right;

    bool m_hasAJob;
    bool m_initialised;

    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;

        if (null == m_transformCharacter)
            m_transformCharacter = m_transform;
        m_cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        Vector3 upVec = m_transformCharacter.up;

        Vector3 dirToCamera = m_cameraTransform.position;
        GfTools.Minus3(ref dirToCamera, m_transform.position);
        GfTools.Normalize(ref dirToCamera);

        float angle = GfTools.AngleDeg(upVec, dirToCamera);
        float auxAngle = 90f + m_xFollowFactor * (angle - 90f);

        transform.rotation = Quaternion.LookRotation(dirToCamera, upVec) * Quaternion.AngleAxis(auxAngle - angle, RIGHT3);
    }

    public void SetUpVec(Vector3 upVec)
    {
        m_defaultUpvec = upVec;
    }
}


