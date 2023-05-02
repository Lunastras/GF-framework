using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class StatsCharacter : NetworkBehaviour
{
    [SerializeField]
    protected NetworkVariable<float> m_maxHealth = new(100);

    [SerializeField]
    private NetworkVariable<CharacterTypes> m_characterType = new(default);

    protected NetworkVariable<PriorityValue<float>> m_maxHealthMultiplier = new(new(1));

    protected NetworkVariable<PriorityValue<float>> m_receivedDamageMultiplier = new(new(1));

    public NetworkVariable<bool> IsDead { get; protected set; } = new(false);

    protected NetworkVariable<float> m_currentHealth = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private int m_characterIndex = -1;

    private int m_particleTriggerDamageListIndex = -1;

    protected bool m_initialised = false;

    // Start is called before the first frame update
    void Start()
    {
        HostilityManager.AddCharacter(this);
        m_initialised = true;
    }

    public virtual void Damage(float damage, ulong enemyNetworkId, int weaponLoadoutIndex = -1, int weaponIndex = -1)
    {
        InternalDamage(damage, enemyNetworkId, true, weaponLoadoutIndex, weaponIndex);
    }

    public virtual void Damage(float damage, int weaponLoadoutIndex = -1, int weaponIndex = -1)
    {
        InternalDamage(damage, 0, false, weaponLoadoutIndex, weaponIndex);
    }

    protected abstract void InternalDamage(float damage, ulong enemyNetworkId, bool hasEnemyNetworkId, int weaponLoadoutIndex, int weaponIndex);

    public virtual void Kill(ulong killerNetworkId, int weaponLoadoutIndex = -1, int weaponIndex = -1)
    {
        InternalKill(killerNetworkId, true, weaponLoadoutIndex, weaponIndex);
    }

    public virtual void Kill()
    {
        InternalKill(0, false, -1, -1);
    }

    protected abstract void InternalKill(ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex);

    public CharacterTypes GetCharacterType()
    {
        return m_characterType.Value;
    }

    public void SetCharacterType(CharacterTypes type)
    {
        if (m_characterType.Value != type && IsServer)
        {
            HostilityManager.RemoveCharacter(this);
            m_characterType.Value = type;
            HostilityManager.AddCharacter(this);
        }
    }

    public override void OnDestroy()
    {
        HostilityManager.RemoveCharacter(this);
    }

    private void OnDisable()
    {
        HostilityManager.RemoveCharacter(this);
    }

    private void OnEnable()
    {
        if (m_initialised)
            HostilityManager.AddCharacter(this);
    }

    public virtual DamageSource GetWeaponDamageSource(int weaponLoadoutIndex, int weaponIndex)
    {
        return null;
    }

    public virtual void OnDamageDealt(float damage, ulong damagedCharacterNetworkId, int weaponLoadoutIndex = -1, int weaponIndex = -1) { }
    public virtual void OnCharacterKilled(ulong damagedCharacter, int weaponLoadoutIndex = -1, int weaponIndex = -1) { }

    public int GetCharacterIndex()
    {
        return m_characterIndex;
    }

    public void SetCharacterIndex(int index)
    {
        m_characterIndex = index;
    }

    public void SetParticleTriggerDamageIndex(int index) { m_particleTriggerDamageListIndex = index; }

    public int GetParticleTriggerDamageIndex() { return m_particleTriggerDamageListIndex; }

    public virtual float GetMaxHealthRaw() { return m_maxHealth.Value; }
    public virtual float GetMaxHealthEffective() { return m_maxHealth.Value * m_maxHealthMultiplier.Value; }
    public virtual float GetCurrentHealth() { return m_currentHealth.Value; }
    public virtual void SetMaxHealthRaw(float maxHealth)
    {
        if (IsServer)
            m_maxHealth.Value = maxHealth;
    }

    public virtual PriorityValue<float> GetMaxHealthMultiplier() { return m_maxHealthMultiplier.Value; }

    public virtual void SetMaxHealthMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (IsServer)
            m_maxHealthMultiplier.Value.SetValue(maxHealthMultiplier, priority, overridePriority);
    }

    public virtual PriorityValue<float> GetReceivedDamageMultiplier() { return m_receivedDamageMultiplier.Value; }

    public virtual void SetReceivedDamageMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (IsServer)
            m_receivedDamageMultiplier.Value.SetValue(maxHealthMultiplier, priority, overridePriority);
    }
}
