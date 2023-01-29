using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;


using static Unity.Mathematics.math;

public class ParticleGravity : JobChild
{
    [SerializeField]
    protected ParticleSystem m_particleSystem;


    [SerializeField]
    protected float m_mass = 5;

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

    private static readonly Vector3 DOWNDIR = Vector3.down;

    protected NativeArray<ParticleSystem.Particle> m_particleList;

    protected int m_numActiveParticles = 0;

    protected bool m_initialised = false;

    protected bool m_hasAJob = false;

    protected

    // Start is called before the first frame update
    void Start()
    {
        if (null == m_particleSystem) m_particleSystem = GetComponent<ParticleSystem>();
        Init();
        m_initialised = true;

        SetDefaultGravityDir(m_defaultGravityDir);
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
        m_numActiveParticles = m_particleSystem.particleCount;
        handle = default;
        if (m_numActiveParticles > 0)
        {
            m_particleList = new(m_numActiveParticles, Allocator.TempJob);

            m_particleSystem.GetParticles(m_particleList, m_numActiveParticles);

            Vector3 gravity3 = m_defaultGravityDir;
            if (m_sphericalParent)
            {
                gravity3 = m_sphericalParent.position;

                var customData = m_particleSystem.customData;
                customData.enabled = true;
                customData.SetVector(ParticleSystemCustomData.Custom1, 0, gravity3.x);
                customData.SetVector(ParticleSystemCustomData.Custom1, 1, gravity3.y);
                customData.SetVector(ParticleSystemCustomData.Custom1, 2, gravity3.z);
                customData.SetVector(ParticleSystemCustomData.Custom1, 3, 1); //1, position
            }

            ParticleGravityJob jobStruct = new ParticleGravityJob(m_particleList, gravity3, deltaTime, m_terminalVelocity, m_mass, m_drag, null != m_sphericalParent, m_gravityCoef);
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
            SetDefaultGravityDir(-movement.UpVecEffective() * System.MathF.Sign(m_gravityCoef));
    }

    public void CopyGravity(ParticleGravity pg)
    {
        Transform sphericalParent = pg.GetParentSpherical();

        if (sphericalParent)
            SetSphericalParent(sphericalParent);
        else
            SetDefaultGravityDir(-pg.GetDefaultGravityDir());

        m_gravityCoef = pg.GetGravityCoef();
    }

    public bool HasSameGravity(GfMovementGeneric movement)
    {
        Transform parentSphericalToCopy = movement.GetParentSpherical();
        bool sameParent = parentSphericalToCopy == m_sphericalParent;
        bool nullParents = null == m_sphericalParent && null == parentSphericalToCopy;
        bool sameCoefSign = System.MathF.Sign(m_gravityCoef) == System.MathF.Sign(movement.GetGravityCoef());

        return sameCoefSign && ((sameParent && !nullParents) || (nullParents && (-movement.UpVecRaw()) == m_defaultGravityDir));
    }

    public bool HasSameGravity(ParticleGravity pg)
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

public struct ParticleGravityJob : IJobParallelFor
{
    NativeArray<ParticleSystem.Particle> m_particleList;

    float3 m_gravity3;
    bool m_hasParent;
    float m_deltaTime;
    float m_maxSpeed;
    float m_acceleration;
    float m_deacceleration;
    float m_gravityMultiplier;

    //Note, if hasParent == true, then gravity3 MUST be normalised
    public ParticleGravityJob(NativeArray<ParticleSystem.Particle> particleList, float3 gravity3, float deltaTime, float maxSpeed, float acceleration, float deacceleration, bool hasParent, float gravityMultiplier)
    {
        m_gravity3 = gravity3;
        m_particleList = particleList;
        m_deltaTime = deltaTime;
        m_maxSpeed = maxSpeed;
        m_acceleration = acceleration;
        m_hasParent = hasParent;
        m_gravityMultiplier = gravityMultiplier;
        m_deacceleration = deacceleration;
    }

    public void Execute(int i)
    {
        ParticleSystem.Particle particle = m_particleList[i];
        float3 currentPos = particle.position;
        float3 velocity = particle.velocity;
        float3 effectiveGravity = m_gravity3;

        if (m_hasParent) //if the m_hasParent is true, then m_gravity3 is the gravitational center of the particle, otherwise it is the direction of the gravity
        {
            effectiveGravity = (effectiveGravity - currentPos);
            float mag = length(effectiveGravity);
            if (mag > 0.000001F) effectiveGravity /= mag;
        }

        effectiveGravity *= sign(m_gravityMultiplier);

        float currentSpeed = length(velocity);

        float dotMovementVelDir = 0;
        if (currentSpeed > 0.000001F)
            dotMovementVelDir = dot(effectiveGravity, velocity / currentSpeed);

        float speedInDesiredDir = currentSpeed * max(0, dotMovementVelDir);
        float3 unwantedVelocity = velocity - effectiveGravity * min(speedInDesiredDir, m_maxSpeed);

        float unwantedSpeed = length(unwantedVelocity);
        if (unwantedSpeed > 0.000001F) unwantedVelocity /= unwantedSpeed;

        float accMagn = min(max(0, m_maxSpeed - speedInDesiredDir), m_deltaTime * m_acceleration * abs(m_gravityMultiplier));
        float deaccMagn = min(unwantedSpeed, m_deacceleration * m_deltaTime);

        velocity += effectiveGravity * accMagn - unwantedVelocity * deaccMagn;

        particle.velocity = velocity;
        m_particleList[i] = particle;
    }
}
