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

    public static void PlayDamageNumbers(Vector3 position, float value, Vector3 upVec)
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
            List<GameObject> emitters = GfPooling.GetPoolList(particleSystemPrefab);
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
                GfPooling.Pool(particleSystemPrefab, 1);
                emitters = GfPooling.GetPoolList(particleSystemPrefab);
                spawnedEmitter = emitters[emitters.Count - 1].GetComponent<ParticleHoming>();
                spawnedEmitter.CopyGravity(gravityReference);
            }

            if (!spawnedEmitter.gameObject.activeSelf)
                spawnedEmitter.gameObject.SetActive(true);


            ParticleSystem.EmitParams emitParams = new();
            emitParams.applyShapeToPosition = true;

            if (null != positions)
            {
                int countPositions = positions.Count;
                ParticleSystem ps = spawnedEmitter.GetParticleSystem();
                for (int i = 0; i < countPositions; ++i)
                {
                    emitParams.position = positions[i];
                    ps.Emit(emitParams, numberToEmit);
                }
            }
            else
            {
                emitParams.position = position;
                spawnedEmitter.GetParticleSystem().Emit(emitParams, numberToEmit);
            }

        }
    }

    public static void SpawnPowerItems(Vector3 position, int numberToEmit, GravityReference gravityReference = default)
    {
        EmitHomingParticleSystem(Instance.m_powerItemsPrefab, position, numberToEmit, gravityReference);
    }

    protected static void ClearParticleSystems(GameObject ps)
    {
        List<GameObject> emitters = GfPooling.GetPoolList(ps);
        int count = emitters.Count;
        for (int i = 0; i < count; ++i)
        {
            emitters[i].GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

    public static void DisableNonEmittingGraves()
    {
        List<GameObject> emitters = GfPooling.GetPoolList(Instance.m_gravesParticlesPrefab);
        int count = emitters.Count;
        for (int i = 0; i < count; ++i)
        {
            emitters[i].SetActive(emitters[i].GetComponent<ParticleSystem>().IsAlive(true));
        }
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
