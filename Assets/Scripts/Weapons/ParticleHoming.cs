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
    protected float m_acceleration = 5;

    [SerializeField]
    protected float m_deacceleration = 5;

    [SerializeField]
    protected float m_maxSpeed = 50;

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
    protected float m_gravityCoef = 1;

    private static readonly Vector3 DOWNDIR = Vector3.down;

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
        UpdateDefaultGravityCustomData();
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
        m_numActiveParticles = m_particleSystem.particleCount;

        handle = default;
        if (m_numActiveParticles > 0)
        {
            m_particleList = new(m_numActiveParticles, Allocator.TempJob);
            m_particleSystem.GetParticles(m_particleList, m_numActiveParticles);

            Vector3 targetPos = DOWNDIR;
            if(m_mainTarget) targetPos = m_mainTarget.position;

            Vector3 gravity3 = m_defaultGravityDir;
            if (m_sphericalParent)
            {
                gravity3 = m_sphericalParent.position;

                var customData = m_particleSystem.customData;
                float sign = Mathf.Sign(m_gravityCoef);
                customData.SetVector(ParticleSystemCustomData.Custom1, 0, gravity3.x * sign);
                customData.SetVector(ParticleSystemCustomData.Custom1, 1, gravity3.y * sign);
                customData.SetVector(ParticleSystemCustomData.Custom1, 2, gravity3.z * sign);
                customData.SetVector(ParticleSystemCustomData.Custom1, 3, 1); //1, position
            }

            ParticleHomingJob jobStruct = new ParticleHomingJob(m_particleList, targetPos, deltaTime, m_maxSpeed
            , m_acceleration, m_gravity, m_deacceleration, m_drag, m_targetRadius
            , m_gravityCoef, gravity3, null != m_sphericalParent, null != m_mainTarget);

            handle = jobStruct.Schedule(m_numActiveParticles, min(32, m_numActiveParticles));
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
        Transform sphericalParent = movement.GetParentSpherical();
        m_gravityCoef = movement.GetGravityCoef();

        if (sphericalParent)
            SetSphericalParent(sphericalParent);
        else
            SetDefaultGravityDir(-movement.UpVecEffective());
    }

    public void CopyGravity(ParticleHoming pg)
    {
        Transform sphericalParent = pg.GetParentSpherical();
        m_gravityCoef = pg.GetGravityCoef();

        if (sphericalParent)
            SetSphericalParent(sphericalParent);
        else
            SetDefaultGravityDir(pg.GetDefaultGravityDir());

    }

    public bool HasSameGravity(GfMovementGeneric movement)
    {
        Transform parentSphericalToCopy = movement.GetParentSpherical();
        bool sameParent = parentSphericalToCopy == m_sphericalParent;
        bool nullParents = null == m_sphericalParent && null == parentSphericalToCopy;
        bool sameCoefSign = System.MathF.Sign(m_gravityCoef) == System.MathF.Sign(movement.GetGravityCoef());

        return sameCoefSign && ((sameParent && !nullParents) || (nullParents && (-movement.UpVecRaw()) == m_defaultGravityDir));
    }

    public bool HasSameGravity(ParticleHoming pg)
    {
        Transform parentSphericalToCopy = pg.GetParentSpherical();
        bool sameParent = parentSphericalToCopy == m_sphericalParent;
        bool nullParents = null == m_sphericalParent && null == parentSphericalToCopy;
        bool sameCoefSign = System.MathF.Sign(m_gravityCoef) == System.MathF.Sign(pg.GetGravityCoef());

        return sameCoefSign && ((sameParent && !nullParents) || (nullParents && pg.GetDefaultGravityDir() == m_defaultGravityDir));
    }

    public Vector3 GetDefaultGravityDir() { return m_defaultGravityDir; }
    public void SetDefaultGravityDir(Vector3 upVec)
    {
        m_defaultGravityDir = upVec.normalized;
        if (m_defaultGravityDir.sqrMagnitude < 0.000001f)
            m_defaultGravityDir = DOWNDIR;

        UpdateDefaultGravityCustomData();
    }

    public void SetGravityCoef(float coef)
    {
        m_gravityCoef = coef;
        UpdateDefaultGravityCustomData();
    }

    public float GetGravityCoef()
    {
        return m_gravityCoef;
    }

    //Updates the custom data for the particle system with the given default gravity dir
    private void UpdateDefaultGravityCustomData()
    {
        var customData = m_particleSystem.customData;
        customData.enabled = true;
        float sign = Mathf.Sign(m_gravityCoef);
        customData.SetVector(ParticleSystemCustomData.Custom1, 0, sign * m_defaultGravityDir.x);
        customData.SetVector(ParticleSystemCustomData.Custom1, 1, sign * m_defaultGravityDir.y);
        customData.SetVector(ParticleSystemCustomData.Custom1, 2, sign * m_defaultGravityDir.z);
        customData.SetVector(ParticleSystemCustomData.Custom1, 3, 0); //0, direction
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
    public float m_drag;
    public float m_targetRadius;
    float3 m_gravity3;

    float m_gravity;

    bool m_hasParent;
    bool m_hasTarget;

    float m_gravityMultiplier;



    public ParticleHomingJob(NativeArray<ParticleSystem.Particle> particleList, float3 targetPos, float deltaTime, float maxSpeed, float acceleration,float gravity, float deacceleration, float drag, float targetRadius, float gravityMultiplier, float3 gravity3, bool hasParent, bool hasTarget)
    {
        m_targetPos = targetPos;
        m_particleList = particleList;
        m_deltaTime = deltaTime;
        m_maxSpeed = maxSpeed;
        m_acceleration = acceleration;
        m_deacceleration = deacceleration;
        m_targetRadius = targetRadius;
        m_gravityMultiplier = gravityMultiplier;
        m_gravity3 = gravity3;
        m_hasParent = hasParent;
        m_hasTarget = hasTarget;
        m_drag = drag;
        m_gravity = gravity;
    }

    public void Execute(int i)
    {
        ParticleSystem.Particle particle = m_particleList[i];
        float3 currentPos = particle.position;
        float3 velocity = particle.velocity;

        float3 dirToTarget = m_targetPos - currentPos;
        float3 dirToPlanet = m_gravity3 - currentPos;

        bool followsTarget = m_hasTarget && length(dirToTarget) < m_targetRadius;
        bool usesParentGravity = !followsTarget && m_hasParent;

        float targetPosCoefF = Convert.ToSingle(followsTarget);
        float notTargetPosCoefF = 1f - targetPosCoefF;

        float3 movDir = m_gravity3 * Convert.ToSingle(!usesParentGravity && !followsTarget)
                    + dirToTarget * targetPosCoefF
                    + dirToPlanet * Convert.ToSingle(usesParentGravity);

        float acceleration = m_acceleration * targetPosCoefF + m_gravity * abs(m_gravityMultiplier) * notTargetPosCoefF;
        float deacceleration = m_deacceleration * targetPosCoefF +  m_drag * notTargetPosCoefF;

        float targetDist = length(movDir);

        if (targetDist > 0.000001F)
            movDir /= targetDist;

        float currentSpeed = length(velocity);

        float dotMovementVelDir = 0;
        if (currentSpeed > 0.000001F)
            dotMovementVelDir = dot(movDir, velocity / currentSpeed);

        float speedInDesiredDir = currentSpeed * max(0, dotMovementVelDir);
        float3 unwantedVelocity = velocity - movDir * min(speedInDesiredDir, m_maxSpeed);

        float unwantedSpeed = length(unwantedVelocity);
        if (unwantedSpeed > 0.000001F) unwantedVelocity /= unwantedSpeed;

        float accMagn = min(max(0, m_maxSpeed - speedInDesiredDir), m_deltaTime * acceleration);
        float deaccMagn = min(unwantedSpeed, deacceleration * m_deltaTime);

        velocity += movDir * accMagn;
        velocity -= unwantedVelocity * deaccMagn;

        particle.velocity = velocity;
        m_particleList[i] = particle;
    }
}
