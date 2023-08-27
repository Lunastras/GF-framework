using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using static Unity.Mathematics.math;
using Unity.Mathematics;


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

    private float m_timeUntilPlay = 0;
    private int m_requestedPhase = 0;

    private bool m_delayForcePlay = true;

    public bool DestoryWhenDone = false;

    public GameObject m_objectToDestroy = null;

    protected GfMovementGeneric m_movementParent = null;

    protected float[] m_weaponMultipliers = Enumerable.Repeat(1f, (int)WeaponMultiplierTypes.COUNT_TYPES).ToArray();

    private static readonly Vector3 DESTROY_POSITION = new Vector3(9999, 9999, 9999);

    // Start is called before the first frame update
    void Start()
    {
        if (m_statsCharacter == null) m_statsCharacter = GetComponent<StatsCharacter>();

        for (int i = 0; i < m_turretPhases.Length; ++i)
        {
            var weapons = m_turretPhases[i].Weapons;
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
                var weapons = m_turretPhases[i].Weapons;
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
                if (m_autoPlay && !IsPlaying(true))
                { //phase ended
                    ++m_currentPhaseIndex;

                    bool playNext = m_currentPhaseIndex < m_turretPhases.Length;

                    if (m_currentPhaseIndex >= m_turretPhases.Length)
                        m_currentPhaseIndex = 0;
                    else
                        playNext = false;

                    if (playNext) Play();
                }

                if (m_timeUntilPlay > 0)
                {
                    m_timeUntilPlay -= Time.deltaTime;
                    if (m_timeUntilPlay <= 0) Play(m_delayForcePlay, m_requestedPhase);
                }
            }
        }
    }

    public void Stop(bool killBullets, bool allPhases = true)
    {
        if (GfServerManager.HasAuthority)
        {
            InternalStop(-1, killBullets, allPhases);
            StopClientRpc(-1, killBullets, allPhases);
        }
        else if (!IsSpawned)
        {
            InternalStop(-1, killBullets, allPhases);
        }
    }

    public void Stop(int phaseToStop, bool killBullets)
    {
        if (GfServerManager.HasAuthority)
        {
            InternalStop(phaseToStop, killBullets, false);
            StopClientRpc(phaseToStop, killBullets, false);
        }
        else if (!IsSpawned)
        {
            InternalStop(phaseToStop, killBullets, false);
        }
    }

    [ClientRpc]
    protected void StopClientRpc(int phaseToStop, bool killBullets, bool allPhases)
    {
        if (!NetworkManager.IsHost && !NetworkManager.IsServer) InternalStop(phaseToStop, killBullets, allPhases);
    }

    protected void InternalStop(int phaseToStop, bool killBullets, bool allPhases)
    {
        if (allPhases)
        {
            for (int i = 0; i < m_turretPhases.Length; ++i)
            {
                InternalStop(i, killBullets);
            }
        }
        else
        {
            if (phaseToStop == -1)
                phaseToStop = m_currentPhaseIndex;

            InternalStop(phaseToStop, killBullets);
        }
    }

    protected void InternalStop(int phaseToStop, bool killBullets)
    {
        if (m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[phaseToStop].Weapons;

            for (int i = systems.Length - 1; i >= 0; --i)
                systems[i].StopFiring(killBullets);
        }
    }


    public bool IsPlaying(bool onlyCurentPhase = false)
    {
        bool isPlaying = false;

        if (onlyCurentPhase && m_turretPhases.Length > 0)
        {
            isPlaying = IsPlayingPhase(m_currentPhaseIndex);
        }
        else
        {
            for (int i = 0; i < m_turretPhases.Length && !isPlaying; ++i)
            {
                isPlaying |= IsPlayingPhase(i);
            }
        }

        return isPlaying;
    }

    protected bool IsPlayingPhase(int phase)
    {
        bool isPlaying = false;
        WeaponGeneric[] systems = m_turretPhases[phase].Weapons;
        int length = systems.Length;

        for (int j = 0; j < length && !isPlaying; ++j)
            isPlaying |= systems[j].IsFiring();

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
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].Weapons;
            int length = systems.Length;
            for (int i = 0; i < length && !isPlaying; ++i)
                isPlaying |= systems[i].IsAlive();
        }
        else
        {
            for (int i = 0; i < m_turretPhases.Length && !isPlaying; ++i)
            {
                WeaponGeneric[] systems = m_turretPhases[i].Weapons;
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
            WeaponGeneric[] systems = m_turretPhases[i].Weapons;
            int length = systems.Length;

            for (int j = 0; j < length; ++j)
                systems[j].SetSeed(seed);
        }
    }

    public void SetFireType(FireType fireType)
    {
        if (m_fireType != fireType)
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

    public int GetCurrentPhase()
    {
        return m_currentPhaseIndex;
    }

    public WeaponGeneric GetWeapon(int phaseIndex, int weaponIndex)
    {
        return m_turretPhases[phaseIndex].Weapons[weaponIndex];
    }

    public int GetNumPhases()
    {
        return m_turretPhases.Length;
    }

    public int GetNumWeapons(int phaseIndex)
    {
        return m_turretPhases[phaseIndex].Weapons.Length;
    }

    public GfMovementGeneric GetMovementParent()
    {
        return m_movementParent;
    }

    public void SetMovementParent(GfMovementGeneric parent)
    {
        for (int i = 0; i < m_turretPhases.Length; ++i)
        {
            WeaponGeneric[] systems = m_turretPhases[i].Weapons;
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
            WeaponGeneric[] systems = m_turretPhases[i].Weapons;
            int length = systems.Length;

            for (int j = 0; j < length; ++j)
                systems[j].SetStatsCharacter(character);
        }
    }

    public void Play(bool forcePlay = false, int phase = -1, bool stopPlayingPhases = true)
    {
        if (GfServerManager.HasAuthority)
        {
            InternalPlay(forcePlay, phase, stopPlayingPhases);
            PlayClientRpc(forcePlay, phase, stopPlayingPhases);
        }
        else if (!IsSpawned)
        {
            InternalPlay(forcePlay, phase, stopPlayingPhases);
        }
    }

    [ClientRpc]
    protected void PlayClientRpc(bool forcePlay, int phase, bool stopPlayingPhases)
    {
        if (!NetworkManager.IsHost && !NetworkManager.IsServer) InternalPlay(forcePlay, phase, stopPlayingPhases);
    }

    protected void InternalPlay(bool forcePlay, int phase, bool stopPlayingPhases)
    {
        bool phaseChanged = phase != m_currentPhaseIndex;
        if (m_turretPhases.Length > 0)
            phase %= m_turretPhases.Length;

        bool phaseAlreadyPlaying = IsPlayingPhase(phase);

        phaseAlreadyPlaying &= !forcePlay;

        if (phaseChanged && stopPlayingPhases)
        {
            Stop(false);
            phaseAlreadyPlaying = false;
        }

        m_currentPhaseIndex = phase;

        if (!phaseAlreadyPlaying && m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].Weapons;

            for (int i = 0; i < systems.Length; ++i)
            {
                systems[i].Fire();
            }
        }
    }

    public void SetRotation(Quaternion rotation)
    {
        if (m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].Weapons;
            int systemsLength = systems.Length;
            for (int i = 0; i < systemsLength; ++i)
                systems[i].transform.rotation = rotation;
        }
    }

    public void SetTarget(Transform target)
    {
        if (m_turretPhases.Length > 0)
        {
            WeaponGeneric[] systems = m_turretPhases[m_currentPhaseIndex].Weapons;
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

    public void EraseAllBullets(StatsCharacter characterResponsible, float3 centerOfErase, float speedFromCenter, float eraseRadius)
    {
        for (int i = 0; i < m_turretPhases.Length; ++i)
        {
            WeaponGeneric[] systems = m_turretPhases[i].Weapons;
            int length = systems.Length;

            for (int j = 0; j < length; ++j)
                systems[j].EraseAllBullets(characterResponsible, centerOfErase, speedFromCenter, eraseRadius);
        }
    }

    public virtual float GetMultiplier(WeaponMultiplierTypes multiplierType) { return m_weaponMultipliers[(int)multiplierType]; }

    public virtual bool SetMultiplier(WeaponMultiplierTypes multiplierType, float multiplier)
    {
        bool changedValue = m_weaponMultipliers[(int)multiplierType] != multiplier;
        if (changedValue)
        {
            float extraMultiplier;
            for (int i = 0; i < m_turretPhases.Length; ++i)
            {
                WeaponGeneric[] systems = m_turretPhases[i].Weapons;
                int length = systems.Length;

                for (int j = 0; j < length; ++j)
                {
                    if (m_turretPhases[i].WeaponMultipliers.Length > j)
                        extraMultiplier = m_turretPhases[i].WeaponMultipliers[j];
                    else
                        extraMultiplier = 1;

                    systems[i].SetMultiplier(multiplierType, multiplier * extraMultiplier);
                }
            }
        }

        m_weaponMultipliers[(int)multiplierType] = multiplier;
        return changedValue;
    }
}

[System.Serializable]
public class WeaponTurretPhase
{
    [SerializeReference]
    public WeaponGeneric[] Weapons = new WeaponGeneric[1];

    [SerializeReference]
    public float[] WeaponMultipliers = Enumerable.Repeat(1f, (int)WeaponMultiplierTypes.COUNT_TYPES).ToArray();
}
