using System.Collections;
using System.Collections.Generic;
using Unity;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;
using static Unity.Mathematics.math;
using UnityEngine.Rendering;

public class ParticleInterCollision : JobChild
{
    [SerializeField]
    protected ParticleSystem m_particleSystem;

    [SerializeField]
    protected bool m_callbackOnFirstCollision = false;

    [SerializeField]
    protected bool m_collidesWithOwnParticles = false;

    [SerializeField]
    protected float m_minimumParticleDistanceSq = 25;

    [SerializeField]
    protected uint m_priority = 0;

    [SerializeField]
    protected ParticleCollisionEventType m_selfEvent = ParticleCollisionEventType.NONE;

    [SerializeField]
    protected ParticleCollisionEventType m_otherEvent = ParticleCollisionEventType.NONE;

    protected bool m_initialised = false;

    protected bool m_hasAJob = false;

    protected NativeArray<ParticleSystem.Particle> m_particlesList;

    protected NativeArray<ParticleCollisionCallbackData> m_particleCallbacks;
    protected static NativeList<ParticleSystem.Particle> AllParticles;
    protected static NativeArray<ParticleCollisionSystemData> AllParticleSystemsData;

    protected static int CountSystemsAdded = 0;

    protected int m_selfSystemIndex = -1;

    protected float m_scheduleIntervalVariance = 0.1f;

    protected float m_timeUntilScedule;

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

