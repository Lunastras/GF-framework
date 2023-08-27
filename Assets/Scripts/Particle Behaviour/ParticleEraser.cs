using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;
using static Unity.Mathematics.math;

public class ParticleEraser : JobChild
{
    [SerializeField]
    protected ParticleSystem m_particleSystem;

    protected bool m_initialised = false;

    protected bool m_hasAJob = false;

    protected float3 m_eraseCenter;

    protected float m_eraseRadius;

    protected float m_speedFromCenter;

    protected bool m_eraseParticles = false;

    protected NativeArray<ParticleSystem.Particle> m_particlesList;

    protected void Awake()
    {
        if (null == m_particleSystem) m_particleSystem = GetComponent<ParticleSystem>();
    }

    protected void Start()
    {
        InitJobChild();
        m_initialised = true;
        var customData = m_particleSystem.customData;
        customData.enabled = true;
    }

    protected void OnEnable()
    {
        m_eraseParticles = false;
        if (m_initialised)
            InitJobChild();
    }

    protected void OnDisable()
    {
        DeinitJobChild();
    }

    protected void OnDestroy()
    {
        if (m_particlesList.IsCreated) m_particlesList.Dispose();
    }

    public static bool CreateErasingJobHandle(ParticleSystem particleSystem, float3 eraseCenter, float speedFromCenter, float eraseRadius, out NativeArray<ParticleSystem.Particle> particlesList, out JobHandle handle)
    {
        bool hasJob = false;
        handle = default;
        particlesList = default;
        int particleCount = particleSystem.particleCount;
        if (particleCount > 0)
        {
            hasJob = true;
            particlesList = new(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            particleSystem.GetParticles(particlesList, particleCount);
            ParticleEraserJob jobStruct = new ParticleEraserJob(particlesList, eraseCenter, speedFromCenter, eraseRadius);
            handle = jobStruct.Schedule(particleCount, min(16, particleCount));
        }

        return hasJob;
    }

    public static void EraseParticles(ParticleSystem particleSystem, float3 eraseCenter, float speedFromCenter, float eraseRadius)
    {
        CreateErasingJobHandle(particleSystem, eraseCenter, speedFromCenter, eraseRadius, out NativeArray<ParticleSystem.Particle> particlesList, out JobHandle handle);
        handle.Complete();
        particlesList.Dispose();
    }

    public override bool GetJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512)
    {
        m_hasAJob = false;
        handle = default;

        if (m_eraseParticles && JobParent.CanSchedule(JobScheduleTypes.PARTICLE_ERASE) && 0 < m_particleSystem.particleCount)
        {
            int particleCount = m_particleSystem.particleCount;
            OnJobSchedule?.Invoke();

            CreateErasingJobHandle(m_particleSystem, m_eraseCenter, m_speedFromCenter, m_eraseRadius, out m_particlesList, out handle);

            m_hasAJob = true;
        }

        return m_hasAJob;
    }

    public override void OnJobFinished(float deltaTime, UpdateTypes updateType)
    {
        if (m_hasAJob)
        {
            m_eraseParticles = false;
            m_particleSystem.SetParticles(m_particlesList);
            m_particlesList.Dispose();
        }
    }

    public virtual void EraseParticles(float3 eraseCenter, float speedFromCenter, float eraseRadius)
    {
        m_eraseCenter = eraseCenter;
        m_speedFromCenter = speedFromCenter;
        m_eraseRadius = eraseRadius;
        m_eraseParticles = true;
    }
}


[BurstCompile]
internal struct ParticleEraserJob : IJobParallelFor
{
    NativeArray<ParticleSystem.Particle> m_particleList;
    float3 m_center;
    float m_speed;

    float m_radius;

    public ParticleEraserJob(NativeArray<ParticleSystem.Particle> particleList, float3 center, float speed, float radius)
    {
        m_particleList = particleList;
        m_center = center;
        m_speed = speed;
        m_radius = radius;
    }

    public void Execute(int i)
    {
        ParticleSystem.Particle particle = m_particleList[i];

        float distanceFromCenter = length(m_center - (float3)particle.position);

        if (distanceFromCenter <= m_radius)
        {
            particle.remainingLifetime = min(particle.remainingLifetime, distanceFromCenter / m_speed);
        }

        m_particleList[i] = particle;
    }
}

