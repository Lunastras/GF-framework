using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleSingleDamage : ParticleCollision
{
    [SerializeField]
    private ParticleSingleDamageData damageData;

    [SerializeField]
    private StatsCharacter statsCharacter;

    public Transform target { get; set; } = null;

    private bool destroyWhenStopped = false;

    private static Dictionary<ParticleSingleDamageData, HashSet<ParticleSingleDamage>> releasedFireSources = new(23);

    public void Play()
    {
        particleSystem.Play();
    }

    private void OnEnable()
    {
        statsCharacter = null;
        target = null;
    }

    private void OnDestroy()
    {
        if (releasedFireSources.ContainsKey(damageData))
            releasedFireSources[damageData].Remove(this);
    }

    protected override void InternalAwake()
    {
        statsCharacter = null == statsCharacter ? GetComponent<StatsCharacter>() : statsCharacter;
    }

    private void FixedUpdate()
    {
        if (destroyWhenStopped && !particleSystem.IsAlive(true))
        {
            Debug.Log("ahaahaaaaaaa the time is done, let's kill ourselves");
            ParticleSingleDamage psd = this;
            DestroyFiringSource(ref psd);
        }
           

        if (null == target)
            return;

        transform.LookAt(target);
    }

    public void Stop()
    {
        particleSystem.Stop();
    }

    protected virtual bool HitTarget(StatsCharacter target)
    {
      //  Debug.Log("GONNA DAMAJE IT " + target.name);
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        target.Damage(damageData.damage, statsCharacter);

        return true;
    }

    protected virtual bool HitNonDamageTarget(StatsCharacter target)
    {
        // target.Damage(damage, characterStats);

        return true;
    }

    protected virtual void HitCollision(GameObject other)
    {
    }

    private void OnParticleSystemStopped()
    {
        Debug.Log("I have stopped and I AM ALIVE IS: " + particleSystem.IsAlive(true));
    }

    protected override void CollisionBehaviour(GameObject other, ParticleCollisionEvent collisionEvent)
    {
       // Debug.Log("Okk i hit dis bich " + other.name);
        StatsCharacter collisionStats = other.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool hitSelf = statsCharacter == collisionStats;
            bool canDamageEnemy = HostilityManager.CanDamage(statsCharacter, collisionStats);

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

    public void SetStatsCharacter(StatsCharacter value)
    {
        statsCharacter = value;

        foreach (Transform child in transform)
        {
            child.GetComponent<ParticleSingleDamage>().SetStatsCharacter(value);
        }
    }

    public StatsCharacter GetStatsCharacter()
    {
        return statsCharacter;
    }

    public void SetDamageData(ParticleSingleDamageData hitBoxValues)
    {
        this.damageData = hitBoxValues;
    }

    public ParticleSingleDamageData GetDamageData()
    {
        return damageData;
    }

    public static void SetNewParticleSingleDamage(ParticleSingleDamage original, ref ParticleSingleDamage copy)
    {
        // Debug.Log("the copied ps is " + original.name);
        // Debug.Log("and it's copied for " + copy.name);

        bool hasInstanceOfOriginal = HasInstanceOf(original);

        if (copy.particleSystem.IsAlive(true) || hasInstanceOfOriginal)
        {
            ParticleSingleDamage newSource = GetNewFiringSource(original);
            newSource.transform.SetParent(copy.transform.parent);

            DestroyFiringSource(ref copy);
            copy = newSource;        
        }

        copy.transform.localPosition = original.transform.localPosition;
        copy.transform.localRotation = original.transform.localRotation;
        copy.SetDamageData(original.damageData);

        //copy the particle system if an instantiated one couldn't be found
        if(!hasInstanceOfOriginal)
            CopyParticleSystem.CopyFrom(original.particleSystem, copy.particleSystem);
        

        var subEmitterModule = copy.particleSystem.subEmitters;
        if(subEmitterModule.enabled)
        {
            int countEmitters = subEmitterModule.subEmittersCount;
            int childCount = copy.transform.childCount;

            while (childCount > countEmitters)
            {
                ParticleSingleDamage child = copy.transform.GetChild(0).GetComponent<ParticleSingleDamage>();
                DestroyFiringSource(ref child);
                --childCount;
            }

            for (int i = 0; i < countEmitters; ++i)
            {
                ParticleSingleDamage child;
                ParticleSingleDamage subOriginal = subEmitterModule.GetSubEmitterSystem(i).GetComponent<ParticleSingleDamage>();

                if (i < childCount)
                {
                    child = copy.transform.GetChild(0).GetComponent<ParticleSingleDamage>();
                    SetNewParticleSingleDamage(subOriginal, ref child);
                }
                else
                {
                    child = GetNewFiringSource(subOriginal);
                    child.transform.SetParent(copy.transform);
                }
            }
        }
    }

    private static bool HasInstanceOf(ParticleSingleDamage value)
    {
        return releasedFireSources.ContainsKey(value.damageData) && 0 < releasedFireSources[value.damageData].Count;
    }
    private static ParticleSingleDamage GetCopyOf(ParticleSingleDamage value)
    {
        if(releasedFireSources.ContainsKey(value.damageData))
        {
            foreach (ParticleSingleDamage item in releasedFireSources[value.damageData])
            {
                releasedFireSources[value.damageData].Remove(item);
                return item;
            }
        }
     
        return null;
    }

    // public static ParticleSingleDamage GetNewFiringSource()
    // {
    //return GfPooling.PoolInstantiate(WeaponMaster.GetTemplate()).GetComponent<ParticleSingleDamage>();
    //}

    public static ParticleSingleDamage GetNewFiringSource(ParticleSingleDamage copy = null)
    {
        ParticleSingleDamage psd;

        if (null != copy && HasInstanceOf(copy))
        {
            psd = GetCopyOf(copy);
        } 
        else
        {
            psd = GfPooling.PoolInstantiate(WeaponMaster.GetTemplate()).GetComponent<ParticleSingleDamage>();
            if (releasedFireSources.ContainsKey(psd.damageData))
                releasedFireSources[psd.damageData].Remove(psd);
        }

        psd.destroyWhenStopped = false;
        return psd;
    }

    public static void DestroyFiringSource(ref ParticleSingleDamage firingSource)
    {
        if (firingSource.particleSystem.IsAlive(true))
        {
            firingSource.Stop();
            firingSource.destroyWhenStopped = true;
            firingSource.transform.SetParent(WeaponMaster.GetActiveFireSourcesParent());

            if(!releasedFireSources.ContainsKey(firingSource.damageData))
                releasedFireSources.Add(firingSource.damageData, new(7));
            
            releasedFireSources[firingSource.damageData].Add(firingSource);
        } else
        {       
            firingSource.destroyWhenStopped = false;
            GfPooling.DestroyInsert(firingSource.gameObject);
        }

        foreach (Transform child in firingSource.transform)
        {
            ParticleSingleDamage psd = child.GetComponent<ParticleSingleDamage>();
            DestroyFiringSource(ref psd);
        }

        firingSource = null;
    }
}

[System.Serializable]
public class ParticleSingleDamageData
{
    public float damage = 10;
    public bool canDamageSelf = false;
}
