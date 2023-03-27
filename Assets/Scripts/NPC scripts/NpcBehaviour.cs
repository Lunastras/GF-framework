using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcBehaviour : NpcController
{
    [System.Serializable]
    public struct HostileValues
    {
        public float m_likelyHoodToCircleTarget;
        public float m_timeBetweenPhases;
        //the angle between the direction to the enemy and the direction of the circling movement
        public float m_circleAngleDegFromTarget;

        public float m_timeBetweenAttacks;
        public float m_varianceInValues;
        public bool m_runsIfLowHealth;
    }

    protected enum EngageModes
    {
        APPROACH,
        CIRLCE_CLOCKWISE,
        CIRCLE_COUNTER
    }


    [SerializeField]
    protected HostileValues m_hostileValues;

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

    private float m_timeUntilNextWalkChange = 0;

    private Vector3 m_spawnPos = default;

    protected float m_timeUntilChangePhase = 0;

    protected float m_timeUntilFireChange = 0;
    protected EngageModes m_currentPhase;

    private StatsNpc m_statsNpc = null;

    private float m_timeUntilNextEnemyCheck = 0;

    // Start is called before the first frame update
    void Awake()
    {
        Initialize();
    }


    void Start()
    {
        // GetDestinationManager().isEma();

        // destinations.SetDestination(GameManager.gameManager.GetPlayer(), true, true);
        m_spawnPos = m_transform.position;

        float randomRadian = GfRandom.Range(0, Mathf.PI * 2.0f);
        m_movement.SetMovementDir(-new Vector3(Mathf.Cos(randomRadian), 0, Mathf.Sin(randomRadian)).normalized);

        if (null == m_statsNpc)
            m_statsNpc = GetComponent<StatsNpc>();

        if (null == m_turret)
            m_turret = GetComponent<WeaponTurret>();

        if (m_turret)
            m_turret.SetStatsCharacter(m_statsNpc);
    }

    protected override void BeforeStateUpdate(float deltaTime)
    {
        m_timeUntilChangePhase -= deltaTime;
        m_timeUntilFireChange -= deltaTime;
        m_timeUntilNextWalkChange -= deltaTime;
        m_timeUntilNextEnemyCheck -= deltaTime;
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
                    if (CheckCanSeeTarget(character.transform, m_lineOfSightLength, false, false, true))
                    {
                        return character.gameObject;
                    }
                }
            }
        }

        return null;
    }

    protected override void EngageEnemyBehaviour(float deltaTime, Vector3 dirToTarget)
    {
        if (0 > m_timeUntilFireChange)
        {

            if (!m_destination.WasDestroyed() && null != m_turret)
            {
                Transform enemyTransform = m_destination.TransformDest;
                m_turret.SetTarget(enemyTransform);
                m_turret.Play(true);
            }

            m_timeUntilFireChange = m_hostileValues.m_timeBetweenAttacks
                                * (1 + GfRandom.Range(-m_hostileValues.m_varianceInValues, m_hostileValues.m_varianceInValues));
        }

        if (0 > m_timeUntilChangePhase)
        {
            m_timeUntilChangePhase = m_hostileValues.m_timeBetweenPhases
                                * (1.0f + GfRandom.Range(-m_hostileValues.m_varianceInValues, m_hostileValues.m_varianceInValues));

            float randomNumber = GfRandom.GetRandomNum();

            if (randomNumber > m_hostileValues.m_likelyHoodToCircleTarget) //approach target
            {
                m_currentPhase = EngageModes.APPROACH;
            }
            else
            {
                m_currentPhase = EngageModes.CIRLCE_CLOCKWISE;
                if (GfRandom.GetRandomNum() >= 0.5f)
                    m_currentPhase = EngageModes.CIRCLE_COUNTER;
            }
        }


        Vector3 movementDir = dirToTarget;
        GfTools.Normalize(ref dirToTarget);

        switch (m_currentPhase)
        {
            case EngageModes.CIRLCE_CLOCKWISE:
                dirToTarget = Quaternion.AngleAxis(-m_hostileValues.m_circleAngleDegFromTarget, m_movement.GetUpVecRaw()) * dirToTarget;
                break;

            case EngageModes.CIRCLE_COUNTER:
                dirToTarget = Quaternion.AngleAxis(m_hostileValues.m_circleAngleDegFromTarget, m_movement.GetUpVecRaw()) * dirToTarget;
                break;
        }

        m_movement.SetMovementDir(dirToTarget);
    }


    protected override void NoDestinationsBehaviour(float deltaTime)
    {
        m_currentSpeedMultiplier = m_walkSpeedMultiplyer;
        if (0 >= m_timeUntilNextWalkChange)
        {
            m_timeUntilNextWalkChange = m_intervalBetweenWalkPhases * (1 - GfRandom.Range(-m_varianceRate, m_varianceRate));

            float movementDirMagnitude = m_movement.MovementDirRaw.magnitude;

            if (movementDirMagnitude > 0.9f)
            {
                m_movement.SetMovementDir(Vector3.zero);
            }
            else
            {
                Vector3 dirToSpawn = m_spawnPos - m_transform.position;
                dirToSpawn.y = 0;

                if (dirToSpawn.magnitude >= m_maxDstFromSpawnWalk)
                {
                    m_movement.SetMovementDir(dirToSpawn.normalized);
                }
                else
                {
                    float randomRadian = GfRandom.Range(0, 360);
                    m_movement.SetMovementDir(Random.insideUnitSphere);
                }
            }
        }

        if (0 >= m_timeUntilNextEnemyCheck)
        {
            m_timeUntilNextEnemyCheck = m_intervalBetweenEnemyChecks * (1 - GfRandom.Range(-m_varianceRate, m_varianceRate));
            //  Debug.Log("I am check for enemies around me");
            GameObject target = CheckForEnemiesAround();
            if (target != null) SetDestination(target.transform, true, true);
        }
    }

    protected override void CalculatePathDirection(float deltaTime, Vector3 dirToTarget)
    {
        if (m_turret) m_turret.Stop();
        m_movement.SetMovementDir(dirToTarget.normalized);
    }

    protected override void LowLifeBehaviour(float deltaTime, Vector3 dirToTarget)
    {
        //   Debug.Log("i have low life");
        m_movement.SetMovementDir(-dirToTarget.normalized);
        m_turret.Stop();
    }


    protected override void LostTargetBehaviour(float deltaTime)
    {
        //Debug.Log("i lost the target");
        PauseMovement(1);
        m_movement.SetMovementDir(Vector3.zero);
        m_turret.Stop();
        m_currentState = NpcState.NO_DESTINATION;
        SetDestination(null);
    }

    protected override void DestinationDestroyedBehaviour(float deltaTime)
    {
        //  Debug.Log("enemy was destroyed");
        m_movement.SetMovementDir(Vector3.zero);
        m_turret.Stop();
        m_currentState = NpcState.NO_DESTINATION;
        SetDestination(null);
    }
}
