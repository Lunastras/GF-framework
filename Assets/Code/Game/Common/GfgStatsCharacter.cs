
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Linq;
using System;
using Unity.Mathematics;

public abstract class GfgStatsCharacter : NetworkBehaviour
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

    protected PriorityValue<float> m_receivedDamageMultiplier = new(1);

    protected bool m_isDead = false;

    protected GfcNetworkVariable<float> m_currentHealth;

    protected NetworkTransform m_networkTransform;

    protected int[] m_characterIndexes = Enumerable.Repeat(-1, (int)CharacterIndexType.COUNT_TYPES).ToArray();

    protected bool m_initialised = false;

    protected NetworkObject m_networkObject;

    protected int m_enemiesEngagingCount = 0;

    protected new bool IsOwner { get { return !NetworkManager.Singleton || IsOwner; } }

    //used for external callbacks
    public Action<GfgStatsCharacter, DamageData> OnKilled = null;

    //only valid for this specific entity (e.g. its own weapons)
    public Action<GfgStatsCharacter, DamageData> OnKilledUnique = null;

    protected bool HasAuthority { get { return GfcManagerServer.HasAuthority; } }

    protected void Awake()
    {
        m_networkTransform = GetComponent<NetworkTransform>();
        m_networkObject = NetworkManager.Singleton ? GetComponent<NetworkObject>() : null;
        m_currentHealth = new(this, 0);
        Init();
    }

    protected void Start() { }

    protected virtual void Init()
    {
        if (HasAuthority && m_networkObject && !m_networkObject.IsSpawned)
            m_networkObject.Spawn();

        if (m_initialised)
        {
            GfgManagerCharacters.AddCharacter(this);
            if (!HasAuthority) RequestCharacterTypeServerRpc();

            if (m_networkTransform) m_networkTransform.enabled = true;

            if (HasAuthority && m_currentHealth.Value == 0) m_currentHealth.Value = GetMaxHealth();
            m_isDead = false;
            m_initialised = true;
        }
    }

    protected virtual void Deinit()
    {
        OnKilled = null;
        GfgManagerCharacters.RemoveCharacter(this);
        if (HasAuthority && m_networkObject && m_networkObject.IsSpawned)
        {
            m_networkObject.Despawn(false);
        }
    }

    public virtual void SetPitch(float pitch) { }

    public virtual float GetPitch() { return 1; }

    public ThreatDetails GetThreatDetails()
    {
        return m_threatDetails;
    }

    public virtual void EraseAllBullets(GfgStatsCharacter aCharacterResponsible, float3 aCenterOfErase, float aSpeedFromCenter, float aEraseRadius) { }

    public override void OnNetworkSpawn()
    {
        Init();
    }

    protected void OnDisable()
    {
        Deinit();
    }

    protected abstract void InternalDamage(DamageData aDamageData, bool aIsServerCall);

    protected abstract void InternalKill(DamageData aDamageData, bool aIsServerCall);

    [ClientRpc]
    protected virtual void DamageClientRpc(DamageData aDamageData)
    {
        if (!HasAuthority) InternalDamage(aDamageData, true);
    }

    public virtual void Damage(float aDamage, DamageType aDamageType = DamageType.NORMAL, GfgStatsCharacter anEnemy = null)
    {
        DamageData damageData = new()
        {
            Damage = aDamage,
            DamageType = aDamageType,
            DamagePosition = transform.position,
            DamageNormal = Vector3.zero,
            HasEnemyNetworkId = anEnemy,
            EnemyNetworkId = anEnemy ? anEnemy.NetworkObjectId : 0,
        };

        Damage(damageData);
    }

    public virtual void Damage(DamageData aDamageData)
    {
        /*We call InternalDamage() before calling the DamageClientRpc() because InternalDamage() can call the Kill rpc function.
        The problem with this is that Unity has a bug where clientRpcs called from ClientRpcs are only called on the host.*/
        InternalDamage(aDamageData, HasAuthority);
        if (HasAuthority && m_sendDamageRpc)
            DamageClientRpc(aDamageData);
    }

    public virtual void SetCheckpointState(CheckpointState aCheckpointState) { }

    public virtual void OnHardCheckpoint() { }



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

    public virtual void SetTarget(GfgStatsCharacter aTarget) { }


    [ClientRpc]
    protected void NotifyEnemyEngagingClientRpc(ulong eEnemyNetworkId)
    {
        if (HasAuthority) InternalNotifyEnemyEngaging(eEnemyNetworkId);
    }

    public void NotifyEnemyEngaging(ulong enemyNetworkId)
    {
        InternalNotifyEnemyEngaging(enemyNetworkId);
        if (HasAuthority)
            NotifyEnemyEngagingClientRpc(enemyNetworkId);
    }

    public virtual void SpawnBehaviour() { }

    //called when an enemy is approaching this character
    protected virtual void InternalNotifyEnemyEngaging(ulong aEnemyNetworkId)
    {
        ++m_enemiesEngagingCount;
    }

    [ClientRpc]
    protected void NotifyEnemyDisengagingClientRpc(ulong aEnemyNetworkId)
    {
        if (HasAuthority) InternalNotifyEnemyDisengaging(aEnemyNetworkId);
    }

    public void NotifyEnemyDisengaging(ulong aEnemyNetworkId)
    {
        InternalNotifyEnemyDisengaging(aEnemyNetworkId);
        if (HasAuthority)
            NotifyEnemyDisengagingClientRpc(aEnemyNetworkId);
    }


    //called when an enemy is approaching this character
    protected virtual void InternalNotifyEnemyDisengaging(ulong aEnemyNetworkId)
    {
        ++m_enemiesEngagingCount;
    }

    //called when an enemy stop engaging
    public virtual void NotifyEnemyDisengaged(GfgStatsCharacter aCharacter)
    {
        --m_enemiesEngagingCount;
        if (m_enemiesEngagingCount < 0) m_enemiesEngagingCount = 0;
    }

    public virtual float GetDeltaTimeCoef() { return GfcManagerServer.GetDeltaTimeCoef(m_characterType); }

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
        return !m_isDead && m_checkpointable && !GfgManagerGame.IsMultiplayer; //disable checkpoint states if we are in multiplayer
    }

    public void SetCheckpointable(bool aCheckpointable) { m_checkpointable = aCheckpointable; }

    [ServerRpc]
    private void RequestCharacterTypeServerRpc()
    {
        SetCharacterType(m_characterType);
    }

    public void SetCharacterType(CharacterTypes aCharactertype)
    {
        if (m_characterType != aCharactertype && HasAuthority)
        {
            InternalSetCharacterType(aCharactertype);
            SetCharacterTypeClientRpc(aCharactertype);
        }
    }

    [ClientRpc]
    private void SetCharacterTypeClientRpc(CharacterTypes aCharactertype)
    {
        if (!HasAuthority) InternalSetCharacterType(aCharactertype);
    }

    private void InternalSetCharacterType(CharacterTypes aCharactertype)
    {
        if (m_characterType != aCharactertype && HasAuthority)
        {
            GfgManagerCharacters.RemoveCharacter(this);
            m_characterType = aCharactertype;
            GfgManagerCharacters.AddCharacter(this);
        }
    }

    public NetworkObject GetNetworkObject() { return m_networkObject; }

    public virtual DamageSource GetWeaponDamageSource(int aWeaponLoadoutIndex, int aWeaponIndex)
    {
        return null;
    }

    public virtual void OnDamageDealt(DamageData aDamageData, bool isServerCall) { }
    public virtual void OnCharacterKilled(DamageData aDamageData, bool isServerCall = false) { }

    public int GetCharacterIndex(CharacterIndexType aType)
    {
        return m_characterIndexes[(int)aType];
    }

    public void SetCharacterIndex(int aIndex, CharacterIndexType aType)
    {
        m_characterIndexes[(int)aType] = aIndex;
    }

    public virtual float GetMaxHealth() { return m_maxHealth; }

    public virtual float GetCurrentHealth() { return m_currentHealth.Value; }

    public virtual void SetMaxHealth(float aMaxHealth)
    {
        if (HasAuthority)
            m_maxHealth = aMaxHealth;
    }

    public virtual PriorityValue<float> GetReceivedDamageMultiplier() { return m_receivedDamageMultiplier; }

    public virtual void SetReceivedDamageMultiplier(float aMaxHealthMultiplier, uint aPriority = 0, bool aOverridePriority = false)
    {
        if (HasAuthority)
            m_receivedDamageMultiplier.SetValue(aMaxHealthMultiplier, aPriority, aOverridePriority);
    }

    public virtual void AddHp(float aHp, bool aClampToMaxHealth = true)
    {
        m_currentHealth.Value += aHp;
        if (aClampToMaxHealth)
            m_currentHealth.Value = Mathf.Clamp(m_currentHealth.Value, 0, GetMaxHealth());
    }

    public virtual void RefillHp()
    {
        if (HasAuthority)
            m_currentHealth.Value = GetMaxHealth();
    }

    public virtual void SetHp(float aHp, bool aClampToMaxHealth = true)
    {
        if (HasAuthority || IsOwner)
        {
            if (aClampToMaxHealth)
                aHp = Mathf.Clamp(aHp, 0, GetMaxHealth());

            m_currentHealth.Value = aHp;
        }
    }
}