using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

using System.Runtime.CompilerServices;

public abstract class ParticleTrigger : MonoBehaviour
{
    protected ParticleSystem m_particleSystem;

    private static List<ParticleSystem.Particle> m_particlesList = null;

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

        int numEnter = m_particleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, m_particlesList, out var colliderData);
        Debug.Log("trigger called bbyy, numenter was:  " + numEnter);

        for (int i = 0; i < numEnter; ++i)
        {
            ParticleSystem.Particle particle = m_particlesList[i];
            if (colliderData.GetColliderCount(i) > 0)
            {
                GameObject hitObject = colliderData.GetCollider(i, 0).gameObject;
                Debug.Log("Okee let's go through all of them, I HIT: " + hitObject.name);

                CollisionBehaviour(ref particle, hitObject);

                m_particlesList[i] = particle;
            }
        }

        m_particleSystem.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, m_particlesList);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    abstract protected void CollisionBehaviour(ref ParticleSystem.Particle particle, GameObject hitObject);
}
