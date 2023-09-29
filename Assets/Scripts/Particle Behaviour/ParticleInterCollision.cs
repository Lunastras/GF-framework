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
using UnityEditor.Rendering;
using Unity.Netcode;

using System.Diagnostics;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using Mono.Cecil;

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

    protected static NativeArray<ParticleSystem.Particle> AllParticles;

    protected static int AllParticlesCount = 0;

    protected static NativeList<ParticleCollisionSystemData> AllParticleSystems = new(16, Allocator.Persistent);

    protected static NativeArray<ParticleCollisionCallbackData> ParticleCallbacks;

    protected static bool ProcedureOngoing = false;

    protected static bool TracksCallbacks = false;

    protected int m_selfSystemIndex = -1;

    protected float m_scheduleIntervalVariance = 0.1f;

    protected float m_timeUntilScedule;

    protected int m_particleStartIndex;

    protected Bounds LastBounds = default;

    protected void Awake()
    {
        if (null == m_particleSystem)
            m_particleSystem = GetComponent<ParticleSystem>();
    }

    protected void Start()
    {
        InitJobChild();
        m_initialised = true;
        var customData = m_particleSystem.customData;
        customData.enabled = true;

        if (!ParticleCallbacks.IsCreated) ParticleCallbacks = new(16, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }

    protected void OnEnable()
    {
        if (m_initialised)
            InitJobChild();
    }

    protected void OnDisable()
    {
        DeinitJobChild();

        bool hasInstance = JobParent.HasInstance();
        int countBrothers = 0;

        if (hasInstance)
        {
            var brotherSystems = JobParent.GetJobChildren(typeof(ParticleInterCollision), UpdateTypes.FIXED_UPDATE);
            countBrothers = brotherSystems.Count;
        }

        //UnityEngine.Debug.Log("Count brothers is: " + countBrothers);
        //uninitialize everything if there are no more systems
        if (countBrothers == 0)
        {
            //UnityEngine.Debug.Log("trying to destroy everything ");

            if (AllParticles.IsCreated) AllParticles.Dispose();
            if (ParticleCallbacks.IsCreated) ParticleCallbacks.Dispose();
            if (AllParticleSystems.IsCreated) AllParticleSystems.Dispose();
        }
    }

    protected void OnDestroy()
    {
    }

    public override void OnOperationStart(float deltaTime, UpdateTypes updateType)
    {
        if (JobParent.CanSchedule(JobScheduleTypes.PARTICLE_COLLISION))
        {
            if (!ProcedureOngoing)
            {
                AllParticlesCount = 0;
                if (!AllParticleSystems.IsCreated)
                    AllParticleSystems = new(16, Allocator.Persistent);

                AllParticleSystems.Clear();
                TracksCallbacks = false;
                ProcedureOngoing = true;
            }

            m_particleStartIndex = AllParticlesCount;
            int particleCount = m_particleSystem.particleCount;

            StoreParticlesToAllArray(m_particleSystem, particleCount);

            if (m_dynamicRadius)
            {
                ParticleSystem.Particle particle;
                int endIndex = m_particleStartIndex + particleCount;
                for (int i = m_particleStartIndex; i < endIndex; ++i)
                {
                    particle = AllParticles[i];
                    particle.startSize = particle.GetCurrentSize(m_particleSystem);
                    AllParticles[i] = particle;
                }
            }

            m_selfSystemIndex = AllParticleSystems.Length;

            AllParticleSystems.Add(new ParticleCollisionSystemData()
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
            });

            TracksCallbacks |= m_callbackOnFirstCollision;
        }
    }

    public override bool GetJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512)
    {
        m_hasAJob = false;
        handle = default;

        if (ProcedureOngoing && 0 < AllParticlesCount)
        {
            int countAllParticles = AllParticlesCount;

            if (TracksCallbacks && ParticleCallbacks.Length < countAllParticles)
            {
                if (ParticleCallbacks.IsCreated)
                    ParticleCallbacks.Dispose();

                int newtLength = ParticleCallbacks.Length;
                while (newtLength < countAllParticles)
                    newtLength <<= 1;
                ParticleCallbacks = new(newtLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            int countSystems = AllParticleSystems.Length;

            var AllWorkingParticles = AllParticles.GetSubArray(0, countAllParticles);

            ParticleInterCollisionJob job = new()
            {
                ParticleSystemsCount = countSystems,

                AllParticles = AllWorkingParticles,
                ParticleCallbacks = ParticleCallbacks,
                AllParticleSystems = AllParticleSystems.AsArray().AsReadOnly(),
                TracksCallbacks = TracksCallbacks
            };

            ParticleSystemBoundsJob boundsJob = new()
            {
                AllParticlesReadonly = AllWorkingParticles.AsReadOnly(),
                AllParticleSystems = AllParticleSystems
            };

            var brotherSystems = JobParent.GetJobChildren(GetType(), updateType);

            //UnityEngine.Debug.Log("The count of systems is: " + countSystems + " the length of the array is: " + AllParticleSystems.Length);
            // UnityEngine.Debug.Log("The count of particles is: " + countAllParticles + " the length of the array is: " + AllParticles.Length);

            boundsJob.Schedule(countSystems, 32).Complete();
            job.Schedule(countAllParticles, 16).Complete();

            ParticleCollisionSystemData systemData;
            ParticleSystem.Particle selfParticle;
            ParticleSystem.Particle otherParticle;
            int totalParticlesFromPreviousSystem = 0;
            int systemParticleCount;
            int lastIndex;

            for (int currentSystem = 0; currentSystem < countSystems; ++currentSystem)
            {
                systemData = AllParticleSystems[currentSystem];
                systemParticleCount = systemData.CountParticles;

                if (systemData.CallbackOnFirstCollision)
                {
                    var selfSystem = brotherSystems[currentSystem] as ParticleInterCollision;
                    lastIndex = systemParticleCount + systemData.ParticleStartIndex;

                    for (int i = systemData.ParticleStartIndex; i < lastIndex; ++i)
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

            ParticleInterCollision system;
            for (int i = 0; i < brotherSystems.Count; ++i)
            {
                system = brotherSystems[i] as ParticleInterCollision;
                system.RetrieveBackParticles();
                system.LastBounds = AllParticleSystems[i].Bounds;
            }
        }

        ProcedureOngoing = false;

        return m_hasAJob;
    }


    public override void OnJobFinished(float deltaTime, UpdateTypes updateType) { }

    public override void OnOperationFinished(float deltaTime, UpdateTypes updateType) { }

    private void RetrieveBackParticles()
    {
        int countParticles = m_particleSystem.particleCount;
        var particleArray = AllParticles.GetSubArray(m_particleStartIndex, countParticles);
        m_particleSystem.SetParticles(particleArray);
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

    private static void StoreParticlesToAllArray(ParticleSystem system, int countParticles)
    {
        int desiredLength = AllParticlesCount + countParticles;
        int currentArrayLength = AllParticles.Length;

        if (!AllParticles.IsCreated || desiredLength > currentArrayLength)
        {
            currentArrayLength = max(currentArrayLength, 1);

            while (desiredLength > currentArrayLength)
                currentArrayLength <<= 1; //double the size until we can fit the particles

            var currentParticles = AllParticles;
            AllParticles = new(currentArrayLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < AllParticlesCount; ++i)
            {
                AllParticles[i] = currentParticles[i];
            }

            if (currentParticles.IsCreated)
                currentParticles.Dispose();
        }

        var subParticleArray = AllParticles.GetSubArray(AllParticlesCount, countParticles);
        system.GetParticles(subParticleArray, countParticles);
        AllParticlesCount += countParticles;
    }

    public static unsafe int Raycast(Ray ray, LayerMask layerMask, List<ParticleRayHit> rayHits)
    {
        rayHits.Clear();
        int hitCount = 0;
        AllParticlesCount = 0;
        var brotherSystems = JobParent.GetJobChildren(typeof(ParticleInterCollision), UpdateTypes.FIXED_UPDATE);
        int countBrothers = brotherSystems.Count;

        if (countBrothers > 0)
        {
            NativeList<ParticleCollisionSystemDataBasic> allParticleSystems = new(countBrothers, Allocator.TempJob);

            int countParticles;
            ParticleInterCollision system;
            ParticleSystem particleSystem;

            //  Stopwatch timePerParse = Stopwatch.StartNew();

            for (int i = 0; i < countBrothers; ++i)
            {
                system = brotherSystems[i] as ParticleInterCollision;
                if (GfPhysics.LayerIsInMask(system.gameObject.layer, layerMask) && system.LastBounds.IntersectRay(ray))
                {
                    particleSystem = system.m_particleSystem;
                    countParticles = particleSystem.particleCount;
                    if (countParticles > 0)
                    {
                        allParticleSystems.Add(new ParticleCollisionSystemDataBasic()
                        {
                            ParticleStartIndex = AllParticlesCount,
                            ParticleSystemIndex = i,
                            CountParticles = countParticles,
                            Radius = particleSystem.collision.radiusScale,
                        });

                        StoreParticlesToAllArray(particleSystem, countParticles);
                    }
                }
            }

            // UnityEngine.Debug.Log("PHASE 1 elapsed time in ms: " + timePerParse.Elapsed.TotalMilliseconds);
            // timePerParse = Stopwatch.StartNew();

            if (AllParticlesCount > 0)
            {
                NativeArray<ParticleRayCollision> particleRayCollisions = new(AllParticlesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                float3 rayDirection = ray.direction;
                float3 rayStart = ray.origin;
                float a = lengthsq(rayDirection);

                new ParticlesRaycast()
                {
                    AllParticleCollisions = particleRayCollisions,
                    AllParticleSystems = allParticleSystems.AsArray().AsReadOnly(),
                    AllParticlesReadonly = AllParticles.AsReadOnly(),

                    ParticleSystemsCount = allParticleSystems.Length,

                    A = a,
                    A2Inv = 1.0f / (a * 2.0f),
                    RayStartLengthSq = lengthsq(rayStart),
                    RayStart = rayStart,
                    RayDirection = rayDirection,
                }.Schedule(AllParticlesCount, 64).Complete();

                //UnityEngine.Debug.Log("PHASE 2 elapsed time in ms: " + timePerParse.Elapsed.TotalMilliseconds);
                // timePerParse = Stopwatch.StartNew();

                hitCount = 0;
                int currentSystemIndex = 0;
                int currentParticleStartIndex = 0;
                ParticleInterCollision currentSystem = null;
                for (int i = 0; i < AllParticlesCount; ++i)
                {
                    if ((currentSystemIndex + 1) < allParticleSystems.Length
                    && i == allParticleSystems[currentSystemIndex + 1].ParticleStartIndex)
                    {
                        currentSystemIndex++;
                        currentParticleStartIndex = allParticleSystems[currentSystemIndex].ParticleStartIndex;
                        currentSystem = brotherSystems[allParticleSystems[currentSystemIndex].ParticleSystemIndex] as ParticleInterCollision;
                    }

                    if (particleRayCollisions[i].HasCollision)
                    {
                        rayHits.Add(new()
                        {
                            CollisionData = particleRayCollisions[i],
                            ParticleIndex = i - currentParticleStartIndex,
                            Particle = AllParticles[i],
                            CollisionSystem = currentSystem,
                        });

                        hitCount++;
                    }
                }

                particleRayCollisions.Dispose();

                //timePerParse.Stop();
                //UnityEngine.Debug.Log("PHASE 3 elapsed time in ms: " + timePerParse.Elapsed.TotalMilliseconds);
            }

            allParticleSystems.Dispose();
        }

        return hitCount;
    }
}


[BurstCompile]
internal struct ParticleInterCollisionJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<ParticleSystem.Particle> AllParticles;

    //public NativeArray<ParticleSystem.Particle>.ReadOnly AllParticlesReadonly;
    public NativeArray<ParticleCollisionSystemData>.ReadOnly AllParticleSystems;

    public NativeArray<ParticleCollisionCallbackData> ParticleCallbacks;

    public bool TracksCallbacks;

    public int ParticleSystemsCount;

    public void Execute(int i)
    {
        int ownSystemIndex = 0;

        while (ownSystemIndex < ParticleSystemsCount && i >= AllParticleSystems[ownSystemIndex].ParticleStartIndex)
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

        for (int currentSystem = 0; currentSystem < ParticleSystemsCount && !destroyedParticle; ++currentSystem)
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
                        otherParticle = AllParticles[j];
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
                                    switch (otherEvent)
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

[BurstCompile]
internal struct ParticlesRaycast : IJobParallelFor
{
    public NativeArray<ParticleRayCollision> AllParticleCollisions;
    public NativeArray<ParticleCollisionSystemDataBasic>.ReadOnly AllParticleSystems;
    public NativeArray<ParticleSystem.Particle>.ReadOnly AllParticlesReadonly;

    public int ParticleSystemsCount;
    //the a coefficient in the quadratic formula


    public float RayStartLengthSq; //lengthsq(RayStart)
    public float3 RayStart;
    public float3 RayDirection;

    //for determining the collision between the line and the sphere, we use the resources found on this website:
    //https://paulbourke.net/geometry/circlesphere/#:~:text=Intersection%20of%20a%20Line%20and%20a%20Sphere%20(or%20circle)&text=If%20this%20is%20less%20than,the%20sphere%20at%20two%20points.


    public float A; //lengthsq(direction)

    public float A2Inv; //a2inv = 1.0f / (2.0f * a)

    public void Execute(int i)
    {
        ParticleRayCollision hitData = default;
        int ownSystemIndex = 0;

        while (ownSystemIndex < ParticleSystemsCount && i >= AllParticleSystems[ownSystemIndex].ParticleStartIndex)
            ++ownSystemIndex;

        ownSystemIndex--;

        ParticleCollisionSystemDataBasic data = AllParticleSystems[ownSystemIndex];
        float radius = AllParticleSystems[ownSystemIndex].Radius * AllParticlesReadonly[i].startSize;

        float3 position = AllParticlesReadonly[i].position;
        float b = 2.0f * ((RayStart.x - position.x) * RayDirection.x + (RayStart.y - position.y) * RayDirection.y + (RayStart.z - position.z) * RayDirection.z);
        float c = lengthsq(position) + RayStartLengthSq - 2 * (position.x * RayStart.x + position.y * RayStart.y + position.z * RayStart.z) - radius * radius;

        float delta = b * b - 4 * A * c;
        if (delta >= 0)
        {
            hitData.HasCollision = true;
            float deltaSqrt = sqrt(delta);
            float u1 = (-b + deltaSqrt) * A2Inv;
            float u2 = (-b - deltaSqrt) * A2Inv;

            float3 collisionPoint1 = new float3(
                position.x + u1 * RayDirection.x
                , position.y + u1 * RayDirection.y
                , position.z + u1 * RayDirection.z
            );

            float3 collisionPoint2 = new float3(
                position.x + u2 * RayDirection.x
                , position.y + u2 * RayDirection.y
                , position.z + u2 * RayDirection.z
            );

            float3 pointEnter;
            float3 pointExit;

            if (lengthsq(RayStart - collisionPoint1) <= lengthsq(RayStart - collisionPoint2))
            {
                pointEnter = collisionPoint1;
                pointExit = collisionPoint2;
            }
            else
            {
                pointEnter = collisionPoint2;
                pointExit = collisionPoint1;
            }

            hitData.PointEnter = pointEnter;
            hitData.PointExit = pointExit;

            //if the radius is 0 and a collision happened, the magnitude between the hits and the position will be 0, so we avoid dividing by 0
            if (radius > 0)
            {
                hitData.NormalEnter = normalize(pointEnter - position);
                hitData.NormalExit = normalize(pointExit - position);
            }
        }

        AllParticleCollisions[i] = hitData;
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

public struct ParticleRayCollision
{
    public Vector3 PointEnter;

    public Vector3 PointExit;

    public Vector3 NormalEnter;

    public Vector3 NormalExit;

    public bool HasCollision;
}

public struct ParticleRayHit
{
    public ParticleRayCollision CollisionData;
    public int ParticleIndex;

    public ParticleSystem.Particle Particle;

    public ParticleInterCollision CollisionSystem;
}

public struct ParticleCollisionSystemDataBasic
{
    public int CountParticles;

    public int ParticleStartIndex;

    public int ParticleSystemIndex;

    public float Radius;
}


