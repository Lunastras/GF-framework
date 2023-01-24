using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameParticles : MonoBehaviour
{
    [SerializeField]
    protected ParticleSystem m_deathDustInstance;
    [SerializeField]
    protected ParticleSystem m_particleDustInstance;
    [SerializeField]
    protected GameObject m_powerItemsPrefab;

    private static GameParticles m_instance;

    private static Transform m_transDeathDust;
    private static Transform m_transParticleDust;


    // Start is called before the first frame update
    void Start()
    {
        if (m_instance) Destroy(m_instance);
        m_instance = this;

        if (null == m_deathDustInstance || null == m_powerItemsPrefab || null == m_deathDustInstance)
            Debug.LogError("One of the particle systems is null.");

        m_transDeathDust = m_deathDustInstance.transform;
        m_transParticleDust = m_particleDustInstance.transform;

    }

    public static void PlayDeathDust(Vector3 position)
    {
        m_transDeathDust.position = position;
        m_instance.m_deathDustInstance.Play(true);
    }

    public static void PlayParticleDust(Vector3 position, Quaternion rotation)
    {
        m_transParticleDust.position = position;
        m_transParticleDust.rotation = rotation;
        m_instance.m_particleDustInstance.Play(true);
    }

    public static void PlayParticleDust(Vector3 position, Vector3 normal)
    {
        PlayParticleDust(position, Quaternion.LookRotation(normal));
    }

    public static void PlayPowerItems(Vector3 position, int numberToEmit, GfMovementGeneric movement = null)
    {
        GameObject spawnedEmitter = GfPooling.Instantiate(m_instance.m_powerItemsPrefab);

        spawnedEmitter.transform.position = position;

        ParticlePlayerCollectible particleCollectible = spawnedEmitter.GetComponent<ParticlePlayerCollectible>();
        if (particleCollectible)
        {
            particleCollectible.GetParticleSystem().Emit(numberToEmit);

            ParticleGravity particleGravity = particleCollectible.GetParticleGravity();
            if (particleGravity)
            {
                Transform sphericalParent = movement.GetParentSpherical();
                if (sphericalParent)
                    particleGravity.SetSphericalParent(sphericalParent);
                else
                    particleGravity.SetDefaultGravityDir(-movement.UpVecEffective());

                particleGravity.SetGravityCoef(movement.GetGravityCoef());
            }
        }
    }
}
