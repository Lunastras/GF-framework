using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static Unity.Mathematics.math;

public class TurretWeapons : NetworkBehaviour
{
    [SerializeField]
    private WeaponTurretPhase[] m_turretPhases = null;

    [SerializeField]
    private bool m_autoPlay = false;

    [SerializeField]
    private StatsCharacter m_statsCharacter = null;

    protected FireType m_fireType = FireType.MAIN;

    private static readonly float[] DEFAULT_WAIT_TIME = { 0 };

    private int m_currentPhaseIndex = 0;

    private bool m_firing = false;

    private float m_timeUntilPlay = 0;
    private int m_requestedPhase = 0;

    private bool m_delayForcePlay = true;

    public bool DestoryWhenDone = false;

    public GameObject m_objectToDestroy = null;

    protected GfMovementGeneric m_movementParent = null;

    private PriorityValue<float> m_speedMultiplier = new(1);
    private PriorityValue<float> m_damageMultiplier = new(1);
    private PriorityValue<float> m_fireRateMultiplier = new(1);

    private static readonly Vector3 DESTROY_POSITION = new Vector3(9999, 9999, 9999);

    // Start is called before the first frame update
    void Start()
    {
        if (m_statsCharacter == null) m_statsCharacter = GetComponent<StatsCharacter>();

        for (int i = 0; i < m_turretPhases.Length; ++i)
        {
            var weapons = m_turretPhases[i].weapons;
            int weaponsLength = weapons.Length;

            for (int j = 0; j < weaponsLength; ++j)
            {
                //weapons[j].m_parentDamageSource = this;
                weapons[j].SetLoadoutWeaponIndex(j);
                weapons[j].SetLoadoutCount(weaponsLength);
                weapons[j].SetLoadoutIndex(i);
                weapons[j].WasSwitchedOn();
            }
        }
    }

    protected void OnEnable()
    {
        if (m_statsCharacter)
        {
            for (int i = 0; i < m_turretPhases.Length; ++i)
            {
                var weapons = m_turretPhases[i].weapons;
                int weaponsLength = weapons.Length;

                for (int j = 0; j < weaponsLength; ++j)
                {
                    weapons[j].WasSwitchedOn();
                }
            }
        }
    }

