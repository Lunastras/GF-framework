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
    public float m_xFollowFactor;

    public Transform m_transformCharacter = null;

    public Vector3 m_defaultUpvec = Vector3.up;

    private static Transform m_cameraTransform;

    private Transform m_transform;

    private static readonly Vector3 RIGHT3 = Vector3.right;

    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
        Camera mainCamera = Camera.main;
        if (mainCamera) m_cameraTransform = mainCamera.transform;
    }


    private void LateUpdate()
    {
        if (m_cameraTransform)
        {
            Vector3 upVec = m_defaultUpvec;
            if (m_transformCharacter) upVec = m_transformCharacter.up;

            Vector3 dirFromCamera = m_transform.position;
            GfTools.Minus3(ref dirFromCamera, m_cameraTransform.position);
            GfTools.Normalize(ref dirFromCamera);

            float angle = GfTools.AngleDeg(upVec, dirFromCamera);
            float auxAngle = 90f + m_xFollowFactor * (angle - 90f);

            transform.rotation = Quaternion.LookRotation(dirFromCamera, upVec) * Quaternion.AngleAxis(auxAngle - angle, RIGHT3);
        }
    }

    public void SetUpVec(Vector3 upVec)
    {
        m_defaultUpvec = upVec;
    }
}


