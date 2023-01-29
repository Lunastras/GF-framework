using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public class ParticleHoming : JobChild
{
    [SerializeField]
    protected ParticleSystem m_particleSystem;

    [SerializeField]
    protected Transform m_mainTarget;

    [SerializeField]
    protected float m_acceleration = 5;

    [SerializeField]
    protected float m_deacceleration = 5;

    [SerializeField]
    protected float m_maxSpeed = 50;

    protected NativeArray<ParticleSystem.Particle> m_particleList;

    protected int m_numActiveParticles = 0;

    protected bool m_initialised = false;

    protected bool m_hasAJob = false;

    // Start is called before the first frame update
    void Start()
    {
        if (null == m_particleSystem) m_particleSystem = GetComponent<ParticleSystem>();
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
        m_mainTarget = null;
        Deinit();
    }

    void OnDestroy()
    {
        Deinit();
    }


    public override bool ScheduleJob(out JobHandle handle, float deltaTime, int batchSize = 512)
    {
        m_hasAJob = false;
        m_numActiveParticles = 0;
        if (m_mainTarget) m_numActiveParticles = m_particleSystem.particleCount;

        handle = default;
        if (m_numActiveParticles > 0)
        {
            m_particleList = new(m_numActiveParticles, Allocator.TempJob);
            m_particleSystem.GetParticles(m_particleList, m_numActiveParticles);

            Vector3 targetPos = m_mainTarget.position;

            ParticleHomingJob jobStruct = new ParticleHomingJob(m_particleList, targetPos, deltaTime, m_maxSpeed, m_acceleration, m_deacceleration);
            handle = jobStruct.Schedule(m_numActiveParticles, min(batchSize, m_numActiveParticles));
            m_hasAJob = true;
        }

        return m_hasAJob;
    }

    public override void OnJobFinished()
    {
        if (m_hasAJob)
        {
            m_particleSystem.SetParticles(m_particleList, m_numActiveParticles);
            m_particleList.Dispose();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {

    }

    public Transform GetTarget()
    {
        return m_mainTarget;
    }

    public void SetTarget(Transform target)
    {
        m_mainTarget = target;
    }
}

[BurstCompile]
public struct ParticleHomingJob : IJobParallelFor
{
    NativeArray<ParticleSystem.Particle> m_particleList;
    public float3 m_targetPos;
    public float m_deltaTime;
    public float m_maxSpeed;
    public float m_acceleration;
    public float m_deacceleration;

    public ParticleHomingJob(NativeArray<ParticleSystem.Particle> particleList, float3 targetPos, float deltaTime, float maxSpeed, float acceleration, float deacceleration)
    {
        m_targetPos = targetPos;
        m_particleList = particleList;
        m_deltaTime = deltaTime;
        m_maxSpeed = maxSpeed;
        m_acceleration = acceleration;
        m_deacceleration = deacceleration;
    }

    public void Execute(int i)
    {
        ParticleSystem.Particle particle = m_particleList[i];
        float3 currentPos = particle.position;
        float3 velocity = particle.velocity;

        float3 dirToTarget = m_targetPos - currentPos;
        normalize(dirToTarget);

        float currentSpeed = length(velocity);

        float dotMovementVelDir = 0;
        if (currentSpeed > 0.000001F)
            dotMovementVelDir = dot(dirToTarget, velocity / currentSpeed);

        float speedInDesiredDir = currentSpeed * max(0, dotMovementVelDir);
        float3 unwantedVelocity = velocity - dirToTarget * min(speedInDesiredDir, m_maxSpeed);

        float unwantedSpeed = length(unwantedVelocity);
        if (unwantedSpeed > 0.000001F) unwantedVelocity /= unwantedSpeed;

        float accMagn = min(max(0, m_maxSpeed - speedInDesiredDir), m_deltaTime * m_acceleration);
        float deaccMagn = min(unwantedSpeed, m_deacceleration * m_deltaTime);

        velocity += dirToTarget * accMagn;
        velocity -= unwantedVelocity * deaccMagn;

        particle.velocity = velocity;
        particle.position = currentPos + float3(5.0f * m_deltaTime, 0, 0);
        m_particleList[i] = particle;
    }
}
