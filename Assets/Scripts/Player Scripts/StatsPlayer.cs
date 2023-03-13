using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsPlayer : StatsCharacter
{
    [SerializeField]
    private WeaponFiring m_playerGun = null;

    [SerializeField]
    private LoadoutManager m_loadoutManager = null;

    [SerializeField]
    private AudioSource m_audioSource = null;

    public static StatsPlayer instance = null;

    private AudioSource m_audioObjectPickUp = null;
    private AudioSource m_audioObjectDamageDealt = null;
    private AudioSource m_audioObjectDamageReceived = null;


    private Transform m_transform = null;

    [SerializeField]
    protected Sound m_damageSound = null;

    [SerializeField]
    protected Sound m_damageDealtSound = null;

    [SerializeField]
    protected Sound m_deathSound = null;
    [SerializeField]
    private Sound m_itemPickUpSound = null;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        m_transform = transform;

        var objPickUp = AudioManager.GetAudioObject(m_transform);
        var objDamageDealt = AudioManager.GetAudioObject(m_transform);
        var objDamageReceived = AudioManager.GetAudioObject(m_transform);

        m_audioObjectPickUp = objPickUp.GetAudioSource();
        m_audioObjectDamageDealt = objDamageDealt.GetAudioSource();
        m_audioObjectDamageReceived = objDamageReceived.GetAudioSource();


        m_currentHealth = m_maxHealth;

        if (null == m_playerGun)
            m_playerGun = FindObjectOfType<WeaponFiring>();

        if (null == m_loadoutManager)
            m_loadoutManager = GetComponent<LoadoutManager>();

        if (null == m_audioSource)
        {
            m_audioSource = GetComponent<AudioSource>();
            if (null == m_audioSource)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        m_initialised = true;
        HostilityManager.AddCharacter(this);
    }

    public override void Damage(float damage, float damageMultiplier = 1, StatsCharacter enemy = null, DamageSource weaponUsed = null)
    {
        if (IsDead && false)
            return;

        m_damageSound.Play(m_audioObjectDamageReceived);

        if (enemy) enemy.OnDamageDealt(damage, this, weaponUsed);
        if (null != weaponUsed) weaponUsed.OnDamageDealt(damage, this);

        damage *= damageMultiplier;
        float damagePercent = damage / m_maxHealth;

        m_loadoutManager.AddExpPercent(-damagePercent);

        m_currentHealth -= damage;
        m_currentHealth = Mathf.Max(0, m_currentHealth);

        // Debug.Log("I HAVE BEEN DAMAGED, i have " + currentHealth + "hp");

        m_damageSound.Play(m_audioSource);

        if (m_currentHealth == 0)
        {
            IsDead = true;
            Kill(enemy, weaponUsed);
        }
    }

    public override void Kill(StatsCharacter killer = null, DamageSource weaponUsed = null)
    {
        if (killer) killer.OnCharacterKilled(this);
        if (null != weaponUsed) weaponUsed.OnCharacterKilled(this);

        IsDead = true;
        // Debug.Log("I DIED");
    }

    public void AddPoints(CollectibleType itemType, float value)
    {
        m_itemPickUpSound.Play(m_audioSource);

        switch (itemType)
        {
            case (CollectibleType.POINTS):

                break;

            case (CollectibleType.POWER):
                m_loadoutManager.AddExpPoints(value);
                break;
        }

    }

    void OnParticleTrigger()
    {
        Debug.Log("TYrigger on playyer was called huuuh");
    }

    public override void OnDamageDealt(float damage, StatsCharacter damagedCharacter, DamageSource weaponUsed = null)
    {
        bool lowHp = damagedCharacter.GetCurrentHealth() <= damagedCharacter.GetMaxHealthRaw() * 0.25f;

        float volume = 1, pitch = 1;
        if (lowHp)
        {
            volume = 1.5f;
            pitch = 2;
        }

        m_damageDealtSound.Play(m_audioObjectDamageDealt, 0, volume, pitch);
    }
}

public enum CollectibleType { POWER, POINTS, HEALTH }