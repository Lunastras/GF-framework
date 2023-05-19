
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;


public abstract class StatsCharacter : NetworkBehaviour
{
    [SerializeField]
    protected NetworkVariable<float> m_maxHealth = new(100);

    [SerializeField]
    private NetworkVariable<CharacterTypes> m_characterType = new(default);

    protected NetworkVariable<PriorityValue<float>> m_maxHealthMultiplier = new(new(1));

    protected NetworkVariable<PriorityValue<float>> m_receivedDamageMultiplier = new(new(1));

    protected bool m_isDead = false;

    protected NetworkVariable<float> m_currentHealth = new(0);

    protected NetworkTransform m_networkTransform;

    private int m_characterIndex = -1;

    private int m_particleTriggerDamageListIndex = -1;

    protected bool m_initialised = false;

    protected NetworkObject m_networkObject;

    protected bool HasAuthority
    {
        get
        {
            bool ret = false;
            if (NetworkManager.Singleton) ret = NetworkManager.Singleton.IsServer;
            return ret;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_networkObject = GetComponent<NetworkObject>();
        HostilityManager.AddCharacter(this);
        m_initialised = true;
    }

    [ClientRpc]
    protected virtual void DamageClientRpc(float damage, ulong enemyNetworkId, int weaponLoadoutIndex, int weaponIndex)
    {
        InternalDamage(damage, enemyNetworkId, true, weaponLoadoutIndex, weaponIndex, true);
    }

    [ClientRpc]
    protected virtual void DamageClientRpc(float damage)
    {
        if (!IsServer) InternalDamage(damage, 0, false, -1, -1, true);
    }

    public virtual void Damage(float damage, ulong enemyNetworkId, int weaponLoadoutIndex = -1, int weaponIndex = -1)
    {
        /*We call InternalDamage() before calling the DamageClientRpc() because InternalDamage() can call the Kill rpc function.
        The problem with this is that Unity has a bug where clientRpcs called from ClientRpcs are only called on the host.*/

        InternalDamage(damage, enemyNetworkId, true, weaponLoadoutIndex, weaponIndex, false);
        if (HasAuthority)
            DamageClientRpc(damage, enemyNetworkId, weaponLoadoutIndex, weaponIndex);
    }

    public virtual void Damage(float damage)
    {
        if (HasAuthority)
            DamageClientRpc(damage);
        else
            InternalDamage(damage, 0, false, -1, -1, false);
    }

    protected abstract void InternalDamage(float damage, ulong enemyNetworkId, bool hasEnemyNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall);


    [ClientRpc]
    protected virtual void KillClientRpc(ulong enemyNetworkId, int weaponLoadoutIndex, int weaponIndex)
    {
        InternalKill(enemyNetworkId, true, weaponLoadoutIndex, weaponIndex, true);
    }

    [ClientRpc]
    protected virtual void KillClientRpc()
    {
        InternalKill(0, false, -1, -1, true);
    }

    public virtual void Kill(ulong killerNetworkId, int weaponLoadoutIndex = -1, int weaponIndex = -1)
    {
        if (HasAuthority)
            KillClientRpc(killerNetworkId, weaponLoadoutIndex, weaponIndex);
        else
            InternalKill(killerNetworkId, true, weaponLoadoutIndex, weaponIndex, false);
    }

    public virtual void Kill()
    {
        if (HasAuthority)
            KillClientRpc();
        else
            InternalKill(0, false, -1, -1, false);
    }

    public virtual bool IsDead()
    {
        return m_isDead;
    }

    protected abstract void InternalKill(ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall);

    public CharacterTypes GetCharacterType()
    {
        return m_characterType.Value;
    }

    public void SetCharacterType(CharacterTypes type)
    {
        if (m_characterType.Value != type && HasAuthority)
        {
            HostilityManager.RemoveCharacter(this);
            m_characterType.Value = type;
            HostilityManager.AddCharacter(this);
        }
    }

    public NetworkObject GetNetworkObject() { return m_networkObject; }

    protected void Deinit()
    {
        HostilityManager.RemoveCharacter(this);
        if (HasAuthority && m_networkObject && m_networkObject.IsSpawned) m_networkObject.Despawn();
    }

    private void OnDisable()
    {
        Deinit();
    }

    public override void OnDestroy()
    {
        Deinit();
    }

    private void OnEnable()
    {
        if (m_initialised)
            HostilityManager.AddCharacter(this);
    }

    public virtual DamageSource GetWeaponDamageSource(int weaponLoadoutIndex, int weaponIndex)
    {
        return null;
    }

    public virtual void OnDamageDealt(float damage, ulong damagedCharacterNetworkId, int weaponLoadoutIndex, int weaponIndex, bool isServerCall) { }
    public virtual void OnCharacterKilled(ulong damagedCharacter, int weaponLoadoutIndex = -1, int weaponIndex = -1, bool isServerCall = false) { }

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

    public virtual float GetMaxHealthRaw() { return m_maxHealth.Value; }
    public virtual float GetMaxHealthEffective() { return m_maxHealth.Value * m_maxHealthMultiplier.Value; }
    public virtual float GetCurrentHealth() { return m_currentHealth.Value; }
    public virtual void SetMaxHealthRaw(float maxHealth)
    {
        if (HasAuthority)
            m_maxHealth.Value = maxHealth;
    }

    public virtual PriorityValue<float> GetMaxHealthMultiplier() { return m_maxHealthMultiplier.Value; }

    public virtual void SetMaxHealthMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (HasAuthority)
            m_maxHealthMultiplier.Value.SetValue(maxHealthMultiplier, priority, overridePriority);
    }

    public virtual PriorityValue<float> GetReceivedDamageMultiplier() { return m_receivedDamageMultiplier.Value; }

    public virtual void SetReceivedDamageMultiplier(float maxHealthMultiplier, uint priority = 0, bool overridePriority = false)
    {
        if (HasAuthority)
            m_receivedDamageMultiplier.Value.SetValue(maxHealthMultiplier, priority, overridePriority);
    }
}
