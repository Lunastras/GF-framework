using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleTurret : MonoBehaviour
{
    [SerializeField]
    private ParticleTurretPhase[] turretPhases;

    [SerializeField]
    private float[] timeBetweenPhases;

    [SerializeField]
    private StatsCharacter statsCharacter;

    private List<ParticleSingleHitSystem> pshSystems;

    private static readonly float[] DEFAULT_WAIT_TIME = { 0 };

    private int currentPhaseIndex = -1;

    private bool firing = false;

    // Start is called before the first frame update
    void Start()
    {
        if (timeBetweenPhases.Length == 0)
            timeBetweenPhases = DEFAULT_WAIT_TIME;

        int highestCountSystems = 1;

        if (null == statsCharacter)
            statsCharacter = GetComponent<StatsCharacter>();

        foreach (ParticleTurretPhase phase in turretPhases)
        {
            highestCountSystems = Mathf.Max(highestCountSystems, phase.particleSystems.Length);
        }

        pshSystems = new(highestCountSystems);

        // for(int i = 0; i < pshSystems.Count; ++i)
        //   pshSystems.Add(null);

        Play(0);
    }

    public void Stop()
    {
        for (int i = pshSystems.Count - 1; i >= 0; --i)
        {
            ParticleSingleHitSystem psh = pshSystems[i];
            psh.Clear();

            ObjectPool<ParticleSingleHitSystem>.Store(ref psh);

            pshSystems.RemoveAt(i);
        }

        firing = false;
    }

    public void Play(int phase = -1)
    {
        phase = System.Math.Max(0, System.Math.Max(phase, currentPhaseIndex));

        Debug.Log("duck1");

        if(phase != currentPhaseIndex)
        {
            Debug.Log("duck2");

            Stop();
            currentPhaseIndex = phase;

            Debug.Log("duck3");


            int numSystems = turretPhases[phase].particleSystems.Length;

            for(int i = 0; i < numSystems; ++i)
            {
                pshSystems.Add(ObjectPool<ParticleSingleHitSystem>.Get());
                pshSystems[i].transform = transform;
                pshSystems[i].SetStatsCharacter(statsCharacter);
                pshSystems[i].SetParticleSystemHit(turretPhases[phase].particleSystems[i]);
            }

            Debug.Log("duck4");

        }

        if (!firing)
        {
            Debug.Log("duck5");

            for (int i = 0; i < pshSystems.Count; ++i)
                pshSystems[i].Play();

            firing = true;
        }
    }
}

[System.Serializable]
public class ParticleTurretPhase
{
    public ParticleSingleHit[] particleSystems;
}
