using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class QuadSpriteGraphics : MonoBehaviour
{
    [SerializeField]
    protected QuadSpriteGraphicsValues quadSpriteValues;

    private static Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame

    private static readonly float THRESHOLD_CALCULATION = 0.99f;

    
    void Update()
    {
        if (!quadSpriteValues.active)
            return;

        Vector3 vecToTarget = (mainCamera.transform.position - transform.position).normalized;

        float xFollowFactor = quadSpriteValues.xFollowFactor;

        if (xFollowFactor < THRESHOLD_CALCULATION)
        {
            float yValue = vecToTarget.y * xFollowFactor;
            vecToTarget.y = 0;
            vecToTarget = vecToTarget.normalized * (float)System.Math.Sqrt(1.0f - yValue);
            vecToTarget.y = yValue;
        }

        transform.rotation = Quaternion.LookRotation(vecToTarget);
    }

    public void SetquadSpriteValues(QuadSpriteGraphicsValues quadSpriteValues)
    {
        this.quadSpriteValues = quadSpriteValues;
    }

    public QuadSpriteGraphicsValues GetquadSpriteValues()
    {
        return quadSpriteValues;
    }
}


[System.Serializable]
public struct QuadSpriteGraphicsValues
{
    public QuadSpriteGraphicsValues(float xFollowFactor = 1.0f, bool active = true)
    {
        this.xFollowFactor = xFollowFactor;
        this.active = active;
    }

    public float xFollowFactor;
    public bool active;
}
