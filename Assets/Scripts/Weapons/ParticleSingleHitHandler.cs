using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private Dictionary<ParticleSingleHit, ParticleSingleHitSystem> activeSystems = new(1);

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
        if (currentParticleSingleHit != null)
        {
            currentParticleSingleHit.Stop();
            ParticleSingleHit.DestroyFiringSource(ref currentParticleSingleHit);
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
        oldParticleSingleHit = currentParticleSingleHit;
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

    public void Play()
    {
        if (null == currentParticleSingleHit)
        {
            if (null != oldParticleSingleHit && oldParticleSingleHit.enabled
                && oldParticleSingleHit.GetStatsCharacter() == statsCharacter
                && oldParticleSingleHit.copiedParticleSingleHit == copiedParticleSingleHit)
            {
                currentParticleSingleHit = copiedParticleSingleHit;
            } 
            else
            {
                currentParticleSingleHit = ParticleSingleHit.GetNewFiringSource(copiedParticleSingleHit);
            }

            SetValuesForSource();
        }

        currentParticleSingleHit.Play();
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

    public void SetParticleSystemHit(ParticleSingleHit psh)
    {
        copiedParticleSingleHit = psh;

        if (null == currentParticleSingleHit)
        {
            currentParticleSingleHit = ParticleSingleHit.GetNewFiringSource(psh);
        }
        else
        {
            ParticleSingleHit.SetNewParticleSingleDamage(psh, ref currentParticleSingleHit);
        }

        SetValuesForSource();
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
} 
