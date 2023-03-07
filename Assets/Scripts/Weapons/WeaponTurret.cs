using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTurret : MonoBehaviour
{
    [SerializeField]
    private WeaponTurretPhase[] m_turretPhases;

    [SerializeField]
    private StatsCharacter m_statsCharacter;

    [SerializeField]
    private bool m_autoPlay;

    [SerializeField]
    private bool m_autoRestarts;

    private static readonly float[] DEFAULT_WAIT_TIME = { 0 };

    private int m_currentPhaseIndex = 0;

    private bool m_firing = false;

    private float m_timeUntilPlay = 0;
    private int m_requestedPhase;

    private bool m_delayForcePlay = true;

    public bool m_destoryWhenDone = false;

    public GameObject m_objectToDestroy;

    private static readonly Vector3 DESTROY_POSITION = new Vector3(99999999, 99999999, 99999999);

    // Start is called before the first frame update
    void Start()
    {
        if (null == m_statsCharacter)
            m_statsCharacter = GetComponent<StatsCharacter>();
    }

    private void Update()
    {
        if (m_destoryWhenDone && !IsAlive(true))
        {
            GfPooling.Destroy(m_objectToDestroy);
        }
        else 
        {
            if (m_autoPlay && m_firing)
            {
                if (!IsAlive(true))
                { //phase ended
                    ++m_currentPhaseIndex;

                    bool playNext = m_currentPhaseIndex < m_turretPhases.Length;

                    if (m_currentPhaseIndex >= m_turretPhases.Length && m_autoRestarts)
                        m_currentPhaseIndex = 0;
                    else
                        playNext = false;


                    if (playNext) Play();
                }
            }

            if (m_timeUntilPlay > 0)
            {
                m_timeUntilPlay -= Time.deltaTime;
                if (m_timeUntilPlay <= 0) Play(m_delayForcePlay, m_requestedPhase);
            }
        }
    }

    public void Stop(bool withChildren = true, ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting)
    {
        if (m_firing && m_turretPhases.Length > 0)
        {
            WeaponBasic[] systems = m_turretPhases[m_currentPhaseIndex].weapons;

            for (int i = systems.Length - 1; i >= 0; --i)
                systems[i].StopFiring();

            m_firing = false;
        }
    }

    public bool IsAlive(bool onlyCurentPhase = false)
    {
        bool isPlaying = false;

        if (onlyCurentPhase && m_turretPhases.Length > 0)
        {
            WeaponBasic[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int length = systems.Length;
            for (int i = 0; i < length && !isPlaying; ++i)
                isPlaying |= systems[i].IsAlive(true);
        }
        else
        {

            for (int i = 0; i < m_turretPhases.Length && !isPlaying; ++i)
            {
                WeaponBasic[] systems = m_turretPhases[i].weapons;
                int length = systems.Length;

                for (int j = 0; j < length && !isPlaying; ++j)
                    isPlaying |= systems[j].IsAlive(true);
            }
        }

        return isPlaying;
    }

    public int GetNumPhases()
    {
        return m_turretPhases.Length;
    }

    /*

    public void Pause(float duration = float.MaxValue)
    {
        if (duration > 0)
        {
            m_timeUntilUnpause = duration;

            if (m_turretPhases.Length > 0)
            {
                WeaponParticle[] systems = m_turretPhases[m_currentPhaseIndex].weaponParticles;

                foreach (ParticleSingleHit system in systems)
                {
                    system.m_particleSystem.Pause(true);
                }
            }
        }
    }

    public void UnPause()
    {
        if (m_timeUntilUnpause > 0 && m_turretPhases.Length > 0)
        {
            m_timeUntilUnpause = 0;

            ParticleSingleHit[] systems = m_turretPhases[m_currentPhaseIndex].weaponParticles;
            int length = systems.Length;
            for (int i = 0; i < length; ++i)
                systems[i].m_particleSystem.Play();
        }
    }*/

    public void Play(bool forcePlay, float delay, int phase)
    {
        m_currentPhaseIndex = phase;
        m_timeUntilPlay = delay;
        m_requestedPhase = phase;
        m_delayForcePlay = forcePlay;
    }

    public void Play(bool forcePlay = false, int phase = -1)
    {
        // Debug.Log("I AM FIRING " + gameObject.name);
        phase = System.Math.Max(0, System.Math.Max(phase, m_currentPhaseIndex));

        bool phaseChanged = phase != m_currentPhaseIndex;
        m_firing &= !forcePlay;

        if (phaseChanged)
        {
            Stop();

            m_currentPhaseIndex = phase;
            m_firing = false;
        }

        if (!m_firing && m_turretPhases.Length > 0)
        {
            m_firing = true;
            WeaponBasic[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int length = systems.Length;
            for (int i = 0; i < length; ++i)
                systems[i].Fire();
        }
    }

    public void SetStatsCharacter(StatsCharacter stats)
    {
        m_statsCharacter = stats;
        int phasesLength = m_turretPhases.Length;
        for (int i = 0; i < phasesLength; ++i)
        {
            WeaponTurretPhase phase = m_turretPhases[i];
            WeaponBasic[] systems = m_turretPhases[i].weapons;
            int systemsLength = systems.Length;
            for (int j = 0; j < systemsLength; ++j)
                systems[j].SetStatsCharacter(stats);
        }
    }

    public void SetRotation(Quaternion rotation)
    {
        if (m_turretPhases.Length > 0)
        {
            WeaponBasic[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int systemsLength = systems.Length;
            for (int i = 0; i < systemsLength; ++i)
                systems[i].transform.rotation = rotation;
        }

    }

    public void SetTarget(Transform target)
    {
        if (m_turretPhases.Length > 0)
        {
            WeaponBasic[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int systemsLength = systems.Length;
            for (int i = 0; i < systemsLength; ++i)
                systems[i].SetTarget(target);
        }

    }

    public void DestroyWhenDone(bool stopParticles = false)
    {
        DestroyWhenDone(gameObject);
    }

    public void DestroyWhenDone(GameObject obj, bool stopParticles = false)
    {
        m_destoryWhenDone = true;
        m_objectToDestroy = obj;
        Stop();
        obj.transform.position = DESTROY_POSITION;
        obj.transform.parent = null;

        //if(stopParticles)

    }

    private void OnDisable()
    {
        SetStatsCharacter(null);
    }
}

[System.Serializable]
public class WeaponTurretPhase
{
    [SerializeReference]
    public WeaponBasic[] weapons;

   // public float timeUntilNextPhase;
}
