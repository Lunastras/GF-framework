﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;

using static Unity.Mathematics.math;

public class GfrQuadFollowCamera : MonoBehaviour
{
    public float m_xFollowFactor;

    public Transform m_transformCharacter = null;

    public Vector3 m_defaultUpvec = Vector3.up;

    private Transform m_transform;

    private static readonly Vector3 RIGHT3 = Vector3.right;

    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
    }

    private void LateUpdate()
    {
        Transform cameraTransform = GfcManagerGame.Camera.transform;
        if (cameraTransform)
        {
            Vector3 upVec = m_defaultUpvec;
            if (m_transformCharacter) upVec = m_transformCharacter.up;

            Vector3 dirFromCamera = m_transform.position;
            GfcTools.Minus3(ref dirFromCamera, cameraTransform.position);
            GfcTools.Normalize(ref dirFromCamera);

            float angle = GfcTools.AngleDegNorm(upVec, dirFromCamera);
            float auxAngle = 90f + m_xFollowFactor * (angle - 90f);

            m_transform.rotation = Quaternion.LookRotation(dirFromCamera, upVec) * Quaternion.AngleAxis(auxAngle - angle, RIGHT3);
        }
    }

    public void SetUpVec(Vector3 upVec)
    {
        m_defaultUpvec = upVec;
    }
}


