using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

using static GfcTools;

public class CornPlayerStats : GfgStatsCharacter
{
    [SerializeField]
    private GfgPlayerController m_playerControler = null;

    [SerializeField]
    private GfMovementGeneric m_movement = null;

    [SerializeField]
    private AudioSource m_audioSource = null;

    private AudioSource m_audioObjectPickUp = null;
    private AudioSource m_audioObjectDamageDealt = null;
    private AudioSource m_audioObjectDamageReceived = null;

    private Transform m_transform = null;

    [SerializeField]
    protected GfcSound m_damageSound = null;

    [SerializeField]
    protected GfcSound m_damageDealtSound = null;

    [SerializeField]
    protected GfcSound m_deathSound = null;
    [SerializeField]
    private GfcSound m_itemPickUpSound = null;

    protected CheckpointStatePlayer m_checkpointState = null;

    protected bool m_initialisedStatsPlayer = false;

    public static CornPlayerStats LocalPlayer { get; protected set; } = null;

    new protected void Start()
    {
        if (!m_initialisedStatsPlayer)
        {
            base.Start();
            m_transform = transform;

            var objPickUp = GfcManagerAudio.GetAudioObject(m_transform);
            var objDamageDealt = GfcManagerAudio.GetAudioObject(m_transform);
            var objDamageReceived = GfcManagerAudio.GetAudioObject(m_transform);

            m_audioObjectPickUp = objPickUp.GetAudioSource();
            m_audioObjectDamageDealt = objDamageDealt.GetAudioSource();
            m_audioObjectDamageReceived = objDamageReceived.GetAudioSource();

            if (HasAuthority)
                m_currentHealth.Value = GetMaxHealth();

            if (null == m_playerControler)
                m_playerControler = GetComponent<GfgPlayerController>();

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

            if (null == m_movement) m_movement = GetComponent<GfMovementGeneric>();

            if (IsOwner)
            {
                GfgManagerLevel.Player = this;
                LocalPlayer = this;
            }

            m_initialised = true;
            GfgManagerCharacters.AddCharacter(this);
            GfgManagerLevel.OnLevelEnd += OnLevelEnd;
            m_initialisedStatsPlayer = true;
        }
    }

    protected void OnLevelEnd()
    {
        m_playerControler.CanTakeInputs = false;

        GfgManagerSaveData.SaveGame();
    }

