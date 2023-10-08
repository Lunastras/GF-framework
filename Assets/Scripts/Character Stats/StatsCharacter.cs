
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Linq;
using System;
using Unity.Mathematics;


public abstract class StatsCharacter : NetworkBehaviour
{
    [SerializeField]
    protected bool m_checkpointable = true;

    [SerializeField]
    protected NetworkVariable<float> m_maxHealth = new(100);

    [SerializeField]
    private NetworkVariable<CharacterTypes> m_characterType = new(default);

    [SerializeField]
    protected int m_threatLevel = 0;

    protected NetworkVariable<PriorityValue<float>> m_maxHealthMultiplier = new(new(1));

    protected NetworkVariable<PriorityValue<float>> m_receivedDamageMultiplier = new(new(1));

    protected bool m_isDead = false;

    protected NetworkVariable<float> m_currentHealth = new(0);

    protected NetworkTransform m_networkTransform;

    protected int[] m_characterIndexes = Enumerable.Repeat(-1, (int)CharacterIndexType.COUNT_TYPES).ToArray();

    protected bool m_initialised = false;

    protected NetworkObject m_networkObject;

    protected int m_enemiesEngagingCount = 0;

    public Action<StatsCharacter, ulong, bool, int, int> OnKilled = null;

    protected bool HasAuthority
    {
        get
        {
            return NetworkManager.Singleton && NetworkManager.Singleton.IsServer;
        }
    }

    // Start is called before the first frame update
    protected void Start()
    {
        m_networkTransform = GetComponent<NetworkTransform>();
        m_networkObject = GetComponent<NetworkObject>();
        m_initialised = true;
        Init();
    }

    protected virtual void Init()
    {
        if (m_initialised)
        {
            HostilityManager.AddCharacter(this);

            if (m_networkTransform) m_networkTransform.enabled = true;

            if (HasAuthority && m_networkObject && !m_networkObject.IsSpawned)
                m_networkObject.Spawn();

            if (HasAuthority) m_currentHealth.Value = GetMaxHealthEffective();
            m_isDead = false;
        }
    }

    protected virtual void Deinit()
    {
        OnKilled = null;
        HostilityManager.RemoveCharacter(this);
        if (HasAuthority && m_networkObject && m_networkObject.IsSpawned)
        {
            m_networkObject.Despawn(false);
        }
    }

    public virtual void SetPitch(float pitch) { }

    public virtual float GetPitch() { return 1; }


    protected void OnDisable()
    {
        Deinit();
    }

    public int GetThreatLevel()
    {
        return m_threatLevel;
    }

    public virtual void EraseAllBullets(StatsCharacter characterResponsible, float3 centerOfErase, float speedFromCenter, float eraseRadius) { }

    protected void OnEnable()
    {
        if (m_initialised)
            Init();
    }

    [ClientRpc]
    protected virtual void DamageClientRpc(float damage, DamageType damageType, ulong enemyNetworkId, int weaponLoadoutIndex, int weaponIndex)
    {
        InternalDamage(damage, damageType, enemyNetworkId, true, weaponLoadoutIndex, weaponIndex, true);
    }

    [ClientRpc]
    protected virtual void DamageClientRpc(float damage, DamageType damageType)
    {
        if (!IsServer) InternalDamage(damage, damageType, 0, false, -1, -1, true);
    }

    public virtual void Damage(float damage, DamageType damageType, ulong enemyNetworkId, int weaponLoadoutIndex = -1, int weaponIndex = -1)
    {
        /*We call InternalDamage() before calling the DamageClientRpc() because InternalDamage() can call the Kill rpc function.
        The problem with this is that Unity has a bug where clientRpcs called from ClientRpcs are only called on the host.*/

        InternalDamage(damage, damageType, enemyNetworkId, true, weaponLoadoutIndex, weaponIndex, false);
        if (HasAuthority)
            DamageClientRpc(damage, damageType, enemyNetworkId, weaponLoadoutIndex, weaponIndex);
    }

    public virtual void Damage(float damage, DamageType damageType = DamageType.NORMAL)
    {
        if (HasAuthority)
            DamageClientRpc(damage, damageType);
        else
            InternalDamage(damage, damageType, 0, false, -1, -1, false);
    }

    public virtual void SetCheckpointState(CheckpointState state) { }

    public virtual void OnHardCheckpoint() { }

    protected abstract void InternalDamage(float damage, DamageType damageType, ulong enemyNetworkId, bool hasEnemyNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall);


    [ClientRpc]
    protected void KillClientRpc(DamageType damageType, ulong enemyNetworkId, int weaponLoadoutIndex, int weaponIndex)
    {
        InternalKill(damageType, enemyNetworkId, true, weaponLoadoutIndex, weaponIndex, true);
    }

    [ClientRpc]
    protected void KillClientRpc(DamageType damageType)
    {
        InternalKill(damageType, 0, false, -1, -1, true);
    }

    public void Kill(DamageType damageType, ulong killerNetworkId, int weaponLoadoutIndex = -1, int weaponIndex = -1)
    {
        if (HasAuthority)
            KillClientRpc(damageType, killerNetworkId, weaponLoadoutIndex, weaponIndex);
        else
            InternalKill(damageType, killerNetworkId, true, weaponLoadoutIndex, weaponIndex, false);
    }

    public virtual void SetTarget(StatsCharacter target) { }


    [ClientRpc]
    protected void NotifyEnemyEngagingClientRpc(ulong enemyNetworkId)
    {
        if (!IsServer) InternalNotifyEnemyEngaging(enemyNetworkId);
    }

