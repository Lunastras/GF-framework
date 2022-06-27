using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSingleTurret : MonoBehaviour
{
    [SerializeField]
    private ParticleSingleDamageData damageData;

    [SerializeField]
    private StatsCharacter characterStats;

    [SerializeField]
    private new ParticleSystem particleSystem;

    public Transform target { get; set; } = null;


    private void FixedUpdate()
    {
        if (null == target)
            return;

        transform.LookAt(target);
    }

    public void Fire()
    {
        particleSystem.Play();
    }

    // Start is called before the first frame update
    void Start()
    {
        characterStats = null == characterStats ? GetComponent<StatsCharacter>() : characterStats;

        particleSystem = null == particleSystem ? GetComponent<ParticleSystem>() : particleSystem;

    }

    void OnParticleCollision(GameObject other)
    {
        //Debug.Log("I HAVE HIT " + other.name);
        CollisionBehaviour(other);
    }

    public void Stop()
    {
        particleSystem.Stop();
    }

    protected virtual bool HitTarget(StatsCharacter target)
    {
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(damageData.damage, characterStats);

        return true;
    }

    protected bool HitNonDamageTarget(StatsCharacter target)
    {
        // target.Damage(damage, characterStats);

        return true;
    }

    protected void HitCollision(GameObject other)
    {
    }

    protected void OnDestroyBehaviour(bool hitEnemy) { }

    public void CollisionBehaviour(GameObject other)
    {

        StatsCharacter collisionStats = other.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool hitSelf = characterStats == collisionStats;
            bool canDamageEnemy = HostilityManager.CanDamage(characterStats, collisionStats);

            //check if it can damage target
            if ((!hitSelf && canDamageEnemy) || (hitSelf && damageData.canDamageSelf))
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

    public void SetDamageData(ParticleSingleDamageData hitBoxValues)
    {
        this.damageData = hitBoxValues;
    }

    public ParticleSingleDamageData GetDamageData()
    {
        return damageData;
    }
}

[System.Serializable]
public class ParticleSingleDamageData
{
    public float damage = 10;
    public bool canDamageSelf = false;
}
