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

    [SerializeField]
    protected GameObject m_gravesParticlesPrefab;

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

    public static void PlayParticleDust(Vector3 position, Vector3 normal, int numParticles = 10)
    {
        ParticleSystem.EmitParams emitParams = new();
        // emitParams.ResetStartLifetime();
        // emitParams.ResetAngularVelocity();
        //emitParams.ResetAxisOfRotation();
        //emitParams.ResetMeshIndex();
        //emitParams.ResetPosition();
        // emitParams.ResetRandomSeed();
        //emitParams.ResetRotation();
        // emitParams.ResetStartColor();
        //emitParams.ResetStartSize();
        //emitParams.ResetVelocity();

        emitParams.position = position;
        //emitParams.axisOfRotation = normal;

        m_transParticleDust.rotation = Quaternion.LookRotation(normal);

        m_instance.m_particleDustInstance.Emit(emitParams, numParticles);
    }

    private static void EmitHomingParticleSystem(GameObject particleSystemPrefab, Vector3 position, int numberToEmit, GfMovementGeneric movement = null)
    {
        if (particleSystemPrefab)
        {
            List<GameObject> emitters = GfPooling.GetPoolList(particleSystemPrefab);
            ParticleHoming spawnedEmitter = null;

            if (null != emitters)
            {
                int count = emitters.Count;
                for (int i = 0; i < count; ++i)
                {
                    ParticleHoming currentSystem = emitters[i].GetComponent<ParticleHoming>(); //if this is null, something is very wrong

                    bool hasSameGravity = currentSystem.HasSameGravity(movement);
                    if (hasSameGravity || !emitters[i].activeSelf)
                    {
                        if (movement && !hasSameGravity)
                            currentSystem.CopyGravity(movement);

                        spawnedEmitter = currentSystem;
                        break;
                    }
                }
            }

            if (null == spawnedEmitter)
            {
                GfPooling.Pool(particleSystemPrefab, 1);
                emitters = GfPooling.GetPoolList(particleSystemPrefab);
                spawnedEmitter = emitters[emitters.Count - 1].GetComponent<ParticleHoming>();
                spawnedEmitter.CopyGravity(movement);
            }

            if (!spawnedEmitter.gameObject.activeSelf)
                spawnedEmitter.gameObject.SetActive(true);


            ParticleSystem.EmitParams emitParams = new();
            emitParams.applyShapeToPosition = true;
            emitParams.position = position;
            spawnedEmitter.GetParticleSystem().Emit(emitParams, numberToEmit);
        }
    }

    public static void SpawnPowerItems(Vector3 position, int numberToEmit, GfMovementGeneric movement = null)
    {
        EmitHomingParticleSystem(m_instance.m_powerItemsPrefab, position, numberToEmit, movement);
    }

    public static void SpawnGrave(Vector3 position, GfMovementGeneric movement = null)
    {
        EmitHomingParticleSystem(m_instance.m_gravesParticlesPrefab, position, 1, movement);
    }
}
