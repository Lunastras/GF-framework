using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBoxSingleBehaviour : HitBoxGeneric
{
    [SerializeField]
    protected DamageType m_damageType = DamageType.NORMAL;

    [SerializeField]
    protected SingleHitBoxValues hitBoxValues = null;

    private float timeUntilDisable = 0;

    public void Initialize()
    {
        timeUntilDisable = hitBoxValues.lifeSpan > 0 ? hitBoxValues.lifeSpan : float.MaxValue;
    }

    void OnEnable()
    {
        Initialize();
    }

    void FixedUpdate()
    {
        timeUntilDisable -= Time.deltaTime;

        if (0 >= timeUntilDisable && hitBoxValues.destroysObjectWhenDone)
        {
            GfcPooling.Destroy(gameObject);
        }
    }

    protected virtual bool HitTarget(StatsCharacter target, float damageMultiplier)
    {
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(new(hitBoxValues.damage * damageMultiplier, target.transform.position, Vector3.zero, m_damageType, true, GetStatsCharacter().NetworkObjectId));
        GfcPooling.Destroy(gameObject);

        return true;
    }

    protected virtual void HitCollision(Collider other)
    {
        GfcPooling.Destroy(gameObject);
    }

    protected virtual void OnDestroyBehaviour(bool hitEnemy) { }

    public override void CollisionBehaviour(Collider other)
    {
        //Debug.Log("AAA I HIT " + other.name);
        if (0 >= timeUntilDisable)
            return;

        StatsCharacter collisionStats = other.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool hitSelf = characterStats == collisionStats;
            float damageMultiplier = GfcManagerCharacters.DamageMultiplier(characterStats, collisionStats);

            //check if it can damage target
            if (!hitSelf || (hitSelf && hitBoxValues.canDamageSelf))
                HitTarget(collisionStats, damageMultiplier);
        }
        else
        {
            HitCollision(other);
        }
    }

    public void SetHitBoxValues(SingleHitBoxValues hitBoxValues)
    {
        this.hitBoxValues = hitBoxValues;
        Initialize();
    }

    public SingleHitBoxValues GetHitBoxValues()
    {
        return hitBoxValues;
    }

    private void OnTriggerEnter(Collider other)
    {
        CollisionBehaviour(other);
    }
}


[System.Serializable]
public class SingleHitBoxValues
{
    public SingleHitBoxValues(float damage = 1, float lifeSpan = 1.0f, bool canDamageSelf = false, bool destroysObjectWhenDone = true)
    {
        this.damage = damage;
        this.lifeSpan = lifeSpan;
        this.canDamageSelf = canDamageSelf;
        this.destroysObjectWhenDone = destroysObjectWhenDone;
    }

    public float damage;

    public float lifeSpan;

    public bool canDamageSelf;

    public bool destroysObjectWhenDone;
}
