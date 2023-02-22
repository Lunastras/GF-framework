using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcController : MonoBehaviour
{

    [SerializeField]
    protected GfMovementGeneric m_movement;

    //how often the NPC will check if they have direct contact with the target
    //if it's smaller than 0, it will never use raycast checks;
    [SerializeField]
    protected float m_targetCheckCooldown;

    [SerializeField]
    protected float m_updateInterval = 0.05f;

    [SerializeField]
    protected bool m_usesFixedUpdate = true;

    [SerializeField]
    protected float m_fieldOfViewDegrees = 180;

    [SerializeField]
    protected float m_lineOfSightLength = 40;

    [SerializeField]
    private float m_targetTrackingTimeWindow = 5.0f;

    [SerializeField]
    private bool m_updatePhysicsValuesAutomatically = false;

    private float m_timeUntilUnpause;
    private float m_timeUntilNextTargetCheck = 0;
    private float m_timeUntilNextStateUpdate = 0;
    private float m_timeUntilLosetarget = 0;

    //protected DestinationManager destinations;
    protected Destination m_destination;

    //public bool canSeeTarget { get; private set; } = false;

    protected bool m_hasLowLife = false;

    protected float m_currentSpeedMultiplier;

    private float m_stateCheckBias = 0;

    //the interval in seconds the npc will keep track
    //of the exact position of its target upon losing sight of it


    protected enum NpcState
    {
        CHASING_TARGET, //can see target
        SEARCHING_TARGET, //cannot see target
        NO_DESTINATION,
        RUNNING_AWAY,
    }

    protected NpcState npcState;

    // Start is called before the first frame update

    protected void Initialize()
    {
        if (m_movement == null)
        {
            m_movement = GetComponent<GfMovementGeneric>();
        }
    }

    void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        SetDestination(null);
        npcState = NpcState.NO_DESTINATION;
    }

    void Update()
    {
        if(!m_usesFixedUpdate)
        {
            float deltaTime = Time.deltaTime;
            m_timeUntilNextStateUpdate -= deltaTime;

            if (0 >= m_timeUntilNextStateUpdate)
            {
                float stateDelta = m_updateInterval * m_stateCheckBias - m_timeUntilNextStateUpdate;
                m_stateCheckBias = Random.Range(0.9f, 1.1f);
                m_timeUntilNextStateUpdate = m_updateInterval * m_stateCheckBias + m_timeUntilNextStateUpdate;

                stateDelta = System.MathF.Max(deltaTime, stateDelta);
                m_timeUntilNextStateUpdate = System.MathF.Max(deltaTime, m_timeUntilNextStateUpdate);
                StateUpdate(stateDelta, m_timeUntilNextStateUpdate);
            }
        }  
    }

    void FixedUpdate()
    {
        if(m_usesFixedUpdate)
        {
            float deltaTime = Time.deltaTime;
            StateUpdate(deltaTime, deltaTime);
        }
    }

    void StateUpdate(float deltaTime, float timeUntilNextUpdate)
    {
        m_timeUntilUnpause -= deltaTime;
        m_timeUntilLosetarget = Mathf.Max(-1, m_timeUntilLosetarget - deltaTime);
        m_timeUntilNextTargetCheck = Mathf.Max(-1, m_timeUntilNextTargetCheck - deltaTime);

        if (0 >= m_timeUntilUnpause)
        {
            m_timeUntilUnpause = 0;
            if (m_destination.WasDestroyed() && NpcState.CHASING_TARGET == npcState)
            {
                DestinationDestroyedBehaviour();
                npcState = NpcState.NO_DESTINATION;
            }

            if (NpcState.NO_DESTINATION != npcState || !m_destination.WasDestroyed())
            {
                MoveTowardsDestination(m_destination);
            }
            else
            {
                NoDestinationsBehaviour();
            }

            m_movement.UpdatePhysics(deltaTime, true, timeUntilNextUpdate, m_updatePhysicsValuesAutomatically);
        }
    }

    void LateUpdate()
    {
        m_movement.Move(Time.deltaTime);
    }

    protected bool CheckCanSeeTarget(Transform target, float lineOfSightLength, bool currentlySeesTarget, bool unlimitedFov = false, bool forceRayCast = false)
    {
        bool auxCanSeeTarget = currentlySeesTarget;

        // Debug.Log("CHECKIES target " + destination.WasDestroyed());
        bool validTarget = target != null && target.gameObject.activeSelf;

        if ((forceRayCast || 0 >= m_timeUntilNextTargetCheck) && validTarget)
        {
            Vector3 dirToTarget = target.position - transform.position;
            float distanceFromTarget = dirToTarget.magnitude;
            dirToTarget = dirToTarget.normalized;

            float dotTargetView = Vector3.Dot(transform.forward, dirToTarget);

            auxCanSeeTarget = unlimitedFov || (distanceFromTarget <= lineOfSightLength
                                && Mathf.Cos(m_fieldOfViewDegrees / 2.0f * Mathf.Deg2Rad) <= dotTargetView);

            //check if uses raycast
            if (m_targetCheckCooldown >= 0 && auxCanSeeTarget)
            {
                // Debug.Log("CHECKIES target RAY 2");
                m_timeUntilNextTargetCheck = m_targetCheckCooldown * GfRandom.Range(0.5f, 1.5f);

                Vector3 offsetSource = Vector3.up;
                Vector3 offsetDest = Vector3.up * 1.0f;

                dirToTarget = (target.position + offsetDest) - (transform.position + offsetSource);
                distanceFromTarget = dirToTarget.magnitude;
                dirToTarget = dirToTarget.normalized;

                Ray ray = new(transform.position + offsetSource, dirToTarget);

                RaycastHit[] hits = GfPhysics.GetRaycastHits();

                auxCanSeeTarget = 0 == Physics.RaycastNonAlloc(ray, hits, distanceFromTarget, GfPhysics.CollisionsNoGroundLayers());

                //check if the hit's distance is within 5 units of the target
                if (!auxCanSeeTarget)
                {
                    auxCanSeeTarget = hits[0].distance >= (distanceFromTarget - 1.0f);
                }
            }
        }

        return auxCanSeeTarget;
    }

    protected virtual void MoveTowardsDestination(Destination destination)
    {
        Vector3 dirToTarget = destination.LastKnownPosition() - transform.position;
        bool canSeeTarget = CheckCanSeeTarget(destination.TransformDest, m_lineOfSightLength, NpcState.CHASING_TARGET == npcState);

        Vector3 targetPosition = destination.LastKnownPosition();
        // Debug.Log("TARGET POSITION IS " + targetPosition);
        // Debug.Log("REALPOSITION IS " + destination.RealPosition());

        if (canSeeTarget)
        {
            m_timeUntilLosetarget = m_targetTrackingTimeWindow;
            npcState = NpcState.CHASING_TARGET;
        }
        else
        {
            npcState = NpcState.SEARCHING_TARGET;
        }

        bool auxCanSeeTarget = 0 < m_timeUntilLosetarget;

        if (destination.IsEnemy)
        {
            //Debug.Log("ye enemy "+ canSeeTarget);

            if (m_hasLowLife)
            {
                npcState = NpcState.RUNNING_AWAY;
                LowLifeBehaviour(dirToTarget);
            }
            else if (auxCanSeeTarget)
            {
                destination.UpdatePosition();
                EngageEnemyBehaviour(dirToTarget);
            }
        }

        if (NpcState.SEARCHING_TARGET == npcState)
        {
            if (CheckArrivedAtDestination())
            {
                if (destination.IsEnemy)
                {
                    //check if enemy can currently see target
                    canSeeTarget = CheckCanSeeTarget(destination.TransformDest, m_lineOfSightLength, canSeeTarget, true, true);

                    if (canSeeTarget)
                    {
                        npcState = NpcState.CHASING_TARGET;
                    }
                    else
                    {
                        LostTargetBehaviour();
                    }
                }
                else
                {
                    ArrivedAtDestinationBehaviour();
                }
            }
            else
            {
                CalculatePathDirection(dirToTarget);
            }
        }
    }

    protected virtual void EngageEnemyBehaviour(Vector3 dirToTarget)
    {

        m_movement.SetMovementDir(dirToTarget.normalized, m_movement.UpVecEffective());
    }

    protected virtual void LowLifeBehaviour(Vector3 dirToTarget)
    {
        m_movement.SetMovementDir(-dirToTarget.normalized, m_movement.UpVecEffective());
    }

    /** TODO
 * Calculates path towards destination and changes the 
 * movementDirNorm accordingly
 */
    protected virtual void CalculatePathDirection(Vector3 dirToTarget)
    {

        m_movement.SetMovementDir(dirToTarget.normalized, m_movement.UpVecEffective());
    }

    protected virtual void ArrivedAtDestinationBehaviour()
    {
        m_movement.SetMovementDir(Vector3.zero);
    }

    protected virtual void LostTargetBehaviour()
    {
        m_movement.SetMovementDir(Vector3.zero);
    }

    protected virtual void DestinationDestroyedBehaviour()
    {
        PauseMovement(3);
        m_movement.SetMovementDir(Vector3.zero);
    }

    protected virtual void NoDestinationsBehaviour()
    {

    }

    public void SetDestination(Transform destinationTrans, bool isEnemy = false, bool canLoseTrackOfTarget = true)
    {
        m_destination = new Destination(destinationTrans, isEnemy, canLoseTrackOfTarget);
    }

    public void SetDestination(Vector3 destinationPos)
    {
        m_destination = new Destination(destinationPos);
    }

    public virtual void SetRunAwayFromTarget(bool value)
    {
        m_hasLowLife = value;
    }

    public void PauseMovement(float durationInSeconds = float.MaxValue)
    {
        m_movement.SetMovementDir(Vector3.zero);

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
        Vector3 targetPosition = m_destination.LastKnownPosition();
        // Debug.Log("CHECKING IF I ARRIVED AT " + targetPosition);
        //Debug.Log("the name of the target is: " + destination.TransformDest.name);
        Vector3 dirToTarget = targetPosition - transform.position;

        if (Mathf.Abs(transform.position.y - targetPosition.y) < 1)
        {
            float horizontalDistance = new Vector2(dirToTarget.x, dirToTarget.z).magnitude;

            return horizontalDistance <= 1f;
        }

        return false;
    }

    public bool CanSeeTarget()
    {
        return NpcState.CHASING_TARGET == npcState;
    }
}


