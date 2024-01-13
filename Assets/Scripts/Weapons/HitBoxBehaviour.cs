using System.Collections.Generic;
using UnityEngine;

public class HitBoxBehaviour : HitBoxGeneric
{
    protected class HitInfo
    {
        public void Clear()
        {
            hitCount = 0;
            nextTimeOfDamage = 0;
        }
        public int hitCount = 0;
        public float nextTimeOfDamage = 0;
    }

    [SerializeField]
    protected DamageType m_damageType = DamageType.NORMAL;

    [SerializeField]
    protected float m_damage = 5;

    [SerializeField]
    protected bool m_canDamageSelf = false;

    [SerializeField]
    protected int m_hitsPerTarget = 1;

    [SerializeField]
    protected int m_maxHitsTotal = 1;

    [SerializeField]
    private float m_damageCoolDown = 0.1f;

    [SerializeField]
    protected int m_maxTargets = 1;

    [SerializeField]
    protected bool m_destroysObjectWhenDone = false;

    [SerializeField]
    protected float m_lifeSpan = 1.0f;

    protected int m_totalHitCount = 0;

    protected Dictionary<StatsCharacter, HitInfo> m_hitTargets = null;

    private float m_timeOfDisable;

    private HitInfo[] m_hitInfoStockPile = null;

    private int m_lastHitInfoIndex = -1;

    // Start is called before the first frame update
    void Awake()
    {
        m_hitTargets = new Dictionary<StatsCharacter, HitInfo>(m_maxTargets);
        m_hitInfoStockPile = new HitInfo[m_maxTargets];
        for (int i = 0; i < m_hitInfoStockPile.Length; i++)
        {
            m_hitInfoStockPile[i] = new HitInfo();
        }
    }
    public void Initialize()
    {
        m_lastHitInfoIndex = m_maxTargets - 1;

        // Debug.Log("bullet spawned");
        if (m_lifeSpan > 0.0f && m_destroysObjectWhenDone)
        {
            // Debug.Log("Called GF detroy");
            GfPooling.Destroy(gameObject, m_lifeSpan);
        }

        m_timeOfDisable = Time.time + m_lifeSpan;

        m_hitTargets.Clear();
    }

    void OnEnable()
    {
        Initialize();
    }

    protected virtual bool HitTarget(StatsCharacter target, float damageMultiplier)
    {
        target.Damage(new(m_damage * damageMultiplier, target.transform.position, Vector3.zero, m_damageType, true, characterStats.NetworkObjectId));

        return true;
    }

    protected virtual void HitCollision(Collider other)
    {
        GfPooling.Destroy(gameObject);
    }

    protected virtual void OnDestroyBehaviour(bool hitEnemy) { }

    public override void CollisionBehaviour(Collider other)
    {
        //Debug.Log("AAA I HIT " + other.name);
        if (null == m_hitTargets && Time.time >= m_timeOfDisable)
            return;

        StatsCharacter collisionStats = other.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool willDamage = false;

            bool hitSelf = characterStats == collisionStats;
            float damageMultiplier = HostilityManager.DamageMultiplier(characterStats, collisionStats);

            //check if it can damage target
            if (!hitSelf || (hitSelf && m_canDamageSelf))
            {
                if (m_hitTargets.ContainsKey(collisionStats))
                {

                    willDamage = m_hitTargets[collisionStats].hitCount < m_hitsPerTarget
                                && m_hitTargets[collisionStats].nextTimeOfDamage <= Time.time;

                }
                else if (m_hitTargets.Count < m_maxTargets)
                {

                    willDamage = true;
                    m_hitTargets.Add(collisionStats, m_hitInfoStockPile[m_lastHitInfoIndex--]);
                }

                if (collisionStats != null && willDamage)
                {
                    if (HitTarget(collisionStats, damageMultiplier))
                    {
                        m_totalHitCount++;
                        m_hitTargets[collisionStats].hitCount++;
                        m_hitTargets[collisionStats].nextTimeOfDamage = Time.time + m_damageCoolDown;

                        bool destroyHitbox = (m_hitTargets[collisionStats].hitCount >= m_hitsPerTarget
                                        && m_hitTargets.Count >= m_maxTargets)
                                        || m_totalHitCount >= m_maxHitsTotal;

                        if (destroyHitbox)
                        {
                            OnDestroyBehaviour(willDamage);

                            if (m_destroysObjectWhenDone)
                            {
                                GfPooling.Destroy(gameObject);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            HitCollision(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        CollisionBehaviour(other);
    }

    public void SetDamage(int value)
    {
        m_damage = value;
    }

    public void SetMaxHitsTotal(int value)
    {
        m_maxHitsTotal = value;
    }
    public void SeHitsPertarget(int value)
    {
        m_hitsPerTarget = value;
    }

    public void SetDamageCoolDown(float value)
    {
        m_damageCoolDown = value;
    }

    public void SetMaxTargets(int value)
    {
        m_hitsPerTarget = value;
    }

    public void SetDestroysObjectWhenDone(bool value)
    {
        m_destroysObjectWhenDone = value;
    }
}


