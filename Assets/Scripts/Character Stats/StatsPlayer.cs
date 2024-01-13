using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class StatsPlayer : StatsCharacter
{
    [SerializeField]
    private FiringWeapons m_playerGun = null;

    [SerializeField]
    private LoadoutManager m_loadoutManager = null;

    [SerializeField]
    private PlayerController m_playerControler = null;

    [SerializeField]
    private GfMovementGeneric m_movement = null;

    [SerializeField]
    private OdamaManager m_odamaManager = null;

    [SerializeField]
    private AudioSource m_audioSource = null;

    public static StatsPlayer OwnPlayer { get; protected set; } = null;

    private AudioSource m_audioObjectPickUp = null;
    private AudioSource m_audioObjectDamageDealt = null;
    private AudioSource m_audioObjectDamageReceived = null;


    private Transform m_transform = null;

    [SerializeField]
    protected GfSound m_damageSound = null;

    [SerializeField]
    protected GfSound m_damageDealtSound = null;

    [SerializeField]
    protected GfSound m_deathSound = null;
    [SerializeField]
    private GfSound m_itemPickUpSound = null;

    private HealthUIBehaviour m_healthUI;

    protected CheckpointStatePlayer m_checkpointState = null;

    public static StatsPlayer LocalPlayer { get; protected set; } = null;

    new protected void Start()
    {
        base.Start();
        m_transform = transform;

        var objPickUp = GfAudioManager.GetAudioObject(m_transform);
        var objDamageDealt = GfAudioManager.GetAudioObject(m_transform);
        var objDamageReceived = GfAudioManager.GetAudioObject(m_transform);

        m_audioObjectPickUp = objPickUp.GetAudioSource();
        m_audioObjectDamageDealt = objDamageDealt.GetAudioSource();
        m_audioObjectDamageReceived = objDamageReceived.GetAudioSource();

        if (HasAuthority)
            m_currentHealth.Value = GetMaxHealthEffective();

        if (null == m_playerGun)
            m_playerGun = FindObjectOfType<FiringWeapons>();

        if (null == m_loadoutManager)
            m_loadoutManager = GetComponent<LoadoutManager>();

        if (null == m_playerControler)
            m_playerControler = GetComponent<PlayerController>();

        if (null == m_audioSource)
        {
            m_audioSource = GetComponent<AudioSource>();
            if (null == m_audioSource)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        m_damageSound.LoadAudioClip();
        m_damageDealtSound.LoadAudioClip();
        m_deathSound.LoadAudioClip();
        m_itemPickUpSound.LoadAudioClip();
        m_damageDealtSound.LoadAudioClip();

        if (null == m_odamaManager) m_odamaManager = GetComponent<OdamaManager>();
        if (null == m_movement) m_movement = GetComponent<GfMovementGeneric>();

        if (IsOwner)
        {
            m_healthUI = GfLevelManager.GetHudManager().GetHealthUI();

            if (m_healthUI)
            {
                m_healthUI.SetMaxHealth(m_maxHealth);
                m_healthUI.SetHealthPoints(m_currentHealth.Value);
            }

            GfLevelManager.SetPlayer(this);
            LocalPlayer = this;
        }

        if (IsOwner)
            OwnPlayer = this;

        m_initialised = true;
        HostilityManager.AddCharacter(this);
        GfLevelManager.OnLevelEnd += OnLevelEnd;

    }

    protected void OnLevelEnd()
    {
        m_playerControler.enabled = false;
        m_loadoutManager.enabled = false;
    }

    protected override void InternalDamage(DamageData aDamageData, bool isServerCall)
    {
        if (!m_isDead)
        {
            float damage = m_receivedDamageMultiplier.Value * aDamageData.Damage;

            StatsCharacter enemy = null;
            if (aDamageData.HasEnemyNetworkId)
            {
                enemy = GfGameManager.GetComponentFromNetworkObject<StatsCharacter>(aDamageData.EnemyNetworkId);
                if (enemy)
                {
                    DamageSource damageSource = enemy.GetWeaponDamageSource(aDamageData.WeaponLoadoutIndex, aDamageData.WeaponIndex);
                    if (damageSource) damageSource.OnDamageDealt(damage, this, isServerCall);
                    enemy.OnDamageDealt(damage, NetworkObjectId, aDamageData.WeaponLoadoutIndex, aDamageData.WeaponIndex, isServerCall);
                }
            }

            if (!isServerCall || HasAuthority) //only play sound and damage numbers locally. This will be called once for the server, and twice for clients
            {
                GameParticles.PlayDamageNumbers(m_transform.position, damage, m_movement.GetUpVecRaw());
                m_damageSound.Play(m_audioObjectDamageReceived);
            }

            if (HasAuthority)
            {
                m_loadoutManager.AddPoints(WeaponPointsTypes.EXPERIENCE, -damage);
                m_currentHealth.Value -= damage;
                m_currentHealth.Value = Mathf.Max(0, m_currentHealth.Value);

                if (m_healthUI) m_healthUI.SetHealthPoints(m_currentHealth.Value);

                if (m_currentHealth.Value == 0)
                {
                    m_isDead = true;
                    Kill(aDamageData);
                }
            }
        }
    }

    protected override void Deinit()
    {
        base.Deinit();
        CheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
        GfLevelManager.OnLevelEnd -= OnLevelEnd;
    }

    protected override void Init()
    {
        base.Init();
        CheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
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

    protected override void InternalKill(DamageData aDamageData, bool aIsServerCall)
    {
        if (aIsServerCall)
        {
            StatsCharacter killerStats = GfGameManager.GetComponentFromNetworkObject<StatsCharacter>(aDamageData.EnemyNetworkId);
            //killerStats.Getwea
            if (killerStats)
            {
                DamageSource damageSource = killerStats.GetWeaponDamageSource(aDamageData.WeaponLoadoutIndex, aDamageData.WeaponIndex);
                if (damageSource) damageSource.OnCharacterKilled(this, aIsServerCall);
                killerStats.OnCharacterKilled(NetworkObjectId, aDamageData.WeaponLoadoutIndex, aDamageData.WeaponIndex);
            }


            m_isDead = true;
            Vector3 currentPos = m_transform.position;
            GameParticles.PlayDeathDust(currentPos);
            GfAudioManager.PlayAudio(m_deathSound, m_transform.position);
            OnKilled?.Invoke(this, aDamageData);
            OnKilled = null;
            gameObject.SetActive(false);

            if (IsOwner)
                GfLevelManager.PlayerDied();
        }
    }

    public override void SetCheckpointState(CheckpointState state)
    {
        if (gameObject)
        {
            gameObject.SetActive(true);
            CheckpointStatePlayer checkpointState = state as CheckpointStatePlayer;
            Start();
            m_movement.ReturnToDefaultValues();
            transform.position = checkpointState.Position;
            transform.localScale = checkpointState.Scale;
            m_currentHealth.Value = checkpointState.CurrentHp;
            m_isDead = checkpointState.IsDead;
            m_movement.SetVelocity(checkpointState.Velocity);
            m_loadoutManager.Respawned();
            m_checkpointState = checkpointState;
            //m_movement.d();
            transform.rotation = checkpointState.Rotation;

            m_movement.SetParentTransform(checkpointState.MovementParent, checkpointState.MovementParentPriority, true);
            OnKilled = checkpointState.OnKilled;

            if (checkpointState.MovementParentSpherical)
                m_movement.SetParentSpherical(checkpointState.MovementParentSpherical, checkpointState.MovementGravityPriority, true);
            else
                m_movement.SetUpVec(checkpointState.UpVec, checkpointState.MovementGravityPriority, true);

            CameraController.LookFowardInstance();
            CameraController.SnapToTargetInstance();
        }
    }

    public override void OnHardCheckpoint()
    {
        if (gameObject && IsCheckpointable())
        {
            m_movement.Initialize();
            if (null == m_checkpointState) m_checkpointState = new();
            m_transform = transform;
            m_checkpointState.Position = m_transform.position;
            m_checkpointState.Rotation = m_transform.rotation;
            m_checkpointState.Scale = m_transform.localScale;
            m_checkpointState.CurrentHp = m_currentHealth.Value;
            m_checkpointState.Velocity = m_movement.GetVelocity();
            m_checkpointState.IsDead = m_isDead;
            m_checkpointState.MovementParent = m_movement.GetParentTransform();
            m_checkpointState.MovementParentPriority = m_movement.GetParentPriority();
            m_checkpointState.MovementParentSpherical = m_movement.GetParentSpherical();
            m_checkpointState.MovementGravityPriority = m_movement.GetGravityPriority();
            m_checkpointState.UpVec = m_movement.GetUpVecRaw();
            m_checkpointState.OnKilled = OnKilled;

            CheckpointState state = m_checkpointState;
            CheckpointManager.AddCheckpointState(state);
        }
    }

    public override void SetMaxHealthRaw(float maxHealth)
    {
        m_maxHealth = maxHealth;
        if (m_healthUI) m_healthUI.SetMaxHealth(GetMaxHealthEffective());
    }

    public override void SetMaxHealthMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (m_maxHealthMultiplier.SetValue(maxHealthMultiplier, priority, overridePriority)
            && m_healthUI)
        {
            m_healthUI.SetMaxHealth(GetMaxHealthEffective());
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
                if (IsOwner)
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
            StatsCharacter damagedCharacter = GfGameManager.GetComponentFromNetworkObject<StatsCharacter>(damagedCharacterNetworkId);
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
        if (IsOwner) m_healthUI.SetHealthPoints(m_currentHealth.Value);
    }

    //called when an enemy is approaching this character
    protected override void InternalNotifyEnemyEngaging(ulong enemyNetworkId)
    {
        ++m_enemiesEngagingCount;
        if (IsOwner) GfLevelManager.NotifyEnemyEngaging(m_enemiesEngagingCount, enemyNetworkId);
    }

    //called when an enemy stop engaging
    protected override void InternalNotifyEnemyDisengaging(ulong enemyNetworkId)
    {
        --m_enemiesEngagingCount;
        if (m_enemiesEngagingCount < 0) m_enemiesEngagingCount = 0;
        if (IsOwner) GfLevelManager.NotifyEnemyDisengaging(m_enemiesEngagingCount, enemyNetworkId);
    }
}

public enum CollectibleType { POWER, POINTS, HEALTH }