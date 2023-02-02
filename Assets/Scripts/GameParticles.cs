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
    protected ParticleSystem m_particleDmgNumbersInstance;
    [SerializeField]
    protected GameObject m_powerItemsPrefab;


    private static GameParticles m_instance;

    private static Transform m_transDeathDust;
    private static Transform m_transParticleDust;
    private static Transform m_transDmgNumbersInstance;

    // Start is called before the first frame update
    void Start()
    {
        if (m_instance) Destroy(m_instance);
        m_instance = this;

        if (null == m_deathDustInstance || null == m_powerItemsPrefab || null == m_deathDustInstance)
            Debug.LogError("One of the particle systems is null.");

        m_transDeathDust = m_deathDustInstance.transform;
        m_transParticleDust = m_particleDustInstance.transform;
        m_transDmgNumbersInstance = m_particleDmgNumbersInstance.transform;
    }

    public static void PlayDamageNumbers(Vector3 position, float value, Vector3 upVec)
    {
        ParticleSystem.EmitParams emitParams = new();
        emitParams.ResetStartLifetime();
        emitParams.ResetAngularVelocity();
        emitParams.ResetAxisOfRotation();
        emitParams.ResetMeshIndex();
        //emitParams.ResetPosition();
        emitParams.ResetRandomSeed();
        emitParams.ResetRotation();
        emitParams.ResetStartColor();
        emitParams.ResetStartSize();
        //emitParams.ResetVelocity();

        emitParams.velocity = Random.insideUnitSphere.normalized * value;
        emitParams.position = position;
        emitParams.axisOfRotation = upVec;

        m_instance.m_particleDmgNumbersInstance.Emit(emitParams, 1);
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
        if (GfPooling.PoolSizeAvailable(m_instance.m_powerItemsPrefab) == 0)
            GfPooling.Pool(m_instance.m_powerItemsPrefab, 1);

        List<GameObject> emitors = GfPooling.GetPoolList(m_instance.m_powerItemsPrefab);
        ParticlePlayerCollectible spawnedEmitter = null;

        if (null != emitors)
        {
            int count = emitors.Count;
            for (int i = 0; i < count; ++i)
            {
                ParticlePlayerCollectible currentSystem = emitors[i].GetComponent<ParticlePlayerCollectible>(); //if this is null, something is very wrong

                ParticleGravity pg = currentSystem.GetParticleGravity();
                bool hasSameGravity = pg.HasSameGravity(movement);
                if (hasSameGravity || !emitors[i].activeSelf)
                {
                    if (!hasSameGravity)
                        pg.CopyGravity(movement);
                    else
                        pg.gameObject.SetActive(true);

                    spawnedEmitter = currentSystem;

                    break;
                }
            }
        }

        if (null == spawnedEmitter)
        {
            spawnedEmitter = GfPooling.PoolInstantiate(m_instance.m_powerItemsPrefab).GetComponent<ParticlePlayerCollectible>();
            spawnedEmitter.GetParticleGravity().CopyGravity(movement);
        }

        spawnedEmitter.transform.position = position;
        spawnedEmitter.GetParticleSystem().Emit(numberToEmit);
    }
}
