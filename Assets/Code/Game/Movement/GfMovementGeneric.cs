using System.Collections.Generic;
using UnityEngine;
using static GfcTools;
public class GfMovementGeneric : MonoBehaviour
{
    #region Variables

    [SerializeField]
    protected GfgStatsCharacter m_statsCharacter;

    [SerializeField]
    protected bool m_useInterpolation;

    [SerializeField]
    protected float m_stepOffset = 0.3f;
    [SerializeField]
    protected float m_slopeLimit = 45;
    [SerializeField]
    protected float m_upperSlopeLimit = 130;
    [SerializeField]
    protected QueryTriggerInteraction m_queryTrigger;
    [SerializeField]
    protected float m_fullRotationSeconds = 1; //the time it takes for the model to do a 180 degrees rotation

    [SerializeField]
    protected bool m_useSimpleCollision = true;

    protected PriorityValue<Transform> m_parentSpherical = new();

    protected Transform m_transform;

    protected float m_lastPhysDeltaTime;
    protected static readonly Vector3 UPDIR = Vector3.up;


    //Used to keep track of the supposed current up direction
    //because the rotation is incremented, floating point errors make the rotation inaccurate
    //this value is used to identify and correct these calculation errors, as the UpVec might not actually be equal to the real
    //rotation up vector
    protected Vector3 m_rotationUpVec = UPDIR;

    protected Quaternion m_currentRotation = Quaternion.identity; //the current internal rotation of the object

    private Collider m_collider = null;

    public struct TransformDelta
    {
        public TransformDelta(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            DeltaPosition = deltaPosition;
            DeltaRotation = deltaRotation;
        }

        public Vector3 DeltaPosition;
        public Quaternion DeltaRotation;
    }

    protected PriorityValue<Transform> m_parentTransform = new();

    [HideInInspector]
    protected Vector3 m_velocity = ZERO3;

    protected Vector3 m_upVec = UPDIR;

    protected bool m_isGrounded = false;

    protected Vector3 m_slopeNormal = UPDIR;

    private bool m_adjustedVelocityToParent;  // whether or not the velocity was adjusted to that of the parent upon parenting

    private Quaternion m_parentLastRot;
    private Vector3 m_parentRotMov, m_parentLastPos, m_parentPosMov;
    private double m_timeOfLastParentRotUpdate, m_timeOfLastParentPosUpdate;

    private float m_parentDeltaTimePos, m_parentDeltaTimeRot; //delta of the last movement from the parent. 

    private Vector3 m_interpolationMovement;
    private Quaternion m_interpolationRotation = Quaternion.identity;
    private Quaternion m_desiredRotation = Quaternion.identity;
    private Quaternion m_externalRotation = Quaternion.identity;

    protected Quaternion m_lastRotation;

    private float m_timeBetweenPhysChecks = 0.02f;

    private double m_timeOfLastPhysCheck = 0;

    private float m_previousLerpAlpha;

    private float m_accumulatedTimefactor = 0;

    private float m_rotationSmoothSeconds;
    private float m_rotationSmoothProgress = 1.0f;

    private Vector3 m_initialUpVec;

    [SerializeField]
    private float m_maxOverlaps = 4;
    [SerializeField]

    private float m_maxCasts = 4;

    private bool m_interpolateThisFrame = false;

    private ArchetypeCollision m_archetypeCollision;
    static readonly protected Vector3 ZERO3 = new Vector3(0, 0, 0);
    private readonly float TRACE_SKIN = 0.005F;
    private readonly float TRACEBIAS = 0.01F;
    private readonly float DOWNPULL = 0.05F;

    protected const float OVERLAP_SKIN = 0.0f;
    private const float MIN_DISPLACEMENT = 0.00001F; // min squared length of a displacement vector required for a Move() to proceed.
    private const float MIN_PUSHBACK_DEPTH = 0.00001F;

    private float m_refUpVecSmoothRot;

    protected readonly static Quaternion IDENTITY_QUAT = Quaternion.identity;

    private readonly static List<GfMovementTriggerable> m_triggerResults = new(16);

    protected MgCollisionStruct m_currentGroundCollision;

    protected bool m_initialisedMovementGeneric = false;

    protected GfRunnerTemplate m_runner;

    #endregion

