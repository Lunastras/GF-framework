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
    protected float damage = 5;

    [SerializeField]
    protected bool canDamageSelf = false;

    [SerializeField]
    protected int hitsPerTarget = 1;

    [SerializeField]
    protected int maxHitsTotal = 1;

    [SerializeField]
    private float damageCoolDown = 0.1f;

    [SerializeField]
    protected int maxTargets = 1;

    [SerializeField]
    protected bool destroysObjectWhenDone = false;

    [SerializeField]
    protected float lifeSpan = 1.0f;

    protected int totalHitCount = 0;

    protected Dictionary<StatsCharacter, HitInfo> hitTargets = null;

    private float timeOfDisable;

    private HitInfo[] hitInfoStockPile = null;

    private int lastHitInfoIndex = -1;

    // Start is called before the first frame update
    void Awake()
    {
        hitTargets = new Dictionary<StatsCharacter, HitInfo>(maxTargets);
        hitInfoStockPile = new HitInfo[maxTargets];
        for (int i = 0; i < hitInfoStockPile.Length; i++)
        {
            hitInfoStockPile[i] = new HitInfo();
        }
    }
    public void Initialize()
    {
        lastHitInfoIndex = maxTargets - 1;

        // Debug.Log("bullet spawned");
        if (lifeSpan > 0.0f && destroysObjectWhenDone)
        {
            // Debug.Log("Called GF detroy");
            GfPooling.Destroy(gameObject, lifeSpan);
        }

        timeOfDisable = Time.time + lifeSpan;

        hitTargets.Clear();
    }

    void OnEnable()
    {
        Initialize();
    }

    protected virtual bool HitTarget(StatsCharacter target)
    {
        target.Damage(damage, characterStats);

        return true;
    }

    protected virtual bool HitNonDamageTarget(StatsCharacter target)
    {
        // target.Damage(damage, characterStats);

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
        if (null == hitTargets && Time.time >= timeOfDisable)
            return;

        StatsCharacter collisionStats = other.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool willDamage = false;

            bool hitSelf = characterStats == collisionStats;
            bool canDamageEnemy = HostilityManager.CanDamage(characterStats, collisionStats);

            //check if it can damage target
            if ((!hitSelf && canDamageEnemy) || (hitSelf && canDamageSelf))
            {
                if (hitTargets.ContainsKey(collisionStats))
                {

                    willDamage = hitTargets[collisionStats].hitCount < hitsPerTarget
                                && hitTargets[collisionStats].nextTimeOfDamage <= Time.time;

                }
                else if (hitTargets.Count < maxTargets)
                {

                    willDamage = true;
                    hitTargets.Add(collisionStats, hitInfoStockPile[lastHitInfoIndex--]);
                }

                if (collisionStats != null && willDamage)
                {
                    if (HitTarget(collisionStats))
                    {
                        totalHitCount++;
                        hitTargets[collisionStats].hitCount++;
                        hitTargets[collisionStats].nextTimeOfDamage = Time.time + damageCoolDown;

                        bool destroyHitbox = (hitTargets[collisionStats].hitCount >= hitsPerTarget
                                        && hitTargets.Count >= maxTargets)
                                        || totalHitCount >= maxHitsTotal;

                        if (destroyHitbox)
                        {
                            OnDestroyBehaviour(willDamage);

                            if (destroysObjectWhenDone)
                            {
                                GfPooling.Destroy(gameObject);
                            }
                        }
                    }
                }
            }
            else
            {
                HitNonDamageTarget(collisionStats);
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
        damage = value;
    }

    public void SetMaxHitsTotal(int value)
    {
        maxHitsTotal = value;
    }
    public void SeHitsPertarget(int value)
    {
        hitsPerTarget = value;
    }

    public void SetDamageCoolDown(float value)
    {
        damageCoolDown = value;
    }

    public void SetMaxTargets(int value)
    {
        hitsPerTarget = value;
    }

    public void SetDestroysObjectWhenDone(bool value)
    {
        destroysObjectWhenDone = value;
    }
}


