using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public abstract class GfMovementGeneric : MonoBehaviour
{
    #region Variables
    [SerializeField]
    public bool CanFly;
    [SerializeField]
    protected bool m_useInterpolation;
    [SerializeField]
    protected float m_speed = 9;
    [SerializeField]
    protected float m_mass = 45;
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

    [SerializeField]
    protected float m_maxAngleNoSmoothDeg = 5f;

    protected PriorityValue<float> m_speedMultiplier = new(1);

    protected PriorityValue<float> m_massMultiplier = new(1);


    protected PriorityValue<Transform> m_parentSpherical = new();

    protected Transform m_transform;

    protected float m_previousPhysDeltaTime;
    protected static readonly Vector3 UPDIR = Vector3.up;


    //Used to keep track of the supposed current up direction
    //because the rotation is incremented, floating point errors make the rotation inaccurate
    //this value is used to identify and correct these calculation errors, as the UpVec might not actually be equal to the real
    //rotation up vector
    protected Vector3 m_rotationUpVec = UPDIR;


    //private new Rigidbody rigidbody;
    private Collider m_collider;

    public Vector3 MovementDirRaw { get; protected set; }

    [HideInInspector]
    public bool JumpTrigger = false;

    protected bool m_jumpTriggerReleased;
    protected PriorityValue<Transform> m_parentTransform = new();

    [HideInInspector]
    public Vector3 m_velocity;

    protected Vector3 m_upVec = UPDIR;

    protected bool m_isGrounded = false;

    protected Vector3 m_slopeNormal;

    private bool m_adjustedVelocityToParent;  // whether or not the velocity was adjusted to that of the parent upon parenting

    private Quaternion m_parentLastRot;
    private Vector3 m_parentRotMov, m_parentLastPos, m_parentPosMov;
    private double m_timeOfLastParentRotUpdate, m_timeOfLastParentPosUpdate;

    private float m_parentDeltaTimePos, m_parentDeltaTimeRot; //delta of the last movement from the parent. 

    private Vector3 m_interpolationMovement;
    private Quaternion m_interpolationRotation = Quaternion.identity;
    private Quaternion m_desiredRotation = Quaternion.identity;
    private Quaternion m_externalRotation = Quaternion.identity;

    private Quaternion m_lastRotation;

    private float m_timeBetweenPhysChecks = 0.02f;

    private float m_previousLerpAlpha;

    private float m_accumulatedTimefactor = 0;

    private float m_rotationSmoothSeconds;
    private float m_rotationSmoothProgress = 1.0f;

    private Vector3 m_initialUpVec;

    [SerializeField]
    private float m_maxOverlaps = 4;
    [SerializeField]

    private float m_maxCasts = 4;


    // private float m_timeUntilPhysChecks = 0;

    private double m_timeOfLastPhysCheck;

    private bool m_interpolateThisFrame = false;

    private ArchetypeCollision m_archetypeCollision;
    static readonly protected Vector3 Zero3 = new Vector3(0, 0, 0);
    private readonly float TRACE_SKIN = 0.05F;
    private readonly float TRACEBIAS = 0.1F;
    private readonly float DOWNPULL = 0.25F;

    protected const float OVERLAP_SKIN = 0.0f;
    private const float MIN_DISPLACEMENT = 0.000000001F; // min squared length of a displacement vector required for a Move() to proceed.
    private const float MIN_PUSHBACK_DEPTH = 0.000001F;

    private float m_refUpVecSmoothRot;

    protected readonly static Quaternion IDENTITY_QUAT = Quaternion.identity;

    private static List<GfMovementTriggerable> m_triggerResults;


    #endregion

    private void Start()
    {
        m_triggerResults = new(2);
        m_transform = transform;
        m_slopeNormal = m_upVec;
        m_lastRotation = m_transform.rotation;
        ValidateCollisionArchetype();
        m_archetypeCollision.UpdateValues();
        InternalStart();
    }

    protected abstract void InternalStart();
    protected virtual void BeforePhysChecks(float deltaTime) { }
    protected virtual void AfterPhysChecks(float delteTime) { }

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

    private Vector3 GetParentMovement(Vector3 position, float deltaTime, double currentTime)
    {
        Vector3 movement = Zero3;
        Transform parentTransform = m_parentTransform;
        if (parentTransform)
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
                movement = m_parentPosMov;
                GfTools.Add3(ref position, movement);
            }

            //Calculate the rotation according to the parent's rotation
            Quaternion currentRot = parentTransform.rotation;
            if (!currentRot.Equals(m_parentLastRot)) // .Equals() is different from == (and a bit wrong), but it works better here because of the added accuracy
            {
                Quaternion deltaQuaternion = currentRot * Quaternion.Inverse(m_parentLastRot);
                Vector3 vecFromParent = position - parentTransform.position;
                Vector3 newVecFromParent = deltaQuaternion * vecFromParent;
                m_parentRotMov = newVecFromParent - vecFromParent;

                if (MovementDirRaw == Zero3) //not working properly, gotta fix
                {
                    GfTools.RemoveAxis(ref vecFromParent, m_rotationUpVec);
                    GfTools.RemoveAxis(ref newVecFromParent, m_rotationUpVec);
                    float rotationDegrees = GfTools.SignedAngle(vecFromParent, newVecFromParent, m_rotationUpVec);
                    m_transform.rotation = Quaternion.AngleAxis(rotationDegrees, m_rotationUpVec) * m_transform.rotation;
                }

                m_parentDeltaTimeRot = (float)(currentTime - m_timeOfLastParentRotUpdate);
                m_timeOfLastParentRotUpdate = currentTime;
                m_parentLastRot = currentRot;
                GfTools.Add3(ref movement, m_parentRotMov);
            }

            //adjust the player's velocity to the parent's
            if (!m_adjustedVelocityToParent)
            {
                Vector3 parentVelocity = GetParentVelocity(currentTime, deltaTime);
                m_adjustedVelocityToParent = true;

                ParentingVelocityAdjustVector(ref m_velocity, parentVelocity);
                GfTools.Mult3(ref parentVelocity, m_previousPhysDeltaTime);
                GfTools.Minus3(ref parentVelocity, m_upVec * Vector3.Dot(m_upVec, parentVelocity)); //remove any vertical movement from parent velocity            
                ParentingVelocityAdjustVector(ref m_interpolationMovement, parentVelocity); //adjust interpolation 
            }
        }

        return movement;
    }

    public void Move(float deltaTime)
    {
        double currentTime = Time.timeAsDouble;
        Vector3 movementThisFrame = GetParentMovement(m_transform.position, deltaTime, currentTime);

        if (m_interpolateThisFrame)
        {
            float timeSincePhysCheck = (float)(currentTime - m_timeOfLastPhysCheck);
            float timeBetweenChecks = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks);
            float alpha = System.MathF.Min(1.0f, timeSincePhysCheck / timeBetweenChecks);
            float timefactor = alpha - m_previousLerpAlpha;
            m_previousLerpAlpha = alpha;
            m_accumulatedTimefactor += timefactor;

            movementThisFrame.x += m_interpolationMovement.x * timefactor;
            movementThisFrame.y += m_interpolationMovement.y * timefactor;
            movementThisFrame.z += m_interpolationMovement.z * timefactor;

            Quaternion currentRot = m_transform.rotation;
            if (!m_lastRotation.Equals(currentRot))
                m_externalRotation *= Quaternion.Inverse(m_lastRotation) * currentRot;

            if (!m_interpolationRotation.Equals(IDENTITY_QUAT))
            {
                currentRot *= Quaternion.LerpUnclamped(IDENTITY_QUAT, m_interpolationRotation, timefactor);
                m_transform.rotation = currentRot;
            }
        }

        UpdateSphericalOrientation(deltaTime, true);

        m_transform.position += movementThisFrame;
        m_lastRotation = m_transform.rotation;
    }


    public bool UpdatePhysics(float deltaTime, bool updateParentMovement = true, float timeUntilNextUpdate = -1, bool updatePhysicsValues = true, bool ignorePhysics = false)
    {
        if (m_interpolateThisFrame)
        {
            float correctionFactor = 1.0f - m_accumulatedTimefactor;
            m_accumulatedTimefactor = m_previousLerpAlpha = 0;

            m_transform.position += m_interpolationMovement * correctionFactor;
            Quaternion correctedRotation = GfTools.QuaternionFraction(m_interpolationRotation, correctionFactor);
            m_transform.rotation = m_desiredRotation * m_externalRotation;
            m_interpolationRotation = m_externalRotation = IDENTITY_QUAT;
        }

        double currentTime = Time.timeAsDouble;

        if (timeUntilNextUpdate < 0)
            timeUntilNextUpdate = (float)(currentTime - m_timeOfLastPhysCheck);

        m_timeBetweenPhysChecks = timeUntilNextUpdate;
        m_timeOfLastPhysCheck = currentTime;
        m_previousPhysDeltaTime = deltaTime;

        UpdateSphericalOrientation(deltaTime, false);
        if (updateParentMovement)
            m_transform.position += GetParentMovement(m_transform.position, deltaTime, currentTime);

        Quaternion initialRot = m_transform.rotation;
        Vector3 initialPos = m_transform.position;

        BeforePhysChecks(deltaTime);

        Vector3 position = m_transform.position;

        m_interpolateThisFrame = m_useInterpolation; //&& Time.deltaTime < m_timeBetweenPhysChecks;

        bool foundCollisions = false;
        if (!ignorePhysics)
        {
            Vector3 lastNormal = Zero3;

            Collider[] colliderbuffer = GfPhysics.GetCollidersArray();
            int layermask = GfPhysics.GetLayerMask(gameObject.layer);

            if (updatePhysicsValues)
            {
                ValidateCollisionArchetype();
                m_archetypeCollision.UpdateValues();
            }


            if (m_useSimpleCollision)
                foundCollisions = UpdatePhysicsDiscrete(ref position, deltaTime, colliderbuffer, layermask);
            else
                foundCollisions = UpdatePhysicsContinuous(ref position, deltaTime, colliderbuffer, GfPhysics.GetRaycastHits(), layermask);
        }
        else
        {
            GfTools.Add3(ref position, deltaTime * m_velocity);
        }

        AfterPhysChecks(deltaTime);

        /* TRACING SECTION END*/
        if (m_interpolateThisFrame)
        {
            m_interpolationMovement = position;
            GfTools.Minus3(ref m_interpolationMovement, initialPos);
            m_transform.position = initialPos;

            m_desiredRotation = m_transform.rotation;
            if (!initialRot.Equals(m_desiredRotation)) //== operator is more appropriate but it doesn't have enough accuray
            {
                m_interpolationRotation = Quaternion.Inverse(initialRot) * m_desiredRotation;
                m_transform.rotation = initialRot;
            }
        }
        else
        {
            m_transform.position = position;
            UpdateSphericalOrientation(deltaTime, false);
        }

        m_lastRotation = m_transform.rotation;

        if (!m_isGrounded) m_slopeNormal = m_upVec;

        return foundCollisions;
    }

    private bool UpdatePhysicsDiscrete(ref Vector3 position, float deltaTime, Collider[] colliderbuffer, int layermask)
    {
        bool foundCollisions = false;

        Vector3 lastNormal = Zero3;
        Vector3 addedDownpull = Zero3;

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
                GfTools.Add3(ref position, _trace);

            m_archetypeCollision.Overlap(position, layermask, m_queryTrigger, colliderbuffer, out numCollisions);

            ActorOverlapFilter(ref numCollisions, m_collider, colliderbuffer); /* filter ourselves out of the collider buffer */
            for (int ci = numCollisions - 1; ci >= 0; ci--)/* pushback against the first valid penetration found in our collider buffer */
            {
                Collider otherc = colliderbuffer[ci];
                Transform othert = otherc.transform;

                if (Physics.ComputePenetration(m_collider, position, m_transform.rotation, otherc,
                    othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                {
                    foundCollisions = true;

                    position += normal * (mindistance + TRACE_SKIN);

                    MgCollisionStruct collision = new MgCollisionStruct(normal, m_upVec, otherc, Zero3, false, m_archetypeCollision, true, true, position);
                    collision.isGrounded = CheckGround(collision);
                    m_isGrounded |= collision.isGrounded;

                    collision.selfPosition = position;
                    collision.selfVelocity = m_velocity;

                    if (MIN_DISPLACEMENT <= _tracelen)
                    {
                        Vector3 traceDir = _trace / _tracelen;
                        float _traceLenInv = 1.0f / _tracelen;
                        GfTools.Mult3(ref traceDir, _traceLenInv);

                        float velNormalDot = System.MathF.Max(0, -Vector3.Dot(normal, traceDir));
                        float pushbackLength = velNormalDot * mindistance;

                        float distance = System.MathF.Max(0, _tracelen - pushbackLength);
                        timefactor = System.MathF.Max(0f, timefactor - distance * _traceLenInv);
                    }

                    CallTriggerableEvents(ref collision);
                    // if (Vector3.Dot(m_velocity, normal) <= 0F) /* only consider normals that we are technically penetrating into */
                    DetermineGeometryType(ref m_velocity, ref lastNormal, ref collision, ref geometryclips);
                    MgOnCollision(ref collision);
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
        Vector3 lastNormal = Zero3;
        Vector3 addedDownpull = Zero3;
        bool appliedDownpull = m_isGrounded;
        if (appliedDownpull)
        {
            addedDownpull = m_slopeNormal * DOWNPULL;
            GfTools.Minus3(ref position, addedDownpull);
        }

        /* OVERLAP SECTION START*/
        int numbumps = 0;
        int geometryclips = 0;
        int numCollisions = -1;
        m_isGrounded = false;

        /* attempt an overlap pushback at this current position */
        while (numbumps++ < m_maxOverlaps && numCollisions != 0)
        {
            bool groundCheckPhase = appliedDownpull;
            m_archetypeCollision.Overlap(position, layermask, m_queryTrigger, colliderbuffer, out numCollisions);
            ActorOverlapFilter(ref numCollisions, m_collider, colliderbuffer); /* filter ourselves out of the collider buffer */
            for (int ci = numCollisions - 1; ci >= 0; ci--)/* pushback against the first valid penetration found in our collider buffer */
            {
                Collider otherc = colliderbuffer[ci];
                Transform othert = otherc.transform;

                if (Physics.ComputePenetration(m_collider, position, m_transform.rotation, otherc,
                    othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                {
                    foundCollisions = true;
                    Vector3 auxPosition = position + normal * (mindistance + MIN_PUSHBACK_DEPTH);
                    MgCollisionStruct collision = new MgCollisionStruct(normal, m_upVec, otherc, Zero3, false, m_archetypeCollision, true, true, auxPosition);
                    collision.isGrounded = CheckGround(collision);
                    m_isGrounded |= collision.isGrounded;
                    collision.selfPosition = position;
                    collision.selfVelocity = m_velocity;
                    CallTriggerableEvents(ref collision);

                    if (!groundCheckPhase)/* resolve pushback using closest exit distance if no downpull was added*/
                    {
                        position = auxPosition;

                        if (Vector3.Dot(m_velocity, normal) <= 0F) /* only consider normals that we are technically penetrating into */
                            DetermineGeometryType(ref m_velocity, ref lastNormal, ref collision, ref geometryclips);
                        break;
                    }

                    MgOnCollision(ref collision);
                    position = collision.selfPosition;
                }
                else
                {
                    // MgOnCollision(new MgCollisionStruct(normal, UpVec, otherc, mindistance, position - normal * mindistance, false, true, false));
                    --numCollisions;
                }
            } // for (int ci = 0; ci < numoverlaps; ci++)

            if (appliedDownpull) //remove downpull and continue overlap check normally
            {
                appliedDownpull = false;
                GfTools.Add3(ref position, addedDownpull);
            }
        }// while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)
        /* OVERLAP SECTION END*/

        if (appliedDownpull) GfTools.Add3(ref position, addedDownpull); //cancel downpull if we didn't touch anything

        /* TRACING SECTION START*/
        float timefactor = 1F;
        numbumps = 0;

        while (numbumps++ <= m_maxCasts && timefactor > 0)
        {
            Vector3 _trace = m_velocity * deltaTime * timefactor;

            float _tracelen = _trace.magnitude;


            if (_tracelen > MIN_DISPLACEMENT)
            {
                Vector3 traceDir = _trace;
                float _traceLenInv = 1.0f / _tracelen;
                GfTools.Mult3(ref traceDir, _traceLenInv);

                m_archetypeCollision.Trace(position, traceDir, _tracelen, layermask, m_queryTrigger, tracesbuffer, TRACEBIAS, out numCollisions);/* prevent tunneling by using this skin length */
                MgCollisionStruct collision = ActorTraceFilter(ref numCollisions, out int _i0, TRACEBIAS, m_collider, tracesbuffer, position);

                if (_i0 > -1 && collision.touched) /* we found something in our trace */
                {
                    foundCollisions = true;
                    RaycastHit _closest = tracesbuffer[_i0];
                    collision.isGrounded = CheckGround(collision);

                    float _dist = System.MathF.Max(_closest.distance - TRACE_SKIN, 0F);

                    timefactor -= _dist * _traceLenInv;
                    GfTools.Mult3(ref traceDir, _dist);
                    GfTools.Add3(ref position, traceDir);

                    collision.selfPosition = position;
                    collision.selfVelocity = m_velocity;

                    CallTriggerableEvents(ref collision);
                    DetermineGeometryType(ref m_velocity, ref lastNormal, ref collision, ref geometryclips);
                    MgOnCollision(ref collision);

                    position = collision.selfPosition;

                }
                else /* Discovered noting along our linear path */
                {
                    GfTools.Add3(ref position, _trace);
                    break;
                }
            }
            else break;

            //break; //if (_tracelen > MIN_DISPLACEMENT)        
        } // while (numbumps++ <= MAX_BUMPS && timefactor > 0)

        return foundCollisions;
    }

    private const float INV_180 = 0.005555555555f;

    public void UpdateSphericalOrientationInstant()
    {
        Quaternion upVecRotCorrection = Quaternion.FromToRotation(m_rotationUpVec, m_upVec);
        upVecRotCorrection = Quaternion.FromToRotation(m_rotationUpVec, m_upVec);

        m_transform.rotation = upVecRotCorrection * m_transform.rotation;
        m_rotationUpVec = upVecRotCorrection * m_rotationUpVec;

        m_rotationSmoothProgress = 1;
    }

    private void UpdateSphericalOrientation(float deltaTime, bool rotateOrientation)
    {
        if (m_parentSpherical.GetValue())
        {
            m_upVec = transform.position;
            GfTools.Minus3(ref m_upVec, m_parentSpherical.GetValue().position);
            GfTools.Normalize(ref m_upVec);
        }

        if (rotateOrientation && !m_upVec.Equals(m_rotationUpVec))
        {
            Quaternion upVecRotCorrection;

            if (m_rotationSmoothProgress < 0.9999f)
            {
                m_rotationSmoothProgress = Mathf.SmoothDamp(m_rotationSmoothProgress, 1, ref m_refUpVecSmoothRot, m_rotationSmoothSeconds);
                Vector3 auxVec = Vector3.Lerp(m_initialUpVec, m_upVec, m_rotationSmoothProgress);
                upVecRotCorrection = Quaternion.FromToRotation(m_rotationUpVec, auxVec);
            }
            else
            {
                upVecRotCorrection = Quaternion.FromToRotation(m_rotationUpVec, m_upVec);
            }

            m_transform.rotation = upVecRotCorrection * m_transform.rotation;
            m_rotationUpVec = upVecRotCorrection * m_rotationUpVec;
            m_desiredRotation = upVecRotCorrection * m_desiredRotation;
        }
    }

    private float GetStepHeight(Vector3 point, Vector3 position)
    {
        Vector3 bottomPos = m_archetypeCollision.GetLocalBottomPoint();
        Vector3 localStepHitPos = (point - position);

        return Vector3.Dot(m_upVec, localStepHitPos) - Vector3.Dot(m_upVec, bottomPos);
    }

    private void DetermineGeometryType(ref Vector3 velocity, ref Vector3 lastNormal, ref MgCollisionStruct collision, ref int geometryclips)
    {
        bool underSlopeLimit = m_slopeLimit > collision.upVecAngle;
        m_isGrounded |= collision.isGrounded;
        bool recalculateVelocity = true;

        //not perfect TODO
        //Check for stair, if no stair is found, then perform normal velocity calculations
        if (!underSlopeLimit)
        {
            float stepHeight = GetStepHeight(collision.GetPoint(), collision.selfPosition);
            Vector3 startPoint = collision.GetPoint();
            GfTools.Add3(ref startPoint, m_upVec);
            Ray stairRay = new Ray(startPoint, -m_upVec);

            if (stepHeight <= m_stepOffset && stepHeight > 0.0001f && collision.collider.Raycast(stairRay, out RaycastHit stairHit, 2))
            {
                MgCollisionStruct stairCollision = new(stairHit.normal, m_upVec, stairHit.collider, stairHit.point, true, m_archetypeCollision, true, false, collision.selfPosition);
                stairCollision.isGrounded = CheckGround(stairCollision);
                if (stairCollision.isGrounded)
                {
                    collision = stairCollision;
                    m_isGrounded = true;
                    collision.selfPosition += m_upVec * (stepHeight);
                    recalculateVelocity = false;
                }
            }
        }

        switch (geometryclips)
        {
            case 0: /* the first penetration plane has been identified in the feedback loop */
                if (recalculateVelocity)
                {
                    ClipVelocity(ref velocity, collision);
                    geometryclips = 1;
                }

                break;
            case 1: /* two planes have been discovered, which potentially result in a crease */
                if (recalculateVelocity)
                {
                    if (!collision.isGrounded)
                    {
                        Vector3 crease = Vector3.Cross(lastNormal, collision.normal);
                        crease.Normalize();
                        velocity = Vector3.Project(velocity, crease);
                    }
                    else ClipVelocity(ref velocity, collision);

                    geometryclips = 2;
                }

                break;
            case 2: /* three planes have been detected, our velocity must be cancelled entirely. */
                if (collision.isGrounded)
                    ClipVelocity(ref velocity, collision);
                else
                    velocity = Zero3;

                geometryclips = 2;
                break;
        }

        lastNormal = collision.normal;
    }

    protected virtual void ClipVelocity(ref Vector3 velocity, MgCollisionStruct collision)
    {
        if (Vector3.Dot(velocity, collision.normal) < 0F) //only do these calculations if the normal is facing away from the velocity
        {
            float dotSlopeVel = Vector3.Dot(m_slopeNormal, velocity); //dot of the previous slope

            if (collision.upVecAngle > m_upperSlopeLimit)
                GfTools.Minus3(ref velocity, m_slopeNormal * System.MathF.Max(0, dotSlopeVel));


            if (collision.isGrounded)
            {
                GfTools.RemoveAxis(ref velocity, m_slopeNormal);
                GfTools.RemoveAxis(ref velocity, collision.normal);
                m_slopeNormal = collision.normal;
            }

            GfTools.Minus3(ref velocity, (Vector3.Dot(velocity, collision.normal)) * collision.normal);
        }
    }

    protected void CallTriggerableEvents(ref MgCollisionStruct collisionStruct)
    {
        collisionStruct.collider.GetComponents<GfMovementTriggerable>(m_triggerResults);
        int count = m_triggerResults.Count;
        for (int j = 0; j < count; ++j)
        {
            m_triggerResults[j].MgOnTrigger(collisionStruct, this);
        }
    }

    // Simply a copy of ArchetypeHeader.OverlapFilters.FilterSelf() with trigger checking
    private void ActorOverlapFilter(ref int _overlapsfound, Collider _self, Collider[] _colliders)
    {
        for (int i = _overlapsfound - 1; i >= 0; i--)
        {
            Collider col = _colliders[i];
            bool filterout = col == _self;

            // we only want to filter out triggers that aren't the actor. Having an imprecise implementation of this filter
            // may lead to unintended consequences for the end-user.
            if (!filterout && col.isTrigger)
            {
                MgCollisionStruct ownCollision = new MgCollisionStruct(Zero3, m_upVec, col, Zero3, false, m_archetypeCollision, true, true, col.transform.position);
                CallTriggerableEvents(ref ownCollision);
                filterout = true;
            }

            if (filterout)
            {
                _overlapsfound--;

                if (i < _overlapsfound)
                    _colliders[i] = _colliders[_overlapsfound];
            }
        }
    }

    // Simply a copy of ArchetypeHeader.TraceFilters.FindClosestFilterInvalids() with added trigger functionality
    private MgCollisionStruct ActorTraceFilter(ref int _tracesfound, out int _closestindex, float _bias, Collider _self, RaycastHit[] _hits, Vector3 position)
    {
        float _closestdistance = Mathf.Infinity;
        _closestindex = -1;

        MgCollisionStruct collisionToReturn = default;

        for (int i = _tracesfound - 1; i >= 0; i--)
        {
            _hits[i].distance -= _bias + OVERLAP_SKIN;
            RaycastHit _hit = _hits[i];
            Collider _col = _hit.collider;
            float _tracelen = _hit.distance;
            bool filterout = _tracelen < -OVERLAP_SKIN || _col == _self;

            MgCollisionStruct collision = new MgCollisionStruct(_hit.normal, m_upVec, _hit.collider, _hit.point, true, m_archetypeCollision, _tracelen >= 0, false, position);
            // if we aren't already filtering ourselves out, check to see if we're a collider
            if (!filterout && _hit.collider.isTrigger && MgOnTriggerHit(ref collision))
            {
                CallTriggerableEvents(ref collision);
                filterout = true;
            }

            if (filterout)
            {
                _tracesfound--;

                if (i < _tracesfound)
                    _hits[i] = _hits[_tracesfound];
            }
            else if (MgOnTraceHit(ref collision) && collision.touched && _tracelen < _closestdistance)
            {
                _closestdistance = _tracelen;
                _closestindex = i;
                collisionToReturn = collision;
            }
            //s else MgOnCollision(ownCollision);
        }

        return collisionToReturn;
    }

    protected bool CheckGround(MgCollisionStruct collision)
    {
        return m_slopeLimit >= collision.upVecAngle
        && GfPhysics.LayerIsInMask(collision.collider.gameObject.layer, GfPhysics.GroundLayers());
    }

    private static void ParentingVelocityAdjustVector(ref Vector3 child, Vector3 parent)
    {
        Vector3 parentNorm = parent.normalized;

        float velocityDot = System.MathF.Max(0, Vector3.Dot(child.normalized, parentNorm));
        float speedToDecrease = System.MathF.Min(child.magnitude, velocityDot * parent.magnitude);
        child -= parentNorm * speedToDecrease;
    }

    public void SetParentTransform(Transform parent, uint priority = 0, bool overridePriority = false)
    {
        if (parent)
        {
            if (m_parentTransform.SetValue(parent, priority, overridePriority) && parent != m_parentTransform)
            {
                if (m_parentTransform.GetValue()) DetachFromParentTransform(true, priority);
                m_adjustedVelocityToParent = false;
                m_timeOfLastParentPosUpdate = m_timeOfLastParentRotUpdate = Time.time;

                m_parentRotMov = m_parentPosMov = Zero3;
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
        if (m_parentTransform.GetValue() && m_parentTransform.SetValue(null, priority, overridePriority))
        {
            if (addVelocity)
            {
                Vector3 parentVelocity = GetParentVelocity(Time.time, Time.deltaTime);

                //remove velocity that is going down
                float verticalFallSpeed = System.MathF.Max(0, -Vector3.Dot(m_upVec, parentVelocity));

                parentVelocity.x += m_upVec.x * verticalFallSpeed;
                parentVelocity.y += m_upVec.y * verticalFallSpeed;
                parentVelocity.z += m_upVec.z * verticalFallSpeed;

                GfTools.Add3(ref m_interpolationMovement, parentVelocity * m_previousPhysDeltaTime); //TODO
                GfTools.Add3(ref m_velocity, parentVelocity);
            }

            m_parentPosMov = m_parentRotMov = Zero3;
        }
    }

    private void BeginUpVecSmoothing(Vector3 newUpVec)
    {
        float angleDifference = GfTools.AngleDeg(m_rotationUpVec, newUpVec);
        m_rotationSmoothSeconds = angleDifference * INV_180 * m_fullRotationSeconds;
        m_rotationSmoothProgress = 0;
        m_initialUpVec = m_rotationUpVec;
        m_refUpVecSmoothRot = 0;
    }

    public void SetParentSpherical(Transform parent, uint priority = 0, bool overridePriority = false)
    {
        if (parent)
        {
            if (m_parentSpherical.SetValue(parent, priority, overridePriority) && parent != m_parentSpherical)
            {
                Vector3 newUpVec = m_transform.position - parent.position;
                GfTools.Normalize(ref newUpVec);
                BeginUpVecSmoothing(newUpVec);
            }
        }
        else
        {
            DetachFromParentSpherical(priority, UPDIR, overridePriority);
        }
    }

    public void CopyGravity(GfMovementGeneric movement, bool overridePriority = false)
    {
        Transform sphericalParent = movement.GetParentSpherical();//todo

        if (sphericalParent)
            SetParentSpherical(sphericalParent, priority, overridePriority);
        else
            SetUpVec(movement.UpVecEffective(), priority, overridePriority);
    }

    public void DetachFromParentSpherical(uint priority, Vector3 newUpVec, bool overridePriority = false)
    {
        if (overridePriority || m_gravityPriority <= priority)
        {
            m_gravityPriority = priority;
            m_upVec = newUpVec;

            if (!newUpVec.Equals(m_upVec) || m_parentSpherical)
                BeginUpVecSmoothing(newUpVec);

            m_parentSpherical = null;
        }
    }

    public void SetUpVec(Vector3 upVec, uint priority = 0, bool overridePriority = false)
    {
        DetachFromParentSpherical(priority, upVec, overridePriority);
    }

    public Vector3 GetParentVelocity(double time, float deltaTime)
    {
        Vector3 parentVelocity = Zero3;
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

    public Vector3 MovementDirComputed()
    {
        Vector3 movDir = MovementDirRaw;
        if (!CanFly || m_isGrounded) GfTools.RemoveAxisKeepMagnitude(ref movDir, m_upVec); //remove vertical component

        if (!m_slopeNormal.Equals(m_upVec) && !CanFly)
        {
            float mag = movDir.magnitude;
            GfTools.StraightProjectOnPlane(ref movDir, m_slopeNormal, m_upVec);
            movDir.Normalize();
            GfTools.Mult3(ref movDir, mag);
        }

        return movDir;
    }

    public Vector3 UpVecEstimated()
    {
        if (m_parentSpherical.GetValue())
            return (transform.position - m_parentSpherical.GetValue().position).normalized;
        else
            return m_upVec;
    }

    //The orientation's upvec without any external rotations, should be used when rotating the character
    public Vector3 UpvecRotation()
    {
        return m_rotationUpVec;
    }

    //The orientation's upvec without any external rotations, should be used when rotating the character
    public Vector3 UpVecEffective()
    {
        return m_upVec;
    }

    public Vector3 UpVecRaw()
    {
        return m_upVec;
    }

    public Transform GetParentTransform()
    {
        return m_parentTransform;
    }

    public PriorityValue<Transform> GetParentSpherical()
    {
        return m_parentSpherical;
    }

    public PriorityValue<float> GetSpeedMultiplier()
    {
        return m_speedMultiplier;
    }

    public PriorityValue<float> GetMassMultiplier()
    {
        return m_massMultiplier;
    }

    public void SetSpeedMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        m_speedMultiplier.SetValue(multiplier, priority, overridePriority);
    }

    public void SetMassMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        m_massMultiplier.SetValue(multiplier, priority, overridePriority);
    }

    public virtual void SetMovementDir(Vector3 dir)
    {
        MovementDirRaw = dir;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void SetMovementSpeed(float speed) { this.m_speed = speed; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool GetIsGrounded() { return m_isGrounded; }
    protected virtual void MgOnCollision(ref MgCollisionStruct collision) { }

    protected virtual bool MgOnTraceHit(ref MgCollisionStruct collision) { return true; }
    protected virtual bool MgOnTriggerHit(ref MgCollisionStruct collision) { return collision.touched; }
}

#region MiscType

public struct MgCollisionStruct
{

    public MgCollisionStruct(Vector3 normal, Vector3 upVec, Collider collider, Vector3 point, bool calculatedPoint, ArchetypeCollision archetypeCollision, bool touched, bool overlap, Vector3 positionSelf)
    {
        this.collider = collider;
        this.normal = normal;
        this.point = point;
        upVecAngle = GfTools.AngleDeg(normal, upVec) + 0.00001F;
        isGrounded = false;
        this.archetypeCollision = archetypeCollision;
        this.calculatedPoint = calculatedPoint;
        this.touched = touched;
        this.overlap = overlap;
        this.selfPosition = positionSelf;
        selfVelocity = default;
    }
    public Vector3 selfPosition; //the position of the main object at the time of contact
    public Collider collider;
    public Vector3 normal;
    public float upVecAngle;
    private Vector3 point;
    //if the normal and distance were computed
    public bool isGrounded;

    private ArchetypeCollision archetypeCollision;

    private bool calculatedPoint;

    public bool touched;

    public bool overlap;

    public Vector3 selfVelocity;

    public Vector3 GetPoint()
    {
        if (!calculatedPoint)
        {
            calculatedPoint = true;
            point = archetypeCollision.InnerRayCast(-normal);
            GfTools.Add3(ref point, selfPosition);
        }

        return point;
    }
}

#endregion



