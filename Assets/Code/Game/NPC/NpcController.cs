using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Netcode;

using GfgPathFindingNamespace;

public class NpcController : MonoBehaviour
{

    [SerializeField]
    protected GfMovementGeneric m_movement;

    //how often the NPC will check if they have direct contact with the target
    //if it's smaller than 0, it will never use raycast checks;
    [SerializeField]
    protected float m_targetCheckCooldown = 0.3f;

    [SerializeField]
    protected float m_pathFindingCooldown = 0.3f;

    [SerializeField]
    protected float m_updateInterval = 0.05f;

    [SerializeField]
    protected float m_cosFieldOfView = 0.0f;

    [SerializeField]
    protected float m_lineOfSightLength = 40;

    [SerializeField]
    private float m_targetTrackingTimeWindow = 5.0f;

    [SerializeField]
    private GfgPathfinding m_pathFindingManager;

    private float m_timeUntilUnpause;
    private float m_timeUntilNextTargetCheck = 0;
    private float m_timeUntilNextStateUpdate = 0;
    private float m_timeUntilLosetarget = 0;

    //protected DestinationManager destinations;
    protected GfgDestination m_destination = new(null);

    //public bool canSeeTarget { get; private set; } = false;

    protected bool m_runsAwayFromTarget = false;

    protected float m_currentSpeedMultiplier;

    protected Transform m_transform;
    protected NpcState m_currentState;

    protected NativeList<float3> m_pathToDestination;

    //the interval in seconds the npc will keep track
    //of the exact position of its target upon losing sight of it

    protected enum NpcState
    {
        ENGAGING_TARGET, //can see target
        SEARCHING_TARGET, //cannot see target
        NO_DESTINATION,
        RUNNING_AWAY,
    }

    // Start is called before the first frame update
    protected virtual void Initialize()
    {
        m_pathToDestination = new(32, Allocator.Persistent);
        m_transform = transform;
        m_destination.SelfGfgStatsCharacter = GetComponent<GfgStatsCharacter>();

        if (m_movement == null)
        {
            m_movement = GetComponent<GfMovementGeneric>();
        }
    }

    private void OnEnable()
    {
        SetDestination(null);
        m_currentState = NpcState.NO_DESTINATION;
        //if m_transform is null, that means Initialize() was not called. If so, do not initialize the job
    }

    protected void OnDestroy()
    {
        if (m_pathToDestination.IsCreated) m_pathToDestination.Dispose();
        m_destination.RemoveDestination();
    }

    private void OnDisable()
    {
        m_destination.RemoveDestination();
    }

    public virtual void WasKilled()
    {
        m_destination.RemoveDestination();
    }

