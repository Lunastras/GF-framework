using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePlayerCollectible : ParticleTrigger
{
    [SerializeField]
    private float numPoints;

    [SerializeField]
    private CollectibleType type;

    // Start is called before the first frame update
    protected override void InternalAwake()
    {
        
    }

    private void Start() {
        Collider cool = StatsPlayer.instance.GetComponent<Collider>();
        m_particleSystem.trigger.AddCollider(cool);
    }

    protected override void CollisionBehaviour(ref ParticleSystem.Particle particle, GameObject hitObject)
    {
        StatsPlayer playerStats = hitObject.GetComponent<StatsPlayer>();

        if(playerStats) {
            playerStats.AddPoints(type, numPoints);
            particle.position = DESTROY_POS;
            particle.velocity = Vector3.zero;
        }
    }
}
