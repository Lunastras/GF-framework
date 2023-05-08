using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePlayerCollectible : ParticleTrigger
{
    [SerializeField]
    private float m_numPoints = 0;

    [SerializeField]
    private CollectibleType m_type = CollectibleType.HEALTH;

    [SerializeField]
    private float m_destroyTime = 0;

    [SerializeField]
    private ParticleHoming m_particleHoming = null;

    private Transform m_player = null;

    // Start is called before the first frame update
    protected override void InternalAwake()
    {
        if (null == m_particleHoming) m_particleHoming = GetComponent<ParticleHoming>();
    }

    private void FixedUpdate()
    {
        if (m_particleHoming && GameManager.Instance && m_player != GameManager.GetPlayer())
        {
            if (m_player) m_particleSystem.trigger.RemoveCollider(m_player);

            m_player = GameManager.GetPlayer();
            m_particleHoming.SetTarget(m_player);
            m_particleSystem.trigger.AddCollider(m_player);
        }
    }

    private void OnParticleSystemStopped()
    {
        gameObject.SetActive(false);
    }

    public ParticleHoming GetParticleHoming() { return m_particleHoming; }
    public ParticleSystem GetParticleSystem() { return m_particleSystem; }

    protected override void CollisionBehaviour(ref ParticleSystem.Particle particle, GameObject hitObject)
    {
        StatsPlayer playerStats = hitObject.GetComponent<StatsPlayer>();

        if (playerStats && particle.remainingLifetime >= m_destroyTime)
        {
            playerStats.AddPoints(m_type, m_numPoints);
            particle.remainingLifetime = m_destroyTime;
        }
    }
}
