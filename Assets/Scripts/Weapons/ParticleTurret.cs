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

    [SerializeField]
    private bool autoPlay;

    [SerializeField]
    private bool autoRestarts;

    private static readonly float[] DEFAULT_WAIT_TIME = { 0 };

    private int currentPhaseIndex = 0;

    private bool firing = false;

    private float timeUntilPlay = 0;
    private int requestedPhase;

    private float timeUntilUnpause = 0;

    private 

    // Start is called before the first frame update
    void Start()
    {
        if (timeBetweenPhases.Length == 0)
            timeBetweenPhases = DEFAULT_WAIT_TIME;

        if (null == statsCharacter)
            statsCharacter = GetComponent<StatsCharacter>();
    }

    private void FixedUpdate() {
       
        if(timeUntilUnpause <= 0) {
            if(autoPlay && firing) {

                ParticleSingleHit[] systems = turretPhases[currentPhaseIndex].particleSystems;
                int length = systems.Length;
                bool isPlaying = false;
                for(int i = 0; i < length && !isPlaying; ++i) {
                    isPlaying |= systems[i].particleSystem.IsAlive(true);
                }

                if(!isPlaying) { //phase ended
                    ++currentPhaseIndex;

                    bool playNext = currentPhaseIndex < turretPhases.Length;

                    if(currentPhaseIndex >= turretPhases.Length && autoRestarts) {
                        currentPhaseIndex = 0;
                    } else {
                        playNext = false;
                    }

                    if(playNext)
                        Play();
                
                }
            }
        
            if(timeUntilPlay > 0) {

                timeUntilPlay -= Time.deltaTime;

                if(timeUntilPlay <= 0) {
                    Play(requestedPhase);  
                }
            }
        } else {
            timeUntilUnpause -= Time.deltaTime;
            if(timeUntilUnpause <= 0) {
                UnPause();
            }
        }     
    }

    public void Stop()
    {
        if(firing) {
            ParticleSingleHit[] systems = turretPhases[currentPhaseIndex].particleSystems;

            for (int i = systems.Length - 1; i >= 0; --i)
            {
                systems[i].Stop();
            }
           
            firing = false;
        }       
    }

    public int GetNumPhases() {
        return turretPhases.Length;
    }

    public void Pause(float duration = float.MaxValue) {
        if(duration > 0) {
            timeUntilUnpause = duration;

            ParticleSingleHit[] systems = turretPhases[currentPhaseIndex].particleSystems;

            foreach(ParticleSingleHit system in systems) {
                system.particleSystem.Pause(true);
            }
        }     
    }

    public void UnPause() {
        if(timeUntilUnpause > 0) {
            timeUntilUnpause = 0;

            ParticleSingleHit[] systems = turretPhases[currentPhaseIndex].particleSystems;

            foreach(ParticleSingleHit system in systems) {
                system.particleSystem.Play();
            }
        }
    }

    public void Play(int phase, float delay) {
        currentPhaseIndex = phase;
        timeUntilPlay = delay;
        requestedPhase = phase;
    }

    public void Play(int phase = -1, bool forcePlay = false)
    {
        Debug.Log("I AM FIRING " + gameObject.name);
        phase = System.Math.Max(0, System.Math.Max(phase, currentPhaseIndex));

        bool phaseChanged = phase != currentPhaseIndex;
        firing |= forcePlay;

        if(phaseChanged) {
            currentPhaseIndex = phase;
            firing = false;
            Stop();
        }       

        if(!firing) {
            firing = true;
            ParticleSingleHit[] systems = turretPhases[currentPhaseIndex].particleSystems;
            foreach(ParticleSingleHit system in systems) {
                system.Play();
            }
        }
    }

    public void SetStatsCharacter(StatsCharacter stats) {
        statsCharacter = stats;
        foreach(ParticleTurretPhase phase in turretPhases) {
            foreach(ParticleSingleHit hit in phase.particleSystems) {
                hit.SetStatsCharacter(stats);
            }
        }
    }

    public void SetRotation(Quaternion rotation) {
        foreach(ParticleSingleHit hit in turretPhases[currentPhaseIndex].particleSystems) {
                hit.transform.rotation = rotation;
        }      
    }

    public void SetTarget(Transform target) {
        foreach(ParticleSingleHit hit in turretPhases[currentPhaseIndex].particleSystems) {
                hit.target = target;
        }
    }
}

[System.Serializable]
public class ParticleTurretPhase
{
    public ParticleSingleHit[] particleSystems;
}
