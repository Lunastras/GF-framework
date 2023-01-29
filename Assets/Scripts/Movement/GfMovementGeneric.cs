using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public abstract class GfMovementGeneric : MonoBehaviour
{
    #region Variables
    [SerializeField]
    public bool CanFly;
    [SerializeField]
    private bool m_useInterpolation;
    [SerializeField]
    protected float m_speed = 7;
    [SerializeField]
    protected float m_mass = 35;
    [SerializeField]
    protected float m_stepOffset = 0.3f;
    [SerializeField]
    protected float m_slopeLimit = 45;
    [SerializeField]
    protected float m_upperSlopeLimit = 100;
    [SerializeField]
    private QueryTriggerInteraction m_queryTrigger;

    [SerializeField]
    private bool m_useSimpleCollision = true;
    protected Transform m_parentSpherical;

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
    public Vector3 MovementDirUpVec { get; protected set; } //the up direction of the given movement direction (NOT NECESSARY TO BE PERPENDICULAR)

    [HideInInspector]
    public bool JumpTrigger = false;

    protected bool m_jumpTriggerReleased;
    protected Transform m_parentTransform;

    [HideInInspector]
    public Vector3 m_velocity;

    protected Vector3 m_upVec = UPDIR;

    protected bool m_isGrounded = false;

    protected Vector3 m_slopeNormal;
    protected uint m_parentTransformPriority = 0;
    protected uint m_gravityPriority = 0;

    private bool m_adjustedVelocityToParent;  // whether or not the velocity was adjusted to that of the parent upon parenting

    private Quaternion m_parentLastRot;
    private Vector3 m_parentRotMov, m_parentLastPos, m_parentPosMov;
    private double m_timeOfLastParentRotUpdate, m_timeOfLastParentPosUpdate;

    private float m_parentDeltaTimePos, m_parentDeltaTimeRot; //delta of the last movement from the parent. 

    private Vector3 m_interpolationMovement;
    private Quaternion m_interpolationRotation = Quaternion.identity;

    private float m_timeBetweenPhysChecks = 0.02f;

    private float m_previousLerpAlpha;

    private float m_accumulatedTimefactor = 0;

    private float m_rotationSmoothTime;
    private float m_rotationSmoothProgress;

    private Vector3 m_initialUpVec;

    [SerializeField]
    private float m_maxOverlaps = 4;
    [SerializeField]

    private float m_maxCasts = 4;


    // private float m_timeUntilPhysChecks = 0;

    private double m_timeOfLastPhysCheck;

    private bool m_interpolateThisFrame = false;
    private bool m_validatedParentVertical; //whether the vertical movement of the parent was accounted to the actor's position

    private ArchetypeCollision m_collisionArchetype;
    static readonly protected Vector3 Zero3 = new Vector3(0, 0, 0);
    private readonly float TRACE_SKIN = 0.01F;
    private readonly float TRACEBIAS = 0.1F;
    private readonly float DOWNPULL = 0.25F;
    private const float MIN_DISPLACEMENT = 0.000000001F; // min squared length of a displacement vector required for a Move() to proceed.
    private const float MIN_PUSHBACK_DEPTH = 0.000001F;

    private float m_currentRotationSpeed;


    private const float MAX_ANGLE_NO_SMOOTH = 5f;

    protected float m_gravityCoef = 1.0f;

    #endregion

    private void Start()
    {
        m_transform = transform;
        m_slopeNormal = m_upVec;
        ValidateCollisionArchetype();
        m_collisionArchetype.UpdateValues();

        InternalStart();
    }
    protected abstract void InternalStart();
    protected virtual void BeforePhysChecks(float deltaTime) { }
    protected virtual void AfterPhysChecks(float delteTime) { }

    private void ValidateCollisionArchetype()
    {
        if (null == m_collider || null == m_collisionArchetype)
        {
            //type checking is not available in c# without an IDictionary, so we check until one is found. It works because there are only a few anywyay
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider)
            {
                m_collider = capsuleCollider;
                m_collisionArchetype = new ArchetypeCapsule(capsuleCollider);
            }
            else
            {
                SphereCollider sphereCollider = GetComponent<SphereCollider>();
                if (sphereCollider)
                {
                    m_collider = sphereCollider;
                    m_collisionArchetype = new ArchetypeSphere(sphereCollider);
                }
            }

            if (null == m_collisionArchetype)
            {
                m_collisionArchetype = new ArchetypeCollision();
                if (GetComponent<SphereCollider>() != null)
                    Debug.LogError("The collider attached to: " + gameObject.name + " is not supported by MovementGeneric");
            }
        }
    }

    private Vector3 GetParentMovement(Vector3 position, float deltaTime, double currentTime)
    {
        Vector3 movement = Zero3;
        if (m_parentTransform != null)
        {
            Vector3 currentParentPos = m_parentTransform.position;
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
            Quaternion currentRot = m_parentTransform.rotation;
            if (!currentRot.Equals(m_parentLastRot)) // .Equals() is different from == (and a bit wrong), but it works better here because of the added accuracy
            {
                Quaternion deltaQuaternion = currentRot * Quaternion.Inverse(m_parentLastRot);
                Vector3 vecFromParent = position - m_parentTransform.position;
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

                AdjustVector(ref m_velocity, parentVelocity);
                GfTools.Mult3(ref parentVelocity, m_previousPhysDeltaTime);
                GfTools.Minus3(ref parentVelocity, m_upVec * Vector3.Dot(m_upVec, parentVelocity)); //remove any vertical movement from parent velocity            
                AdjustVector(ref m_interpolationMovement, parentVelocity); //adjust interpolation 
            }
        }

        return movement;
    }

    public void Move(float deltaTime)
    {
        double currentTime = Time.timeAsDouble;
        m_transform.position += GetParentMovement(m_transform.position, deltaTime, currentTime);

        if (m_interpolateThisFrame)
        {
            float timeSincePhysCheck = (float)(currentTime - m_timeOfLastPhysCheck);
            float timeBetweenChecks = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks);
            float alpha = System.MathF.Min(1.0f, timeSincePhysCheck / timeBetweenChecks);
            float timefactor = alpha - m_previousLerpAlpha;
            m_previousLerpAlpha = alpha;
            m_accumulatedTimefactor += timefactor;

            m_transform.position += m_interpolationMovement * timefactor;
            m_transform.rotation *= Quaternion.LerpUnclamped(Quaternion.identity, m_interpolationRotation, timefactor);

            UpdateSphericalOrientation(deltaTime, true);
        }
    }


    public void UpdatePhysics(float deltaTime, bool updateParentMovement = true, float timeUntilNextUpdate = -1, bool updatePhysicsValues = true)
    {
        if (m_interpolateThisFrame)
        {
            float correctionFactor = 1.0f - m_accumulatedTimefactor;
            m_accumulatedTimefactor = m_previousLerpAlpha = 0;

            m_transform.position += m_interpolationMovement * correctionFactor;
            Quaternion correctedRotation = GfTools.QuaternionFraction(m_interpolationRotation, correctionFactor);
            m_transform.rotation = m_transform.rotation * correctedRotation;
            m_interpolationRotation = Quaternion.identity;
        }

        double currentTime = Time.timeAsDouble;

        if (timeUntilNextUpdate < 0)
            timeUntilNextUpdate = (float)(currentTime - m_timeOfLastPhysCheck);

        m_timeBetweenPhysChecks = timeUntilNextUpdate;
        m_timeOfLastPhysCheck = currentTime;
        m_previousPhysDeltaTime = deltaTime;

        UpdateSphericalOrientation(deltaTime, false);
        Quaternion currentRotation = m_transform.rotation;
        if (updateParentMovement)
            m_transform.position += GetParentMovement(m_transform.position, deltaTime, currentTime);
        Vector3 position = m_transform.position;

        BeforePhysChecks(deltaTime);

        m_interpolateThisFrame = m_useInterpolation; //&& Time.deltaTime < m_timeBetweenPhysChecks;

        Vector3 lastNormal = Zero3;

        Collider[] colliderbuffer = GfPhysics.GetCollidersArray();
        int layermask = GfPhysics.GetLayerMask(gameObject.layer);

        if (updatePhysicsValues)
        {
            ValidateCollisionArchetype();
            m_collisionArchetype.UpdateValues();
        }


        if (m_useSimpleCollision)
            UpdatePhysicsDiscrete(ref position, deltaTime, colliderbuffer, layermask);
        else
            UpdatePhysicsContinuous(ref position, deltaTime, colliderbuffer, GfPhysics.GetRaycastHits(), layermask);

        /* TRACING SECTION END*/
        if (m_interpolateThisFrame)
        {
            m_interpolationMovement = position - m_transform.position;
            Quaternion desiredRotation = m_transform.rotation;
            if (!currentRotation.Equals(desiredRotation)) //== operator is more appropriate but it doesn't have enough accuray
            {
                m_interpolationRotation = Quaternion.Inverse(currentRotation) * desiredRotation;
                m_transform.rotation = currentRotation;
            }
        }
        else
        {
            m_transform.position = position;
            UpdateSphericalOrientation(deltaTime, false);
        }


        if (!m_isGrounded) m_slopeNormal = m_upVec;

        AfterPhysChecks(deltaTime);
    }



    private void UpdatePhysicsDiscrete(ref Vector3 position, float deltaTime, Collider[] colliderbuffer, int layermask)
    {
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

            m_collisionArchetype.Overlap(position, layermask, m_queryTrigger, colliderbuffer, out numCollisions);
            ActorOverlapFilter(ref numCollisions, m_collider, colliderbuffer); /* filter ourselves out of the collider buffer */
            for (int ci = numCollisions - 1; ci >= 0; ci--)/* pushback against the first valid penetration found in our collider buffer */
            {
                Collider otherc = colliderbuffer[ci];
                Transform othert = otherc.transform;

                if (Physics.ComputePenetration(m_collider, position, m_transform.rotation, otherc,
                    othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                {
                    MgCollisionStruct collision = new MgCollisionStruct(normal, m_upVec, otherc, position - normal * mindistance);
                    collision.isGrounded = CheckGround(collision);
                    m_isGrounded |= collision.isGrounded;

                    position += normal * (mindistance + TRACE_SKIN);

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

                    // if (Vector3.Dot(m_velocity, normal) <= 0F) /* only consider normals that we are technically penetrating into */
                    DetermineGeometryType(ref m_velocity, ref lastNormal, collision, ref geometryclips, ref position);
                    MgOnCollision(collision);

                    break;
                }
                else
                {
                    --numCollisions;
                }
            } // for (int ci = 0; ci < numoverlaps; ci++)

        }// while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)
    }

    private void UpdatePhysicsContinuous(ref Vector3 position, float deltaTime, Collider[] colliderbuffer, RaycastHit[] tracesbuffer, int layermask)
    {

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
            m_collisionArchetype.Overlap(position, layermask, m_queryTrigger, colliderbuffer, out numCollisions);
            ActorOverlapFilter(ref numCollisions, m_collider, colliderbuffer); /* filter ourselves out of the collider buffer */
            for (int ci = numCollisions - 1; ci >= 0; ci--)/* pushback against the first valid penetration found in our collider buffer */
            {
                Collider otherc = colliderbuffer[ci];
                Transform othert = otherc.transform;

                if (Physics.ComputePenetration(m_collider, position, m_transform.rotation, otherc,
                    othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                {
                    MgCollisionStruct collision = new MgCollisionStruct(normal, m_upVec, otherc, position - normal * mindistance);
                    collision.isGrounded = CheckGround(collision);
                    m_isGrounded |= collision.isGrounded;

                    if (!groundCheckPhase)/* resolve pushback using closest exit distance if no downpull was added*/
                    {
                        position += normal * (mindistance + MIN_PUSHBACK_DEPTH);

                        if (Vector3.Dot(m_velocity, normal) <= 0F) /* only consider normals that we are technically penetrating into */
                            DetermineGeometryType(ref m_velocity, ref lastNormal, collision, ref geometryclips, ref position);
                        break;
                    }

                    MgOnCollision(collision);
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

                m_collisionArchetype.Trace(position, traceDir, _tracelen, layermask, m_queryTrigger, tracesbuffer, TRACEBIAS, out numCollisions);/* prevent tunneling by using this skin length */
                MgCollisionStruct collision = ActorTraceFilter(ref numCollisions, out int _i0, TRACEBIAS, m_collider, tracesbuffer, position);

                if (_i0 > -1) /* we found something in our trace */
                {
                    RaycastHit _closest = tracesbuffer[_i0];
                    collision.isGrounded = CheckGround(collision);

                    float _dist = System.MathF.Max(_closest.distance - TRACE_SKIN, 0F);
                    timefactor -= _dist * _traceLenInv;
                    GfTools.Mult3(ref traceDir, _dist);
                    GfTools.Add3(ref position, traceDir);

                    DetermineGeometryType(ref m_velocity, ref lastNormal, collision, ref geometryclips, ref position);
                    MgOnCollision(collision);
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
    }

    private void UpdateSphericalOrientation(float deltaTime, bool rotateOrientation)
    {
        if (m_parentSpherical)
        {
            m_upVec = System.MathF.Sign(m_gravityCoef) * (transform.position - m_parentSpherical.position).normalized;
        }

        if (rotateOrientation && m_upVec != m_rotationUpVec)
        {
            float angleDifference = GfTools.Angle(m_upVec, m_rotationUpVec);
            if (MAX_ANGLE_NO_SMOOTH <= angleDifference && 1.0f <= m_rotationSmoothProgress)
                m_rotationSmoothProgress = 0;

            Quaternion upVecRotCorrection;

            if (m_rotationSmoothProgress < 1.0f)
            {
                m_rotationSmoothProgress = Mathf.SmoothDamp(m_rotationSmoothProgress, 1.0f, ref m_currentRotationSpeed, m_rotationSmoothTime);
                Vector3 auxVec = Vector3.Lerp(m_initialUpVec, m_upVec, m_rotationSmoothProgress);
                upVecRotCorrection = Quaternion.FromToRotation(m_rotationUpVec, auxVec);
            }
            else
            {
                upVecRotCorrection = Quaternion.FromToRotation(m_rotationUpVec, m_upVec);
            }

            m_transform.rotation = upVecRotCorrection * m_transform.rotation;
            m_rotationUpVec = upVecRotCorrection * m_rotationUpVec;

            //Debug.Log("Offset angle is: " + GfTools.Angle(m_upVec, m_transform.up) + " and the error tracker is: " + GfTools.Angle(m_upVec, m_rotationUpVec));
        }
    }

    private float GetStepHeight(ref RaycastHit hit, ref Vector3 position)
    {
        Vector3 bottomPos = -(1 * 0.5f) * m_upVec;
        Vector3 localStepHitPos = (hit.point - position);

        return Vector3.Dot(m_upVec, localStepHitPos) - Vector3.Dot(m_upVec, bottomPos);
    }

    private void DetermineGeometryType(ref Vector3 velocity, ref Vector3 lastNormal, MgCollisionStruct collision, ref int geometryclips, ref Vector3 position)
    {
        bool underSlopeLimit = m_slopeLimit > collision.angle;
        m_isGrounded |= collision.isGrounded;
        bool recalculateVelocity = true;
        /*

        //Check for stair, if no stair is found, then perform normal velocity calculations
        if (!collision.pushback && !underSlopeLimit)
        {
            float stepHeight = GetStepHeight(ref hit, ref collider, UpVec, ref position);

            // Debug.Log("huuh second AND i potentially found a stair ");

            if (stepHeight <= stepOffset && stepHeight > 0.01f)
            {
                IsGrounded = true;
                //  Debug.Log("yeeee found stair ");
                position += UpVec * (stepHeight);
                recalculateVelocity = false;
            }
            else
            {
                // Debug.Log("neah step is wayyy too small");
            }
        }
        */

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

            if (collision.angle > m_upperSlopeLimit)
                GfTools.Minus3(ref velocity, m_slopeNormal * System.MathF.Max(0, dotSlopeVel));


            if (collision.angle < m_slopeLimit)
            {
                GfTools.RemoveAxis(ref velocity, m_slopeNormal);
                GfTools.RemoveAxis(ref velocity, collision.normal);
                m_slopeNormal = collision.normal;
            }

            GfTools.Minus3(ref velocity, (Vector3.Dot(velocity, collision.normal)) * collision.normal);
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
                MgCollisionStruct ownCollision = new MgCollisionStruct(Zero3, m_upVec, col, Zero3);
                GfMovementTriggerable trigger = col.GetComponent<GfMovementTriggerable>();
                if (trigger) trigger.MgOnTrigger(ownCollision, this);
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
            _hits[i].distance -= _bias;
            RaycastHit _hit = _hits[i];
            Collider _col = _hit.collider;
            float _tracelen = _hit.distance;
            bool filterout = _tracelen < 0F || _col == _self;

            // if we aren't already filtering ourselves out, check to see if we're a collider
            if (!filterout && _hit.collider.isTrigger)
            {
                MgCollisionStruct ownCollision = new MgCollisionStruct(-_hit.normal, m_upVec, m_collider, _hit.point - position);
                GfMovementTriggerable trigger = _col.GetComponent<GfMovementTriggerable>();
                if (trigger) trigger.MgOnTrigger(ownCollision, this);
                filterout = true;
            }

            if (filterout)
            {
                _tracesfound--;

                if (i < _tracesfound)
                    _hits[i] = _hits[_tracesfound];
            }
            else
            {
                MgCollisionStruct collision = new MgCollisionStruct(_hit.normal, m_upVec, _hit.collider, _hit.point - position);

                if (_tracelen < _closestdistance)
                {
                    _closestdistance = _tracelen;
                    _closestindex = i;
                    collisionToReturn = collision;
                }
                else MgOnCollision(collision);
            }
        }

        return collisionToReturn;
    }

    protected bool CheckGround(MgCollisionStruct collision)
    {
        return m_slopeLimit >= collision.angle
        && GfPhysics.LayerIsInMask(collision.collider.gameObject.layer, GfPhysics.GroundLayers());
    }

    private static void AdjustVector(ref Vector3 child, Vector3 parent)
    {
        Vector3 parentNorm = parent.normalized;

        float velocityDot = System.MathF.Max(0, Vector3.Dot(child.normalized, parentNorm));
        float speedToDecrease = System.MathF.Min(child.magnitude, velocityDot * parent.magnitude);
        child -= parentNorm * speedToDecrease;
    }

    public void SetParentTransform(Transform parent, uint priority = 0)
    {
        if (parent && parent != m_parentTransform && (m_parentTransformPriority <= priority))
        {
            if (m_parentTransform) DetachFromParentTransform(true, priority);
            m_adjustedVelocityToParent = m_validatedParentVertical = false;
            m_timeOfLastParentPosUpdate = m_timeOfLastParentRotUpdate = Time.time;

            m_parentRotMov = m_parentPosMov = Zero3;
            m_parentLastPos = parent.position;
            m_parentLastRot = parent.rotation;
            m_parentTransform = parent;
            m_parentTransformPriority = priority;
        }
    }

    public void DetachFromParentTransform(bool addVelocity = true, uint priority = 0)
    {
        if (m_parentTransform && (m_parentTransformPriority <= priority))
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
            m_parentTransform = null;
            m_parentTransformPriority = 0;
        }
    }

    public void SetParentSpherical(Transform parent, float smoothTime, uint priority = 0, float gravityCoef = 1)
    {
        if (parent && parent != m_parentSpherical && m_gravityPriority <= priority)
        {
            m_parentSpherical = parent;
            m_gravityPriority = priority;
            m_gravityCoef = gravityCoef;

            if (smoothTime > 0)
            {
                m_rotationSmoothTime = smoothTime;
                m_rotationSmoothProgress = 0;
                m_initialUpVec = m_rotationUpVec;
            }
        }
    }

    public void CopyGravity(GfMovementGeneric movement, float smoothTime)
    {
        uint priority = movement.m_gravityPriority;
        Transform sphericalParent = movement.GetParentSpherical();

        if (sphericalParent)
            SetParentSpherical(sphericalParent, smoothTime, priority);
        else
            SetUpVec(movement.UpVecEffective(), smoothTime, priority);

        m_gravityCoef = movement.GetGravityCoef();
    }

    public void DetachFromParentSpherical(uint priority, Vector3 newUpVec, float smoothTime, float gravityCoef = 1)
    {
        if (m_gravityPriority <= priority)
        {
            m_parentSpherical = null;
            m_gravityPriority = priority;
            m_gravityCoef = gravityCoef;

            if (smoothTime > 0)
            {
                m_rotationSmoothTime = smoothTime;
                m_rotationSmoothProgress = 0;
                m_initialUpVec = m_rotationUpVec;
            }

            m_upVec = newUpVec;
        }
    }

    public void DetachFromParentSpherical(float smoothTime, uint priority = 0)
    {
        DetachFromParentSpherical(priority, UPDIR, smoothTime);
    }

    public void SetUpVec(Vector3 upVec, float smoothTime, uint priority = 0, float gravityCoef = 1)
    {
        DetachFromParentSpherical(priority, upVec, smoothTime, gravityCoef);
    }

    public void SetGravityCoef(float gravityCoef)
    {
        m_upVec *= System.MathF.Sign(m_gravityCoef) * System.MathF.Sign(gravityCoef);
        m_gravityCoef = gravityCoef;
    }

    public float GetGravityCoef()
    {
        return m_gravityCoef;
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
        if (!CanFly || m_isGrounded) GfTools.RemoveAxisKeepMagnitude(ref movDir, MovementDirUpVec); //remove vertical component

        if ((!m_upVec.Equals(MovementDirUpVec)) && !movDir.Equals(Zero3)) //project on plane
            movDir = GfTools.RotationTo(MovementDirUpVec, m_upVec) * movDir;

        if (!m_slopeNormal.Equals(m_upVec) && !CanFly)
        {
            float mag = movDir.magnitude;
            GfTools.StraightProjectOnPlane(ref movDir, m_slopeNormal, m_upVec);
            movDir.Normalize();
            GfTools.Mult3(ref movDir, mag);
        }

        return movDir;
    }

    public virtual void SetMovementDir(Vector3 dir)
    {
        SetMovementDir(dir, UPDIR);
    }

    public Vector3 UpVecEstimated()
    {
        if (m_parentSpherical)
            return (transform.position - m_parentSpherical.position).normalized;
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
        return m_upVec * System.MathF.Sign(m_gravityCoef);
    }

    public Transform GetParentTransform()
    {
        return m_parentTransform;
    }

    public Transform GetParentSpherical()
    {
        return m_parentSpherical;
    }

    public uint GetParentTransformPriorityy()
    {
        return m_parentTransformPriority;
    }

    public uint GetParentSphericalPriority()
    {
        return m_gravityPriority;
    }

    public virtual void SetMovementDir(Vector3 dir, Vector3 upVec)
    {
        MovementDirUpVec = upVec;
        MovementDirRaw = dir;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void SetMovementSpeed(float speed) { this.m_speed = speed; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool GetIsGrounded() { return m_isGrounded; }
    protected abstract void MgOnCollision(MgCollisionStruct collision);
}

#region MiscType

public struct MgCollisionStruct
{

    public MgCollisionStruct(Vector3 normal, Vector3 upVec, Collider collider, Vector3 point)
    {
        this.collider = collider;
        this.normal = normal;
        this.point = point;
        angle = GfTools.Angle(normal, upVec) + 0.00001F;
        isGrounded = false;
    }
    public Collider collider;
    public Vector3 normal;
    public float angle;
    public Vector3 point;
    //if the normal and distance were computed
    public bool isGrounded;
}

#endregion



