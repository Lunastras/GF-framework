using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;
using static Unity.Mathematics.math;

public class ParticleHoming : JobChild
{
    [SerializeField]
    protected ParticleSystem m_particleSystem;

    [SerializeField]
    protected Transform m_mainTarget;

    [SerializeField]
    protected float m_physInterval = 0.1f;

    [SerializeField]
    protected float m_acceleration = 5;

    [SerializeField]
    protected float m_deacceleration = 5;

    [SerializeField]
    protected float m_maxSpeed = 50;

    [SerializeField]
    protected float m_maxSpeedHoming = 100;

    [SerializeField]
    protected float m_targetRadius = 0.4f;

    [SerializeField]
    protected float m_gravity = 5;

    [SerializeField]
    protected float m_drag = 5;

    [SerializeField]
    protected Transform m_sphericalParent;

    [SerializeField]
    protected Vector3 m_defaultGravityDir = DOWNDIR;

    [SerializeField]
    protected bool m_defaultSettingsOnDisable = true;



    protected static readonly Vector3 DOWNDIR = Vector3.down;

    protected NativeArray<ParticleSystem.Particle> m_particleList;

    protected int m_numActiveParticles = 0;

    protected bool m_initialised = false;

    protected bool m_hasAJob = false;

    protected float m_timeUntilPhysUpdate = 0;

    public GfMovementGeneric MovementGravityReference = null;

    // Start is called before the first frame update
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
        UpdateDefaultGravityCustomData();
    }

    protected void OnEnable()
    {
        if (m_initialised)
            InitJobChild();
    }

    protected void OnDisable()
    {
        DeinitJobChild();
        if (m_defaultSettingsOnDisable)
            ResetToDefault();
    }

    protected void OnDestroy()
    {
        DeinitJobChild();
    }

    public void ResetToDefault()
    {
        m_mainTarget = m_sphericalParent = null;
        MovementGravityReference = null;
        m_defaultGravityDir = DOWNDIR;
        UpdateDefaultGravityCustomData();
    }


    public override bool GetJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512)
    {
        m_hasAJob = false;
        handle = default;

        m_timeUntilPhysUpdate -= deltaTime;

        if (0 >= m_timeUntilPhysUpdate)
        {
            float timeSinceLastCheck = System.MathF.Max(deltaTime, System.MathF.Min(2 * m_physInterval, m_physInterval - m_timeUntilPhysUpdate));
            m_timeUntilPhysUpdate += m_physInterval;
            m_numActiveParticles = m_particleSystem.particleCount;
            //Debug.Log("The num of particles i have is: " + m_numActiveParticles + " name is: " + gameObject.name);
            if (m_numActiveParticles > 0)
            {
                if (MovementGravityReference)
                {
                    GfMovementGeneric auxMovement = MovementGravityReference;
                    CopyGravity(MovementGravityReference);
                    MovementGravityReference = auxMovement;
                }

                m_particleList = new(m_numActiveParticles, Allocator.TempJob);
                m_particleSystem.GetParticles(m_particleList, m_numActiveParticles);

                Vector3 targetPos = DOWNDIR;
                if (m_mainTarget) targetPos = m_mainTarget.position;

                Vector3 gravity3 = m_defaultGravityDir;
                if (m_sphericalParent)
                {
                    gravity3 = m_sphericalParent.position;

                    var customData = m_particleSystem.customData;
                    customData.SetVector(ParticleSystemCustomData.Custom1, 0, gravity3.x);
                    customData.SetVector(ParticleSystemCustomData.Custom1, 1, gravity3.y);
                    customData.SetVector(ParticleSystemCustomData.Custom1, 2, gravity3.z);
                    customData.SetVector(ParticleSystemCustomData.Custom1, 3, 1); //1, position
                }

                ParticleHomingJob jobStruct = new ParticleHomingJob(m_particleList, targetPos, timeSinceLastCheck, m_maxSpeed, m_maxSpeedHoming
                , m_acceleration, m_gravity, m_deacceleration, m_drag, m_targetRadius * m_targetRadius
                , gravity3, null != m_sphericalParent, null != m_mainTarget);

                handle = jobStruct.Schedule(m_numActiveParticles, min(16, m_numActiveParticles));
                m_hasAJob = true;
            }
        }

        return m_hasAJob;
    }

    public ParticleSystem GetParticleSystem()
    {
        return m_particleSystem;
    }

    public override void OnJobFinished(float deltaTime, UpdateTypes updateType)
    {
        if (m_hasAJob)
        {
            m_particleSystem.SetParticles(m_particleList, m_numActiveParticles);
            m_particleList.Dispose();
        }
    }

    public Transform GetTarget()
    {
        return m_mainTarget;
    }

    public void SetTarget(Transform target)
    {
        m_mainTarget = target;
    }

    public Transform GetParentSpherical()
    {
        return m_sphericalParent;
    }

    public void SetSphericalParent(Transform parent)
    {
        m_sphericalParent = parent;
    }

    public void CopyGravity(GfMovementGeneric movement)
    {
        CopyGravity(movement.GetGravityReference());
    }

    public void CopyGravity(ParticleHoming pg)
    {
        CopyGravity(pg.GetGravityReference());
    }

    public void CopyGravity(GravityReference gravityReference)
    {
        if (gravityReference.SphericalParent)
            SetSphericalParent(gravityReference.SphericalParent);
        else
            SetDefaultGravityDir(-gravityReference.UpVec);

        MovementGravityReference = null;
    }

    public bool HasSameGravity(GravityReference gravityReference)
    {
        Transform parentSphericalToCopy = gravityReference.SphericalParent;
        bool sameParent = parentSphericalToCopy == m_sphericalParent;
        bool nullParents = null == m_sphericalParent && null == parentSphericalToCopy;

        return ((sameParent && !nullParents) || (nullParents && (-gravityReference.UpVec) == m_defaultGravityDir));
    }

    public bool HasSameGravity(GfMovementGeneric movement)
    {
        return HasSameGravity(movement.GetGravityReference());
    }

    public bool HasSameGravity(ParticleHoming pg)
    {
        return HasSameGravity(pg.GetGravityReference());
    }

    public Vector3 GetDefaultGravityDir() { return m_defaultGravityDir; }
    public void SetDefaultGravityDir(Vector3 upVec)
    {
        m_defaultGravityDir = upVec;
        SetSphericalParent(null);
        UpdateDefaultGravityCustomData();
    }

    //Updates the custom data for the particle system with the given default gravity dir
    private void UpdateDefaultGravityCustomData()
    {
        var customData = m_particleSystem.customData;
        customData.SetVector(ParticleSystemCustomData.Custom1, 0, m_defaultGravityDir.x);
        customData.SetVector(ParticleSystemCustomData.Custom1, 1, m_defaultGravityDir.y);
        customData.SetVector(ParticleSystemCustomData.Custom1, 2, m_defaultGravityDir.z);
        customData.SetVector(ParticleSystemCustomData.Custom1, 3, 0); //0, direction
    }

    public GravityReference GetGravityReference()
    {
        return new(-m_defaultGravityDir, m_sphericalParent);
    }
}

