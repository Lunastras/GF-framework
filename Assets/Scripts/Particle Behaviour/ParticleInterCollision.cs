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
using Unity.IO.LowLevel.Unsafe;

public class ParticleInterCollision : JobChild
{
    [SerializeField]
    protected ParticleSystem m_particleSystem;

    [SerializeField]
    protected bool m_dynamicRadius = false;

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

    protected static NativeList<ParticleSystem.Particle> AllParticles;
    protected static NativeArray<ParticleCollisionSystemData> AllParticleSystems;

    protected static NativeArray<ParticleCollisionCallbackData> ParticleCallbacks;

    protected static bool TracksCallbacks = false;

    protected static int CountSystemsAdded = 0;

    protected int m_selfSystemIndex = -1;

    protected float m_scheduleIntervalVariance = 0.1f;

    protected float m_timeUntilScedule;

    protected int m_particleStartIndex;

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
                TracksCallbacks = false;
                CountSystemsAdded = 0;
                int countSystems = JobParent.GetJobChildren(GetType(), updateType).Count;
                AllParticles = new(32 * countSystems, Allocator.TempJob);
                AllParticleSystems = new(countSystems, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            }

            int particleCount = m_particleSystem.particleCount;
            m_particlesList = new(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_particleSystem.GetParticles(m_particlesList, particleCount);

            if (m_dynamicRadius)
            {
                ParticleSystem.Particle particle;
                for (int i = 0; i < particleCount; ++i)
                {
                    particle = m_particlesList[i];
                    particle.startSize = particle.GetCurrentSize(m_particleSystem);
                    m_particlesList[i] = particle;
                }
            }

            m_particleStartIndex = AllParticles.Length;
            AllParticles.AddRange(m_particlesList);

            m_selfSystemIndex = CountSystemsAdded;
            float radius = m_particleSystem.main.startSize.constant * m_particleSystem.collision.radiusScale;

            AllParticleSystems[CountSystemsAdded] = new ParticleCollisionSystemData()
            {
                Priority = m_priority,
                CollidesWithOwnParticles = m_collidesWithOwnParticles,
                CountParticles = particleCount,
                BounceCoef = 1,
                Layer = gameObject.layer,
                LayerMask = GfPhysics.GetLayerMask(gameObject.layer),
                MinimumParticleDistanceSq = m_minimumParticleDistanceSq,
                CallbackOnFirstCollision = m_callbackOnFirstCollision,
                Radius = m_particleSystem.collision.radiusScale,
                SelfEvent = m_selfEvent,
                OtherEvent = m_otherEvent,
                ParticleStartIndex = m_particleStartIndex

            };

            TracksCallbacks |= m_callbackOnFirstCollision;
            CountSystemsAdded++;
        }
    }

    public override bool GetJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512)
    {
        m_hasAJob = false;
        handle = default;

        if (AllParticles.IsCreated)
        {
            int countAllParticles = AllParticles.Length;
            NativeArray<ParticleSystem.Particle> allParticlesReadonly = new(countAllParticles, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            if (TracksCallbacks)
                ParticleCallbacks = new(countAllParticles, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            else
                ParticleCallbacks = new(0, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            allParticlesReadonly.CopyFrom(AllParticles);

            ParticleInterCollisionJob job = new()
            {
                AllParticles = AllParticles,
                AllParticlesReadonly = allParticlesReadonly.AsReadOnly(),
                ParticleCallbacks = ParticleCallbacks,
                AllParticleSystems = AllParticleSystems.AsReadOnly(),
                TracksCallbacks = TracksCallbacks
            };

            ParticleSystemBoundsJob boundsJob = new()
            {
                AllParticlesReadonly = allParticlesReadonly.AsReadOnly(),
                AllParticleSystems = AllParticleSystems
            };

            var brotherSystems = JobParent.GetJobChildren(GetType(), updateType);

            boundsJob.Schedule(AllParticleSystems.Length, 32).Complete();
            job.Schedule(countAllParticles, 16).Complete();


            ParticleSystem.Particle selfParticle;
            ParticleSystem.Particle otherParticle;
            int totalParticlesFromPreviousSystem = 0;
            int systemParticleCount;
            int lastIndex;

            int systemsCount = AllParticleSystems.Length;
            for (int currentSystem = 0; currentSystem < systemsCount; ++currentSystem)
            {
                systemParticleCount = AllParticleSystems[currentSystem].CountParticles;

                if (AllParticleSystems[currentSystem].CallbackOnFirstCollision)
                {
                    var selfSystem = brotherSystems[currentSystem] as ParticleInterCollision;
                    lastIndex = systemParticleCount + AllParticleSystems[currentSystem].ParticleStartIndex;

                    for (int i = AllParticleSystems[currentSystem].ParticleStartIndex; i < lastIndex; ++i)
                    {
                        if (0 <= ParticleCallbacks[i].HitParticleIndex)
                        {
                            selfParticle = AllParticles[i];
                            otherParticle = AllParticles[ParticleCallbacks[i].HitParticleIndex];

                            var otherSystem = brotherSystems[ParticleCallbacks[i].HitParticleSystemIndex] as ParticleInterCollision;
                            selfSystem.OnParticleCallback(ref selfParticle, ref otherParticle, otherSystem);

                            AllParticles[i] = selfParticle;
                            AllParticles[ParticleCallbacks[i].HitParticleIndex] = otherParticle;
                        }
                    }
                }

                totalParticlesFromPreviousSystem += systemParticleCount;
            }

            for (int i = 0; i < brotherSystems.Count; ++i)
            {
                (brotherSystems[i] as ParticleInterCollision).RetrieveBackParticles();
            }

            allParticlesReadonly.Dispose();
            AllParticleSystems.Dispose();
            ParticleCallbacks.Dispose();
            AllParticles.Dispose();
        }

        return m_hasAJob;
    }


    public override void OnJobFinished(float deltaTime, UpdateTypes updateType) { }

    public override void OnOperationFinished(float deltaTime, UpdateTypes updateType) { }

    public void SetParticle(ParticleSystem.Particle particle, int index)
    {
        m_particlesList[index] = particle;
    }

    public void RetrieveBackParticles()
    {
        int countParticles = m_particlesList.Length;
        for (int i = 0; i < countParticles; ++i)
        {
            m_particlesList[i] = AllParticles[m_particleStartIndex + i];
        }

        m_particleSystem.SetParticles(m_particlesList);
        m_particlesList.Dispose();
    }

    protected virtual void OnParticleCallback(ref ParticleSystem.Particle selfParticle, ref ParticleSystem.Particle otherParticle, ParticleInterCollision otherSystem)
    {
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
    public NativeArray<ParticleSystem.Particle> AllParticles;

    public NativeArray<ParticleSystem.Particle>.ReadOnly AllParticlesReadonly;
    public NativeArray<ParticleCollisionSystemData>.ReadOnly AllParticleSystems;

    public NativeArray<ParticleCollisionCallbackData> ParticleCallbacks;

    public bool TracksCallbacks;

    public void Execute(int i)
    {
        int systemsCount = AllParticleSystems.Length;
        int ownSystemIndex = 0;

        while (ownSystemIndex < systemsCount && i >= AllParticleSystems[ownSystemIndex].ParticleStartIndex)
            ++ownSystemIndex;

        ownSystemIndex--;

        //  ownSystemIndex--;
        float minimumParticleDistanceSq = AllParticleSystems[ownSystemIndex].MinimumParticleDistanceSq;
        bool collidesWithOwnParticles = AllParticleSystems[ownSystemIndex].CollidesWithOwnParticles;
        bool callbackOnFirstCollision = AllParticleSystems[ownSystemIndex].CallbackOnFirstCollision;
        int layerMask = AllParticleSystems[ownSystemIndex].LayerMask;
        float bounceCoef = AllParticleSystems[ownSystemIndex].BounceCoef;

        int otherLayer;

        ParticleSystem.Particle otherParticle;
        ParticleSystem.Particle selfParticle = AllParticles[i];

        int j;
        ParticleCollisionCallbackData callbackData = new()
        {
            HitParticleIndex = -1,
            HitParticleSystemIndex = -1
        };

        Bounds selfBounds = AllParticleSystems[ownSystemIndex].Bounds;
        Bounds otherBounds;

        float3 normal;
        float distanceBetweenCenters;
        float distanceBetweenCentersSq;
        float distance;
        float3 vecToOther;

        uint otherPriority;
        uint selfPriority = AllParticleSystems[ownSystemIndex].Priority;

        ParticleCollisionEventType otherEvent;
        ParticleCollisionEventType selfEvent = AllParticleSystems[ownSystemIndex].SelfEvent;

        float3 selfVelocity;
        float3 otherVelocity;

        float otherRadius;
        float selfRadius = AllParticleSystems[ownSystemIndex].Radius;

        float otherEffectiveRadius;
        float selfEffectiveRadius = selfRadius * AllParticles[i].startSize;

        float3 otherPosition;
        float3 selfPosition = selfParticle.position;

        bool ignoreOtherEvent;
        int systemParticleCount;
        int totalParticlesFromPreviousSystem = 0;
        bool destroyedParticle = false;
        bool hasEvent;

        int lastParticleIndex;

        for (int currentSystem = 0; currentSystem < systemsCount && !destroyedParticle; ++currentSystem)
        {
            otherLayer = AllParticleSystems[currentSystem].Layer;
            otherEvent = AllParticleSystems[currentSystem].OtherEvent;
            otherPriority = AllParticleSystems[currentSystem].Priority;
            ignoreOtherEvent = selfPriority > otherPriority;
            systemParticleCount = AllParticleSystems[currentSystem].CountParticles;
            otherBounds = AllParticleSystems[currentSystem].Bounds;
            hasEvent = selfEvent != ParticleCollisionEventType.NONE || (!ignoreOtherEvent && otherEvent != ParticleCollisionEventType.NONE);

            if (GfPhysics.LayerIsInMask(otherLayer, layerMask) && (currentSystem != ownSystemIndex || collidesWithOwnParticles)
            && (callbackOnFirstCollision || hasEvent) && selfBounds.Intersects(otherBounds))
            {
                otherRadius = AllParticleSystems[currentSystem].Radius;
                lastParticleIndex = AllParticleSystems[currentSystem].ParticleStartIndex + AllParticleSystems[currentSystem].CountParticles;
                //do not calculate collision with the same particle
                for (j = AllParticleSystems[currentSystem].ParticleStartIndex; j < lastParticleIndex && !destroyedParticle; ++j)
                {
                    if (currentSystem != ownSystemIndex || j != i)
                    {
                        otherParticle = AllParticlesReadonly[j];
                        otherPosition = otherParticle.position;

                        vecToOther = selfPosition - otherPosition;
                        distanceBetweenCentersSq = lengthsq(vecToOther);

                        if (minimumParticleDistanceSq >= distanceBetweenCentersSq)
                        {
                            otherEffectiveRadius = otherRadius * otherParticle.startSize;
                            distanceBetweenCenters = sqrt(distanceBetweenCentersSq);
                            distance = distanceBetweenCenters - (otherEffectiveRadius + selfEffectiveRadius);

                            if (distance <= 0) //collided
                            {
                                if (distanceBetweenCenters >= 0.01f)
                                {
                                    normal = vecToOther / distanceBetweenCenters;
                                    selfPosition -= normal * (distance * 0.5f);
                                    selfParticle.position = selfPosition;
                                }
                                else
                                    normal = new(0, 0, 0);

                                if (callbackOnFirstCollision && callbackData.HitParticleIndex == -1)
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

                                            selfVelocity = bounceCoef * reflect(selfVelocity, normal);
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
                                    switch (selfEvent)
                                    {
                                        case (ParticleCollisionEventType.BOUNCE):
                                            if (distanceBetweenCenters >= 0.01f)
                                            {
                                                selfVelocity = selfParticle.velocity;
                                                otherVelocity = otherParticle.velocity;

                                                selfVelocity = bounceCoef * reflect(selfVelocity, normal);
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

        if (TracksCallbacks)
            ParticleCallbacks[i] = callbackData;

        AllParticles[i] = selfParticle;
    }
}


//calculate the bounds of each particle system
[BurstCompile]
internal struct ParticleSystemBoundsJob : IJobParallelFor
{
    public NativeArray<ParticleCollisionSystemData> AllParticleSystems;
    public NativeArray<ParticleSystem.Particle>.ReadOnly AllParticlesReadonly;

    public void Execute(int i)
    {
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
        float radius = AllParticleSystems[i].Radius;
        float effectiveRadius;
        int lastParticle = AllParticleSystems[i].ParticleStartIndex + AllParticleSystems[i].CountParticles - 1;
        Vector3 position;
        for (int j = AllParticleSystems[i].ParticleStartIndex; j <= lastParticle; ++j)
        {
            position = AllParticlesReadonly[j].position;
            effectiveRadius = radius * AllParticlesReadonly[j].startSize;
            minX = min(minX, position.x - radius);
            minY = min(minY, position.y - radius);
            minZ = min(minZ, position.z - radius);

            maxX = max(maxX, position.x + radius);
            maxY = max(maxY, position.y + radius);
            maxZ = max(maxZ, position.z + radius);
        }

        Bounds bound = new();
        bound.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        var data = AllParticleSystems[i];
        data.Bounds = bound;
        AllParticleSystems[i] = data;
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
    public float MinimumParticleDistanceSq;

    public bool CollidesWithOwnParticles;

    public bool CallbackOnFirstCollision;

    public uint Priority;
    public int Layer;

    public int LayerMask;

    public float BounceCoef;
    public int CountParticles;

    public int ParticleStartIndex;

    public float Radius;

    public ParticleCollisionEventType SelfEvent;

    public ParticleCollisionEventType OtherEvent;

    public Bounds Bounds;
}


