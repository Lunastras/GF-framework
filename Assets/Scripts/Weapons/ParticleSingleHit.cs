using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleSingleHit : ParticleCollision
{
    [SerializeField]
    private ParticleSingleDamageData damageData;

    [SerializeField]
    private StatsCharacter statsCharacter;

    public Transform target { get; set; } = null;

    private static Dictionary<ParticleSingleDamageData, HashSet<ParticleSingleHit>> releasedFireSources = new(23);

    public void Play()
    {
        particleSystem.Play();
    }

    private void OnEnable()
    {
        statsCharacter = null;
        target = null;
    }

    private void OnDisable()
    {
        statsCharacter = null;
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
        if (transform.parent == WeaponMaster.GetActiveFireSourcesParent() && !particleSystem.IsAlive(true))
        {
            ParticleSingleHit psd = this;
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
            child.GetComponent<ParticleSingleHit>().SetStatsCharacter(value);
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

    private static void CopySubEmitters(ParticleSingleHit original, ref ParticleSingleHit copy)
    {
        var originalEmitterModule = original.particleSystem.subEmitters;
        if (originalEmitterModule.enabled)
        {
            int childCount = copy.transform.childCount;

            while (0 <= --childCount)
            {
                ParticleSingleHit child = copy.transform.GetChild(0).GetComponent<ParticleSingleHit>();
                DestroyFiringSource(ref child);
            }

            var copyEmitterModule = copy.particleSystem.subEmitters;
            int countEmitters = copyEmitterModule.subEmittersCount;

            for (int i = 0; i < countEmitters; ++i)
            {
                ParticleSingleHit originalChild = originalEmitterModule.GetSubEmitterSystem(i).GetComponent<ParticleSingleHit>();
                ParticleSingleHit child = GetNewFiringSource(originalChild);

                child.transform.SetParent(copy.transform);

                child.transform.localPosition = copy.transform.localPosition;
                child.transform.localRotation = copy.transform.localRotation;
            }
        }
    }

    public static void SetNewParticleSingleDamage(ParticleSingleHit original, ref ParticleSingleHit copy)
    {
        // Debug.Log("the copied ps is " + original.name);
        // Debug.Log("and it's copied for " + copy.name);

        bool hasInstanceOfOriginal = HasInstanceOf(original);

        if (copy.particleSystem.IsAlive(true) || hasInstanceOfOriginal)
        {
            ParticleSingleHit newSource = GetNewFiringSource(original);
            newSource.transform.SetParent(copy.transform.parent);
            newSource.target = copy.target;
            newSource.SetStatsCharacter(copy.statsCharacter);

            DestroyFiringSource(ref copy);
            copy = newSource;        
        }


        copy.transform.localPosition = original.transform.localPosition;
        copy.transform.localRotation = original.transform.localRotation;

        if (releasedFireSources.ContainsKey(copy.damageData))
            releasedFireSources[copy.damageData].Remove(copy);

        copy.SetDamageData(original.damageData);

        //copy the particle system if an instantiated one couldn't be found
        if(!hasInstanceOfOriginal)
            CopyParticleSystem.CopyFrom(original.particleSystem, copy.particleSystem);

        CopySubEmitters(original, ref copy);
    }

    private static bool HasInstanceOf(ParticleSingleHit value)
    {
        return releasedFireSources.ContainsKey(value.damageData) && 0 < releasedFireSources[value.damageData].Count;
    }

    private static ParticleSingleHit GetCopyOf(ParticleSingleHit value)
    {
        if(releasedFireSources.ContainsKey(value.damageData))
        {
            foreach (ParticleSingleHit item in releasedFireSources[value.damageData])
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

    public static ParticleSingleHit GetNewFiringSource(ParticleSingleHit copy = null)
    {
        ParticleSingleHit psh;

        if (null != copy && HasInstanceOf(copy))
        {
            psh = GetCopyOf(copy);
            psh.gameObject.SetActive(true);
            psh.transform.localPosition = copy.transform.localPosition;
            psh.transform.localRotation = copy.transform.localRotation;
            CopySubEmitters(copy, ref psh);       
        } 
        else
        {
            psh = GfPooling.PoolInstantiate(WeaponMaster.GetTemplate()).GetComponent<ParticleSingleHit>();

            if (releasedFireSources.ContainsKey(psh.damageData))
                releasedFireSources[psh.damageData].Remove(psh);

            if (null != copy)
                SetNewParticleSingleDamage(copy, ref psh);
        }

        if (releasedFireSources.ContainsKey(copy.damageData))
            releasedFireSources[copy.damageData].Remove(copy);

        return psh;
    }

    public static void DestroyFiringSource(ref ParticleSingleHit firingSource)
    {
        if (firingSource.particleSystem.IsAlive(true))
        {
            firingSource.Stop();
            firingSource.transform.SetParent(WeaponMaster.GetActiveFireSourcesParent());           
        } else
        {       
            GfPooling.DestroyInsert(firingSource.gameObject);
        }

        if (!releasedFireSources.ContainsKey(firingSource.damageData))
            releasedFireSources.Add(firingSource.damageData, new(7));

        if (false == releasedFireSources[firingSource.damageData].Contains(firingSource))
            releasedFireSources[firingSource.damageData].Add(firingSource);

        foreach (Transform child in firingSource.transform)
        {
            ParticleSingleHit psd = child.GetComponent<ParticleSingleHit>();
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
