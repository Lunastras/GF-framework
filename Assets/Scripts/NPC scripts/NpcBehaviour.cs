using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcBehaviour : NpcController
{
    [System.Serializable]
    public class HostileValues
    {
        public float likelyHoodToCircleTarget = 0.0f;
        public float timeBetweenPhases = 4;

        public float timeBetweenFire = 5;
        public float varianceInValues = 0.2f;
        public bool runsIfLowHealth = true;
    }

    protected enum EngageModes
    {
        APPROACH,
        CIRLCE_CLOCKWISE,
        CIRCLE_COUNTER
    }


    [SerializeField]
    protected HostileValues hostileValues;

    [SerializeField]
    protected ParticleSingleHit turret;

    [SerializeField]
    protected float intervalBetweenWalkPhases = 4.0f;

    [SerializeField]
    protected float intervalBetweenEnemyChecks = 4.0f;

    [SerializeField]
    protected float varianceRate = 0.5f;

    [SerializeField]
    protected float walkSpeedMultiplyer = 0.5f;

    //The maximum distance from spawn until it goes back while walking
    [SerializeField]
    protected float maxDstFromSpawnWalk = 10;

    private float timeOfNextWalkChange = 0;

    private Vector3 spawnPos;

    protected float timeOfChangePhase = 0;

    protected float timeOfFireChange = 0;
    protected EngageModes currentPhase;

    private StatsNpc statsNpc;

    private float timeOfNextEnemyCheck = 0;
    float desiredYDir = 0;



    // Start is called before the first frame update
    void Awake()
    {
        Initialize();
        //hostileValues = new HostileValues();
    }


    void Start()
    {
        // GetDestinationManager().isEma();

        // destinations.SetDestination(GameManager.gameManager.GetPlayer(), true, true);
        spawnPos = transform.position;

        float randomRadian = GfRandom.Range(0, Mathf.PI * 2.0f);
        simpleMovement.SetMovementDir(new Vector3(Mathf.Cos(randomRadian), desiredYDir, Mathf.Sin(randomRadian)));

        if (null == statsNpc)
            statsNpc = GetComponent<StatsNpc>();

        if (null == turret)
            turret = GetComponent<ParticleSingleHit>();
    }

    private GameObject CheckForEnemiesAround() //TODO
    {   /*
        Dictionary<CharacterTypes, HashSet<StatsCharacter>> enemyDict = HostilityManager.GetEnemiesList(statsNpc);
        foreach (CharacterTypes type in enemyDict.Keys)
        {
            foreach (StatsCharacter character in enemyDict[type])
            {
                // Debug.Log("currently checked enemy is " + character.name);
                if (CheckCanSeeTarget(character.transform, lineOfSightLength / 2.0f, false, false, true))
                {
                    return character.gameObject;
                }
            }
        }*/

        return null;
    }

    float horizontalDirOffset;
    protected override void EngageEnemyBehaviour(Vector3 dirToTarget)
    {
        float currentTime = Time.time;
        if (currentTime > timeOfChangePhase)
        {
            timeOfChangePhase = currentTime + hostileValues.timeBetweenPhases
                                * (1.0f + GfRandom.Range(-hostileValues.varianceInValues, hostileValues.varianceInValues));

            float randomNumber = GfRandom.GetRandomNum();
            desiredYDir = GfRandom.Range(-0.9f, 0.2f);

            // Debug.Log("random num: " + randomNumber + " and circle likelyhood: " + hostileValues.likelyHoodToCircleTarget);

            if (randomNumber > hostileValues.likelyHoodToCircleTarget) //approach target
            {
                //Debug.Log("APPROACH");
                currentPhase = EngageModes.APPROACH;
            }
            else
            {
                //Debug.Log("CIRCLE");
                currentPhase = (randomNumber >= hostileValues.likelyHoodToCircleTarget / 2.0f)
                            ? EngageModes.CIRLCE_CLOCKWISE : EngageModes.CIRCLE_COUNTER;
            }
        }

        if (currentTime > timeOfFireChange)
        {

            if (!destination.WasDestroyed() && null != turret)
            {
                Transform enemyTransform = destination.TransformDest;
                turret.target = enemyTransform;
                turret.Play();
                //PauseMovement(turret.GetCurrentPhaseLength());
            }

            timeOfFireChange = currentTime + hostileValues.timeBetweenFire
                                * (1 + GfRandom.Range(-hostileValues.varianceInValues, hostileValues.varianceInValues));
        }


        Vector2 horizontalDir = new Vector2(dirToTarget.x, dirToTarget.z);
        Vector2 newDir;

        switch (currentPhase)
        {
            case EngageModes.CIRLCE_CLOCKWISE:
                newDir = new Vector2(-horizontalDir.y, horizontalDir.x);
                horizontalDir = (newDir + horizontalDirOffset * horizontalDir).normalized;
                break;

            case EngageModes.CIRCLE_COUNTER:
                newDir = new Vector2(horizontalDir.y, -horizontalDir.x);
                horizontalDir = (newDir + horizontalDirOffset * horizontalDir).normalized;
                break;
            case EngageModes.APPROACH:
                // desiredYDir = dirToTarget.y;
                horizontalDir = horizontalDir.normalized;
                break;
        }

        horizontalDir *= 1 + Mathf.Abs(desiredYDir);

        simpleMovement.SetMovementDir(new Vector3(horizontalDir.x, desiredYDir, horizontalDir.y));
    }


    protected override void NoDestinationsBehaviour()
    {
        currentSpeedMultiplier = walkSpeedMultiplyer;
        if (Time.time >= timeOfNextWalkChange)
        {
            statsNpc.ClearDamageList();
            // turret.Pause();

            timeOfNextWalkChange = Time.time + intervalBetweenWalkPhases * (1 - GfRandom.Range(-varianceRate, varianceRate));

            float movementDirMagnitude = simpleMovement.MovementDirRaw.magnitude;

            if (movementDirMagnitude > 0.9f)
            {
                simpleMovement.SetMovementDir(Vector3.zero);
            }
            else
            {
                Vector3 dirToSpawn = spawnPos - transform.position;
                dirToSpawn.y = 0;
                desiredYDir = (GfRandom.GetRandomNum() * 2.0f) - 1.0f;

                if (dirToSpawn.magnitude >= maxDstFromSpawnWalk)
                {
                    simpleMovement.SetMovementDir(dirToSpawn);
                }
                else
                {
                    float randomRadian = GfRandom.Range(0, Mathf.PI * 2.0f);
                    simpleMovement.SetMovementDir(new Vector3(Mathf.Cos(randomRadian), desiredYDir, Mathf.Sin(randomRadian)));
                }
            }
        }

        if (Time.time >= timeOfNextEnemyCheck)
        {
            timeOfNextEnemyCheck = Time.time + intervalBetweenEnemyChecks * (1 - GfRandom.Range(-varianceRate, varianceRate));
            //  Debug.Log("I am check for enemies around me");
            GameObject target = CheckForEnemiesAround();
            if (target != null)
            {
                //Debug.Log("Found SOMETHING AA " + target.name);
                SetDestination(target.transform, true, true);
            }
            else
            {
                // Debug.Log("Found nothing");
            }
        }
    }

    protected override void LowLifeBehaviour(Vector3 dirToTarget)
    {
        Debug.Log("i have low life");
        simpleMovement.SetMovementDir(-dirToTarget);
        turret.Stop();
    }


    protected override void LostTargetBehaviour()
    {
        // Debug.Log("i lost the target");
        PauseMovement(1);
        simpleMovement.SetMovementDir(Vector3.zero);
        turret.Stop();
        npcState = NpcState.NO_DESTINATION;
        SetDestination(null);
    }

    protected override void DestinationDestroyedBehaviour()
    {
        Debug.Log("enemy was destroyed");
        simpleMovement.SetMovementDir(Vector3.zero);
        turret.Stop();
        npcState = NpcState.NO_DESTINATION;
        SetDestination(null);
    }
}