    protected override void InternalDamage(DamageData aDamageData, bool isServerCall)
    {
        if (!m_isDead)
        {
            float damage = m_receivedDamageMultiplier.Value * aDamageData.Damage;

            GfgStatsCharacter enemy;
            if (aDamageData.HasEnemyNetworkId)
            {
                enemy = GfgManagerGame.GetComponentFromNetworkObject<GfgStatsCharacter>(aDamageData.EnemyNetworkId);
                if (enemy)
                {
                    DamageSource damageSource = enemy.GetWeaponDamageSource(aDamageData.WeaponLoadoutIndex, aDamageData.WeaponIndex);
                    if (damageSource) damageSource.OnDamageDealt(damage, this, isServerCall);
                    enemy.OnDamageDealt(aDamageData, isServerCall);
                }
            }

            if (!isServerCall || HasAuthority) //only play sound and damage numbers locally. This will be called once for the server, and twice for clients
            {
                m_damageSound.Play(m_audioObjectDamageReceived);
            }

            if (HasAuthority)
            {
                m_currentHealth.Value -= damage;
                m_currentHealth.Value = Mathf.Max(0, m_currentHealth.Value);

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
        GfgCheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
        GfgManagerLevel.OnLevelEnd -= OnLevelEnd;
    }

    protected override void Init()
    {
        base.Init();
        GfgCheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
    }

    protected override void InternalKill(DamageData aDamageData, bool aIsServerCall)
    {
        if (aIsServerCall)
        {
            GfgStatsCharacter killerStats = GfgManagerGame.GetComponentFromNetworkObject<GfgStatsCharacter>(aDamageData.EnemyNetworkId);

            if (killerStats)
            {
                DamageSource damageSource = killerStats.GetWeaponDamageSource(aDamageData.WeaponLoadoutIndex, aDamageData.WeaponIndex);
                if (damageSource) damageSource.OnCharacterKilled(this, aIsServerCall);
                killerStats.OnCharacterKilled(aDamageData, aIsServerCall);
            }

            m_isDead = true;
            Vector3 currentPos = m_transform.position;
            //GameParticles.PlayDeathDust(currentPos);
            GfcManagerAudio.PlayAudio(m_deathSound, m_transform.position);
            OnKilled?.Invoke(this, aDamageData);
            OnKilled = null;
            gameObject.SetActive(false);

            if (IsOwner)
                GfgManagerLevel.PlayerDied();
            else
                Debug.Log("Not the owner apparently...");
        }
    }

    public override void SetCheckpointState(CheckpointState aCheckpointState)
    {
        if (gameObject)
        {
            gameObject.SetActive(true);
            CheckpointStatePlayer checkpointState = aCheckpointState as CheckpointStatePlayer;

            Start();
            m_movement.ReturnToDefaultValues();
            transform.position = checkpointState.Position;
            transform.localScale = checkpointState.Scale;
            m_currentHealth.Value = checkpointState.CurrentHp;
            m_isDead = checkpointState.IsDead;
            m_movement.SetVelocity(checkpointState.Velocity);
            m_checkpointState = checkpointState;
            transform.rotation = checkpointState.Rotation;

            m_movement.SetParentTransform(checkpointState.MovementParent, checkpointState.MovementParentPriority, true);
            OnKilled = checkpointState.OnKilled;

            if (checkpointState.MovementParentSpherical)
                m_movement.SetParentSpherical(checkpointState.MovementParentSpherical, checkpointState.MovementGravityPriority, true);
            else
                m_movement.SetUpVec(checkpointState.UpVec, checkpointState.MovementGravityPriority, true);

            GfgCameraController.Instance.RevertToDefault();
            GfgCameraController.Instance.SnapToTarget();
        }
    }

    public override void OnHardCheckpoint()
    {
        if (gameObject && IsCheckpointable())
        {
            Start();
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
            GfgCheckpointManager.AddCheckpointState(state);
        }
    }

    public override void SetMaxHealth(float aMaxHealth)
    {
        m_maxHealth = aMaxHealth;
    }

    public void AddPoints(CollectibleType aItemType, float aValue)
    {
        m_itemPickUpSound.Play(m_audioSource);

        switch (aItemType)
        {
            case CollectibleType.POINTS:

                break;

            case CollectibleType.POWER:
                break;

            case CollectibleType.HEALTH:
                break;
        }

    }

    public override void OnDamageDealt(DamageData aDamageData, bool aIsServerCall)
    {
        if (!aIsServerCall)
        {
            GfgStatsCharacter damagedCharacter = GfgManagerGame.GetComponentFromNetworkObject<GfgStatsCharacter>(aDamageData.EnemyNetworkId);
            bool lowHp = false;
            float volume = 1, pitch = 1;

            if (damagedCharacter)
            {
                lowHp = damagedCharacter.GetCurrentHealth() <= damagedCharacter.GetMaxHealth() * 0.25f;
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
        m_currentHealth.Value = GetMaxHealth();
    }

    //called when an enemy is approaching this character
    protected override void InternalNotifyEnemyEngaging(ulong aEnemyNetworkId)
    {
        ++m_enemiesEngagingCount;
        if (IsOwner) GfgManagerLevel.NotifyEnemyEngaging(m_enemiesEngagingCount, aEnemyNetworkId);
    }

    //called when an enemy stop engaging
    protected override void InternalNotifyEnemyDisengaging(ulong aEnemyNetworkId)
    {
        --m_enemiesEngagingCount;
        if (m_enemiesEngagingCount < 0) m_enemiesEngagingCount = 0;
        if (IsOwner) GfgManagerLevel.NotifyEnemyDisengaging(m_enemiesEngagingCount, aEnemyNetworkId);
    }
}

public enum CollectibleType { POWER, POINTS, HEALTH }

public class CheckpointStatePlayer : CheckpointState
{
    public Vector3 Position = Vector3.zero;

    public Quaternion Rotation = Quaternion.identity;

    public Vector3 Scale = new Vector3(1, 1, 1);

    public float CurrentHp = 100;

    public Transform MovementParent = null;

    public uint MovementParentPriority = 0;

    public Transform MovementParentSpherical = null;

    public uint MovementGravityPriority = 0;

    public Vector3 Velocity;

    public Vector3 UpVec;

    public bool IsDead = false;

    public Action<GfgStatsCharacter, DamageData> OnKilled = null;

    public override void ExecuteCheckpointState()
    {
        CornPlayerStats.LocalPlayer.SetCheckpointState(this);
    }
}