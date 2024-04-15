using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Sabresaurus.SabreCSG;

using static GfcTools;

public class StatsPlayer : GfgStatsCharacter
{
    [SerializeField]
    private GfgFiringWeapons m_playerGun = null;

    [SerializeField]
    private GfgLoadoutManager m_LoadoutManager = null;

    [SerializeField]
    private GfgPlayerController m_playerControler = null;

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
    protected GfcSound m_damageSound = null;

    [SerializeField]
    protected GfcSound m_damageDealtSound = null;

    [SerializeField]
    protected GfcSound m_deathSound = null;
    [SerializeField]
    private GfcSound m_itemPickUpSound = null;

    private HealthUIBehaviour m_healthUI;

    protected CheckpointStatePlayer m_checkpointState = null;

    protected bool m_initialisedStatsPlayer = false;

    public static StatsPlayer LocalPlayer { get; protected set; } = null;

    protected List<EquipCharmData> m_aquiredCharms;

    protected List<WeaponData> m_aquiredWeapons;

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
                m_currentHealth.Value = GetMaxHealthEffective();

            if (null == m_playerGun)
                m_playerGun = FindObjectOfType<GfgFiringWeapons>();

            if (null == m_LoadoutManager)
                m_LoadoutManager = GetComponent<GfgLoadoutManager>();

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

            if (null == m_odamaManager) m_odamaManager = GetComponent<OdamaManager>();
            if (null == m_movement) m_movement = GetComponent<GfMovementGeneric>();

            if (IsOwner)
            {
                m_healthUI = InvManagerLevel.GetHudManager().GetHealthUI();

                if (m_healthUI)
                {
                    m_healthUI.SetMaxHealth(m_maxHealth);
                    m_healthUI.SetHealthPoints(m_currentHealth.Value);
                }

                InvManagerLevel.SetPlayer(this);
                LocalPlayer = this;
            }
            else
            {
                Debug.Log(" oh, I am not the owner");
            }

            if (IsOwner)
                OwnPlayer = this;

