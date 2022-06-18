using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBoxSingleBehaviour : HitBoxGeneric
{
    [SerializeField]
    protected SingleHitBoxValues hitBoxValues;

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
            GfPooling.Destroy(gameObject);
        }
    }

    protected virtual bool HitTarget(StatsCharacter target)
    {
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(hitBoxValues.damage, characterStats);
        GfPooling.Destroy(gameObject);

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
        if (0 >= timeUntilDisable)
            return;

        StatsCharacter collisionStats = other.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool hitSelf = characterStats == collisionStats;
            bool canDamageEnemy = HostilityManager.hostilityManager.CanDamage(characterStats, collisionStats);

            //check if it can damage target
            if ((!hitSelf && canDamageEnemy) || (hitSelf && hitBoxValues.canDamageSelf))
            {
                HitTarget(collisionStats);
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
public struct SingleHitBoxValues
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
