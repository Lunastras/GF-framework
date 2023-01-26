using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;


using static Unity.Mathematics.math;

public class SimpleGravity : MonoBehaviour
{
    [SerializeField]
    protected bool m_useJobs = true;
    [SerializeField]
    protected float m_drag = 5;

    [SerializeField]
    protected float m_terminalVelocity = 50;

    [SerializeField]
    protected Transform m_sphericalParent;

    [SerializeField]
    protected Vector3 m_defaultGravityDir = DOWNDIR;

    [SerializeField]
    protected float m_gravityCoef = 1;

    [SerializeField]
    protected QuadFollowCamera m_quadFollowCamera;

    protected Rigidbody m_rb;

    private static readonly Vector3 DOWNDIR = Vector3.down;

    private Transform m_transform;

    private float m_timeSinceLastCollision = 0;

    void Awake()
    {
        m_transform = GetComponent<Transform>();
        m_rb = GetComponent<Rigidbody>();
        SetDefaultGravityDir(m_defaultGravityDir);
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    private void FixedUpdate()
    {
        Vector3 gravity3 = m_defaultGravityDir;
        if (m_sphericalParent) {
            gravity3 = (m_sphericalParent.position - transform.position);
            float mag = gravity3.magnitude;
            if (mag > 0.000001F) gravity3 /= mag;
        }

        GfTools.Mult3(ref gravity3, m_rb.mass * Time.deltaTime);

        m_rb.AddForce(gravity3, ForceMode.VelocityChange); 

        if (m_quadFollowCamera)
            m_quadFollowCamera.SetUpVec(-gravity3);

        if (m_sphericalParent)
        {
            Quaternion correction = Quaternion.FromToRotation(m_transform.up, gravity3);
            m_transform.rotation = correction * m_transform.rotation;
        }
    }

    public Transform GetSphericalParent()
    {
        return m_sphericalParent;
    }

    public void SetSphericalParent(Transform parent)
    {
        m_sphericalParent = parent;
    }

    public Vector3 GetDefaultGravityDir() { return m_defaultGravityDir; }
    public void SetDefaultGravityDir(Vector3 upVec)
    {
        m_defaultGravityDir = upVec.normalized;
        if (m_defaultGravityDir.sqrMagnitude < 0.000001f)
            m_defaultGravityDir = DOWNDIR;

        Quaternion correction = Quaternion.FromToRotation(m_transform.up, upVec);
        m_transform.rotation = correction * m_transform.rotation;
    }

    public void SetGravityCoef(float coef)
    {
        m_gravityCoef = coef;
    }

    public float GetGravityCoef()
    {
        return m_gravityCoef;
    }

    private void OnCollisionEnter(Collision other) {
        
    }
}