    private void Awake()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        if (!m_initialisedMovementGeneric)
        {
            m_runner = GetComponent<GfRunnerTemplate>();
            m_initialisedMovementGeneric = true;
            m_transform = transform;
            m_lastRotation = m_transform.rotation;
            ValidateCollisionArchetype();
            m_archetypeCollision.UpdateValues();
            if (null == m_statsCharacter) m_statsCharacter = GetComponent<GfgStatsCharacter>();
        }
    }

    private void ValidateCollisionArchetype()
    {
        if (null == m_collider || null == m_archetypeCollision)
        {
            //type checking is not available in c# without an IDictionary, so we check until one is found. It works because there are only a few anywyay
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider)
            {
                m_collider = capsuleCollider;
                m_archetypeCollision = new ArchetypeCapsule(capsuleCollider);
            }
            else
            {
                SphereCollider sphereCollider = GetComponent<SphereCollider>();
                if (sphereCollider)
                {
                    m_collider = sphereCollider;
                    m_archetypeCollision = new ArchetypeSphere(sphereCollider);
                }
            }

            if (null == m_archetypeCollision)
            {
                m_archetypeCollision = new ArchetypeCollision();
                if (GetComponent<SphereCollider>() != null)
                    Debug.LogError("The collider attached to: " + gameObject.name + " is not supported by MovementGeneric");
            }
        }
    }
    protected virtual TransformDelta ApplyParentMovement(Vector3 position, float deltaTime, double currentTime)
    {
        TransformDelta parentMovement = GetParentMovement(position, deltaTime, currentTime);
        m_transform.position += parentMovement.DeltaPosition;
        if (!parentMovement.DeltaRotation.Equals(IDENTITY_QUAT))
            m_transform.rotation = parentMovement.DeltaRotation * m_transform.rotation;
        return parentMovement;
    }

    protected TransformDelta GetParentMovement(Vector3 position, float deltaTime, double currentTime)
    {
        Quaternion deltaRotation = Quaternion.identity;
        Vector3 deltaMovement = ZERO3;

        Transform parentTransform = m_parentTransform;
        if (parentTransform && !parentTransform.gameObject.isStatic)
        {
            Vector3 currentParentPos = parentTransform.position;
            if (!currentParentPos.Equals(m_parentLastPos))
            {
                m_parentPosMov.x = currentParentPos.x - m_parentLastPos.x;
                m_parentPosMov.y = currentParentPos.y - m_parentLastPos.y;
                m_parentPosMov.z = currentParentPos.z - m_parentLastPos.z;

                m_parentDeltaTimePos = (float)(currentTime - m_timeOfLastParentPosUpdate);
                m_timeOfLastParentPosUpdate = currentTime;
                m_parentLastPos = currentParentPos;
                deltaMovement = m_parentPosMov;
                Add(ref position, deltaMovement);
            }

            //Calculate the rotation according to the parent's rotation
            Quaternion currentRot = parentTransform.rotation;
            if (!currentRot.Equals(m_parentLastRot)) // .Equals() is different from == (and a bit wrong), but it works better here because of the added accuracy
            {
                Quaternion deltaQuaternion = currentRot * Quaternion.Inverse(m_parentLastRot);
                Vector3 vecFromParent = position - parentTransform.position;
                Vector3 newVecFromParent = deltaQuaternion * vecFromParent;
                m_parentRotMov = newVecFromParent;
                Minus(ref m_parentRotMov, vecFromParent);

                //apply the rotation to the character if it isn't moving
                if (GetVelocity().sqrMagnitude < 0.001f)
                {
                    RemoveOtherAxisFromRotation(ref deltaQuaternion, m_rotationUpVec);
                    deltaRotation = deltaQuaternion;
                }

                m_parentDeltaTimeRot = (float)(currentTime - m_timeOfLastParentRotUpdate);
                m_timeOfLastParentRotUpdate = currentTime;
                m_parentLastRot = currentRot;
                Add(ref deltaMovement, m_parentRotMov);
            }

            //adjust the player's velocity to the parent's
            if (!m_adjustedVelocityToParent)
            {
                Vector3 parentVelocity = GetParentVelocity(currentTime, deltaTime);
                m_adjustedVelocityToParent = true;

                ParentingVelocityAdjustVector(parentVelocity);

                if (m_interpolateThisFrame)
                {
                    Mult(ref parentVelocity, m_lastPhysDeltaTime);
                    Minus(ref parentVelocity, m_upVec * Vector3.Dot(m_upVec, parentVelocity)); //remove any vertical movement from parent velocity            
                    ParentingVelocityAdjustVector(ref m_interpolationMovement, parentVelocity); //adjust interpolation 
                }
            }
        }

        return new TransformDelta(deltaMovement, deltaRotation);
    }

    public virtual void LateUpdateBehaviour(float deltaTime)
    {
        double currentTime = Time.timeAsDouble;
        TransformDelta movementThisFrame = GetParentMovement(m_transform.position, deltaTime, currentTime);

        if (m_interpolateThisFrame)
        {
            float timeSincePhysCheck = GetDeltaTimeCoef() * (float)(currentTime - m_timeOfLastPhysCheck);
            float timeBetweenChecks = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks);
            float alpha = System.MathF.Min(1.0f, timeSincePhysCheck / timeBetweenChecks);
            float timefactor = alpha - m_previousLerpAlpha;
            m_previousLerpAlpha = alpha;
            m_accumulatedTimefactor += timefactor;

            movementThisFrame.DeltaPosition.x += m_interpolationMovement.x * timefactor;
            movementThisFrame.DeltaPosition.y += m_interpolationMovement.y * timefactor;
            movementThisFrame.DeltaPosition.z += m_interpolationMovement.z * timefactor;

            Quaternion currentRot = m_transform.rotation;
            if (!m_lastRotation.Equals(currentRot))
                m_externalRotation *= Quaternion.Inverse(m_lastRotation) * currentRot;

            if (!m_interpolationRotation.Equals(IDENTITY_QUAT))
            {
                currentRot *= Quaternion.LerpUnclamped(IDENTITY_QUAT, m_interpolationRotation, timefactor);
                m_transform.rotation = currentRot;
            }
        }

        UpdateSphericalOrientation(true, deltaTime);

        m_transform.position += movementThisFrame.DeltaPosition;
        if (!movementThisFrame.DeltaRotation.Equals(IDENTITY_QUAT))
            m_transform.rotation = movementThisFrame.DeltaRotation * m_transform.rotation;
        m_lastRotation = m_transform.rotation;
    }

    public virtual bool UpdatePhysics(float deltaTime, float timeUntilNextUpdate, bool ignorePhysics = false, bool validateCollisionArchetype = true)
    {
        if (m_interpolateThisFrame)
        {
            float correctionFactor = 1.0f - m_accumulatedTimefactor;
            m_accumulatedTimefactor = m_previousLerpAlpha = 0;

            Mult(ref m_interpolationMovement, correctionFactor);
            m_transform.position += m_interpolationMovement;
            m_transform.rotation = m_desiredRotation * m_externalRotation;
            m_interpolationRotation = m_externalRotation = IDENTITY_QUAT;
        }

        double currentTime = Time.timeAsDouble;
        m_timeOfLastPhysCheck = currentTime;
        m_lastPhysDeltaTime = deltaTime;
        m_timeBetweenPhysChecks = timeUntilNextUpdate;

        ApplyParentMovement(m_transform.position, deltaTime, currentTime);

        UpdateSphericalOrientation(false, deltaTime);

        Quaternion initialRot = m_transform.rotation;
        Vector3 initialPos = m_transform.position;

        m_runner.BeforePhysChecks(deltaTime);

        Vector3 position = m_transform.position;
        m_interpolateThisFrame = m_useInterpolation; //&& Time.deltaTime < m_timeBetweenPhysChecks;

        bool foundCollisions = false;
        if (!ignorePhysics)
        {
            Collider[] colliderbuffer = GfcPhysics.GetCollidersArray();
            int layermask = GfcPhysics.GetLayerMask(gameObject.layer);

            if (validateCollisionArchetype)
            {
                ValidateCollisionArchetype();
                m_archetypeCollision.UpdateValues();
            }

            if (m_useSimpleCollision)
                foundCollisions = UpdatePhysicsDiscrete(ref position, deltaTime, colliderbuffer, layermask);
            else
                foundCollisions = UpdatePhysicsContinuous(ref position, deltaTime, colliderbuffer, GfcPhysics.GetRaycastHits(), layermask);

            if (!m_isGrounded) PhysicsGroundCheck(ref position);
        }
        else
        {
            Add(ref position, deltaTime * m_velocity);
        }

        m_interpolationMovement = ZERO3;

        m_runner.AfterPhysChecks(deltaTime);

        /* TRACING SECTION END*/
        if (m_interpolateThisFrame)
        {
            Add(ref m_interpolationMovement, position);
            Minus(ref m_interpolationMovement, initialPos);
            m_transform.position = initialPos;

            m_desiredRotation = m_transform.rotation;
            if (!initialRot.Equals(m_desiredRotation)) //== operator is more appropriate but it doesn't have enough accuracy
            {
                m_interpolationRotation = Quaternion.Inverse(initialRot) * m_desiredRotation;
                m_transform.rotation = initialRot;
            }
        }
        else
        {
            m_transform.position = position;
            UpdateSphericalOrientation(false, deltaTime);
        }


        m_lastRotation = m_transform.rotation;

        if (!m_isGrounded)
            m_slopeNormal = m_upVec;

        return foundCollisions;
    }

    protected virtual void PhysicsGroundCheck(ref Vector3 position)
    {
        if (m_currentGroundCollision.collider)
        {
            Ray ray = new(m_archetypeCollision.GetLocalBottomPoint() + position, -m_upVec);
            bool hitSomething = m_currentGroundCollision.collider.Raycast(ray, out RaycastHit hitInfo, DOWNPULL);
            if (hitSomething)
            {
                m_currentGroundCollision.normal = hitInfo.normal;
                m_currentGroundCollision.SetPoint(hitInfo.point);
                m_currentGroundCollision.SetRaycastHit(hitInfo);
                m_currentGroundCollision.isStair = false;
                m_currentGroundCollision.selfPosition = position;
                m_currentGroundCollision.selfUpVecAngle = AngleDegNorm(m_upVec, hitInfo.normal);
                m_currentGroundCollision.isGrounded = CheckGround(ref m_currentGroundCollision);
                m_isGrounded |= m_currentGroundCollision.isGrounded;

                //DetermineGeometryType(ref m_velocity, ref lastNormal, ref m_currentGroundCollision, ref collisions);
                m_runner.MgOnCollision(ref m_currentGroundCollision);
            }
            else
            {
                m_currentGroundCollision.collider = null;
            }
        }
    }

    private bool UpdatePhysicsDiscrete(ref Vector3 position, float deltaTime, Collider[] colliderbuffer, int layermask)
    {
        bool foundCollisions = false;

        Vector3 lastNormal = ZERO3;

        /* OVERLAP SECTION START*/
        int numbumps = 0;
        int geometryclips = 0;
        int numCollisions = -1;
        m_isGrounded = false;
        float timefactor = 1;

        /* attempt an overlap pushback at this current position */
        while (numbumps++ < m_maxOverlaps && numCollisions != 0 && timefactor > 0)
        {
            Vector3 _trace = m_velocity * deltaTime * timefactor;
            float _tracelen = _trace.magnitude;
            if (MIN_DISPLACEMENT <= _tracelen)
                Add(ref position, _trace);

            m_archetypeCollision.Overlap(position, layermask, m_queryTrigger, colliderbuffer, out numCollisions);

            ActorOverlapFilter(ref numCollisions, m_collider, colliderbuffer, ref position); /* filter ourselves out of the collider buffer */
            for (int ci = numCollisions - 1; ci >= 0; ci--)/* pushback against the first valid penetration found in our collider buffer */
            {
                Collider otherCollider = colliderbuffer[ci];
                Transform othert = otherCollider.transform;

                if (Physics.ComputePenetration(m_collider, position, m_transform.rotation, otherCollider,
                    othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                {
                    foundCollisions = true;

                    float pushback = mindistance + TRACE_SKIN;
                    position.x += normal.x * pushback;
                    position.y += normal.y * pushback;
                    position.z += normal.z * pushback;

                    MgCollisionStruct collision = new(normal, m_upVec, otherCollider, ZERO3, false, m_archetypeCollision, true, position);
                    collision.isGrounded = CheckGround(ref collision);
                    m_isGrounded |= collision.isGrounded;

                    collision.selfPosition = position;

                    if (MIN_DISPLACEMENT <= _tracelen)
                    {
                        float _traceLenInv = 1.0f / _tracelen;
                        Vector3 traceDir = _trace * _traceLenInv;

                        float velNormalDot = System.MathF.Max(0, -Vector3.Dot(normal, traceDir));
                        float pushbackLength = velNormalDot * mindistance;

                        float distance = System.MathF.Max(0, _tracelen - pushbackLength);
                        timefactor = System.MathF.Max(0f, timefactor - distance * _traceLenInv);
                    }

                    CallTriggerableEvents(otherCollider.gameObject, normal, ZERO3, ref position);
                    collision.selfVelocity = m_velocity;

                    // if (Vector3.Dot(m_velocity, normal) <= 0F) /* only consider normals that we are technically penetrating into */
                    DetermineGeometryType(ref m_velocity, ref lastNormal, ref collision, ref geometryclips);
                    m_runner.MgOnCollision(ref collision);
                    position = collision.selfPosition;

                    break;
                }
                else
                {
                    --numCollisions;
                }
            } // for (int ci = 0; ci < numoverlaps; ci++)

        }// while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)

        return foundCollisions;
    }

    private bool UpdatePhysicsContinuous(ref Vector3 position, float deltaTime, Collider[] colliderbuffer, RaycastHit[] tracesbuffer, int layermask)
    {
        bool foundCollisions = false;
        Vector3 lastNormal = ZERO3;

        /* OVERLAP SECTION START*/
        int numbumps = 0;
        int geometryclips = 0;
        int numCollisions = -1;
        m_isGrounded = false;

        /* attempt an overlap pushback at this current position */
        while (numbumps++ < m_maxOverlaps && numCollisions != 0)
        {
            m_archetypeCollision.Overlap(position, layermask, m_queryTrigger, colliderbuffer, out numCollisions);
            ActorOverlapFilter(ref numCollisions, m_collider, colliderbuffer, ref position); /* filter ourselves out of the collider buffer */
            for (int ci = numCollisions - 1; ci >= 0; ci--)/* pushback against the first valid penetration found in our collider buffer */
            {
                Collider otherc = colliderbuffer[ci];
                Transform othert = otherc.transform;

                if (Physics.ComputePenetration(m_collider, position, m_transform.rotation, otherc,
                    othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                {
                    foundCollisions = true;
                    position += normal * (mindistance + MIN_PUSHBACK_DEPTH);
                    MgCollisionStruct collision = new(normal, m_upVec, otherc, ZERO3, false, m_archetypeCollision, true, position);
                    collision.isGrounded = CheckGround(ref collision);
                    if (collision.isGrounded && otherc) m_currentGroundCollision = collision;
                    m_isGrounded |= collision.isGrounded;


                    CallTriggerableEvents(collision.collider.gameObject, normal, ZERO3, ref position);
                    collision.selfVelocity = m_velocity;

                    //DetermineGeometryType(ref m_velocity, m_velocity.normalized, ref lastNormal, ref collision, ref geometryclips);

                    m_runner.MgOnCollision(ref collision);
                    position = collision.selfPosition;
                    break;
                }
                else
                {
                    // MgOnCollision(new MgCollisionStruct(normal, UpVec, otherc, mindistance, position - normal * mindistance, false, true, false));
                    --numCollisions;
                }
            } // for (int ci = 0; ci < numoverlaps; ci++)       
        }// while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)
        /* OVERLAP SECTION END*/

        /* TRACING SECTION START*/
        float timefactor = 1F;
        numbumps = 0;

        while (numbumps++ <= m_maxCasts && timefactor > 0)
        {
            Vector3 trace = m_velocity * deltaTime * timefactor;
            float tracelen = trace.magnitude;

            if (tracelen > MIN_DISPLACEMENT)
            {
                Vector3 traceDir = trace;
                float _traceLenInv = 1.0f / tracelen;
                Mult(ref traceDir, _traceLenInv);

                m_archetypeCollision.Trace(position, traceDir, tracelen, layermask, m_queryTrigger, tracesbuffer, TRACEBIAS, out numCollisions);/* prevent tunneling by using this skin length */
                ActorTraceFilter(ref numCollisions, out int _i0, TRACEBIAS, m_collider, tracesbuffer, ref position);

                if (_i0 > -1) /* we found something in our trace */
                {
                    foundCollisions = true;
                    RaycastHit closestHit = tracesbuffer[_i0];
                    MgCollisionStruct collision = new(closestHit.normal, m_upVec, closestHit.collider, closestHit.point, true, m_archetypeCollision, false, position);

                    collision.isGrounded = CheckGround(ref collision);

                    float distanceFromHit = System.MathF.Max(closestHit.distance - TRACE_SKIN, 0F);

                    timefactor -= distanceFromHit * _traceLenInv;
                    Mult(ref traceDir, distanceFromHit);
                    Add(ref position, traceDir);

                    collision.selfPosition = position;

                    CallTriggerableEvents(collision.collider.gameObject, collision.normal, ZERO3, ref position); //todo
                    collision.selfVelocity = m_velocity;

                    DetermineGeometryType(ref m_velocity, ref lastNormal, ref collision, ref geometryclips);
                    m_runner.MgOnCollision(ref collision);

                    position = collision.selfPosition;

                }
                else /* Discovered noting along our linear path */
                {
                    Add(ref position, trace);
                    break;
                }
            }
            else break;

            //break; //if (_tracelen > MIN_DISPLACEMENT)        
        } // while (numbumps++ <= MAX_BUMPS && timefactor > 0)

        return foundCollisions;
    }

    private const float INV_180 = 0.005555555555f;

    protected void UpdateSphericalOrientation(bool rotateOrientation, float deltaTime)
    {
        if (m_parentSpherical.Value)
        {
            m_upVec = transform.position;
            Minus(ref m_upVec, m_parentSpherical.Value.position);
            Normalize(ref m_upVec);
        }

        if (rotateOrientation && !m_upVec.Equals(m_rotationUpVec))
        {
            Quaternion upVecRotCorrection;

            if (m_rotationSmoothProgress < 0.9999f)
            {
                m_rotationSmoothProgress = Mathf.SmoothDamp(m_rotationSmoothProgress, 1, ref m_refUpVecSmoothRot, m_rotationSmoothSeconds, int.MaxValue, deltaTime);
                Vector3 auxVec = Vector3.Lerp(m_initialUpVec, m_upVec, m_rotationSmoothProgress);
                Normalize(ref auxVec);
                upVecRotCorrection = Quaternion.FromToRotation(m_rotationUpVec, auxVec);
            }
            else
            {
                upVecRotCorrection = Quaternion.FromToRotation(m_rotationUpVec, m_upVec);
            }

            SetRotation(upVecRotCorrection * GetTransformRotation());
            m_rotationUpVec = upVecRotCorrection * m_rotationUpVec;
            m_desiredRotation = upVecRotCorrection * m_desiredRotation;
            m_currentRotation = upVecRotCorrection * m_currentRotation; //used for tracking how we rotated the character so far
        }
    }

    private float GetStepHeight(Vector3 point, Vector3 position)
    {
        Vector3 bottomPos = m_archetypeCollision.GetLocalBottomPoint();
        Vector3 localStepHitPos = (point - position);

        Debug.DrawRay(position, bottomPos, Color.red, 0.1f);
        Debug.DrawRay(position, point - position, Color.green, 0.1f);

        return Vector3.Dot(m_upVec, localStepHitPos - bottomPos);
    }

    private void DetermineGeometryType(ref Vector3 velocity, ref Vector3 lastNormal, ref MgCollisionStruct collision, ref int geometryClips)
    {
        bool underSlopeLimit = m_slopeLimit > collision.selfUpVecAngle;
        m_isGrounded |= collision.isGrounded;
        bool recalculateVelocity = true;
        if (collision.isGrounded && collision.collider) m_currentGroundCollision = collision;
        Vector3 normal = collision.normal;

        //Check for stair, if no stair is found, then perform normal velocity calculations
        if (!collision.isGrounded && false)
        {
            float stepHeight = GetStepHeight(collision.GetPoint(), collision.selfPosition);
            if (stepHeight <= m_stepOffset && stepHeight > 0.001f) //todo it randomly walks up walls
            {
                bool stairIsGrounded = CheckGround(ref collision, collision.GetHitUpVecAngle());

                if (stairIsGrounded)
                {
                    //Debug.Log("yes it was a stair");
                    m_isGrounded = true;
                    collision.selfPosition += m_upVec * (stepHeight + 0.05f);
                    recalculateVelocity = false;
                    normal = collision.GetRaycastHit().normal;
                    collision.isStair = true;
                    collision.isGrounded = true;
                }
            }
            //else Debug.Log("The height is: " + stepHeight + " The offset is: " + m_stepOffset);
        }

        if (recalculateVelocity)
        {
            if (!collision.isGrounded && geometryClips == 1)
            {
                Vector3 crease = Vector3.Cross(lastNormal, normal);
                Normalize(ref crease);
                Project(ref velocity, crease);
            }
            else
            {
                if (collision.selfUpVecAngle > m_upperSlopeLimit)
                    Minus(ref velocity, m_slopeNormal * System.MathF.Max(0, Vector3.Dot(m_slopeNormal, velocity)));

                if (collision.isGrounded)
                {
                    RemoveAxis(ref velocity, m_slopeNormal);
                    m_slopeNormal = normal;
                }

                Mult(ref normal, Vector3.Dot(velocity, normal));
                Minus(ref velocity, normal);
            }
        }

        geometryClips++;
        lastNormal = collision.normal;
    }

    protected void CallTriggerableEvents(GameObject gameObject, Vector3 normal, Vector3 point)
    {
        gameObject.GetComponents<GfMovementTriggerable>(m_triggerResults);
        int count = m_triggerResults.Count;

        for (int i = 0; i < count; ++i)
        {
            m_triggerResults[i].MgOnTrigger(this);
        }
    }

    protected void CallTriggerableEvents(GameObject gameObject, Vector3 normal, Vector3 point, ref Vector3 position)
    {
        gameObject.GetComponents<GfMovementTriggerable>(m_triggerResults);
        int count = m_triggerResults.Count;

        if (count > 0)
        {
            m_transform.position = position;

            for (int i = 0; i < count; ++i)
            {
                m_triggerResults[i].MgOnTrigger(this);
            }

            position = m_transform.position;
        }
    }

    // Simply a copy of ArchetypeHeader.OverlapFilters.FilterSelf() with trigger checking
    private void ActorOverlapFilter(ref int overlapsfound, Collider _self, Collider[] _colliders, ref Vector3 position)
    {
        for (int i = overlapsfound - 1; i >= 0; i--)
        {
            Collider col = _colliders[i];
            bool filterout = col == _self;

            // we only want to filter out triggers that aren't the actor. Having an imprecise implementation of this filter
            // may lead to unintended consequences for the end-user.
            if (!filterout && col.isTrigger)
            {
                CallTriggerableEvents(col.gameObject, ZERO3, ZERO3, ref position);
                filterout = true;
            }

            if (filterout)
            {
                overlapsfound--;

                if (i < overlapsfound)
                    _colliders[i] = _colliders[overlapsfound];
            }
        }
    }

    // Simply a copy of ArchetypeHeader.TraceFilters.FindClosestFilterInvalids() with added trigger functionality
    private void ActorTraceFilter(ref int tracesfound, out int closestindex, float bias, Collider self, RaycastHit[] hits, ref Vector3 position)
    {
        float _closestdistance = Mathf.Infinity;
        closestindex = -1;

        for (int i = tracesfound - 1; i >= 0; i--)
        {
            hits[i].distance -= bias + OVERLAP_SKIN;
            RaycastHit hit = hits[i];
            Collider col = hit.collider;
            float tracelen = hit.distance;
            bool filterout = tracelen < -OVERLAP_SKIN || col == self;

            // if we aren't already filtering ourselves out, check to see if we're a collider
            if (!filterout && hit.collider.isTrigger)
            {
                CallTriggerableEvents(hit.collider.gameObject, hit.normal, hit.point, ref position);
                filterout = true;
            }

            if (filterout)
            {
                tracesfound--;

                if (i < tracesfound)
                    hits[i] = hits[tracesfound];
            }
            else if (tracelen >= 0 && tracelen < _closestdistance)
            {
                _closestdistance = tracelen;
                closestindex = i;
            }
        }
    }

    protected bool CheckGround(ref MgCollisionStruct collision)
    {
        return CheckGround(ref collision, collision.selfUpVecAngle);
    }

    protected virtual bool CheckGround(ref MgCollisionStruct collision, float normalAngle)
    {
        return m_slopeLimit >= normalAngle
        //&& 1 == collision.selfNormal.sqrMagnitude
        && GfcPhysics.LayerIsInMask(collision.collider.gameObject.layer, GfcPhysics.GroundLayers());
    }

    private static void ParentingVelocityAdjustVector(ref Vector3 selfVelocity, Vector3 parentVelocity)
    {
        Vector3 parentNorm = parentVelocity.normalized;

        float velocityDot = System.MathF.Max(0, Vector3.Dot(selfVelocity.normalized, parentNorm));
        float speedToDecrease = System.MathF.Min(selfVelocity.magnitude, velocityDot * parentVelocity.magnitude);
        selfVelocity -= parentNorm * speedToDecrease;
    }

    private void ParentingVelocityAdjustVector(Vector3 parentVelocity)
    {
        Vector3 selfVelocity = GetVelocity();
        ParentingVelocityAdjustVector(ref selfVelocity, parentVelocity);
        SetVelocity(selfVelocity);
    }

    public void SetParentTransform(Transform parent, uint priority = 0, bool overridePriority = false)
    {
        if (parent)
        {
            Transform oldParent = m_parentTransform;
            if (m_parentTransform.CanSet(priority, overridePriority) && parent != m_parentTransform)
            {
                if (oldParent) DetachFromParentTransform(true, priority);
                m_parentTransform.SetValue(parent, priority, overridePriority);
                m_adjustedVelocityToParent = false;
                m_timeOfLastParentPosUpdate = m_timeOfLastParentRotUpdate = Time.timeAsDouble;

                m_parentRotMov = m_parentPosMov = ZERO3;
                m_parentLastPos = parent.position;
                m_parentLastRot = parent.rotation;
            }
        }
        else
        {
            DetachFromParentTransform(true, priority, overridePriority);
        }
    }

    public void DetachFromParentTransform(bool addVelocity = true, uint priority = 0, bool overridePriority = false)
    {
        Transform oldParent = m_parentTransform;
        if (oldParent && m_parentTransform.SetValue(null, priority, overridePriority))
        {
            if (addVelocity)
            {
                Vector3 parentVelocity = GetParentVelocity(Time.time, Time.deltaTime);

                //remove velocity that is going down
                float verticalFallSpeed = System.MathF.Max(0, -Vector3.Dot(m_upVec, parentVelocity));
                float deltaTimeCoef = GetDeltaTimeCoef();

                parentVelocity.x += m_upVec.x * verticalFallSpeed;
                parentVelocity.y += m_upVec.y * verticalFallSpeed;
                parentVelocity.z += m_upVec.z * verticalFallSpeed;
                Div(ref parentVelocity, deltaTimeCoef);

                //TODO, not sure why i put todo here, probably because this doesn't work well with time stop, but last Phys delta time should be affected by the time stop delta coef, so i dunno
                if (m_interpolateThisFrame) Add(ref m_interpolationMovement, parentVelocity * m_lastPhysDeltaTime);
                AddVelocity(parentVelocity);
            }

            if (m_currentGroundCollision.collider && oldParent == m_currentGroundCollision.collider.transform)
                m_currentGroundCollision.collider = null;
            m_parentPosMov = m_parentRotMov = ZERO3;
        }
    }

    private void BeginUpVecSmoothing(Vector3 newUpVec)
    {
        float angleDifference = AngleDegNorm(m_rotationUpVec, newUpVec);
        m_rotationSmoothSeconds = angleDifference * INV_180 * m_fullRotationSeconds;
        m_rotationSmoothProgress = 0;
        m_initialUpVec = m_rotationUpVec;
        m_refUpVecSmoothRot = 0;
    }

    public bool SetParentSpherical(Transform parent, uint priority = 0, bool overridePriority = false)
    {
        bool changedGravity = false;
        if (parent)
        {
            Transform currentParent = m_parentSpherical;
            if (m_parentSpherical.SetValue(parent, priority, overridePriority) && m_parentSpherical != currentParent)
            {
                changedGravity = true;
                Vector3 newUpVec = m_transform.position - parent.position;
                Normalize(ref newUpVec);
                BeginUpVecSmoothing(newUpVec);
            }
        }
        else
        {
            changedGravity = true;
            DetachFromParentSpherical(priority, UPDIR, overridePriority);
        }

        return changedGravity;
    }

    public bool CopyGravityFrom(GfMovementGeneric movement, bool overridePriority = false)
    {
        Transform sphericalParent = movement.GetParentSpherical();//todo
        uint priority = movement.GetGravityPriority();
        bool ret;
        if (sphericalParent)
            ret = SetParentSpherical(sphericalParent, priority, overridePriority);
        else
            ret = SetUpVec(movement.GetUpVecRaw(), priority, overridePriority);

        return ret;
    }

    public void OrientToUpVecForced()
    {
        m_rotationSmoothProgress = 1;
        UpdateSphericalOrientation(true, 0);
    }

    public void ReturnToDefaultValues()
    {
        m_velocity = Vector3.zero;
        SetParentTransform(null, 0, true);
        DetachFromParentSpherical(0, UPDIR, true);
        m_interpolateThisFrame = false;
        m_interpolationMovement = Vector3.zero;
        m_interpolationRotation = Quaternion.identity;
        m_currentGroundCollision.collider = null;
        OrientToUpVecForced();
    }

    public bool DetachFromParentSpherical(uint priority, Vector3 newUpVec, bool overridePriority = false)
    {
        bool changedParent = m_parentSpherical.SetValue(null, priority, overridePriority);
        if (changedParent)
        {
            if (!newUpVec.Equals(m_upVec))
                BeginUpVecSmoothing(newUpVec);

            m_upVec = newUpVec;
        }

        return changedParent;
    }

    public bool SetUpVec(Vector3 upVec, uint priority = 0, bool overridePriority = false)
    {
        return DetachFromParentSpherical(priority, upVec, overridePriority);
    }

    public Vector3 GetParentVelocity(double time, float deltaTime)
    {
        Vector3 parentVelocity = ZERO3;
        float maxTimeSinceLastFrame = System.MathF.Max(Time.fixedDeltaTime, deltaTime) * 2.0f; //if more than 2 frame/fixed update passed since movement, assume parent was stationary

        if (time - m_timeOfLastParentPosUpdate <= maxTimeSinceLastFrame && 0 < m_parentDeltaTimePos)
        {
            float invTimeSinceLastFrame = 1.0f / m_parentDeltaTimePos;

            parentVelocity.x = m_parentPosMov.x * invTimeSinceLastFrame;
            parentVelocity.y = m_parentPosMov.y * invTimeSinceLastFrame;
            parentVelocity.z = m_parentPosMov.z * invTimeSinceLastFrame;
        }

        if (time - m_timeOfLastParentRotUpdate <= maxTimeSinceLastFrame && 0 < m_parentDeltaTimeRot)
        {
            float invTimeSinceLastFrame = 1.0f / m_parentDeltaTimeRot;

            parentVelocity.x += m_parentRotMov.x * invTimeSinceLastFrame;
            parentVelocity.y += m_parentRotMov.y * invTimeSinceLastFrame;
            parentVelocity.z += m_parentRotMov.z * invTimeSinceLastFrame;
        }

        return parentVelocity;
    }

    public Vector3 MovementDirComputed(Vector3 movDir, bool canFly)
    {
        //remove vertical component if we can't fly
        if (!canFly)
        {
            RemoveAxisKeepMagnitude(ref movDir, m_upVec);

            if (!m_slopeNormal.Equals(m_upVec))
            {
                float mag = movDir.magnitude;
                StraightProjectOnPlane(ref movDir, m_slopeNormal, m_upVec);
                movDir.Normalize();
                Mult(ref movDir, mag);
            }
        }

        return movDir;
    }

    public Vector3 UpVecEstimated()
    {
        if (m_parentSpherical.Value)
            return (transform.position - m_parentSpherical.Value.position).normalized;
        else
            return m_upVec;
    }

    //The orientation's upvec without any external rotations, should be used when rotating the character
    public Vector3 GetUpvecRotation() { return m_rotationUpVec; }

    public Vector3 GetUpVecRaw() { return m_upVec; }

    public Transform GetParentTransform() { return m_parentTransform; }

    public float GetSlopeLimitDeg() { return m_slopeLimit; }

    public float GetUpperSlopeLimitDeg() { return m_upperSlopeLimit; }

    public void SetSlopeLimitDeg(float limitDeg) { m_slopeLimit = limitDeg; }

    public void SetUpperSlopeLimitDeg(float limitDeg) { m_upperSlopeLimit = limitDeg; }

    public Transform GetParentSpherical() { return m_parentSpherical; }

    public GfRunnerTemplate GetRunnerTemplate() { return m_runner; }

    public uint GetGravityPriority() { return m_parentSpherical.GetPriority(); }

    public uint GetParentPriority() { return m_parentTransform.GetPriority(); }

    public Quaternion GetCurrentRotation() { return m_currentRotation; }

    public virtual Quaternion GetTransformRotation() { return m_transform.rotation; }

    public GfgStatsCharacter GetGfgStatsCharacter() { return m_statsCharacter; }

    public void SetStatsCharacter(GfgStatsCharacter GfgStatsCharacter) { m_statsCharacter = GfgStatsCharacter; }

    public float GetDeltaTimeCoef()
    {
        float coef = 1;
        if (m_statsCharacter)
            coef = m_statsCharacter.GetDeltaTimeCoef();

        return coef;
    }

    public virtual Vector3 AddVelocity(Vector3 force)
    {
        Add(ref m_velocity, force);
        return m_velocity;
    }

    public void SetCurrentRotation(Quaternion currentRotation)
    {
        m_currentRotation = currentRotation;
    }

    public virtual void SetPosition(Vector3 position, bool local = false)
    {
        if (local)
            m_transform.localPosition = position;
        else
            m_transform.position = position;
    }


    public virtual void SetRotation(Quaternion rotation, bool local = false)
    {
        if (local)
            m_transform.rotation = rotation;
        else
            m_transform.localRotation = rotation;
    }

    public virtual bool UsesRigidbody() { return false; }

    public virtual Vector3 GetVelocity()
    {
        return m_velocity;
    }

    public Vector3 GetSlope() { return m_slopeNormal; }

    public virtual void SetVelocity(Vector3 velocity) { m_velocity = velocity; }

    public GravityReference GetGravityReference() { return new(m_upVec, m_parentSpherical); }

    public virtual bool GetIsGrounded() { return m_isGrounded; }

    public virtual void SetIsGrounded(bool isGrounded) { m_isGrounded = isGrounded; }
}

#region MiscType

public unsafe struct MgCollisionStruct
{
    public MgCollisionStruct(Vector3 selfNormal, Vector3 upVec, Collider collider, Vector3 point, bool calculatedPoint, ArchetypeCollision archetypeCollision, bool overlap, Vector3 selfPosition)
    {
        this.collider = collider;
        this.normal = selfNormal;
        this.point = point;
        selfUpVecAngle = AngleDegNorm(selfNormal, upVec) + 0.00001F;
        //Debug.Log("angle is " + selfUpVecAngle);
        isGrounded = false;
        this.archetypeCollision = archetypeCollision;
        this.calculatedPoint = calculatedPoint;
        this.overlap = overlap;
        this.selfPosition = selfPosition;
        selfVelocity = default;
        colliderHit = null;
        isStair = false;
        this.upVec = upVec;
        calculatedHitUpVecAngle = false;
        hitUpVecAngle = 0;
    }

    private RaycastHit* colliderHit; //we use a pointer in order to avoid allocating unnecessary memory, in the case that colliderHit is not used

    private float hitUpVecAngle;

    private bool calculatedHitUpVecAngle;
    public Vector3 selfPosition; //the position of the main object at the time of contact
    public Collider collider;

    public Vector3 upVec;

    public Vector3 normal; //normal of the self collider
    public float selfUpVecAngle;
    private Vector3 point;
    //if the normal and distance were computed
    public bool isGrounded;

    private ArchetypeCollision archetypeCollision;

    private bool calculatedPoint;

    public bool overlap;

    public bool isStair;

    public Vector3 selfVelocity;

    public Vector3 GetPoint()
    {
        if (!calculatedPoint)
        {
            calculatedPoint = true;
            point = archetypeCollision.InnerRayCast(-normal);
            Add(ref point, selfPosition);
        }

        return point;
    }

    public void SetPoint(Vector3 point)
    {
        calculatedPoint = true;
        this.point = point;
    }

    public void SetRaycastHit(RaycastHit hit)
    {
        colliderHit = &hit;
    }

    public float GetHitUpVecAngle()
    {
        if (!calculatedHitUpVecAngle)
        {
            calculatedHitUpVecAngle = true;
            hitUpVecAngle = AngleDegNorm(upVec, GetRaycastHit().normal);
        }

        return hitUpVecAngle;
    }

    public RaycastHit GetRaycastHit()
    {
        if (colliderHit == null)
        {
            RaycastHit hit;

            if (collider)
            {
                Vector3 rayStart = GetPoint() + upVec;
                Ray stairRay = new Ray(rayStart, -upVec);
                collider.Raycast(stairRay, out hit, 2);
            }

            colliderHit = &hit;
        }

        return *colliderHit;
    }
}

#endregion



