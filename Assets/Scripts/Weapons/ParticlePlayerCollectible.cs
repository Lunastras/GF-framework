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

    // Start is called before the first frame update
    protected override void InternalAwake()
    {
        m_particleHoming = GetComponent<ParticleHoming>();
    }

    protected void Start()
    {
        Transform player = GameManager.GetPlayer();
        m_particleSystem.trigger.AddCollider(player);
        m_particleHoming.SetTarget(player);
    }

    private void OnParticleSystemStopped()
    {
        m_particleHoming.ResetToDefault();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (m_particleHoming && GameManager.Manager)
        {
            Transform player = GameManager.GetPlayer();
            m_particleHoming.SetTarget(player);
        }
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