[BurstCompile]
public struct ParticleHomingJob : IJobParallelFor
{
    NativeArray<ParticleSystem.Particle> m_particleList;
    public float3 m_targetPos;
    public float m_deltaTime;
    public float m_maxSpeed;

    public float m_maxSpeedHoming;
    public float m_acceleration;
    public float m_deacceleration;
    public float m_drag;
    public float m_targetRadiusSquared;
    float3 m_gravity3;

    float m_gravity;

    bool m_hasParent;
    bool m_hasTarget;

    public ParticleHomingJob(NativeArray<ParticleSystem.Particle> particleList, float3 targetPos
    , float deltaTime, float maxSpeed, float maxSpeedHoming, float acceleration, float gravity, float deacceleration
    , float drag, float targetRadiusSquared, float3 gravity3, bool hasParent
    , bool hasTarget)
    {
        m_targetPos = targetPos;
        m_particleList = particleList;
        m_deltaTime = deltaTime;
        m_maxSpeed = maxSpeed;
        m_acceleration = acceleration;
        m_deacceleration = deacceleration;
        m_targetRadiusSquared = targetRadiusSquared;
        m_gravity3 = gravity3;
        m_hasParent = hasParent;
        m_hasTarget = hasTarget;
        m_drag = drag;
        m_gravity = gravity;
        m_maxSpeedHoming = maxSpeedHoming;
    }

    public void Execute(int i)
    {
        ParticleSystem.Particle particle = m_particleList[i];
        float3 currentPos = particle.position;
        float3 velocity = particle.velocity;

        float3 dirToTarget = m_targetPos - currentPos;
        float3 dirToPlanet = m_gravity3 - currentPos;

        bool followsTarget = m_hasTarget && lengthsq(dirToTarget) <= m_targetRadiusSquared;
        bool usesParentGravity = !followsTarget && m_hasParent;

        float followsTargetF = Convert.ToSingle(followsTarget);
        float notFollowingTargetF = 1f - followsTargetF;

        float3 movDir = m_gravity3 * Convert.ToSingle(!usesParentGravity && !followsTarget)
                    + dirToTarget * followsTargetF
                    + dirToPlanet * Convert.ToSingle(usesParentGravity);

        float currentSpeed = length(velocity);
        //float gravityCoef = 1;
        bool isMoving = currentSpeed >= 0.02f; //do not apply gravity if we are stationary, this is to prevent bouncing
        float gravityCoef = (1.0f - Convert.ToSingle(!isMoving) * 0.99f);
        float acceleration = m_acceleration * followsTargetF + m_gravity * notFollowingTargetF * gravityCoef;
        float deacceleration = m_deacceleration * followsTargetF + m_drag * notFollowingTargetF;

        float movDirLengthSq = lengthsq(movDir);
        if (movDirLengthSq > 0.00001F)
            movDir /= sqrt(movDirLengthSq);


        float dotMovementVelDir = 0;
        if (currentSpeed > 0.00001F)
            dotMovementVelDir = dot(movDir, velocity / currentSpeed);

        float maxSpeed = m_maxSpeed * notFollowingTargetF + m_maxSpeedHoming * followsTargetF;

        float speedInDesiredDir = currentSpeed * max(0, dotMovementVelDir);
        float3 unwantedVelocity = velocity - movDir * min(speedInDesiredDir, maxSpeed);
        //if (maxSpeed > speedInDesiredDir) unwantedVelocity -= movDir * dot(movDir, deacceleration);

        float unwantedSpeed = length(unwantedVelocity);
        if (unwantedSpeed > 0.00001F) unwantedVelocity /= unwantedSpeed;

        float accMagn = min(max(0, maxSpeed - speedInDesiredDir), m_deltaTime * acceleration);
        float deaccMagn = min(unwantedSpeed, deacceleration * m_deltaTime);

        velocity += movDir * accMagn;
        velocity -= unwantedVelocity * deaccMagn;

        particle.velocity = velocity;
        m_particleList[i] = particle;
    }
}
