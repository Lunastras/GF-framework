using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class StatsPlayer : StatsCharacter
{
    [SerializeField]
    private WeaponFiring m_playerGun = null;

    [SerializeField]
    private LoadoutManager m_loadoutManager = null;

    [SerializeField]
    private GfMovementGeneric m_movement = null;

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

    private HealthUIBehaviour m_healthUI;

    private void Awake()
    {
        if (IsOwner)
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

        m_currentHealth.Value = m_maxHealth.Value;

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

        if (null == m_movement) m_movement = GetComponent<GfMovementGeneric>();

        if (IsOwner)
        {
            m_healthUI = GameManager.GetHudManager().GetHealthUI();

            if (m_healthUI)
            {
                m_healthUI.SetMaxHealth(m_maxHealth.Value);
                m_healthUI.SetHealthPoints(m_currentHealth.Value);
            }

            GameManager.SetPlayer(transform);
        }


        m_initialised = true;
        HostilityManager.AddCharacter(this);
    }

    protected override void InternalDamage(float damage, ulong enemyNetworkId, bool hasEnemyNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall)
    {
        Debug.Log("Damaged, max health is: " + m_maxHealth.Value);

        if (!m_isDead)
        {
            damage *= m_receivedDamageMultiplier.Value;

            StatsCharacter enemy = null;
            if (hasEnemyNetworkId)
            {
                enemy = GameManager.GetComponentFromNetworkObject<StatsCharacter>(enemyNetworkId);
                if (enemy)
                {
                    DamageSource damageSource = enemy.GetWeaponDamageSource(weaponLoadoutIndex, weaponIndex);
                    if (damageSource) damageSource.OnDamageDealt(damage, this, isServerCall);
                    enemy.OnDamageDealt(damage, NetworkObjectId, weaponLoadoutIndex, weaponIndex, isServerCall);
                }
            }

            if (!isServerCall || HasAuthority) //only play sound and damage numbers locally. This will be called once for the server, and twice for clients
            {
                GameParticles.PlayDamageNumbers(m_transform.position, damage, m_movement.GetUpVecRaw());
                m_damageSound.Play(m_audioObjectDamageReceived);
                m_loadoutManager.AddPoints(WeaponPointsTypes.EXPERIENCE, -damage);
            }

            if (HasAuthority)
            {
                m_currentHealth.Value -= damage;
                m_currentHealth.Value = Mathf.Max(0, m_currentHealth.Value);

                if (m_healthUI) m_healthUI.SetHealthPoints(m_currentHealth.Value);

                if (m_currentHealth.Value == 0)
                {
                    m_isDead = true;
                    Kill(enemyNetworkId, weaponLoadoutIndex, weaponIndex);
                }
            }
        }
    }

    public override DamageSource GetWeaponDamageSource(int weaponLoadoutIndex, int weaponIndex)
    {
        DamageSource ret = null;
        if (m_loadoutManager && weaponLoadoutIndex > -1 && weaponIndex > -1 && weaponLoadoutIndex == m_loadoutManager.GetCurrentLoadoutIndex())
        {
            ret = m_loadoutManager.GetWeapons()[weaponIndex];
        }

        return ret;
    }

    protected override void InternalKill(ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall)
    {
        if (isServerCall)
        {
            StatsCharacter killerStats = GameManager.GetComponentFromNetworkObject<StatsCharacter>(killerNetworkId);
            //killerStats.Getwea
            if (killerStats)
            {
                DamageSource damageSource = killerStats.GetWeaponDamageSource(weaponLoadoutIndex, weaponIndex);
                if (damageSource) damageSource.OnCharacterKilled(this, isServerCall);
                killerStats.OnCharacterKilled(NetworkObjectId, weaponLoadoutIndex, weaponIndex);
            }


            m_isDead = true;
            Vector3 currentPos = m_transform.position;
            GameParticles.PlayDeathDust(currentPos);
            AudioManager.PlayAudio(m_deathSound, m_transform.position);

            GameManager.PlayerDied();

            Debug.Log("I died, max health is: " + m_maxHealth.Value);
        }
    }

    public override void SetMaxHealthRaw(float maxHealth)
    {
        if (!IsClient)
        {
            m_maxHealth.Value = maxHealth;
            if (m_healthUI) m_healthUI.SetMaxHealth(m_maxHealth.Value * m_maxHealthMultiplier.Value);
        }
    }

    public override void SetMaxHealthMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (!IsClient && m_maxHealthMultiplier.Value.SetValue(maxHealthMultiplier, priority, overridePriority) && m_healthUI)
        {
            m_healthUI.SetMaxHealth(m_maxHealth.Value * m_maxHealthMultiplier.Value);
        }
    }

    public void AddPoints(CollectibleType itemType, float value)
    {
        m_itemPickUpSound.Play(m_audioSource);

        switch (itemType)
        {
            case (CollectibleType.POINTS):

                break;

            case (CollectibleType.POWER):
                m_loadoutManager.AddPointsAll(WeaponPointsTypes.EXPERIENCE, value);
                break;

            case (CollectibleType.HEALTH):
                m_currentHealth.Value += value;
                if (m_healthUI) m_healthUI.SetHealthPoints(m_currentHealth.Value);
                break;
        }

    }

    public override void OnDamageDealt(float damage, ulong damagedCharacterNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall)
    {
        if (!isServerCall)
        {
            StatsCharacter damagedCharacter = GameManager.GetComponentFromNetworkObject<StatsCharacter>(damagedCharacterNetworkId);
            bool lowHp = false;
            float volume = 1, pitch = 1;

            if (damagedCharacter)
            {
                damagedCharacter.OnCharacterKilled(NetworkObjectId, weaponLoadoutIndex, weaponIndex);
                lowHp = damagedCharacter.GetCurrentHealth() <= damagedCharacter.GetMaxHealthRaw() * 0.25f;
            }

            if (lowHp)
            {
                volume = 1.5f;
                pitch = 2;
            }

            m_damageDealtSound.Play(m_audioObjectDamageDealt, 0, volume, pitch);
        }

    }

    public override void RefillHp()
    {
        m_currentHealth.Value = GetMaxHealthEffective();
        Debug.Log("max hp is " + m_maxHealth.Value + " multiplier is: " + m_maxHealthMultiplier.Value.Value);
        if (IsOwner) m_healthUI.SetHealthPoints(m_currentHealth.Value);
    }

    public override void Respawn()
    {
        base.Respawn();
        if (m_healthUI) m_healthUI.SetHealthPoints(m_currentHealth.Value);
    }

    //called when an enemy is approaching this character
    public override void NotifyEnemyEngaging(StatsCharacter character)
    {
        ++m_enemiesEngagingCount;
        if (IsOwner) GameManager.SetEnemiesEngagingCount(m_enemiesEngagingCount);
    }

    //called when an enemy stop engaging
    public override void NotifyEnemyDisengaged(StatsCharacter character)
    {
        --m_enemiesEngagingCount;
        if (m_enemiesEngagingCount < 0) m_enemiesEngagingCount = 0;
        if (IsOwner) GameManager.SetEnemiesEngagingCount(m_enemiesEngagingCount);
    }
}

public enum CollectibleType { POWER, POINTS, HEALTH }