    private void Update()
    {
        if (GfServerManager.HasAuthority || !HasNetworkObject)
        {
            if (DestoryWhenDone && !IsAlive())
            {
                GfPooling.Destroy(m_objectToDestroy);
            }
            else
            {
                if (m_autoPlay && m_firing)
                {
                    if (!IsFiring(true))
                    { //phase ended
                        ++m_currentPhaseIndex;

                        bool playNext = m_currentPhaseIndex < m_turretPhases.Length;

                        if (m_currentPhaseIndex >= m_turretPhases.Length)
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
    }

    public void Stop(bool killBullets)
    {
        if (m_firing && m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].weapons;

            for (int i = systems.Length - 1; i >= 0; --i)
                systems[i].StopFiring(killBullets);

            m_firing = false;
        }
    }

    public bool IsFiring(bool onlyCurentPhase = false)
    {
        bool isPlaying = false;

        if (onlyCurentPhase && m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int length = systems.Length;
            for (int i = 0; i < length && !isPlaying; ++i)
                isPlaying |= systems[i].IsFiring();
        }
        else
        {
            for (int i = 0; i < m_turretPhases.Length && !isPlaying; ++i)
            {
                WeaponGeneric[] systems = m_turretPhases[i].weapons;
                int length = systems.Length;

                for (int j = 0; j < length && !isPlaying; ++j)
                    isPlaying |= systems[j].IsFiring();
            }
        }

        return isPlaying;
    }

    private void OnDisable()
    {
        DestoryWhenDone = false;
    }

    public bool IsAlive(bool onlyCurentPhase = false)
    {
        bool isPlaying = false;

        if (onlyCurentPhase && m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int length = systems.Length;
            for (int i = 0; i < length && !isPlaying; ++i)
                isPlaying |= systems[i].IsAlive();
        }
        else
        {
            for (int i = 0; i < m_turretPhases.Length && !isPlaying; ++i)
            {
                WeaponGeneric[] systems = m_turretPhases[i].weapons;
                int length = systems.Length;

                for (int j = 0; j < length && !isPlaying; ++j)
                    isPlaying |= systems[j].IsAlive();
            }
        }

        return isPlaying;
    }

    public void SetSeed(uint seed)
    {
        for (int i = 0; i < m_turretPhases.Length; ++i)
        {
            WeaponGeneric[] systems = m_turretPhases[i].weapons;
            int length = systems.Length;

            for (int j = 0; j < length; ++j)
                systems[j].SetSeed(seed);
        }
    }

    public void SetFireType(FireType fireType)
    {
        if (m_firing && m_fireType != fireType)
        {
            m_fireType = fireType;
            Play(true, m_currentPhaseIndex);
        }

        m_fireType = fireType;
    }

    public FireType GetFireType()
    {
        return m_fireType;
    }

    public void SetCurrentPhase(int phase)
    {
        m_currentPhaseIndex = phase;
        if (m_firing) Play(false, phase);
    }

    public int GetCurrentPhase()
    {
        return m_currentPhaseIndex;
    }

    public WeaponGeneric GetWeapon(int phaseIndex, int weaponIndex)
    {
        return m_turretPhases[phaseIndex].weapons[weaponIndex];
    }

    public int GetNumPhases()
    {
        return m_turretPhases.Length;
    }

    public int GetNumWeapons(int phaseIndex)
    {
        return m_turretPhases[phaseIndex].weapons.Length;
    }

    public GfMovementGeneric GetMovementParent()
    {
        return m_movementParent;
    }

    public void SetMovementParent(GfMovementGeneric parent)
    {
        for (int i = 0; i < m_turretPhases.Length; ++i)
        {
            WeaponGeneric[] systems = m_turretPhases[i].weapons;
            int length = systems.Length;

            for (int j = 0; j < length; ++j)
                systems[j].SetMovementParent(parent);
        }
    }

    public StatsCharacter GetStatsCharacter() { return m_statsCharacter; }

    public void SetStatsCharacter(StatsCharacter character)
    {
        m_statsCharacter = character;
        for (int i = 0; i < m_turretPhases.Length; ++i)
        {
            WeaponGeneric[] systems = m_turretPhases[i].weapons;
            int length = systems.Length;

            for (int j = 0; j < length; ++j)
                systems[j].SetStatsCharacter(character);
        }
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

    /*
    public void Play(bool forcePlay, float delay, int phase)
    {
        m_currentPhaseIndex = phase;
        m_timeUntilPlay = delay;
        m_requestedPhase = phase;
        m_delayForcePlay = forcePlay;
    }*/

    public void Play(bool forcePlay = false, int phase = -1)
    {
        if (GfServerManager.HasAuthority)
        {
            InternalPlay(forcePlay, phase);
            PlayClientRpc(forcePlay, phase);
        }
        else if (!IsSpawned)
        {
            InternalPlay(forcePlay, phase);
        }
    }

    [ClientRpc]
    protected void PlayClientRpc(bool forcePlay, int phase)
    {
        if (!NetworkManager.IsHost && !NetworkManager.IsServer) InternalPlay(forcePlay, phase);
    }

    protected void InternalPlay(bool forcePlay, int phase)
    {
        bool phaseChanged = phase != m_currentPhaseIndex;
        if (m_turretPhases.Length > 0)
            phase %= m_turretPhases.Length;

        m_firing &= !forcePlay;

        if (phaseChanged)
        {
            Stop(false);
            m_firing = false;
        }

        m_currentPhaseIndex = phase;
        if (!m_firing && m_turretPhases.Length > 0)
        {
            m_firing = true;
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].weapons;

            for (int i = 0; i < systems.Length; ++i)
            {
                float damageMultiplier = 1;
                float fireRateMultiplier = 1;
                float speedMultiplier = 1;

                if (0 < m_turretPhases[m_currentPhaseIndex].DamageMultipliers.Length)
                    damageMultiplier = m_turretPhases[m_currentPhaseIndex].DamageMultipliers[min(i, m_turretPhases[m_currentPhaseIndex].DamageMultipliers.Length - 1)];

                if (0 < m_turretPhases[m_currentPhaseIndex].FireRateMultipliers.Length)
                    fireRateMultiplier = m_turretPhases[m_currentPhaseIndex].FireRateMultipliers[min(i, m_turretPhases[m_currentPhaseIndex].FireRateMultipliers.Length - 1)];

                if (0 < m_turretPhases[m_currentPhaseIndex].SpeedMultipliers.Length)
                    speedMultiplier = m_turretPhases[m_currentPhaseIndex].SpeedMultipliers[min(i, m_turretPhases[m_currentPhaseIndex].SpeedMultipliers.Length - 1)];

                systems[i].SetDamageMultiplier(m_damageMultiplier * damageMultiplier);
                systems[i].SetFireRateMultiplier(m_fireRateMultiplier * fireRateMultiplier);
                systems[i].SetSpeedMultiplier(m_speedMultiplier * speedMultiplier);
                systems[i].Fire();
            }
        }
    }

    public void SetRotation(Quaternion rotation)
    {
        if (m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int systemsLength = systems.Length;
            for (int i = 0; i < systemsLength; ++i)
                systems[i].transform.rotation = rotation;
        }
    }

    public void SetTarget(Transform target)
    {
        if (m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int systemsLength = systems.Length;
            for (int i = 0; i < systemsLength; ++i)
                systems[i].SetTarget(target);
        }
    }

    public void DestroyWhenDone(bool stopParticles = false)
    {
        DestroyWhenDone(gameObject);
    }

    public void DestroyWhenDone(GameObject obj, bool killBullets = false)
    {
        DestoryWhenDone = true;

        m_objectToDestroy = obj;
        Stop(killBullets);
        obj.transform.position = DESTROY_POSITION;
        obj.transform.parent = null;
    }

    public virtual PriorityValue<float> GetSpeedMultiplier() { return m_speedMultiplier; }

    public virtual bool SetSpeedMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = m_speedMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue && m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int weaponCount = systems.Length;
            for (int i = 0; i < weaponCount; ++i)
            {
                systems[i].SetSpeedMultiplier(multiplier);
            }
        }

        return changedValue;
    }

    public virtual PriorityValue<float> GetFireRateMultiplier() { return m_fireRateMultiplier; }
    public virtual bool SetFireRateMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = m_fireRateMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue && m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int weaponCount = systems.Length;
            for (int i = 0; i < weaponCount; ++i)
            {
                systems[i].SetFireRateMultiplier(multiplier);
            }
        }

        return changedValue;
    }

    public virtual PriorityValue<float> GetDamageMultiplier() { return m_damageMultiplier; }
    public virtual bool SetDamageMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        bool changedValue = m_damageMultiplier.SetValue(multiplier, priority, overridePriority);
        if (changedValue && m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].weapons;
            int weaponCount = systems.Length;
            for (int i = 0; i < weaponCount; ++i)
            {
                systems[i].SetDamageMultiplier(multiplier);
            }
        }

        return changedValue;
    }
}

[System.Serializable]
public class WeaponTurretPhase
{
    [SerializeReference]
    public WeaponGeneric[] weapons;
    public float[] DamageMultipliers;
    public float[] SpeedMultipliers;
    public float[] FireRateMultipliers;
    // public float timeUntilNextPhase;
}
