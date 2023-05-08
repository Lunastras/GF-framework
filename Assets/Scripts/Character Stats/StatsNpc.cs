using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

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

        m_networkTransform = GetComponent<NetworkTransform>();

        if (null == m_audioSource)
        {
            m_audioSource = GetComponent<AudioSource>();
            if (null == m_audioSource)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        m_transform = transform;

        if (HasAuthority)
            m_currentHealth.Value = GetMaxHealthEffective();

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

        m_networkObject = GetComponent<NetworkObject>();
        if (m_networkTransform) m_networkTransform.enabled = true;
        if (HasAuthority && m_networkObject && !m_networkObject.IsSpawned) m_networkObject.Spawn();
    }

    protected override void InternalKill(ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall)
    {
        if (isServerCall)
        {
            Vector3 currentPos = m_transform.position;
            GameParticles.PlayDeathDust(currentPos);
            GameParticles.SpawnPowerItems(currentPos, m_powerItemsToDrop, m_movement);
            GameParticles.SpawnGrave(currentPos, m_movement);

            for (int i = 0; i < m_itemDropsAfterDeath.Length; i++)
            {
                for (int j = 0; j < m_dropsToSpawn[Mathf.Min(m_dropsToSpawn[i], m_dropsToSpawn.Length - 1)]; j++)
                {
                    GameObject obj = GfPooling.Instantiate(m_itemDropsAfterDeath[i]);
                    obj.transform.position = m_transform.position;
                    GfMovementGeneric objMovement = obj.GetComponent<GfMovementGeneric>();
                    if (objMovement) objMovement.CopyGravityFrom(m_movement);
                }
            }

            AudioManager.PlayAudio(m_deathSound, m_transform.position);

            if (hasKillerNetworkId)
            {
                StatsCharacter statsKiller = GameManager.GetComponentFromNetworkObject<StatsCharacter>(killerNetworkId);
                if (statsKiller)
                {
                    DamageSource damageSource = statsKiller.GetWeaponDamageSource(weaponLoadoutIndex, weaponIndex);
                    if (damageSource) damageSource.OnCharacterKilled(this, isServerCall);
                    statsKiller.OnCharacterKilled(NetworkObjectId, weaponLoadoutIndex, weaponIndex);
                }
            }

            m_isDead = true;

            if (null != m_graphics)
                m_graphics.SetActive(false);

            if (null != m_objectCollider)
                m_objectCollider.enabled = false;

            if (null != m_npcController)
                m_npcController.PauseMovement();

            HostilityManager.RemoveCharacter(this);

            if (null != m_turret && HasAuthority)
                m_turret.DestroyWhenDone(gameObject);
        }
    }

    public override DamageSource GetWeaponDamageSource(int weaponLoadoutIndex, int weaponIndex)
    {
        DamageSource ret = null;
        if (m_turret && weaponLoadoutIndex > -1 && weaponIndex > -1)
        {
            ret = m_turret.GetWeapon(weaponLoadoutIndex, weaponIndex);
        }
        return ret;
    }

    protected override void InternalDamage(float damage, ulong enemyNetworkId, bool hasEnemyNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall)
    {
        Debug.Log("Internal Damage called, is ServerCall: " + isServerCall);
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

            float simulatedHealth = m_currentHealth.Value - damage;

            if (!isServerCall || HasAuthority) //only play sound and damage numbers locally. This will be called once for the server, and twice for clients
            {
                GameParticles.PlayDamageNumbers(m_transform.position, damage, m_movement.GetUpVecRaw());
                if (simulatedHealth <= GetMaxHealthEffective() * m_lowHealthThreshold)
                    m_damageSound.Play(m_audioSource, 1.5f, 2);
                else
                    m_damageSound.Play(m_audioSource);
            }

            if (HasAuthority)
            {
                m_currentHealth.Value = simulatedHealth;

                if (m_currentHealth.Value <= 0)
                {
                    Kill(enemyNetworkId, weaponLoadoutIndex, weaponIndex);
                }
                else if (null != m_npcController && enemy && damage > 0)
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
    }
}
