using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*

public class ParticleSingleHitHandler : MonoBehaviour
{
    [SerializeField]
    private ParticleSingleHitSystem pshSystem;

    public void Awake()
    {
        StatsCharacter stats = pshSystem.GetStatsCharacter();
        if (null == stats)
            pshSystem.SetStatsCharacter(GetComponent<StatsCharacter>());


        pshSystem.transform = transform;
    }

    public ParticleSingleHitSystem GetSystem()
    {
        return pshSystem;
    }
}


[System.Serializable]
public class ParticleSingleHitSystem
{
    [SerializeField]
    private ParticleSingleHit copiedParticleSingleHit;

    [SerializeField]
    private StatsCharacter statsCharacter;

    private ParticleSingleHit currentParticleSingleHit;

    private Transform target;

    private Quaternion rotationOverride = default;

    public Transform transform;

    private Dictionary<ParticleSingleDamageData, ParticleSingleHit> activeSystems = new();

    public bool isPlaying { get; private set; } = false;

    public void SetStatsCharacter(StatsCharacter stats)
    {
        statsCharacter = stats;

        if (null != currentParticleSingleHit)
            currentParticleSingleHit.SetStatsCharacter(stats);
    }

    public StatsCharacter GetStatsCharacter()
    {
        return statsCharacter;
    }

    public void Stop()
    {
        isPlaying = false;
        if (currentParticleSingleHit != null)
        {
            activeSystems.Add(currentParticleSingleHit.GetDamageData(), currentParticleSingleHit);
            ParticleSingleHit.DestroyFiringSource(ref currentParticleSingleHit);
        }
    }

    public void ReleaseSystem(ParticleSingleDamageData damageData)
    {
        if (activeSystems.TryGetValue(damageData, out ParticleSingleHit psh))
        {
            ParticleSingleHit.DestroyFiringSource(ref psh);
            activeSystems.Remove(damageData);
        }      
    }

    private void SetValuesForSource()
    {
        currentParticleSingleHit.transform.SetParent(transform);
        currentParticleSingleHit.SetStatsCharacter(statsCharacter);

        if (copiedParticleSingleHit != null)
        {
            currentParticleSingleHit.transform.localPosition = copiedParticleSingleHit.transform.localPosition;
            currentParticleSingleHit.transform.localRotation = copiedParticleSingleHit.transform.localRotation;
        }

        if (rotationOverride != default)
            currentParticleSingleHit.transform.rotation = rotationOverride;
        else
            currentParticleSingleHit.target = target;

        currentParticleSingleHit.gameObject.SetActive(true);
    }

    public void SetRotation(Quaternion rotation)
    {
        target = null;
        if (currentParticleSingleHit)
        {
            currentParticleSingleHit.target = null;
            currentParticleSingleHit.transform.rotation = rotation;
            rotationOverride = rotation;
        }
    }

    public void Play(bool forcePlay = false)
    {
        if(!isPlaying || forcePlay)
        {
            isPlaying = true;

            if (null == currentParticleSingleHit)
                AquireFiringSource();


            currentParticleSingleHit.Play();
        }    
    }

    public void SetTarget(Transform target)
    {
        this.target = target;

        if (null != currentParticleSingleHit)
            currentParticleSingleHit.target = target;

        rotationOverride = default;
    }

    public ParticleSingleHit GetCopiedParticleSystemHit()
    {
        return copiedParticleSingleHit;
    }

    private void AquireFiringSource()
    {
        if (null != copiedParticleSingleHit)
        {
            ParticleSingleDamageData damageData = copiedParticleSingleHit.GetDamageData();
            if (activeSystems.TryGetValue(damageData, out currentParticleSingleHit))
            {
                activeSystems.Remove(damageData);
            }
            else
            {
                currentParticleSingleHit = ParticleSingleHit.GetNewFiringSource(copiedParticleSingleHit);
            }        
        }
        else
        {
            Debug.LogError("The copied particle system is null, it needs to be set to a value");
        }

        SetValuesForSource();
    }

    public void SetParticleSystemHit(ParticleSingleHit psh)
    {
        copiedParticleSingleHit = psh;
        Stop();
    }

    public void SetParticleSystemHit(ParticleSingleHitHandler psh)
    {
        SetParticleSystemHit(psh.GetSystem().copiedParticleSingleHit);
    }

    public void SetParticleSystemHit(ParticleSingleHitSystem psh)
    {
        SetParticleSystemHit(psh.copiedParticleSingleHit);
    }

    public void Clear()
    {
        target = null;
        SetStatsCharacter(null);
        Stop();
    }
} */
