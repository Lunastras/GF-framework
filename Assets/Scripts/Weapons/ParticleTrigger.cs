using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

using System.Runtime.CompilerServices;

public abstract class ParticleTrigger : MonoBehaviour
{
    [SerializeField]
    protected ParticleSystem m_particleSystem;

    private static List<ParticleSystem.Particle> m_particlesList = null;

    private static readonly Vector3 m_destroyPosition = new(100000000, -100000000, 100000000);

    // Start is called before the first frame update
    void Awake()
    {
        m_particlesList = null == m_particlesList ? new(64) : m_particlesList;
        m_particleSystem = null == m_particlesList ? m_particleSystem : GetComponent<ParticleSystem>();

        InternalAwake();
    }

    private void Start()
    {
        InternalStart();
    }

    virtual protected void InternalAwake() { }
    virtual protected void InternalStart() { }


    void OnParticleTrigger()
    {
        if (m_particlesList == null)
            return;

        int numEnter = m_particleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, m_particlesList, out var colliderData);

        for (int i = 0; i < numEnter; ++i)
        {
            ParticleSystem.Particle particle = m_particlesList[i];
            if (colliderData.GetColliderCount(i) > 0)
            {
                GameObject hitObject = colliderData.GetCollider(i, 0).gameObject;

                CollisionBehaviour(ref particle, hitObject);
                particle.position = m_destroyPosition;

                m_particlesList[i] = particle;
            }
        }

        m_particleSystem.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, m_particlesList);
    }

    protected static void DestroyParticle(ref ParticleSystem.Particle particle)
    {
        particle.position = m_destroyPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    abstract protected void CollisionBehaviour(ref ParticleSystem.Particle particle, GameObject hitObject);
}
