using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcBehaviour : NpcController
{
    [System.Serializable]
    public class HostileValues
    {
        public float m_likelyHoodToCircleTarget = 0.0f;
        public float m_timeBetweenPhases = 4;

        public float m_timeBetweenFire = 5;
        public float m_varianceInValues = 0.2f;
        public bool m_runsIfLowHealth = true;
    }

    protected enum EngageModes
    {
        APPROACH,
        CIRLCE_CLOCKWISE,
        CIRCLE_COUNTER
    }


    [SerializeField]
    protected HostileValues m_hostileValues = null;

    [SerializeField]
    protected WeaponTurret m_turret = null;

    [SerializeField]
    protected float m_intervalBetweenWalkPhases = 4.0f;

    [SerializeField]
    protected float m_intervalBetweenEnemyChecks = 4.0f;

    [SerializeField]
    protected float m_varianceRate = 0.5f;

    [SerializeField]
    protected float m_walkSpeedMultiplyer = 0.5f;

    //The maximum distance from spawn until it goes back while walking
    [SerializeField]
    protected float m_maxDstFromSpawnWalk = 10;

    private float m_timeOfNextWalkChange = 0;

    private Vector3 m_spawnPos = default;

    protected float m_timeOfChangePhase = 0;

    protected float m_timeOfFireChange = 0;
    protected EngageModes m_currentPhase;

    private StatsNpc m_statsNpc = null;

    private float m_timeOfNextEnemyCheck = 0;
    float m_desiredYDir = 0;



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
        m_spawnPos = transform.position;

        float randomRadian = GfRandom.Range(0, Mathf.PI * 2.0f);
        m_movement.SetMovementDir(-new Vector3(Mathf.Cos(randomRadian), m_desiredYDir, Mathf.Sin(randomRadian)).normalized);

        if (null == m_statsNpc)
            m_statsNpc = GetComponent<StatsNpc>();

        if (null == m_turret)
            m_turret = GetComponent<WeaponTurret>();

        if (m_turret)
            m_turret.SetStatsCharacter(m_statsNpc);
    }

    private GameObject CheckForEnemiesAround()
    {
        int numTypes = HostilityManager.GetNumTypes();
        for (int i = 0; i < numTypes; ++i)
        {
            List<StatsCharacter> enemyList = HostilityManager.GetEnemiesList((int)(m_statsNpc.GetCharacterType()), i);
            if (null != enemyList)
            {
                int listLength = enemyList.Count;
                for (int j = 0; j < listLength; ++j)
                {
                    StatsCharacter character = enemyList[i];
                    if (CheckCanSeeTarget(character.transform, m_lineOfSightLength / 2.0f, false, false, true))
                    {
                        return character.gameObject;
                    }
                }
            }
        }

        return null;
    }

    float horizontalDirOffset = 0;
    protected override void EngageEnemyBehaviour(Vector3 dirToTarget)
    {
        float currentTime = Time.time;
        if (currentTime > m_timeOfChangePhase)
        {
            m_timeOfChangePhase = currentTime + m_hostileValues.m_timeBetweenPhases
                                * (1.0f + GfRandom.Range(-m_hostileValues.m_varianceInValues, m_hostileValues.m_varianceInValues));

            float randomNumber = GfRandom.GetRandomNum();
            m_desiredYDir = GfRandom.Range(-0.9f, 0.2f);

            // Debug.Log("random num: " + randomNumber + " and circle likelyhood: " + hostileValues.likelyHoodToCircleTarget);

            if (randomNumber > m_hostileValues.m_likelyHoodToCircleTarget) //approach target
            {
                //Debug.Log("APPROACH");
                m_currentPhase = EngageModes.APPROACH;
            }
            else
            {
                //Debug.Log("CIRCLE");
                m_currentPhase = (randomNumber >= m_hostileValues.m_likelyHoodToCircleTarget / 2.0f)
                            ? EngageModes.CIRLCE_CLOCKWISE : EngageModes.CIRCLE_COUNTER;
            }
        }

        if (currentTime > m_timeOfFireChange)
        {

            if (!m_destination.WasDestroyed() && null != m_turret)
            {
                Transform enemyTransform = m_destination.TransformDest;
                m_turret.SetTarget(enemyTransform);
                m_turret.Play(true);
            }

            m_timeOfFireChange = currentTime + m_hostileValues.m_timeBetweenFire
                                * (1 + GfRandom.Range(-m_hostileValues.m_varianceInValues, m_hostileValues.m_varianceInValues));
        }


        Vector2 horizontalDir = new Vector2(dirToTarget.x, dirToTarget.z);
        Vector2 newDir;

        switch (m_currentPhase)
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

        horizontalDir *= 1 + Mathf.Abs(m_desiredYDir);

        m_movement.SetMovementDir(new Vector3(horizontalDir.x, m_desiredYDir, horizontalDir.y).normalized);
    }


    protected override void NoDestinationsBehaviour()
    {
        m_currentSpeedMultiplier = m_walkSpeedMultiplyer;
        if (Time.time >= m_timeOfNextWalkChange)
        {
            m_timeOfNextWalkChange = Time.time + m_intervalBetweenWalkPhases * (1 - GfRandom.Range(-m_varianceRate, m_varianceRate));

            float movementDirMagnitude = m_movement.MovementDirRaw.magnitude;

            if (movementDirMagnitude > 0.9f)
            {
                m_movement.SetMovementDir(Vector3.zero);
            }
            else
            {
                Vector3 dirToSpawn = m_spawnPos - transform.position;
                dirToSpawn.y = 0;
                m_desiredYDir = (GfRandom.GetRandomNum() * 2.0f) - 1.0f;

                if (dirToSpawn.magnitude >= m_maxDstFromSpawnWalk)
                {
                    m_movement.SetMovementDir(dirToSpawn.normalized);
                }
                else
                {
                    float randomRadian = GfRandom.Range(0, Mathf.PI * 2.0f);
                    m_movement.SetMovementDir(new Vector3(Mathf.Cos(randomRadian), m_desiredYDir, Mathf.Sin(randomRadian)).normalized);
                }
            }
        }

        if (Time.time >= m_timeOfNextEnemyCheck)
        {
            m_timeOfNextEnemyCheck = Time.time + m_intervalBetweenEnemyChecks * (1 - GfRandom.Range(-m_varianceRate, m_varianceRate));
            //  Debug.Log("I am check for enemies around me");
            GameObject target = CheckForEnemiesAround();
            if (target != null) SetDestination(target.transform, true, true);
        }
    }

    protected override void LowLifeBehaviour(Vector3 dirToTarget)
    {
        Debug.Log("i have low life");
        m_movement.SetMovementDir(-dirToTarget.normalized);
        m_turret.Stop();
    }


    protected override void LostTargetBehaviour()
    {
        Debug.Log("i lost the target");
        PauseMovement(1);
        m_movement.SetMovementDir(Vector3.zero);
        m_turret.Stop();
        npcState = NpcState.NO_DESTINATION;
        SetDestination(null);
    }

    protected override void DestinationDestroyedBehaviour()
    {
        Debug.Log("enemy was destroyed");
        m_movement.SetMovementDir(Vector3.zero);
        m_turret.Stop();
        npcState = NpcState.NO_DESTINATION;
        SetDestination(null);
    }
}
