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

    protected static GameParticles Instance;

    protected static Transform m_transDeathDust;
    protected static Transform m_transParticleDust;
    protected static Transform m_transDmgNumbersInstance;

    // Start is called before the first frame update
    protected void Start()
    {
        if (Instance) Destroy(Instance);
        Instance = this;

        if (null == m_deathDustInstance || null == m_powerItemsPrefab || null == m_deathDustInstance)
            Debug.LogError("One of the particle systems is null.");

        m_transDeathDust = m_deathDustInstance.transform;
        m_transParticleDust = m_particleDustInstance.transform;
        m_transDmgNumbersInstance = m_particleDmgNumbersInstance.transform;
    }

    public static void PlayDamageNumbers(Vector3 position, float value, Vector3 upVec, float emissionRadius = 1)
    {
        if (value > 0)
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

            emissionRadius *= 0.5f;
            GfcTools.Add3(ref position, Random.onUnitSphere * emissionRadius);
            emitParams.velocity = Random.insideUnitSphere.normalized * value;
            emitParams.position = position;
            emitParams.axisOfRotation = upVec;

            Instance.m_particleDmgNumbersInstance.Emit(emitParams, 1);
        }
    }

    public static void PlayDeathDust(Vector3 position)
    {
        m_transDeathDust.position = position;
        Instance.m_deathDustInstance.Play(true);
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

        Instance.m_particleDustInstance.Emit(emitParams, numParticles);
    }

    protected static void EmitHomingParticleSystem(GameObject particleSystemPrefab, Vector3 position, int numberToEmit, GravityReference gravityReference = default, List<Vector3> positions = null)
    {
        if (particleSystemPrefab)
        {
            List<GameObject> emitters = GfcPooling.GetPoolList(particleSystemPrefab);
            ParticleHoming spawnedEmitter = null;

            if (null != emitters)
            {
                int count = emitters.Count;
                for (int i = 0; i < count; ++i)
                {
                    ParticleHoming currentSystem = emitters[i].GetComponent<ParticleHoming>(); //if this is null, something is very wrong

                    bool hasSameGravity = currentSystem.HasSameGravity(gravityReference);
                    if (hasSameGravity || !emitters[i].activeSelf)
                    {
                        if (!hasSameGravity)
                            currentSystem.CopyGravity(gravityReference);

                        spawnedEmitter = currentSystem;
                        break;
                    }
                }
            }

            if (null == spawnedEmitter)
            {
                GfcPooling.Pool(particleSystemPrefab, 1);
                emitters = GfcPooling.GetPoolList(particleSystemPrefab);
                spawnedEmitter = emitters[emitters.Count - 1].GetComponent<ParticleHoming>();
                spawnedEmitter.CopyGravity(gravityReference);
            }

            ParticleSystem ps = spawnedEmitter.GetParticleSystem();

            if (!spawnedEmitter.gameObject.activeSelf)
            {
                spawnedEmitter.gameObject.SetActive(true);

                //there is a weird bug that happens when a particle is spawned right after the object is activated
                //where the sprite of the particle will just spin. I only noticed this with my shaders, and it only applies to the first particle (and its not even consistent, sometimes the first particle will be ok)
                //emiting a particle randomly like we do here seems to fix the problem
                ParticleSystem.EmitParams emitParamsAux = new();
                emitParamsAux.startLifetime = 0;
                emitParamsAux.position = new Vector3(9999, 9999, 9999);

                ps.Emit(emitParamsAux, 1);
            }


            ParticleSystem.EmitParams emitParams = new();
            emitParams.applyShapeToPosition = true;

            if (null != positions)
            {
                int countPositions = positions.Count;
                for (int i = 0; i < countPositions; ++i)
                {
                    emitParams.position = positions[i];
                    ps.Emit(emitParams, numberToEmit);
                }
            }
            else
            {
                emitParams.position = position;
                ps.Emit(emitParams, numberToEmit);
            }
        }
    }

    public static void SpawnPowerItems(Vector3 position, int numberToEmit, GravityReference gravityReference = default)
    {
        EmitHomingParticleSystem(Instance.m_powerItemsPrefab, position, numberToEmit, gravityReference);
    }

    protected static void ClearParticleSystems(GameObject ps, bool disable = true)
    {
        List<GameObject> emitters = GfcPooling.GetPoolList(ps);
        int count = emitters.Count;
        for (int i = 0; i < count; ++i)
        {
            emitters[i].GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (disable && emitters[i].activeSelf)
            {
                emitters[i].SetActive(false);
            }
        }
    }

    public static void ClearGraves()
    {
        ClearParticleSystems(Instance.m_gravesParticlesPrefab);
    }

    public static void ClearPowerItems()
    {
        ClearParticleSystems(Instance.m_powerItemsPrefab);
    }

    public static void ClearParticles()
    {
        ClearParticleSystems(Instance.m_gravesParticlesPrefab);
        ClearParticleSystems(Instance.m_powerItemsPrefab);

        Instance.m_deathDustInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Instance.m_particleDmgNumbersInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Instance.m_particleDmgNumbersInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public static void SpawnGraves(List<Vector3> positions, GravityReference gravityReference = default)
    {
        EmitHomingParticleSystem(Instance.m_gravesParticlesPrefab, Vector3.zero, 1, gravityReference, positions);
    }

    public static void SpawnGrave(Vector3 position, GravityReference gravityReference = default)
    {
        EmitHomingParticleSystem(Instance.m_gravesParticlesPrefab, position, 1, gravityReference);
    }
}
