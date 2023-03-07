using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsNpc : StatsCharacter
{
    [SerializeField]
    protected GameObject m_graphics;

    [SerializeField]
    protected GfMovementGeneric m_movement;


    [SerializeField]
    protected int m_powerItemsToDrop = 5;

    [SerializeField]
    protected GameObject[] m_itemDropsAfterDeath;

    [SerializeField]
    protected int[] m_dropsToSpawn;
    [SerializeField]
    protected float m_lowHealthThreshold = 0.3f;

    [SerializeField]
    protected float m_aggroDamage = 20;

    [SerializeField]
    protected GameObject m_mainGameObject;

    [SerializeField]
    protected NpcController m_npcController;

    [SerializeField]
    protected Collider m_objectCollider;

    [SerializeField]
    protected WeaponTurret m_turret;
    protected AudioSource m_audioSource;

    [SerializeField]
    protected Sound m_damageSound;
    [SerializeField]
    protected Sound m_deathSound;
    [SerializeField]

    //The damage values in total from the enemies
    protected float m_biggestDamageReceived = 0;

    protected StatsCharacter m_lastEnemy;
    protected float m_damageFromLastEnemy = 0;

    protected Transform m_transform;

    // Start is called before the first frame update

    void Start()
    {
        if (null == m_objectCollider)
            m_objectCollider = GetComponent<Collider>();


        if (null == m_movement)
            m_movement = GetComponent<GfMovementGeneric>();

        if (null == m_npcController)
            m_npcController = GetComponent<NpcController>();

        if (null == m_turret)
            m_turret = GetComponent<WeaponTurret>();

        if (null == m_mainGameObject)
            m_mainGameObject = gameObject;

        if (null == m_audioSource)
        {
            m_audioSource = GetComponent<AudioSource>();
            if (null == m_audioSource)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        m_transform = transform;

        m_currentHealth = GetMaxHealthEffective();

        m_initialised = true;
        HostilityManager.AddCharacter(this);
    }

    private void OnEnable()
    {
        if (m_initialised)
        {
            m_maxHealthMultiplier = 1;
            m_currentHealth = m_maxHealth;


            HostilityManager.AddCharacter(this);

            if (null != m_graphics)
                m_graphics.SetActive(true);

            if (null != m_objectCollider)
                m_objectCollider.enabled = true;
        }
    }

    public override void Kill(StatsCharacter killer = null, DamageSource weaponUsed = null)
    {
        if (killer) killer.OnCharacterKilled(this);
        if (null != weaponUsed) weaponUsed.OnCharacterKilled(this);
        Vector3 pos = m_transform.position;
        GameParticles.PlayDeathDust(pos);
        GameParticles.PlayPowerItems(pos, m_powerItemsToDrop, m_movement);
        GameParticles.SpawnGrave(pos, m_movement);

        //  return;

        for (int i = 0; i < m_itemDropsAfterDeath.Length; i++)
        {
            for (int j = 0; j < m_dropsToSpawn[Mathf.Min(m_dropsToSpawn[i], m_dropsToSpawn.Length - 1)]; j++)
            {
                GameObject obj = GfPooling.Instantiate(m_itemDropsAfterDeath[i]);
                obj.transform.position = m_transform.position;
                GfMovementGeneric objMovement = obj.GetComponent<GfMovementGeneric>();
                if (objMovement)
                {
                    objMovement.CopyGravity(m_movement);
                }
            }
        }

        AudioManager.PlayAudio(m_deathSound, m_transform.position);

        IsDead = true;

        if (null != m_graphics)
            m_graphics.SetActive(false);

        if (null != m_objectCollider)
            m_objectCollider.enabled = false;

        if (null != m_npcController)
            m_npcController.PauseMovement();

        HostilityManager.RemoveCharacter(this);

        if (null != m_turret)
            m_turret.DestroyWhenDone(gameObject);
    }

    public override void Damage(float damage, float damageMultiplier = 1, StatsCharacter enemy = null, DamageSource weaponUsed = null)
    {
        if (IsDead)
            return;

        if (enemy) enemy.OnDamageDealt(damage, this, weaponUsed);
        if (null != weaponUsed) weaponUsed.OnDamageDealt(damage, this);

        damage *= damageMultiplier;
        GameParticles.PlayDamageNumbers(m_transform.position, damage, m_movement.UpVecEffective());

        // return;
        m_currentHealth -= damage;

        if (m_currentHealth <= GetMaxHealthEffective() * m_lowHealthThreshold)
        {
            m_damageSound.Play(m_audioSource, 1.5f, 2);
        }
        else
        {
            m_damageSound.Play(m_audioSource);
        }

        if (m_currentHealth <= 0)
        {
            Kill(enemy, weaponUsed);
        }
        else if (m_npcController != null && enemy != null && damage > 0)
        {
            if (m_lastEnemy != enemy)
            {
                m_lastEnemy = enemy;
                m_damageFromLastEnemy = 0;
            }

            m_damageFromLastEnemy += damage;

            if (m_damageFromLastEnemy >= m_aggroDamage)
                m_npcController.SetDestination(enemy.transform, true);
        }
    }
}
