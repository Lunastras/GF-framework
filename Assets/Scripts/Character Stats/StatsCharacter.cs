using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class StatsCharacter : NetworkBehaviour
{
    [SerializeField]
    protected NetworkVariable<float> m_maxHealth = new(100);

    [SerializeField]
    private NetworkVariable<CharacterTypes> m_characterType = null;

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

    public abstract void Damage(float damage, float damageMultiplier = 1, StatsCharacter enemy = null, DamageSource weaponUsed = null);

    public abstract void Kill(StatsCharacter killer = null, DamageSource weaponUsed = null);

    public CharacterTypes GetCharacterType()
    {
        return m_characterType.Value;
    }

    public void SetCharacterType(CharacterTypes type)
    {
        if (m_characterType.Value != type && !IsClient)
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

    public virtual void OnDamageDealt(float damage, StatsCharacter damagedCharacter, DamageSource weaponUsed = null) { }
    public virtual void OnCharacterKilled(StatsCharacter damagedCharacter, DamageSource weaponUsed = null) { }

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
        if (!IsClient)
            m_maxHealth.Value = maxHealth;
    }

    public virtual PriorityValue<float> GetMaxHealthMultiplier() { return m_maxHealthMultiplier.Value; }

    public virtual void SetMaxHealthMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (!IsClient)
            m_maxHealthMultiplier.Value.SetValue(maxHealthMultiplier, priority, overridePriority);
    }

    public virtual PriorityValue<float> GetReceivedDamageMultiplier() { return m_receivedDamageMultiplier.Value; }

    public virtual void SetReceivedDamageMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (!IsClient)
            m_receivedDamageMultiplier.Value.SetValue(maxHealthMultiplier, priority, overridePriority);
    }
}
