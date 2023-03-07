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

    public static List<ParticleSystem.Particle> m_particlesList = null;

    protected static readonly Vector3 DESTROY_POS = new(100000000, -100000000, 100000000);

    // Start is called before the first frame update
    void Awake()
    {
        m_particlesList = null == m_particlesList ? new(64) : m_particlesList;
        m_particleSystem = GetComponent<ParticleSystem>();

        InternalAwake();
    }

    abstract protected void InternalAwake();

    void OnParticleTrigger()
    {
        if (m_particlesList == null)
            return;

        int numEnter = m_particleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, m_particlesList, out var colliderData);

        for (int i = 0; i < numEnter; ++i)
        {
            ParticleSystem.Particle particle = m_particlesList[i];
            int colliderCount = colliderData.GetColliderCount(i);
            for (int j = 0; j < colliderCount; ++j)
            {
                GameObject hitObject = colliderData.GetCollider(i, j).gameObject;
                CollisionBehaviour(ref particle, hitObject);
                m_particlesList[i] = particle;
            }
        }

        m_particleSystem.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, m_particlesList);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    abstract protected void CollisionBehaviour(ref ParticleSystem.Particle particle, GameObject hitObject);
}
