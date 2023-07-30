using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class StatsNpc : StatsCharacter
{
    [SerializeField]
    protected GameObject m_testPrefab;

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
    protected TurretWeapons m_turret;
    protected AudioSource m_audioSource;

    [SerializeField]
    protected Sound m_damageSound;
    [SerializeField]
    protected Sound m_deathSound;

    //The damage values in total from the enemies
    protected float m_biggestDamageReceived = 0;

    protected StatsCharacter m_lastEnemy;
    protected float m_damageFromLastEnemy = 0;

    protected Transform m_transform;

    public CheckpointStateNpc m_checkpointStateNpc = null;


    // Start is called before the first frame update

    void Start()
    {
        if (!m_initialised)
        {
            if (null == m_objectCollider)
                m_objectCollider = GetComponent<Collider>();

            if (null == m_movement)
                m_movement = GetComponent<GfMovementGeneric>();

            if (null == m_npcController)
                m_npcController = GetComponent<NpcController>();

            if (null == m_turret)
                m_turret = GetComponent<TurretWeapons>();

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

            m_damageSound.LoadAudioClip();
            m_deathSound.LoadAudioClip();

            m_transform = transform;

            m_networkObject = GetComponent<NetworkObject>();
            if (m_networkTransform) m_networkTransform.enabled = true;
            if (HasAuthority && m_networkObject && !m_networkObject.IsSpawned) m_networkObject.Spawn();

            if (HasAuthority)
                m_currentHealth.Value = GetMaxHealthEffective();


            m_initialised = true;
            HostilityManager.AddCharacter(this);
        }
    }

    new protected void OnEnable()
    {
        base.OnEnable();
        if (m_initialised)
        {
            m_currentHealth = m_maxHealth;

            if (null != m_graphics)
                m_graphics.SetActive(true);

            if (null != m_objectCollider)
                m_objectCollider.enabled = true;

            m_networkObject = GetComponent<NetworkObject>();
            if (m_networkTransform) m_networkTransform.enabled = true;
            if (HasAuthority && m_networkObject && !m_networkObject.IsSpawned) m_networkObject.Spawn();
        }

        CheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
    }

    new protected void OnDisable()
    {
        base.OnDisable();
        CheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
    }

    public override void SetCheckpointState(CheckpointState state)
    {
        if (IsCheckpointable())
        {
            CheckpointStateNpc checkpointStateNpc = state as CheckpointStateNpc;
            Start();
            Respawn();
            m_movement.ReturnToDefaultValues();
            transform.SetPositionAndRotation(checkpointStateNpc.Position, checkpointStateNpc.Rotation);
            transform.localScale = checkpointStateNpc.Scale;
            m_currentHealth.Value = checkpointStateNpc.CurrentHp;
            m_npcController.SetDestination(null);
            m_turret.Stop(true);
            m_turret.SetCurrentPhase(0);
            m_movement.SetVelocity(checkpointStateNpc.Velocity);
            m_movement.SetParentTransform(checkpointStateNpc.MovementParent, checkpointStateNpc.MovementParentPriority, true);

            if (checkpointStateNpc.WasFollowingPlayer)
                m_npcController.SetDestination(GameManager.GetPlayer());

            if (checkpointStateNpc.MovementParentSpherical)
                m_movement.SetParentSpherical(checkpointStateNpc.MovementParentSpherical, checkpointStateNpc.MovementGravityPriority, true);
            else
                m_movement.SetUpVec(checkpointStateNpc.UpVec, checkpointStateNpc.MovementGravityPriority, true);

            m_isDead = false;
            m_checkpointStateNpc = checkpointStateNpc;
        }
    }

    public override void OnHardCheckpoint()
    {
        if (IsCheckpointable())
        {
            if (null == m_checkpointStateNpc) m_checkpointStateNpc = new();

            m_checkpointStateNpc.Position = m_transform.position;
            m_checkpointStateNpc.Rotation = m_transform.rotation;
            m_checkpointStateNpc.Scale = m_transform.localScale;
            m_checkpointStateNpc.CurrentHp = m_currentHealth.Value;
            m_checkpointStateNpc.Prefab = GfPooling.GetPrefab(gameObject.name);
            m_checkpointStateNpc.Velocity = m_movement.GetVelocity();
            m_checkpointStateNpc.MovementParent = m_movement.GetParentTransform();
            m_checkpointStateNpc.MovementParentPriority = m_movement.GetParentPriority();
            m_checkpointStateNpc.MovementParentSpherical = m_movement.GetParentSpherical();
            m_checkpointStateNpc.MovementGravityPriority = m_movement.GetGravityPriority();
            m_checkpointStateNpc.UpVec = m_movement.GetUpVecRaw();
            m_checkpointStateNpc.WasFollowingPlayer = GameManager.GetPlayer() == m_npcController.GetDestinationTransform();


            CheckpointState state = m_checkpointStateNpc;
            CheckpointManager.AddCheckpointState(state);
        }
    }

    protected override void InternalKill(ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall)
    {
        if (isServerCall)
        {
            Vector3 currentPos = m_transform.position;
            GameParticles.PlayDeathDust(currentPos);
            GameParticles.SpawnPowerItems(currentPos, m_powerItemsToDrop, m_movement.GetGravityReference());
            GameParticles.SpawnGrave(currentPos, m_movement.GetGravityReference());

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

            m_npcController.WasKilled();

            m_isDead = true;
            CheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;

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