    public virtual bool GetPathFindingJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512)
    {
        bool scheduleJob = m_pathFindingManager
                        && (m_currentState == NpcState.SEARCHING_TARGET || m_currentState == NpcState.ENGAGING_TARGET)
                        && JobParent.CanSchedule(JobScheduleTypes.PATHFINDING)
                        && m_destination.HasDestination;

        if (scheduleJob)
        {
            m_pathToDestination.Clear();
            scheduleJob &= m_pathFindingManager.GetPathfindingJob(m_transform.position
                            , m_destination.LastKnownPosition()
                            , m_pathToDestination, out handle);
        }
        else
        {
            handle = default;
        }

        return scheduleJob;
    }

    public virtual void OnPathFindingJobFinished(float deltaTime, UpdateTypes updateType) { }

    void FixedUpdate()
    {
        float deltaTime = Time.deltaTime;
        m_timeUntilNextStateUpdate -= deltaTime;

        if (0 >= m_timeUntilNextStateUpdate && GfcManagerServer.HasAuthority)
        {
            float stateDelta = m_updateInterval - m_timeUntilNextStateUpdate;
            m_timeUntilNextStateUpdate = m_updateInterval * UnityEngine.Random.Range(0.9f, 1.1f);

            stateDelta = System.MathF.Max(deltaTime, stateDelta);
            StateUpdate(stateDelta, m_timeUntilNextStateUpdate);
        }
    }

    void LateUpdate()
    {
        if (GfcManagerServer.HasAuthority)
            m_movement.LateUpdateBehaviour(Time.deltaTime);
    }

    protected virtual void BeforeStateUpdate(float deltaTime) { }

    protected void StateUpdate(float deltaTime, float timeUntilNextUpdate)
    {
        m_timeUntilUnpause -= deltaTime;
        m_timeUntilLosetarget -= deltaTime;
        m_timeUntilNextTargetCheck -= deltaTime;

        if (0 >= m_timeUntilUnpause)
        {
            BeforeStateUpdate(deltaTime);
            m_timeUntilUnpause = 0;
            if (m_destination.WasDestroyed() && NpcState.ENGAGING_TARGET == m_currentState)
            {
                DestinationDestroyedBehaviour(deltaTime);
                m_currentState = NpcState.NO_DESTINATION;
            }

            if (m_destination.HasDestination)
            {
                MoveTowardsDestination(deltaTime, m_destination);
            }
            else
            {
                NoDestinationsBehaviour(deltaTime);
            }

            m_movement.UpdatePhysics(deltaTime, timeUntilNextUpdate, false);
            AfterStateUpdate(deltaTime);
        }
    }

    protected virtual void AfterStateUpdate(float deltaTime) { }



    protected bool CheckCanSeeTarget(Transform target, float lineOfSightLength, bool currentlySeesTarget, bool unlimitedFov = false, bool forceRayCast = false)
    {
        bool validTarget = target != null && target.gameObject.activeSelf;
        bool auxCanSeeTarget = currentlySeesTarget && validTarget;

        if (validTarget && (forceRayCast || 0 >= m_timeUntilNextTargetCheck))
        {
            m_timeUntilNextTargetCheck = m_targetCheckCooldown;
            Vector3 currentPosition = m_transform.position;
            Vector3 dirToTarget = target.position;
            GfcTools.Minus(ref dirToTarget, currentPosition);
            float distanceFromTarget = dirToTarget.magnitude;
            if (distanceFromTarget >= 0.00001f) GfcTools.Div(ref dirToTarget, distanceFromTarget);

            auxCanSeeTarget = distanceFromTarget <= lineOfSightLength
                                && (unlimitedFov || m_cosFieldOfView <= Vector3.Dot(m_transform.forward, dirToTarget));

            //check if uses raycast
            if (m_targetCheckCooldown >= 0 && auxCanSeeTarget)
            {
                Ray ray = new(currentPosition, dirToTarget);
                RaycastHit[] hits = GfcPhysics.GetRaycastHits();
                auxCanSeeTarget = 0 == Physics.RaycastNonAlloc(ray, hits, distanceFromTarget, GfcPhysics.NonCharacterCollisions());
                //check if the hit's distance is within x amount of units away from the destination
                //I was thinking of using auxCanSeeTarget|=, but as far as I know that is the OR bitwise operator ' | ', not the boolean check operator ' || ' and I am not sure if it will actually skip the second bool, || skips the second bool if the first one is true. 
                auxCanSeeTarget = auxCanSeeTarget || hits[0].distance >= (distanceFromTarget - 0.2f);
            }
        }

        return auxCanSeeTarget;
    }

    protected virtual void MoveTowardsDestination(float deltaTime, GfgDestination destination)
    {
        bool canSeeTarget = CheckCanSeeTarget(destination.TransformDest, m_lineOfSightLength, NpcState.ENGAGING_TARGET == m_currentState);
        if (canSeeTarget || 0 < m_timeUntilLosetarget) destination.UpdatePosition();

        Vector3 targetPosition = destination.LastKnownPosition();
        Vector3 dirToTarget = targetPosition;
        GfcTools.Minus(ref dirToTarget, m_transform.position);

        if (canSeeTarget)
        {
            m_timeUntilLosetarget = m_targetTrackingTimeWindow;
            m_currentState = NpcState.ENGAGING_TARGET;
        }
        else
        {
            m_currentState = NpcState.SEARCHING_TARGET;
        }

        if (destination.IsEnemy && m_runsAwayFromTarget)
        {
            m_currentState = NpcState.RUNNING_AWAY;
            LowLifeBehaviour(deltaTime, dirToTarget);
        }
        else if (destination.IsEnemy && canSeeTarget)
        {
            m_currentState = NpcState.ENGAGING_TARGET;
            EngageEnemyBehaviour(deltaTime, dirToTarget);
        }

        if (NpcState.SEARCHING_TARGET == m_currentState)
        {
            if (CheckArrivedAtDestination())
            {
                if (destination.IsEnemy)
                {
                    //check if enemy can currently see target
                    canSeeTarget = CheckCanSeeTarget(destination.TransformDest, m_lineOfSightLength, canSeeTarget, true, true);

                    if (canSeeTarget)
                    {
                        m_currentState = NpcState.ENGAGING_TARGET;
                        EngageEnemyBehaviour(deltaTime, GetPathDirection(dirToTarget));
                    }
                    else
                    {
                        m_currentState = NpcState.NO_DESTINATION;
                        LostTargetBehaviour(deltaTime);
                    }
                }
                else
                {
                    m_currentState = NpcState.NO_DESTINATION;
                    ArrivedAtDestinationBehaviour(deltaTime);
                }
            }
            else
            {
                CalculatePathDirection(deltaTime, dirToTarget);
            }
        }
    }

    protected virtual void EngageEnemyBehaviour(float deltaTime, Vector3 dirToTarget)
    {

        m_movement.GetRunner().SetMovementDir(dirToTarget.normalized);
    }

    protected virtual void LowLifeBehaviour(float deltaTime, Vector3 dirToTarget)
    {
        m_movement.GetRunner().SetMovementDir(-dirToTarget.normalized);
    }

    protected Vector3 GetPathDirection(Vector3 dirToTarget)
    {
        Vector3 movementDir;

        if (m_pathToDestination.Length > 0)
        {
            int pathCount = m_pathToDestination.Length - 1;
            Vector3 nodePosition = m_pathToDestination[pathCount];
            Vector3 position = m_transform.position;

            float dstFromNodeSqrd = (nodePosition - position).sqrMagnitude;
            if (0.001f <= dstFromNodeSqrd) //arrived at node
            {
                m_pathToDestination.RemoveAt(pathCount);
                if (m_pathToDestination.Length > 0)
                {
                    nodePosition = m_pathToDestination[pathCount - 1];
                }
            }

            movementDir = nodePosition;
            GfcTools.Minus(ref movementDir, position);
        }
        else
        {
            movementDir = dirToTarget;
        }

        GfcTools.Normalize(ref movementDir);
        return movementDir;
    }
    protected virtual void CalculatePathDirection(float deltaTime, Vector3 dirToTarget)
    {
        Debug.Log("finding the target");
        m_movement.GetRunner().SetMovementDir(GetPathDirection(dirToTarget));
    }

    protected virtual void ArrivedAtDestinationBehaviour(float deltaTime)
    {
        m_destination.RemoveDestination();
        m_movement.GetRunner().SetMovementDir(Vector3.zero);
    }

    protected virtual void LostTargetBehaviour(float deltaTime)
    {
        m_movement.GetRunner().SetMovementDir(Vector3.zero);
        m_destination.RemoveDestination();
    }

    protected virtual void DestinationDestroyedBehaviour(float deltaTime)
    {
        PauseMovement(1);
        m_movement.GetRunner().SetMovementDir(Vector3.zero);
        m_destination.RemoveDestination();
    }

    protected virtual void NoDestinationsBehaviour(float deltaTime)
    {

    }

    public void SetDestination(Transform destinationTrans, bool isEnemy = false, bool canLoseTrackOfTarget = true)
    {
        if (destinationTrans != m_destination.TransformDest && m_pathToDestination.IsCreated)
            m_pathToDestination.Clear();

        m_destination.SetDestination(destinationTrans, isEnemy, canLoseTrackOfTarget);
        // Debug.Log("I am setting a destination! " + m_destination.HasDestination);
    }

    public Transform GetDestinationTransform()
    {
        return m_destination.TransformDest;
    }

    public void SetDestination(Vector3 destinationPos)
    {
        m_destination.SetDestination(destinationPos);
    }

    public GfgPathfinding GetPathfindingManager()
    {
        return m_pathFindingManager;
    }

    public void SetPathfindingManager(GfgPathfinding pathfinding)
    {
        m_pathFindingManager = pathfinding;
    }

    public virtual void SetRunAwayFromTarget(bool value)
    {
        m_runsAwayFromTarget = value;
    }

    public void PauseMovement(float durationInSeconds = float.MaxValue)
    {
        m_movement.GetRunner().SetMovementDir(Vector3.zero);

        /*
         * if a value is not given or is something like 0
         * then pause for 68.096 years (almost nice?)
         * surely no one will wait that long, right?
         */
        m_timeUntilUnpause = durationInSeconds;
    }

    public void ResumeMovement(float delayInSeconds = -1)
    {
        m_timeUntilUnpause = delayInSeconds;
    }

    //Checks if the npc arrived at the given target
    private bool CheckArrivedAtDestination()
    {
        Vector3 dirToTarget = m_destination.LastKnownPosition(); ;
        GfcTools.Minus(ref dirToTarget, transform.position);

        return dirToTarget.sqrMagnitude < 1f;
    }

    public bool CanSeeTarget()
    {
        return NpcState.ENGAGING_TARGET == m_currentState;
    }
}


