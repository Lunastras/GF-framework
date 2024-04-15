using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgItemMovement : MonoBehaviour
{
    public GfMovementGeneric Movement;

    public float RotationSpeedDeg = 20f;

    public float BopSpeedDeg = 20f;

    public float BopMagnitude = 0.5f;

    public float RotationSpeedCoef = 1;

    public float BopSpeedCoef = 1;

    public float SpeedCoef = 1;

    private float m_currentRotationDeg = 0;
    private float m_bopValueDeg = 0;

    private Transform m_transform;

    private Transform m_movementTransform;

    // Start is called before the first frame update
    void Start()
    {
        if (Movement == null) Movement = GetComponent<GfMovementGeneric>();
        m_currentRotationDeg = UnityEngine.Random.Range(0, 360);
        m_transform = transform;

        m_movementTransform = Movement.transform;
    }

    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.deltaTime;
        Vector3 upVec = Movement.GetUpvecRotation();
        m_currentRotationDeg += SpeedCoef * RotationSpeedCoef * RotationSpeedDeg * deltaTime;
        m_bopValueDeg += SpeedCoef * BopSpeedCoef * BopSpeedDeg * deltaTime;

        m_currentRotationDeg = Mathf.Repeat(m_currentRotationDeg, 360);
        m_bopValueDeg = Mathf.Repeat(m_bopValueDeg, 360);

        Quaternion rotation = Quaternion.AngleAxis(m_currentRotationDeg, upVec);
        GfcTools.Mult3(ref upVec, System.MathF.Sin(m_bopValueDeg * Mathf.Deg2Rad) * BopMagnitude);
        GfcTools.Add3(ref upVec, m_movementTransform.position);

        m_transform.position = upVec;
        m_transform.rotation = rotation;
    }
}
