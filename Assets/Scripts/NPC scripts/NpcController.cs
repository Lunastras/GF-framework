using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcController : MonoBehaviour
{

    [SerializeField]
    protected GfMovementGeneric simpleMovement;

    //how often the NPC will check if they have direct contact with the target
    //if it's smaller than 0, it will never use raycast checks;
    [SerializeField]
    protected float targetCheckCooldown;

    [SerializeField]
    protected float stateUpdateCoolDown = 0.05f;

    [SerializeField]
    protected float fieldOfViewDegrees = 180;

    [SerializeField]
    protected float lineOfSightLength = 40;

    private float timeUntilUnpause;
    private float timeUntilNextTargetCheck = 0;
    private float timeUntilNextStateUpdate = 0;
    private float timeUntilLosetarget = 0;

    //protected DestinationManager destinations;
    protected Destination destination;

    //public bool canSeeTarget { get; private set; } = false;

    protected bool hasLowLife = false;

    protected float currentSpeedMultiplier;

    //the interval in seconds the npc will keep track
    //of the exact position of its target upon losing sight of it
    private readonly float TargetTrackingTimeWindow = 5.0f;

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
        if (simpleMovement == null)
        {
            simpleMovement = GetComponent<GfMovementGeneric>();
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
        float deltaTime = Time.deltaTime;

        timeUntilUnpause = Mathf.Max(-1, timeUntilUnpause - deltaTime);
        timeUntilLosetarget = Mathf.Max(-1, timeUntilLosetarget - deltaTime);
        timeUntilNextTargetCheck = Mathf.Max(-1, timeUntilNextTargetCheck - deltaTime);
        timeUntilNextStateUpdate = Mathf.Max(-1, timeUntilNextStateUpdate - deltaTime);

        if (0 >= timeUntilUnpause && 0 >= timeUntilNextStateUpdate)
        {
            timeUntilNextStateUpdate = stateUpdateCoolDown * GfRandom.Range(0.8f, 1.2f);

            currentSpeedMultiplier = 1.0f;

            if (destination.WasDestroyed() && NpcState.CHASING_TARGET == npcState)
            {
                DestinationDestroyedBehaviour();
                npcState = NpcState.NO_DESTINATION;
            }

            if (NpcState.NO_DESTINATION != npcState || !destination.WasDestroyed())
            {
                MoveTowardsDestination(destination);
            }
            else
            {
                NoDestinationsBehaviour();
            }
        }
    }

    protected bool CheckCanSeeTarget(Transform target, float lineOfSightLength, bool currentlySeesTarget, bool unlimitedFov = false, bool forceRayCast = false)
    {
        bool auxCanSeeTarget = currentlySeesTarget;

        // Debug.Log("CHECKIES target " + destination.WasDestroyed());
        bool validTarget = target != null && target.gameObject.activeSelf;

        if ((forceRayCast || 0 >= timeUntilNextTargetCheck) && validTarget)
        {
            Vector3 dirToTarget = target.position - transform.position;
            float distanceFromTarget = dirToTarget.magnitude;
            dirToTarget = dirToTarget.normalized;

            float dotTargetView = Vector3.Dot(transform.forward, dirToTarget);

            auxCanSeeTarget = unlimitedFov || (distanceFromTarget <= lineOfSightLength
                                && Mathf.Cos(fieldOfViewDegrees / 2.0f * Mathf.Deg2Rad) <= dotTargetView);

            //check if uses raycast
            if (targetCheckCooldown >= 0 && auxCanSeeTarget)
            {
                // Debug.Log("CHECKIES target RAY 2");
                timeUntilNextTargetCheck = targetCheckCooldown * GfRandom.Range(0.5f, 1.5f);

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
        bool canSeeTarget = CheckCanSeeTarget(destination.TransformDest, lineOfSightLength, NpcState.CHASING_TARGET == npcState);

        Vector3 targetPosition = destination.LastKnownPosition();
        // Debug.Log("TARGET POSITION IS " + targetPosition);
        // Debug.Log("REALPOSITION IS " + destination.RealPosition());

        if (canSeeTarget)
        {
            timeUntilLosetarget = TargetTrackingTimeWindow;
            npcState = NpcState.CHASING_TARGET;
        }
        else
        {
            npcState = NpcState.SEARCHING_TARGET;
        }

        bool auxCanSeeTarget = 0 < timeUntilLosetarget;

        if (destination.IsEnemy)
        {
            //Debug.Log("ye enemy "+ canSeeTarget);

            if (hasLowLife)
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
                    canSeeTarget = CheckCanSeeTarget(destination.TransformDest, lineOfSightLength, canSeeTarget, true, true);

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

        simpleMovement.SetMovementDir(dirToTarget);
    }

    protected virtual void LowLifeBehaviour(Vector3 dirToTarget)
    {
        simpleMovement.SetMovementDir(-dirToTarget);
    }

    /** TODO
 * Calculates path towards destination and changes the 
 * movementDirNorm accordingly
 */
    protected virtual void CalculatePathDirection(Vector3 dirToTarget)
    {

        simpleMovement.SetMovementDir(dirToTarget);
    }

    protected virtual void ArrivedAtDestinationBehaviour()
    {
        simpleMovement.SetMovementDir(Vector3.zero);
    }

    protected virtual void LostTargetBehaviour()
    {
        simpleMovement.SetMovementDir(Vector3.zero);
    }

    protected virtual void DestinationDestroyedBehaviour()
    {
        PauseMovement(3);
        simpleMovement.SetMovementDir(Vector3.zero);
    }

    protected virtual void NoDestinationsBehaviour()
    {

    }

    public void SetDestination(Transform destinationTrans, bool isEnemy = false, bool canLoseTrackOfTarget = true)
    {
        destination = new Destination(destinationTrans, isEnemy, canLoseTrackOfTarget);
    }

    public void SetDestination(Vector3 destinationPos)
    {
        destination = new Destination(destinationPos);
    }

    public virtual void SetRunAwayFromTarget(bool value)
    {
        hasLowLife = value;
    }

    public void PauseMovement(float durationInSeconds = float.MaxValue)
    {
        simpleMovement.SetMovementDir(Vector3.zero);

        /*
         * if a value is not given or is something like 0
         * then pause for 68.096 years (almost nice?)
         * surely no one will wait that long, right?
         */
        timeUntilUnpause = durationInSeconds;
    }

    public void ResumeMovement(float delayInSeconds = -1)
    {
        timeUntilUnpause = delayInSeconds;
    }

    //Checks if the npc arrived at the given target
    private bool CheckArrivedAtDestination()
    {
        Vector3 targetPosition = destination.LastKnownPosition();
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