    public override void OnOperationStart(float deltaTime, UpdateTypes updateType)
    {
        if (JobParent.CanSchedule(JobScheduleTypes.PARTICLE_COLLISION))
        {
            if (!AllParticles.IsCreated)
            {
                CountSystemsAdded = 0;
                int countSystems = JobParent.GetJobChildren(GetType(), updateType).Count;
                AllParticles = new(32 * countSystems, Allocator.TempJob);
                AllParticleSystemsData = new(countSystems, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            }

            int particleCount = m_particleSystem.particleCount;
            m_particlesList = new(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_particleSystem.GetParticles(m_particlesList, particleCount);

            AllParticles.AddRange(m_particlesList);

            m_selfSystemIndex = CountSystemsAdded;
            float radius = m_particleSystem.main.startSize.constant * m_particleSystem.collision.radiusScale;
            AllParticleSystemsData[CountSystemsAdded] = new(gameObject.layer, particleCount, m_priority, radius, m_selfEvent, m_otherEvent);
            CountSystemsAdded++;
        }
    }

    public override bool GetJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512)
    {
        m_hasAJob = false;
        handle = default;

        if (JobParent.CanSchedule(JobScheduleTypes.PARTICLE_COLLISION) && 0 < m_particleSystem.particleCount)
        {
            OnJobSchedule?.Invoke();
            int particleCount = m_particleSystem.particleCount;

            if (m_callbackOnFirstCollision)
                m_particleCallbacks = new(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            else
                m_particleCallbacks = new(0, Allocator.TempJob);

            ParticleInterCollisionJob jobStruct = new ParticleInterCollisionJob(m_selfSystemIndex, 1
            , m_callbackOnFirstCollision, m_collidesWithOwnParticles, m_minimumParticleDistanceSq, GfPhysics.GetLayerMask(gameObject.layer), m_particlesList
            , AllParticles.AsArray().AsReadOnly(), AllParticleSystemsData.AsReadOnly()
            , m_particleCallbacks);

            handle = jobStruct.Schedule(particleCount, min(64, particleCount));

            m_hasAJob = true;
        }

        return m_hasAJob;
    }

    public void SetParticle(ParticleSystem.Particle particle, int index)
    {
        m_particlesList[index] = particle;
    }

    public ParticleSystem.Particle GetParticle(int index)
    {
        return m_particlesList[index];
    }

    public override void OnJobFinished(float deltaTime, UpdateTypes updateType)
    {
        if (m_particleCallbacks.IsCreated && m_particleCallbacks.Length > 0)
        {
            var brotherSystems = JobParent.GetJobChildren(GetType(), updateType);
            ParticleSystem.Particle selfParticle;
            ParticleSystem.Particle otherParticle;

            for (int i = 0; i < m_particleCallbacks.Length; ++i)
            {
                if (0 <= m_particleCallbacks[i].HitParticleIndex)
                {
                    var otherSystem = brotherSystems[m_particleCallbacks[i].HitParticleSystemIndex] as ParticleInterCollision;
                    selfParticle = m_particlesList[i];
                    otherParticle = otherSystem.GetParticle(m_particleCallbacks[i].HitParticleIndex);

                    OnParticleCallback(ref selfParticle, ref otherParticle, otherSystem);

                    m_particlesList[i] = selfParticle;
                    otherSystem.SetParticle(otherParticle, m_particleCallbacks[i].HitParticleIndex);
                }
            }
        }
    }

    public override void OnOperationFinished(float deltaTime, UpdateTypes updateType)
    {
        if (AllParticles.IsCreated)
        {
            AllParticles.Dispose();
            AllParticleSystemsData.Dispose();
        }

        if (m_particleCallbacks.IsCreated)
            m_particleCallbacks.Dispose();

        if (m_hasAJob)
            m_particleSystem.SetParticles(m_particlesList);

        if (m_particlesList.IsCreated)
            m_particlesList.Dispose();
    }

    protected virtual void OnParticleCallback(ref ParticleSystem.Particle selfParticle, ref ParticleSystem.Particle otherParticle, ParticleInterCollision otherSystem)
    {
        selfParticle.startColor = new Color32(255, 0, 0, 255);
        otherParticle.startColor = new Color32(0, 255, 0, 255);
    }

    public void SetPriority(uint priority) { m_priority = priority; }

    public uint GetPriority() { return m_priority; }

    public void SetSelfCollisionEvent(ParticleCollisionEventType selfEvent) { m_selfEvent = selfEvent; }

    public ParticleCollisionEventType GetSelfCollisionEvent() { return m_selfEvent; }

    public void SetOtherCollisionEvent(ParticleCollisionEventType otherEvent) { m_otherEvent = otherEvent; }

    public ParticleCollisionEventType GetOtherCollisionEvent() { return m_otherEvent; }

    public ParticleSystem GetParticleSystem() { return m_particleSystem; }
}


[BurstCompile]
internal struct ParticleInterCollisionJob : IJobParallelFor
{
    float m_minimumParticleDistanceSq;
    float m_bounceCoef;

    int m_ownIndex;

    bool m_collidesWithOwnParticles;

    int m_layerMask;

    bool m_callbackOnFirstCollision;

    NativeArray<ParticleSystem.Particle> m_particleList;

    NativeArray<ParticleSystem.Particle>.ReadOnly m_allParticles;
    NativeArray<ParticleCollisionSystemData>.ReadOnly m_allParticleSystems;

    NativeArray<ParticleCollisionCallbackData> m_particleCallbacks;

    public ParticleInterCollisionJob(int ownIndex, float bounceCoef, bool callbackOnFirstCollision, bool collidesWithOwnParticles, float minimumParticleDistanceSq, int layerMask, NativeArray<ParticleSystem.Particle> particleList
    , NativeArray<ParticleSystem.Particle>.ReadOnly allParticles
    , NativeArray<ParticleCollisionSystemData>.ReadOnly allParticleSystemsListsLength
    , NativeArray<ParticleCollisionCallbackData> particleCallbacks)
    {
        m_ownIndex = ownIndex;
        m_particleList = particleList;
        m_allParticles = allParticles;
        m_allParticleSystems = allParticleSystemsListsLength;
        m_collidesWithOwnParticles = collidesWithOwnParticles;
        m_bounceCoef = bounceCoef;
        m_minimumParticleDistanceSq = minimumParticleDistanceSq;
        m_particleCallbacks = particleCallbacks;
        m_layerMask = layerMask;
        m_callbackOnFirstCollision = callbackOnFirstCollision;
    }

    public void Execute(int i)
    {
        int otherLayer;
        int ownLayer = m_allParticleSystems[m_ownIndex].Layer;

        ParticleSystem.Particle otherParticle;
        ParticleSystem.Particle selfParticle = m_particleList[i];

        int j;
        ParticleCollisionCallbackData callbackData = new();
        callbackData.HitParticleIndex = -1;
        callbackData.HitParticleSystemIndex = -1;

        float3 normal;
        float distanceBetweenCenters;
        float distanceBetweenCentersSq;
        float distance;
        float3 vecToOther;

        uint otherPriority;
        uint selfPriority = m_allParticleSystems[m_ownIndex].Priority;

        ParticleCollisionEventType otherEvent;
        ParticleCollisionEventType selfEvent = m_allParticleSystems[m_ownIndex].SelfEvent;

        float3 selfVelocity;
        float3 otherVelocity;

        float otherRadius;
        float selfRadius = m_allParticleSystems[m_ownIndex].Radius;

        float3 otherPosition;
        float3 selfPosition = selfParticle.position;

        bool ignoreOtherEvent;
        int systemParticleCount;
        int totalParticlesFromPreviousSystem = 0;
        int systemsCount = m_allParticleSystems.Length;
        bool destroyedParticle = false;
        bool hasEvent = false;

        for (int currentSystem = 0; currentSystem < systemsCount && !destroyedParticle; ++currentSystem)
        {
            otherLayer = m_allParticleSystems[currentSystem].Layer;
            otherEvent = m_allParticleSystems[currentSystem].OtherEvent;
            otherPriority = m_allParticleSystems[currentSystem].Priority;
            ignoreOtherEvent = selfPriority > otherPriority;
            systemParticleCount = m_allParticleSystems[currentSystem].CountParticles;
            hasEvent = selfEvent != ParticleCollisionEventType.NONE || (!ignoreOtherEvent && otherEvent != ParticleCollisionEventType.NONE);

            if (GfPhysics.LayerIsInMask(otherLayer, m_layerMask) && (currentSystem != m_ownIndex || m_collidesWithOwnParticles)
            && (m_callbackOnFirstCollision || hasEvent))
            {
                otherRadius = m_allParticleSystems[currentSystem].Radius;
                //do not calculate collision with the same particle
                for (j = 0; j < systemParticleCount && !destroyedParticle; ++j)
                {
                    if (currentSystem != m_ownIndex || j != i)
                    {
                        otherParticle = m_allParticles[j + totalParticlesFromPreviousSystem];

                        otherPosition = otherParticle.position;
                        vecToOther = selfPosition - otherPosition;
                        distanceBetweenCentersSq = lengthsq(vecToOther);

                        if (m_minimumParticleDistanceSq >= distanceBetweenCentersSq)
                        {
                            distanceBetweenCenters = sqrt(distanceBetweenCentersSq);
                            distance = distanceBetweenCenters - (otherRadius + selfRadius);

                            if (distance <= 0) //collided
                            {
                                if (m_callbackOnFirstCollision)
                                {
                                    destroyedParticle = !hasEvent;
                                    callbackData.HitParticleIndex = j;
                                    callbackData.HitParticleSystemIndex = currentSystem;
                                }

                                switch (selfEvent)
                                {
                                    case (ParticleCollisionEventType.BOUNCE):
                                        if (distanceBetweenCenters >= 0.01f)
                                        {
                                            selfVelocity = selfParticle.velocity;
                                            otherVelocity = otherParticle.velocity;

                                            normal = vecToOther / distanceBetweenCenters;

                                            selfVelocity = m_bounceCoef * reflect(selfVelocity, normal);
                                            selfParticle.velocity = selfVelocity;
                                        }

                                        break;

                                    case (ParticleCollisionEventType.DESTROY):
                                        destroyedParticle = true;
                                        selfParticle.remainingLifetime = 0;
                                        break;
                                }


                                if ((int)selfEvent != (int)otherEvent)
                                {
                                    switch (otherEvent)
                                    {
                                        case (ParticleCollisionEventType.BOUNCE):
                                            if (distanceBetweenCenters >= 0.01f)
                                            {
                                                selfVelocity = selfParticle.velocity;
                                                otherVelocity = otherParticle.velocity;

                                                normal = vecToOther / distanceBetweenCenters;

                                                selfVelocity = m_bounceCoef * reflect(selfVelocity, normal);
                                                selfParticle.velocity = selfVelocity;
                                            }

                                            break;

                                        case (ParticleCollisionEventType.DESTROY):
                                            destroyedParticle = true;
                                            selfParticle.remainingLifetime = 0;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            totalParticlesFromPreviousSystem += systemParticleCount;
        }

        if (m_callbackOnFirstCollision)
            m_particleCallbacks[i] = callbackData;

        m_particleList[i] = selfParticle;
    }
}

public enum ParticleCollisionEventType
{
    NONE,
    BOUNCE,
    DESTROY,
}

public struct ParticleCollisionCallbackData
{
    public int HitParticleIndex;

    public int HitParticleSystemIndex;
}

public struct ParticleCollisionSystemData
{
    public uint Priority;
    public int Layer;
    public int CountParticles;

    public float Radius;

    public ParticleCollisionEventType SelfEvent;

    public ParticleCollisionEventType OtherEvent;

    public ParticleCollisionSystemData(int layer, int countParticles, uint priority, float radius, ParticleCollisionEventType selfEvent, ParticleCollisionEventType otherEvent)
    {
        CountParticles = countParticles;
        Layer = layer;
        Priority = priority;
        SelfEvent = selfEvent;
        OtherEvent = otherEvent;
        Radius = radius;
    }
}


