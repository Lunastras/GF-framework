using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretBehaviour : MonoBehaviour
{
    [System.Serializable]
    public enum PhasesEndModes
    {
        DESTROY_GAMEOBJECT,
        DESTROY_THIS,
        LOOP
    }

    [System.Serializable]
    public struct FiringPhase
    {
        [SerializeField]
        public BulletEmitter[] emitters;
    }
    [SerializeField]
    private float[] timeBetweenPhases = null;

    [SerializeField]
    private bool pausesBetweenPhases = true;

    [SerializeField]
    private bool playsOnStart = false;

    [SerializeField]
    private StatsCharacter characterStats = null;

    [SerializeField]
    private PhasesEndModes phasesEndBehaviour = PhasesEndModes.LOOP;

    [SerializeField]
    private FiringPhase[] firingPhases = null;

    private Transform currentTarget = null;
    private Transform currentSpawnPoint = null;
    public int currentPhaseIndex { get; private set; } = 0;
    private float timeOfUnpause = 0;
    private float[] phasesLength = null;

    // Start is called before the first frame update
    void Start()
    {
        if (characterStats == null)
        {
            characterStats = GetComponent<StatsCharacter>();
            if (characterStats == null)
            {
                Debug.LogError("No characterStats set");
            }
        }

        foreach (FiringPhase phase in firingPhases)
        {
            foreach (BulletEmitter emitter in phase.emitters)
            {
                emitter.SetCharacterStats(characterStats);
                emitter.SetSpawnTransform(transform);
            }
        }

        if (firingPhases.Length == 0)
        {
            Debug.LogError("No firing phases exist");
        }

        if (timeBetweenPhases.Length == 0)
        {
            timeBetweenPhases = new float[1];
            timeBetweenPhases[0] = 0.1f;
        }



        UpdatePhasesLength();

        //SetTarget(GameManager.gameManager.GetPlayer());
    }

    void OnEnable()
    {
        currentPhaseIndex = 0;
        timeOfUnpause = playsOnStart ? 0 : float.MaxValue;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Time.time >= timeOfUnpause)
        {
            bool canStillSpawn = false;

            foreach (BulletEmitter emitter in firingPhases[currentPhaseIndex].emitters)
            {
                canStillSpawn |= emitter.Spawn();
            }

            if (!canStillSpawn)
            {
                EndCurrentPhase();

                if (pausesBetweenPhases)
                {
                    Pause();
                }
                else
                {
                    Play(timeBetweenPhases[Mathf.Min(timeBetweenPhases.Length - 1, currentPhaseIndex)]);
                }
            }
        }
    }

    /**Ends the current phase and goes to the next phase
    *Note: This won't play the next phase if pausesBetweenPhases is true
    */
    public void EndCurrentPhase()
    {
        ++currentPhaseIndex;
        if (currentPhaseIndex >= firingPhases.Length)
        {
            switch (phasesEndBehaviour)
            {

                case (PhasesEndModes.DESTROY_GAMEOBJECT):
                    GfPooling.Destroy(gameObject);
                    break;

                case (PhasesEndModes.DESTROY_THIS):
                    Destroy(this);
                    break;

                case (PhasesEndModes.LOOP):
                    currentPhaseIndex = 0;
                    break;
            }
        }

        SetupNewPhase();
    }

    private void SetupNewPhase()
    {
        FiringPhase currentPhase = firingPhases[currentPhaseIndex];

        foreach (BulletEmitter emitter in currentPhase.emitters)
        {
            emitter.Restart();
        }

    }

    /**PlaysNextPhase
    *@param delayInSeconds The delay until the next phase 
    */
    public void PlayNextPhase(float delayInSeconds)
    {
        EndCurrentPhase();
        Play(delayInSeconds);
    }

    /**Plays the given phase
    *@phaseIndex the index of the firing phase
    *@delayInSeconds the delay in seconds until the phase is played
    */

    public void PlayPhase(int phaseIndex, float delayInSeconds = 0)
    {
        currentPhaseIndex = phaseIndex;
        if (phaseIndex >= firingPhases.Length)
        {
            Debug.LogError("Given index is higher than length of phases");
        }
        SetupNewPhase();
        timeOfUnpause = Time.time + delayInSeconds;
    }

    public void SetCurrentPhase(int phaseIndex)
    {
        if (phaseIndex >= firingPhases.Length || phaseIndex < 0)
        {
            Debug.LogError("Given index is not valid: " + phaseIndex);
        }

        currentPhaseIndex = phaseIndex;
        SetupNewPhase();
    }

    public void Pause(float durationInSeconds = float.MaxValue)
    {
        timeOfUnpause = Time.time + durationInSeconds;
    }

    public void Play(float delayInSeconds = 0)
    {
        timeOfUnpause = Mathf.Min(timeOfUnpause, Time.time + Mathf.Max(0, delayInSeconds));
    }

    public void SetTarget(Transform target)
    {
        if (target == null || target == currentTarget)
            return;

        foreach (FiringPhase phase in firingPhases)
        {
            foreach (BulletEmitter emitter in phase.emitters)
            {
                emitter.SetTarget(target);
            }
        }
    }

    public void SetSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint == null || currentSpawnPoint == spawnPoint)
            return;

        foreach (FiringPhase phase in firingPhases)
        {
            foreach (BulletEmitter emitter in phase.emitters)
            {
                emitter.SetSpawnTransform(spawnPoint);
            }
        }
    }

    public void SetEndMode(PhasesEndModes mode)
    {
        phasesEndBehaviour = mode;
    }

    /** Get the length in seconds of the currentPhase
    *NOTE: This fucntion does not support dynamic length of emitters
    *@return the length in seconds
    */
    public float GetCurrentPhaseLength()
    {
        return phasesLength[currentPhaseIndex];
    }

    /**Updates the length of the phases in phasesLength
    *NOTE: This fucntion does not support dynamic length of emitters
    */
    public void UpdatePhasesLength()
    {
        phasesLength = new float[firingPhases.Length];

        for (int i = 0; i < firingPhases.Length; i++)
        {
            float maxLength = 0;
            foreach (BulletEmitter emitter in firingPhases[i].emitters)
            {
                float emitterLength = emitter.GetLength();
                if (emitterLength > maxLength)
                {
                    maxLength = emitterLength;
                }
            }
            phasesLength[i] = maxLength;
        }
    }

    public int GetNumberOfPhases()
    {
        return firingPhases.Length;
    }
}
