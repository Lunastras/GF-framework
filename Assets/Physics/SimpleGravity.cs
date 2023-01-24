using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;


using static Unity.Mathematics.math;

public class SimpleGravity : JobChild
{
    [SerializeField]
    protected bool m_useJobs = true;
    [SerializeField]
    protected float m_drag = 5;

    [SerializeField]
    protected float m_terminalVelocity = 50;

    [SerializeField]
    protected Transform m_sphericalParent;
    m_hasAJob
        [SerializeField]
    protected Vector3 m_defaultGravityDir = DOWNDIR;

    [SerializeField]
    protected float m_gravityCoef = 1;

    [SerializeField]
    protected QuadFollowCamera m_quadFollowCamera;

    protected Rigidbody m_rb;

    protected SimpleGravityJob m_jobStruct;

    protected NativeArray<Vector3> m_velocity;
    protected NativeArray<Vector3> m_gravity3;

    private static readonly Vector3 DOWNDIR = Vector3.down;

    private Transform m_transform;



    protected bool m_hasAJob = false;
    protected bool m_initialised = false;

    void Awake()
    {
        m_transform = GetComponent<Transform>();
        m_rb = GetComponent<Rigidbody>();
        SetDefaultGravityDir(m_defaultGravityDir);
    }
    // Start is called before the first frame update
    void Start()
    {
        Init();
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
        m_hasAJob = false;
        handle = default;
        if (m_useJobs)
        {
            m_hasAJob = true;

            Vector3 gravity3 = m_defaultGravityDir;
            if (m_sphericalParent)
                gravity3 = m_sphericalParent.position;


            m_velocity = new(1, Allocator.TempJob);
            m_gravity3 = new(1, Allocator.TempJob);

            m_velocity[0] = m_rb.velocity;
            m_gravity3[0] = gravity3;
            m_jobStruct = new SimpleGravityJob(m_velocity, transform.position, m_gravity3, deltaTime, m_terminalVelocity, m_rb.mass, m_drag, null != m_sphericalParent, m_gravityCoef);
            handle = m_jobStruct.Schedule();
        }
        else
        {

        }


        return m_hasAJob;
    }

    private void FixedUpdate()
    {
        if (!m_useJobs)
        {

        }
    }

    public override void OnJobFinished()
    {
        if (m_hasAJob)
        {

            m_rb.velocity = m_velocity[0];

            if (m_quadFollowCamera)
                m_quadFollowCamera.SetUpVec(-m_gravity3[0]);

            if (m_sphericalParent)
            {
                Quaternion correction = Quaternion.FromToRotation(m_transform.up, m_gravity3[0]);
                m_transform.rotation = correction * m_transform.rotation;
            }

            m_velocity.Dispose();
            m_gravity3.Dispose();
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
}

public struct SimpleGravityJob : IJob
{
    NativeArray<Vector3> m_velocity;
    float3 m_currentPos;
    NativeArray<Vector3> m_gravity3;
    bool m_hasParent;
    float m_deltaTime;
    float m_maxSpeed;
    float m_acceleration;
    float m_deacceleration;
    float m_gravityMultiplier;

    //Note, if hasParent == true, then gravity3 MUST be normalised
    public SimpleGravityJob(NativeArray<Vector3> velocity, float3 currentPos, NativeArray<Vector3> gravity3, float deltaTime, float maxSpeed, float acceleration, float deacceleration, bool hasParent, float gravityMultiplier)
    {
        m_velocity = velocity;
        m_currentPos = currentPos;
        m_gravity3 = gravity3;
        m_deltaTime = deltaTime;
        m_maxSpeed = maxSpeed;
        m_acceleration = acceleration;
        m_hasParent = hasParent;
        m_gravityMultiplier = gravityMultiplier;
        m_deacceleration = deacceleration;
    }

    public void Execute()
    {
        float3 velocity = m_velocity[0];
        float3 gravity3 = m_gravity3[0];

        if (m_hasParent) //if the m_hasParent is true, then m_gravity3 is the gravitational center of the particle, otherwise it is the direction of the gravity
        {
            gravity3 = (gravity3 - m_currentPos);
            float mag = length(gravity3);
            if (mag > 0.000001F) gravity3 /= mag;
        }

        gravity3 *= sign(m_gravityMultiplier);

        float currentSpeed = length(velocity);

        float dotMovementVelDir = 0;
        if (currentSpeed > 0.000001F)
            dotMovementVelDir = dot(gravity3, velocity / currentSpeed);

        float speedInDesiredDir = currentSpeed * max(0, dotMovementVelDir);

        float accMagn = min(max(0, m_maxSpeed - speedInDesiredDir), m_deltaTime * m_acceleration * abs(m_gravityMultiplier));
        velocity += gravity3 * accMagn;

        m_velocity[0] = velocity;
        m_gravity3[0] = gravity3;
    }
}
