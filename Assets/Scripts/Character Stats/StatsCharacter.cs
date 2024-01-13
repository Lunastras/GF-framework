
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
    protected float m_maxHealth = 100;

    [SerializeField]
    private CharacterTypes m_characterType;

    [SerializeField]
    protected ThreatDetails m_threatDetails;

    [SerializeField]
    protected bool m_sendDamageRpc = false;

    protected PriorityValue<float> m_maxHealthMultiplier = new(1);

    protected PriorityValue<float> m_receivedDamageMultiplier = new(1);

    protected bool m_isDead = false;

    protected NetworkVariable<float> m_currentHealth = new(0);

    protected NetworkTransform m_networkTransform;

    protected int[] m_characterIndexes = Enumerable.Repeat(-1, (int)CharacterIndexType.COUNT_TYPES).ToArray();

    protected bool m_initialised = false;

    protected NetworkObject m_networkObject;

    protected int m_enemiesEngagingCount = 0;

    //used for external callbacks
    public Action<StatsCharacter, DamageData> OnKilled = null;

    //only valid for this specific entity (e.g. its own weapons)
    public Action<StatsCharacter, DamageData> OnKilledUnique = null;

    protected bool HasAuthority
    {
        get
        {
            return GfServerManager.HasAuthority;
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
            if (!HasAuthority) RequestCharacterTypeServerRpc();

            if (m_networkTransform) m_networkTransform.enabled = true;

            if (HasAuthority && m_networkObject && !m_networkObject.IsSpawned)
                m_networkObject.Spawn();

            if (HasAuthority && m_currentHealth.Value == 0) m_currentHealth.Value = GetMaxHealthEffective();
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

    public ThreatDetails GetThreatDetails()
    {
        return m_threatDetails;
    }

    public virtual void EraseAllBullets(StatsCharacter characterResponsible, float3 centerOfErase, float speedFromCenter, float eraseRadius) { }

    protected void OnEnable()
    {
        if (m_initialised)
            Init();
    }

    protected abstract void InternalDamage(DamageData aDamageData, bool aIsServerCall);

    [ClientRpc]
    protected virtual void DamageClientRpc(DamageData damageData)
    {
        if (!HasAuthority) InternalDamage(damageData, true);
    }

    public virtual void Damage(DamageData damageData)
    {
        /*We call InternalDamage() before calling the DamageClientRpc() because InternalDamage() can call the Kill rpc function.
        The problem with this is that Unity has a bug where clientRpcs called from ClientRpcs are only called on the host.*/
        InternalDamage(damageData, HasAuthority);
        if (HasAuthority && m_sendDamageRpc)
            DamageClientRpc(damageData);
    }

    public virtual void SetCheckpointState(CheckpointState state) { }

    public virtual void OnHardCheckpoint() { }


    protected abstract void InternalKill(DamageData aDamageData, bool isServerCall);

    [ClientRpc]
    protected void KillClientRpc(DamageData aDamageData)
    {
        if (!HasAuthority) InternalKill(aDamageData, true);
    }

    public void Kill(DamageData aDamageData = default)
    {
        InternalKill(aDamageData, HasAuthority);
        if (HasAuthority)
            KillClientRpc(aDamageData);
    }

    public virtual void SetTarget(StatsCharacter target) { }


    [ClientRpc]
    protected void NotifyEnemyEngagingClientRpc(ulong enemyNetworkId)
    {
        if (HasAuthority) InternalNotifyEnemyEngaging(enemyNetworkId);
    }

    public void NotifyEnemyEngaging(ulong enemyNetworkId)
    {
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
        if (HasAuthority) InternalNotifyEnemyDisengaging(enemyNetworkId);
    }

    public void NotifyEnemyDisengaging(ulong enemyNetworkId)
    {
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

    public virtual float GetDeltaTimeCoef() { return GfServerManager.GetDeltaTimeCoef(m_characterType); }

    public virtual bool IsDead()
    {
        return m_isDead;
    }


    public CharacterTypes GetCharacterType()
    {
        return m_characterType;
    }

    public bool IsCheckpointable()
    {
        return !m_isDead && m_checkpointable && !GfGameManager.IsMultiplayer; //disable checkpoint states if we are in multiplayer
    }

    public void SetCheckpointable(bool checkpointable) { m_checkpointable = checkpointable; }

    [ServerRpc]
    private void RequestCharacterTypeServerRpc()
    {
        SetCharacterType(m_characterType);
    }

    public void SetCharacterType(CharacterTypes type)
    {
        if (m_characterType != type && HasAuthority)
        {
            InternalSetCharacterType(type);
            SetCharacterTypeClientRpc(type);
        }
    }

    [ClientRpc]
    private void SetCharacterTypeClientRpc(CharacterTypes type)
    {
        if (!HasAuthority) InternalSetCharacterType(type);
    }

    private void InternalSetCharacterType(CharacterTypes type)
    {
        if (m_characterType != type && HasAuthority)
        {
            HostilityManager.RemoveCharacter(this);
            m_characterType = type;
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

    public virtual float GetMaxHealthRaw() { return m_maxHealth; }
    public virtual float GetMaxHealthEffective() { return m_maxHealth * m_maxHealthMultiplier; }
    public virtual float GetCurrentHealth() { return m_currentHealth.Value; }

    public virtual void SetMaxHealthRaw(float maxHealth)
    {
        if (HasAuthority || IsOwner)
            m_maxHealth = maxHealth;
    }

    public virtual PriorityValue<float> GetMaxHealthMultiplier() { return m_maxHealthMultiplier; }

    public virtual void SetMaxHealthMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (HasAuthority || IsOwner)
            m_maxHealthMultiplier.SetValue(maxHealthMultiplier, priority, overridePriority);
    }

    public virtual PriorityValue<float> GetReceivedDamageMultiplier() { return m_receivedDamageMultiplier; }

    public virtual void SetReceivedDamageMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (HasAuthority || IsOwner)
            m_receivedDamageMultiplier.SetValue(maxHealthMultiplier, priority, overridePriority);
    }

    public virtual void AddHp(float hp, bool clampToMaxHealth = true)
    {
        m_currentHealth.Value += hp;
        if (clampToMaxHealth)
            m_currentHealth.Value = Mathf.Clamp(m_currentHealth.Value, 0, GetMaxHealthEffective());
    }

    public virtual void RefillHp()
    {
        if (HasAuthority || IsOwner)
            m_currentHealth.Value = GetMaxHealthEffective();
    }

    public virtual void SetHp(float hp, bool clampToMaxHealth = true)
    {
        if (HasAuthority || IsOwner)
        {
            if (clampToMaxHealth)
                hp = Mathf.Clamp(hp, 0, GetMaxHealthEffective());

            m_currentHealth.Value = hp;
        }
    }
}
