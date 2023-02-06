using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsNpc : StatsCharacter
{
    [SerializeField]
    protected float m_maxHealth = 200;

    public bool IsDead { get; protected set; }

    [SerializeField]
    protected GameObject m_graphics;

    [SerializeField]
    protected GfMovementGeneric m_movement;


    [SerializeField]
    protected Sound m_damageSound;

    [SerializeField]
    protected int m_powerItemsToDrop = 5;

    [SerializeField]
    protected Sound m_deathSound;

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
    private NpcController m_npcController;

    [SerializeField]
    private Collider m_objectCollider;

    [SerializeField]
    private ParticleTurret m_turret;

    [SerializeField]
    private AudioSource m_audioSource;

    protected float m_currentHealth;

    //The damage values in total from the enemies
    private float m_biggestDamageReceived = 0;

    private StatsCharacter m_lastEnemy;
    private float m_damageFromLastEnemy = 0;

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
            m_turret = GetComponent<ParticleTurret>();

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

        m_currentHealth = m_maxHealth;

        m_initialised = true;
        HostilityManager.AddCharacter(this);
    }

    private void OnEnable()
    {
        if (m_initialised)
        {
            m_currentHealth = m_maxHealth;


            HostilityManager.AddCharacter(this);

            if (null != m_graphics)
                m_graphics.SetActive(true);

            if (null != m_objectCollider)
                m_objectCollider.enabled = true;
        }

    }

    public override void Kill()
    {
        GameParticles.PlayDeathDust(transform.position);
        GameParticles.PlayPowerItems(transform.position, m_powerItemsToDrop, m_movement);

        m_deathSound.Play(m_audioSource);
        return;

        for (int i = 0; i < m_itemDropsAfterDeath.Length; i++)
        {
            for (int j = 0; j < m_dropsToSpawn[Mathf.Min(m_dropsToSpawn[i], m_dropsToSpawn.Length - 1)]; j++)
            {
                GameObject obj = GfPooling.Instantiate(m_itemDropsAfterDeath[i]);
                obj.transform.position = transform.position;
                GfMovementGeneric objMovement = obj.GetComponent<GfMovementGeneric>();
                if (objMovement)
                {
                    objMovement.CopyGravity(m_movement, 0);
                }
            }
        }

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

    public override void Damage(float damage, StatsCharacter enemy = null)
    {
        if (IsDead)
            return;

        GameParticles.PlayDamageNumbers(transform.position, damage, m_movement.UpVecEffective());

        // return;
        m_currentHealth -= damage;

        if (m_currentHealth <= m_maxHealth * m_lowHealthThreshold)
        {
            m_damageSound.Play(m_audioSource, 1.5f, 2);
        }
        else
        {
            m_damageSound.Play(m_audioSource);
        }

        if (m_currentHealth <= 0)
        {
            Kill();
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