            m_initialised = true;
            GfgManagerCharacters.AddCharacter(this);
            GfgManagerLevel.OnLevelEnd += OnLevelEnd;
            m_initialisedStatsPlayer = true;
        }
    }

    protected void OnLevelEnd()
    {
        m_playerControler.CanTakeInputs = false;
        //m_LoadoutManager.enabled = false;

        PlayerSaveData saveData = GfgManagerSaveData.GetActivePlayerSaveData();

        NativeArray<Pair<float, CharmAquireInfo>> charmAquireInfos = default;
        NativeArray<WeaponAquireInfo> weaponAquireInfos = default;

        float totalCharmPointsAquired = 0;
        if (m_aquiredCharms != null && m_aquiredCharms.Count > 0)
        {
            int countCharms = m_aquiredCharms.Count;
            charmAquireInfos = new(countCharms, Allocator.Temp);
            for (int i = 0; i < countCharms; ++i)
            {
                float charmPointsAquired = saveData.AddCharm(m_aquiredCharms[i], out CharmAquireInfo aquireInfo);
                charmAquireInfos[i] = new(charmPointsAquired, aquireInfo);
                totalCharmPointsAquired += charmPointsAquired;
            }
        }

        if (m_aquiredWeapons != null && m_aquiredWeapons.Count > 0)
        {
            int countWeapons = m_aquiredWeapons.Count;
            weaponAquireInfos = new(countWeapons, Allocator.Temp);
            for (int i = 0; i < countWeapons; ++i)
            {
                saveData.AddWeapon(m_aquiredWeapons[i], out WeaponAquireInfo aquireInfo);
                weaponAquireInfos[i] = aquireInfo;
            }
        }

        if (charmAquireInfos.IsCreated) charmAquireInfos.Dispose();
        if (weaponAquireInfos.IsCreated) weaponAquireInfos.Dispose();

        GfgManagerSaveData.SaveGame();
    }

    protected override void InternalDamage(DamageData aDamageData, bool isServerCall)
    {
        if (!m_isDead)
        {
            float damage = m_receivedDamageMultiplier.Value * aDamageData.Damage;

            GfgStatsCharacter enemy = null;
            if (aDamageData.HasEnemyNetworkId)
            {
                enemy = GfcManagerGame.GetComponentFromNetworkObject<GfgStatsCharacter>(aDamageData.EnemyNetworkId);
                if (enemy)
                {
                    DamageSource damageSource = enemy.GetWeaponDamageSource(aDamageData.WeaponLoadoutIndex, aDamageData.WeaponIndex);
                    if (damageSource) damageSource.OnDamageDealt(damage, this, isServerCall);
                    enemy.OnDamageDealt(aDamageData, isServerCall);
                }
            }

            if (!isServerCall || HasAuthority) //only play sound and damage numbers locally. This will be called once for the server, and twice for clients
            {
                GameParticles.PlayDamageNumbers(m_transform.position, damage, m_movement.GetUpVecRaw());
                m_damageSound.Play(m_audioObjectDamageReceived);
            }

            if (HasAuthority)
            {
                m_LoadoutManager.AddPoints(WeaponPointsTypes.EXPERIENCE, -damage);
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
        GfgCheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
        GfgManagerLevel.OnLevelEnd -= OnLevelEnd;
    }

    protected override void Init()
    {
        base.Init();
        GfgCheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
    }

    public override DamageSource GetWeaponDamageSource(int aWeaponLoadoutIndex, int aWeaponIndex)
    {
        DamageSource ret = null;
        if (m_LoadoutManager && aWeaponLoadoutIndex > -1 && aWeaponIndex > -1 && aWeaponLoadoutIndex == m_LoadoutManager.GetCurrentLoadoutIndex())
        {
            ret = m_LoadoutManager.GetWeapons()[aWeaponIndex];
        }

        return ret;
    }

    protected override void InternalKill(DamageData aDamageData, bool aIsServerCall)
    {
        if (aIsServerCall)
        {
            GfgStatsCharacter killerStats = GfcManagerGame.GetComponentFromNetworkObject<GfgStatsCharacter>(aDamageData.EnemyNetworkId);
            //killerStats.Getwea
            if (killerStats)
            {
                DamageSource damageSource = killerStats.GetWeaponDamageSource(aDamageData.WeaponLoadoutIndex, aDamageData.WeaponIndex);
                if (damageSource) damageSource.OnCharacterKilled(this, aIsServerCall);
                killerStats.OnCharacterKilled(aDamageData, aIsServerCall);
            }

            m_isDead = true;
            Vector3 currentPos = m_transform.position;
            GameParticles.PlayDeathDust(currentPos);
            GfcManagerAudio.PlayAudio(m_deathSound, m_transform.position);
            OnKilled?.Invoke(this, aDamageData);
            OnKilled = null;
            gameObject.SetActive(false);

            if (IsOwner)
                InvManagerLevel.PlayerDied();
        }
    }

    public void AddCharm(EquipCharmData aCharmmDdata)
    {
        if (m_aquiredCharms == null) m_aquiredCharms = new(4);
        m_aquiredCharms.Add(aCharmmDdata);
    }

    public void AddWeapon(WeaponData aWeaponData)
    {
        if (m_aquiredWeapons == null) m_aquiredWeapons = new(4);
        m_aquiredWeapons.Add(aWeaponData);
    }

    public void SetPlayerRuntimeGameData(PlayerRuntimeGameData aPlayerRuntimeGameData)
    {
        Start();
        int countWeapons = aPlayerRuntimeGameData.EquippedWeapons != null ? aPlayerRuntimeGameData.EquippedWeapons.Count : 0;
        m_LoadoutManager.ClearLoadouts();

        for (int i = 0; i < countWeapons; ++i)
        {
            m_LoadoutManager.SetLoadoutWeapon(i, 0, aPlayerRuntimeGameData.EquippedWeapons[i].WeaponData, false);
        }

        m_LoadoutManager.SetWeaponCapacity(30);
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
            m_LoadoutManager.Respawned();
            m_checkpointState = checkpointState;
            //m_movement.d();
            transform.rotation = checkpointState.Rotation;

            int countWeapons = Count(checkpointState.AquiredWeapons);
            m_aquiredWeapons?.Clear();

            for (int i = 0; i < countWeapons; ++i)
                AddWeapon(checkpointState.AquiredWeapons[i]);

            int countCharms = checkpointState.AquiredCharms != null ? checkpointState.AquiredCharms.Count : 0;
            m_aquiredCharms?.Clear();

            for (int i = 0; i < countCharms; ++i)
                AddCharm(checkpointState.AquiredCharms[i]);

            m_movement.SetParentTransform(checkpointState.MovementParent, checkpointState.MovementParentPriority, true);
            OnKilled = checkpointState.OnKilled;

            if (checkpointState.MovementParentSpherical)
                m_movement.SetParentSpherical(checkpointState.MovementParentSpherical, checkpointState.MovementGravityPriority, true);
            else
                m_movement.SetUpVec(checkpointState.UpVec, checkpointState.MovementGravityPriority, true);

            GfgCameraController.LookFowardInstance();
            GfgCameraController.SnapToTargetInstance();
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

            int countWeapons = m_aquiredWeapons != null ? m_aquiredWeapons.Count : 0;
            m_checkpointState.AquiredWeapons?.Clear();

            for (int i = 0; i < countWeapons; i++)
                m_checkpointState.AquiredWeapons.Add(m_aquiredWeapons[i]);

            int countCharms = m_aquiredCharms != null ? m_aquiredCharms.Count : 0;
            m_checkpointState.AquiredCharms?.Clear();

            for (int i = 0; i < countCharms; i++)
                m_checkpointState.AquiredCharms.Add(m_aquiredCharms[i]);

            CheckpointState state = m_checkpointState;
            GfgCheckpointManager.AddCheckpointState(state);
        }
    }

    public override void SetMaxHealthRaw(float aMaxHealth)
    {
        m_maxHealth = aMaxHealth;
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

    public void AddPoints(CollectibleType aItemType, float aValue)
    {
        m_itemPickUpSound.Play(m_audioSource);

        switch (aItemType)
        {
            case (CollectibleType.POINTS):

                break;

            case (CollectibleType.POWER):
                if (IsOwner)
                    m_LoadoutManager.AddPointsAll(WeaponPointsTypes.EXPERIENCE, aValue);
                break;

            case (CollectibleType.HEALTH):
                m_currentHealth.Value += aValue;
                if (m_healthUI) m_healthUI.SetHealthPoints(m_currentHealth.Value);
                break;
        }

    }

    public override void OnDamageDealt(DamageData aDamageData, bool aIsServerCall)
    {
        if (!aIsServerCall)
        {
            GfgStatsCharacter damagedCharacter = GfcManagerGame.GetComponentFromNetworkObject<GfgStatsCharacter>(aDamageData.EnemyNetworkId);
            bool lowHp = false;
            float volume = 1, pitch = 1;

            if (damagedCharacter)
            {
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