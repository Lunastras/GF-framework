using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleTurret : MonoBehaviour
{
    [SerializeField]
    private ParticleTurretPhase[] m_turretPhases;

    [SerializeField]
    private float[] m_timeBetweenPhases;

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

    private float m_timeUntilUnpause = 0;

    private bool m_delayForcePlay = true;

    // Start is called before the first frame update
    void Start()
    {
        if (m_timeBetweenPhases.Length == 0)
            m_timeBetweenPhases = DEFAULT_WAIT_TIME;

        if (null == m_statsCharacter)
            m_statsCharacter = GetComponent<StatsCharacter>();
    }

    private void Update()
    {
        if (m_timeUntilUnpause <= 0)
        {
            if (m_autoPlay && m_firing)
            {
                if (!IsAlive())
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
        else
        {
            m_timeUntilUnpause -= Time.deltaTime;
            if (m_timeUntilUnpause <= 0) UnPause();
        }
    }

    public void Stop()
    {
        if (m_firing)
        {
            ParticleSingleHit[] systems = m_turretPhases[m_currentPhaseIndex].particleSystems;

            for (int i = systems.Length - 1; i >= 0; --i)
                systems[i].Stop();

            m_firing = false;
        }
    }

    public bool IsAlive(bool allPhases = false)
    {
        bool isPlaying = false;

        if (allPhases)
        {
            for (int i = 0; i < m_turretPhases.Length && !isPlaying; ++i)
            {
                ParticleSingleHit[] systems = m_turretPhases[i].particleSystems;
                int length = systems.Length;

                for (int j = 0; j < length && !isPlaying; ++j)
                {
                    isPlaying |= systems[j].m_particleSystem.IsAlive(true);
                }
            }
        }
        else
        {
            ParticleSingleHit[] systems = m_turretPhases[m_currentPhaseIndex].particleSystems;
            int length = systems.Length;
            for (int i = 0; i < length && !isPlaying; ++i)
            {
                isPlaying |= systems[i].m_particleSystem.IsAlive(true);
            }
        }

        return isPlaying;
    }

    public int GetNumPhases()
    {
        return m_turretPhases.Length;
    }

    public void Pause(float duration = float.MaxValue)
    {
        if (duration > 0)
        {
            m_timeUntilUnpause = duration;

            ParticleSingleHit[] systems = m_turretPhases[m_currentPhaseIndex].particleSystems;

            foreach (ParticleSingleHit system in systems)
            {
                system.m_particleSystem.Pause(true);
            }
        }
    }

    public void UnPause()
    {
        if (m_timeUntilUnpause > 0)
        {
            m_timeUntilUnpause = 0;

            ParticleSingleHit[] systems = m_turretPhases[m_currentPhaseIndex].particleSystems;
            int length = systems.Length;
            for (int i = 0; i < length; ++i)
                systems[i].m_particleSystem.Play();
        }
    }

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

        if (!m_firing)
        {
            m_firing = true;
            ParticleSingleHit[] systems = m_turretPhases[m_currentPhaseIndex].particleSystems;
            int length = systems.Length;
            for (int i = 0; i < length; ++i)
                systems[i].m_particleSystem.Play();
        }
    }

    public void SetStatsCharacter(StatsCharacter stats)
    {
        m_statsCharacter = stats;
        int phasesLength = m_turretPhases.Length;
        for (int i = 0; i < phasesLength; ++i)
        {
            ParticleTurretPhase phase = m_turretPhases[i];
            ParticleSingleHit[] systems = m_turretPhases[i].particleSystems;
            int systemsLength = systems.Length;
            for (int j = 0; j < systemsLength; ++j)
                systems[j].SetStatsCharacter(stats);

        }
    }

    public void SetRotation(Quaternion rotation)
    {
        ParticleSingleHit[] systems = m_turretPhases[m_currentPhaseIndex].particleSystems;
        int systemsLength = systems.Length;
        for (int i = 0; i < systemsLength; ++i)
            systems[i].transform.rotation = rotation;
    }

    public void SetTarget(Transform target)
    {
        ParticleSingleHit[] systems = m_turretPhases[m_currentPhaseIndex].particleSystems;
        int systemsLength = systems.Length;
        for (int i = 0; i < systemsLength; ++i)
            systems[i].Target = target;
    }
}

[System.Serializable]
public class ParticleTurretPhase
{
    public ParticleSingleHit[] particleSystems;
}
