using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StatsCharacter : MonoBehaviour
{
    [SerializeField]
    protected float m_maxHealth = 200;

    [SerializeField]
    private CharacterTypes m_characterType;

    protected float m_maxHealthMultiplier = 1;

    public bool IsDead { get; protected set; }
    protected float m_currentHealth;

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
        return m_characterType;
    }

    public void SetCharacterType(CharacterTypes type)
    {
        if (m_characterType != type)
        {
            HostilityManager.RemoveCharacter(this);
            m_characterType = type;
            HostilityManager.AddCharacter(this);
        }
    }

    private void OnDestroy()
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

    public float GetMaxHealthRaw() { return m_maxHealth; }
    public float GetMaxHealthMultiplier() { return m_maxHealthMultiplier; }
    public float GetMaxHealthEffective() { return m_maxHealth * m_maxHealthMultiplier; }
    public float GetCurrentHealth() { return m_currentHealth; }
    public void SetMaxHealthRaw(float maxHealth) { m_maxHealth = maxHealth; }
    public void SetMaxHealthMultiplier(float maxHealthMultiplier) { m_maxHealthMultiplier = maxHealthMultiplier; }


}