    public void NotifyEnemyEngaging(ulong enemyNetworkId)
    {
        /*We call InternalDamage() before calling the DamageClientRpc() because InternalDamage() can call the Kill rpc function.
        The problem with this is that Unity has a bug where clientRpcs called from ClientRpcs are only called on the host.*/

        InternalNotifyEnemyEngaging(enemyNetworkId);
        if (HasAuthority)
            NotifyEnemyEngagingClientRpc(enemyNetworkId);
    }

    public virtual void SpawnBehaviour() { }

    //called when an enemy is approaching this character
    protected virtual void InternalNotifyEnemyEngaging(ulong enemyNetworkId)
    {
        ++m_enemiesEngagingCount;
    }

    [ClientRpc]
    protected void NotifyEnemyDisengagingClientRpc(ulong enemyNetworkId)
    {
        if (!IsServer) InternalNotifyEnemyDisengaging(enemyNetworkId);
    }

    public void NotifyEnemyDisengaging(ulong enemyNetworkId)
    {
        /*We call InternalDamage() before calling the DamageClientRpc() because InternalDamage() can call the Kill rpc function.
        The problem with this is that Unity has a bug where clientRpcs called from ClientRpcs are only called on the host.*/

        InternalNotifyEnemyDisengaging(enemyNetworkId);
        if (HasAuthority)
            NotifyEnemyDisengagingClientRpc(enemyNetworkId);
    }


    //called when an enemy is approaching this character
    protected virtual void InternalNotifyEnemyDisengaging(ulong enemyNetworkId)
    {
        ++m_enemiesEngagingCount;
    }

    //called when an enemy stop engaging
    public virtual void NotifyEnemyDisengaged(StatsCharacter character)
    {
        --m_enemiesEngagingCount;
        if (m_enemiesEngagingCount < 0) m_enemiesEngagingCount = 0;
    }

    public virtual void Kill(DamageType damageType = DamageType.NORMAL)
    {
        if (HasAuthority)
            KillClientRpc(damageType);
        else
            InternalKill(damageType, 0, false, -1, -1, false);
    }

    protected abstract void InternalKill(DamageType damageType, ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall);


    public virtual float GetDeltaTimeCoef() { return GfServerManager.GetDeltaTimeCoef(m_characterType.Value); }

    public virtual bool IsDead()
    {
        return m_isDead;
    }


    public CharacterTypes GetCharacterType()
    {
        return m_characterType.Value;
    }

    public bool IsCheckpointable()
    {
        return !m_isDead && m_checkpointable && !GfGameManager.IsMultiplayer; //disable checkpoint states if we are in multiplayer
    }

    public void SetCheckpointable(bool checkpointable) { m_checkpointable = checkpointable; }

    public void SetCharacterType(CharacterTypes type)
    {
        if (m_characterType.Value != type && HasAuthority)
        {
            HostilityManager.RemoveCharacter(this);
            m_characterType.Value = type;
            HostilityManager.AddCharacter(this);
        }
    }

    public NetworkObject GetNetworkObject() { return m_networkObject; }

    public virtual DamageSource GetWeaponDamageSource(int weaponLoadoutIndex, int weaponIndex)
    {
        return null;
    }

    public virtual void OnDamageDealt(float damage, ulong damagedCharacterNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall) { }
    public virtual void OnCharacterKilled(ulong damagedCharacter, int weaponLoadoutIndex = -1, int weaponIndex = -1, bool isServerCall = false) { }

    public int GetCharacterIndex(CharacterIndexType type)
    {
        return m_characterIndexes[(int)type];
    }

    public void SetCharacterIndex(int index, CharacterIndexType type)
    {
        m_characterIndexes[(int)type] = index;
    }

    public virtual float GetMaxHealthRaw() { return m_maxHealth.Value; }
    public virtual float GetMaxHealthEffective() { return m_maxHealth.Value * m_maxHealthMultiplier.Value; }
    public virtual float GetCurrentHealth() { return m_currentHealth.Value; }
    public virtual void SetMaxHealthRaw(float maxHealth)
    {
        if (HasAuthority)
            m_maxHealth.Value = maxHealth;
    }

    public virtual PriorityValue<float> GetMaxHealthMultiplier() { return m_maxHealthMultiplier.Value; }

    public virtual void SetMaxHealthMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (HasAuthority)
            m_maxHealthMultiplier.Value.SetValue(maxHealthMultiplier, priority, overridePriority);
    }

    public virtual PriorityValue<float> GetReceivedDamageMultiplier() { return m_receivedDamageMultiplier.Value; }

    public virtual void SetReceivedDamageMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (HasAuthority)
            m_receivedDamageMultiplier.Value.SetValue(maxHealthMultiplier, priority, overridePriority);
    }

    public virtual void AddHp(float hp, bool clampToMaxHealth = true)
    {
        m_currentHealth.Value += hp;
        if (clampToMaxHealth)
            m_currentHealth.Value = Mathf.Clamp(m_currentHealth.Value, 0, GetMaxHealthEffective());
    }

    public virtual void RefillHp()
    {
        m_currentHealth.Value = GetMaxHealthEffective();
    }

    public virtual void SetHp(float hp, bool clampToMaxHealth = true)
    {
        if (clampToMaxHealth)
            hp = Mathf.Clamp(hp, 0, GetMaxHealthEffective());

        m_currentHealth.Value = hp;
    }
}
