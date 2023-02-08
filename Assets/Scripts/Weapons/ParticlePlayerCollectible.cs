using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePlayerCollectible : ParticleTrigger
{
    [SerializeField]
    private float m_numPoints;

    [SerializeField]
    private CollectibleType m_type;

    [SerializeField]
    private float m_destroyTime;
    [SerializeField]
    private ParticleHoming m_particleHoming;

    // Start is called before the first frame update
    protected override void InternalAwake()
    {
        m_particleHoming = GetComponent<ParticleHoming>();
    }

    protected void Start()
    {
        Transform player = GameManager.gameManager.GetPlayer();
        m_particleSystem.trigger.AddCollider(player);
        m_particleHoming.SetTarget(player);
    }

    private void OnParticleSystemStopped()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable() {
        Transform player = GameManager.gameManager.GetPlayer();
        m_particleHoming.SetTarget(player);
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
