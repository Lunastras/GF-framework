using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public abstract class MovementGeneric : MonoBehaviour
{
    #region Variables
    [SerializeField]
    public bool CanFly;
    [SerializeField]
    private bool m_useInterpolation;
    [SerializeField]
    protected float m_speed = 7;
    protected bool m_canMove = true;
    [SerializeField]
    protected float m_mass = 35;
    [SerializeField]
    protected float m_stepOffset = 0.3f;
    [SerializeField]
    protected float m_slopeLimit = 45;
    [SerializeField]
    protected float m_upperSlopeLimit = 100;
    [SerializeField]
    private float m_timeBetweenPhysChecks = 0.02f;

    protected Transform m_transform;

    protected float m_previousPhysDeltaTime;

    //private new Rigidbody rigidbody;
    private CapsuleCollider m_collider;

    public Vector3 MovementDir { get; protected set; }
    protected float m_movementDirMagnitude;

    [HideInInspector]
    public bool JumpTrigger = false;

    protected bool m_jumpTriggerReleased;
    protected Transform m_parentTransform;
    private bool m_adjustedVelocityToParent;  // whether or not the velocity was adjusted to that of the parent upon parenting

    private Vector3 m_parentLastRot, m_parentRotVel, m_parentLastPos, m_parentPosVel;
    private float m_timeOfLastParentRotUpdate, m_timeOfLastParentPosUpdate;

    private float m_parentDeltaTimePos, m_parentDeltaTimeRot; //delta of the last movement from the parent
    protected bool m_hasToAdjustVelocityWhenParenting = true;

    //Because we check the parent's movement after initially parenting, we don't know their speed so we wait one frame to see their velocity
    private bool m_parentWasSetThisFrame;

    [HideInInspector]
    public Vector3 Velocity;

    [HideInInspector]

    public Vector3 UpVec = Vector3.up;

    protected bool IsGrounded = false;

    public Vector3 SlopeNormal { get; private set; }

    private Vector3 m_movementUntilPhysCheck;

    private float m_previousLerpAlpha;

    private float m_accumulatedTimefactor = 0;

    private float m_timeUntilPhysChecks = 0;

    private float m_lastParentMovDeltaTime;

    private bool m_interpolateThisFrame;

    static readonly protected Vector3 Zero3 = new Vector3(0, 0, 0);


    private readonly float SKINEPSILON = 0.02F;
    private readonly float TRACEBIAS = 0.02F;

    private readonly float EXTRATRACE = 0.2F;

    private readonly float DOWNPULL = 0.0F;

    private readonly float VERTICALBIAS = 0.1F;

    private const int MAX_PUSHBACKS = 8; // # of iterations in our Pushback() funcs
    private const int MAX_BUMPS = 6; // # of iterations in our Move() funcs                      // a hit buffer.
    private const float MIN_DISPLACEMENT = 0.000000001F; // min squared length of a displacement vector required for a Move() to proceed.
    private const float MIN_PUSHBACK_DEPTH = 0.05F;

    #endregion

    private void Start()
    {
        Physics.autoSyncTransforms = true;

        m_transform = transform;

        //Application.targetFrameRate = 60;

        // Time.timeScale = 0.1f;
        m_collider = GetComponent<CapsuleCollider>();
        SlopeNormal = UpVec;
        InternalStart();
    }
    protected abstract void InternalStart();

    protected virtual void BeforePhysChecks(float deltaTime) { }
    protected virtual void AfterPhysChecks(float delteTime) { }

    [SerializeField]
    private bool m_calculateParentMovFirst = false;

    public void Move(float deltaTime)
    {
        if (m_calculateParentMovFirst)
        {
            m_parentWasSetThisFrame = false;
            ParentMovement(deltaTime);
            CalculateMovement(deltaTime);
        }
        else
        {
            CalculateMovement(deltaTime);
            ParentMovement(deltaTime);
        }
    }

    private void CalculateMovement(float deltaTime)
    {
        if ((m_timeUntilPhysChecks -= deltaTime) <= 0)
        {
            m_previousPhysDeltaTime = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks + m_timeUntilPhysChecks);
            PhysCheck(m_previousPhysDeltaTime); //actually the current deltatime   
            m_timeUntilPhysChecks += m_timeBetweenPhysChecks;
        }
        else if (m_interpolateThisFrame)
        {
            float timeBetweenChecks = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks);
            float alpha = (timeBetweenChecks - m_timeUntilPhysChecks) / timeBetweenChecks;
            float timefactor = alpha - m_previousLerpAlpha;
            m_previousLerpAlpha = alpha;
            m_accumulatedTimefactor += timefactor;

            m_transform.localPosition += m_movementUntilPhysCheck * timefactor;
        }
    }

    private void ParentMovement(float deltaTime)
    {
        if (m_parentTransform != null)
        {
            float currentTime = Time.time;

            Vector3 frameParentPosMov = Zero3; //raw movement since last position change
            Vector3 currentPos = m_parentTransform.position;
            if (m_parentLastPos != currentPos)
            {
                frameParentPosMov.x = currentPos.x - m_parentLastPos.x;
                frameParentPosMov.y = currentPos.y - m_parentLastPos.y;
                frameParentPosMov.z = currentPos.z - m_parentLastPos.z;

                m_parentDeltaTimePos = System.MathF.Max(deltaTime, currentTime - m_timeOfLastParentPosUpdate);
                m_timeOfLastParentPosUpdate = currentTime;
                m_parentLastPos = currentPos;
                m_parentPosVel = frameParentPosMov;
            }

            //Calculate the rotation according to the parent's rotation
            Vector3 frameParentRotMov = Zero3; //raw movement since last position change
            Vector3 currentRot = m_parentTransform.rotation.eulerAngles;
            if (m_parentLastRot != currentRot)
            {
                Vector3 parentRotation = (currentRot - m_parentLastRot);
                Vector3 dif = m_transform.position - m_parentTransform.position;

                if (m_parentLastRot.x != currentRot.x)
                    frameParentRotMov += Quaternion.AngleAxis(parentRotation.x, Vector3.right) * dif;

                if (m_parentLastRot.y != currentRot.y)
                    frameParentRotMov += Quaternion.AngleAxis(parentRotation.y, Vector3.up) * dif;

                if (m_parentLastRot.z != currentRot.z)
                    frameParentRotMov += Quaternion.AngleAxis(parentRotation.z, Vector3.forward) * dif;

                frameParentRotMov = (m_parentTransform.position + frameParentRotMov) - m_transform.position;

                if (MovementDir == Zero3) //todo, only works with up vec
                    m_transform.Rotate(Vector3.up * parentRotation.y);

                m_parentDeltaTimeRot = System.MathF.Max(deltaTime, currentTime - m_timeOfLastParentRotUpdate);
                m_timeOfLastParentRotUpdate = currentTime;
                m_parentLastRot = currentRot;
                m_parentRotVel = frameParentRotMov;
            }

            GfTools.Add3(ref frameParentPosMov, frameParentRotMov); //combine them into one

            //adjust the player's velocity to the parent's
            if (!m_adjustedVelocityToParent)
            {
                Debug.Log("ADJUSTED VELOCITY");
                Vector3 parentVelocity = GetParentVelocity(currentTime, deltaTime);
                m_adjustedVelocityToParent = true;

                AdjustVector(ref Velocity, parentVelocity);
                GfTools.Mult3(ref parentVelocity, m_previousPhysDeltaTime);
                AdjustVector(ref m_movementUntilPhysCheck, parentVelocity); //adjust interpolation 

                //assume the parent had the same movement last frame
                //GfTools.Mult3(ref frameParentPosMov, 2); //combine them into one
            }

            Debug.Log("The parent movement this frame was: " + frameParentPosMov);

            m_transform.localPosition += frameParentPosMov;
        }
    }

    private static void AdjustVector(ref Vector3 child, Vector3 parent)
    {
        Vector3 velocityNorm = child.normalized;
        float velocityDot = Vector3.Dot(parent.normalized, velocityNorm);

        if (velocityDot > 0)
        {
            float speedToDecrease = velocityDot * parent.magnitude;
            child = (child.magnitude > speedToDecrease) ? (child - velocityNorm * speedToDecrease) : Zero3;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PhysCheck(float deltaTime)
    {
        BeforePhysChecks(deltaTime);

        if (m_interpolateThisFrame)
        {
            Vector3 movementCorrection = m_movementUntilPhysCheck * (1.0f - m_accumulatedTimefactor);
            m_transform.localPosition += movementCorrection;
            m_accumulatedTimefactor = m_previousLerpAlpha = 0;
        }

        //if the framerate is smaller than the interval for physics checks, don't interpolate
        m_interpolateThisFrame = m_useInterpolation && Time.deltaTime < m_timeBetweenPhysChecks;

        Vector3 position = m_transform.position;
        Vector3 velocity = Velocity;
        Quaternion orientation = m_transform.rotation;
        Vector3 upDir = transform.up;
        Vector3 offset = (m_collider.height * 0.5f - m_collider.radius) * upDir; //todo
        Vector3 overlapOffset = offset + upDir * 0.1f; //todo
        float radius = m_collider.radius + SKINEPSILON;

        Vector3 lastNormal = Zero3;
        QueryTriggerInteraction querytype = QueryTriggerInteraction.Ignore;
        Collider self = m_collider;

        Collider[] colliderbuffer = GfPhysics.GetCollidersArray();
        RaycastHit[] tracesbuffer = GfPhysics.GetRaycastHits();
        int layermask = GfPhysics.GetLayerMask(gameObject.layer);

        /* OVERLAP SECTION START*/
        int numbumps = 0;
        int numpushbacks = 0;
        int geometryclips = 0;
        int numoverlaps = -1;

        /* attempt an overlap pushback at this current position */
        while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)
        {
            Overlap(position, overlapOffset, layermask, querytype, colliderbuffer, radius, out numoverlaps);
            ActorOverlapFilter(ref numoverlaps, self, colliderbuffer); /* filter ourselves out of the collider buffer */
            for (int ci = numoverlaps - 1; ci >= 0; ci--)/* pushback against the first valid penetration found in our collider buffer */
            {
                Collider otherc = colliderbuffer[ci];
                Transform othert = otherc.transform;
                Debug.Log("I will check collision with: " + otherc.name);
                MgCollisionStruct collision;

                //m_collider.radius = radius;

                if (Physics.ComputePenetration(self, position, orientation, otherc,
                    othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                {
                    Debug.Log("Penetrated " + otherc.name + " with a normal of: " + normal + " and a dot with velocity of: " + (Vector3.Dot(velocity, normal)) + " and a velocity of: " + velocity);
                    /* resolve pushback using closest exit distance */
                    Vector3 collisionPush = normal * (mindistance + MIN_PUSHBACK_DEPTH); //(mindistance );
                    position += collisionPush;
                    collision = new MgCollisionStruct(normal, UpVec, otherc, mindistance, position - normal * mindistance, true);

                    /* only consider normals that we are technically penetrating into */
                    if (Vector3.Dot(velocity, normal) <= 0F)
                        PM_FlyDetermineImmediateGeometry(ref velocity, ref lastNormal, collision, ref geometryclips, ref position);
                    //Debug.Break();

                    break;
                }
                else
                {
                    collision = new MgCollisionStruct(normal, UpVec, otherc, mindistance, position - normal * mindistance, true);
                    --numoverlaps;
                }

                MgOnCollision(collision);

            } // for (int ci = 0; ci < numoverlaps; ci++)
        }// while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)

        //  m_collider.radius -= SKINEPSILON;

        Debug.Log("finished collision checks ");

        /* OVERLAP SECTION END*/

        /* TRACING SECTION START*/
        float timefactor = 1F;
        int _tracecount = 0;

        Debug.Log("");
        Debug.Log("");

        Debug.Log("Let's trace lmao, IsGrounded is: " + IsGrounded + " the parent velocity is: " + GetParentVelocity(Time.time, Time.deltaTime));

        IsGrounded = false;


        while (numbumps++ <= MAX_BUMPS && timefactor > 0)
        {
            Debug.Log("");
            Vector3 _trace = velocity * deltaTime * timefactor;
            float _tracelen = _trace.magnitude;
            Debug.Log("Velocity is: " + velocity + " Trace is: " + _trace + " with a dir of: " + (_trace / _tracelen) + " and a magnitude of: " + _tracelen);

            // IF unable to trace any further, break and end
            Physics.SyncTransforms();
            if (_tracelen > MIN_DISPLACEMENT)
            {
                Vector3 traceDir = _trace / _tracelen;

                Trace(position, offset, traceDir, _tracelen + EXTRATRACE, layermask, querytype, tracesbuffer, m_collider.radius, TRACEBIAS, out _tracecount);/* prevent tunneling by using this skin length */
                Debug.Log("Traced objects before filter " + _tracecount);
                if (_tracecount > 0)
                {
                    for (int i = 0; i < _tracecount; ++i)
                    {
                        Debug.Log("TOUCHED " + tracesbuffer[i].transform.name);
                    }
                }

                MgCollisionStruct collision = ActorTraceFilter(ref _tracecount, out int _i0, TRACEBIAS, self, tracesbuffer, position);
                Debug.Log("Traced objects after filter " + _tracecount);

                float _dist = 0;
                RaycastHit _closest = default;
                if (_i0 > -1)
                {
                    _closest = tracesbuffer[_i0];
                    _dist = _closest.distance;
                    MgOnCollision(collision);
                }

                if (_i0 > -1 && _dist <= _tracelen) /* we found something in our trace */
                {
                    Debug.Log("I hit some things");

                    _dist = Mathf.Max(_closest.distance - SKINEPSILON, 0F);
                    timefactor -= _dist / _tracelen;

                    GfTools.Mult3(ref traceDir, _dist);
                    GfTools.Add3(ref position, traceDir);

                    /* determine our topology state */
                    PM_FlyDetermineImmediateGeometry(ref velocity, ref lastNormal, collision, ref geometryclips, ref position);
                }
                else /* Discovered an obstruction along our linear path */
                {
                    GfTools.Add3(ref position, _trace);
                    break;
                }
            }
            else
            {
                Debug.Log("meeeen that shiit smoll, not moving");
                break;
            }
            //break; //if (_tracelen > MIN_DISPLACEMENT)        
        } // while (numbumps++ <= MAX_BUMPS && timefactor > 0)

        /* TRACING SECTION END*/

        int safetycount = 0;
        if (numbumps > MAX_BUMPS)
        {
            Overlap(position, offset, layermask, querytype, colliderbuffer, radius, out safetycount); /* Safety check to prevent multiple actors phasing through each other... Feel free to disable this for performance if you'd like*/
            ActorOverlapFilter(ref safetycount, self, colliderbuffer); /* filter ourselves out of the collider buffer, no need to check for triggers */

            if (safetycount != 0) //don't move object if collision was found
                position = m_transform.position;
        }

        if (m_interpolateThisFrame)
            m_movementUntilPhysCheck = position - m_transform.position;
        else
            m_transform.position = position;

        if (!IsGrounded)
        {
            SlopeNormal = UpVec;
            // if(wasGrounded) //TODO remove negative velocity

        }

        Velocity = velocity;
        AfterPhysChecks(deltaTime);
    }

    float timeOfParenting;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetParentTransform(Transform parent)
    {
        if (parent != m_parentTransform)
        {
            Debug.Log("The new parent is: " + parent.name);
            m_adjustedVelocityToParent = false;
            m_timeOfLastParentPosUpdate = m_timeOfLastParentRotUpdate = Time.time;

            m_parentWasSetThisFrame = true;
            m_parentRotVel = m_parentPosVel = Zero3;
            m_parentLastPos = parent.position;
            m_parentLastRot = parent.rotation.eulerAngles;
            m_parentTransform = parent;

            timeOfParenting = Time.time;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DetachFromParent(bool addVelocity = true)
    {
        Debug.Log("DETACH was called with add velocity being: " + addVelocity);

        //  addVelocity = false;
        if (m_parentTransform != null)
        {
            if (addVelocity)
            {
                Vector3 parentVelocity = GetParentVelocity(Time.time, Time.deltaTime);

                //remove velocity that is going down
                float verticalFallSpeed = System.MathF.Max(0, -Vector3.Dot(UpVec, parentVelocity));

                parentVelocity.x += UpVec.x * verticalFallSpeed;
                parentVelocity.y += UpVec.y * verticalFallSpeed;
                parentVelocity.z += UpVec.z * verticalFallSpeed;

                GfTools.Add3(ref m_movementUntilPhysCheck, parentVelocity * m_previousPhysDeltaTime);
                GfTools.Add3(ref Velocity, parentVelocity);

                Debug.Log("I have deparented! with a velocity of: " + parentVelocity);
            }

            m_parentPosVel = m_parentRotVel = Zero3;
            m_parentTransform = null;
        }
        else Debug.Log("The parent was null bruh");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetParentVelocity(float time, float deltaTime)
    {
        Vector3 parentVelocity = Zero3;
        float maxTimeSinceLastFrame = System.MathF.Max(Time.fixedDeltaTime, deltaTime);

        if (time - m_timeOfLastParentPosUpdate <= maxTimeSinceLastFrame && 0 < m_parentDeltaTimePos)
        {
            float invTimeSinceLastFrame = 1.0f / m_parentDeltaTimePos;

            parentVelocity.x = m_parentPosVel.x * invTimeSinceLastFrame;
            parentVelocity.y = m_parentPosVel.y * invTimeSinceLastFrame;
            parentVelocity.z = m_parentPosVel.z * invTimeSinceLastFrame;
        }

        if (time - m_timeOfLastParentRotUpdate <= maxTimeSinceLastFrame && 0 < m_parentDeltaTimeRot)
        {
            float invTimeSinceLastFrame = 1.0f / m_parentDeltaTimeRot;

            parentVelocity.x += m_parentRotVel.x * invTimeSinceLastFrame;
            parentVelocity.y += m_parentRotVel.y * invTimeSinceLastFrame;
            parentVelocity.z += m_parentRotVel.z * invTimeSinceLastFrame;
        }

        return parentVelocity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClipVelocity(ref Vector3 velocity, MgCollisionStruct collision)
    {
        if (Vector3.Dot(velocity, collision.normal) < 0F) //only do these calculations if the normal is facing away from the velocity
        {
            float dotSlopeVel = Vector3.Dot(SlopeNormal, velocity); //dot of the previous slope

            if (collision.angle > m_upperSlopeLimit)
                GfTools.Minus3(ref velocity, SlopeNormal * System.MathF.Max(0, dotSlopeVel));


            if (collision.angle < m_slopeLimit)
            {
                GfTools.Minus3(ref velocity, SlopeNormal * dotSlopeVel);
                SlopeNormal = collision.normal;
                Debug.Log("I have set my slopeNormal to " + SlopeNormal);
            }

            GfTools.Minus3(ref velocity, (Vector3.Dot(velocity, collision.normal)) * collision.normal);

            Debug.Log("The velocity change was: " + (-(Vector3.Dot(velocity, collision.normal)) * collision.normal));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetStepHeight(ref RaycastHit hit, ref CapsuleCollider collider, Vector3 UpVec, ref Vector3 position)
    {
        Vector3 bottomPos = -(collider.height * 0.5f) * UpVec;
        Vector3 localStepHitPos = (hit.point - position);

        return Vector3.Dot(UpVec, localStepHitPos) - Vector3.Dot(UpVec, bottomPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PM_FlyDetermineImmediateGeometry(ref Vector3 velocity, ref Vector3 lastNormal, MgCollisionStruct collision, ref int geometryclips, ref Vector3 position)
    {
        bool underSlopeLimit = m_slopeLimit > collision.angle;
        IsGrounded |= underSlopeLimit;
        bool recalculateVelocity = true;

        /*
        //Check for stair, if no stair is found, then perform normal velocity calculations
        if (IsGrounded && (!underSlopeLimit) && (0 == geometryclips || (1 << 0) == geometryclips))
        {
            float stepHeight = GetStepHeight(ref hit, ref collider, UpVec, ref position);

           // Debug.Log("huuh second AND i potentially found a stair ");

            if (stepHeight <= stepOffset && stepHeight > 0.01f)
            {
                IsGrounded = true;
              //  Debug.Log("yeeee found stair ");
                position += UpVec * (stepHeight);
                recalculateVelocity = false;
            } else
            {
               // Debug.Log("neah step is wayyy too small");
            }
        }
        */

        //  Debug.Log("Geometry clips num is " + geometryclips);
        switch (geometryclips)
        {
            case 0: /* the first penetration plane has been identified in the feedback loop */
                // Debug.Log("FIRST SWITCH");
                if (recalculateVelocity)
                {
                    ClipVelocity(ref velocity, collision);
                    geometryclips = 1 << 0;
                }

                break;
            case (1 << 0): /* two planes have been discovered, which potentially result in a crease */

                // Debug.Log("SECOND SWITCH");

                if (recalculateVelocity)
                {
                    float creaseEpsilon = System.MathF.Cos(Mathf.Deg2Rad * m_slopeLimit);
                    //Debug.Log("SECOND SWITCH ");

                    if (Vector3.Dot(lastNormal, collision.normal) < creaseEpsilon)
                    {
                        //   Debug.Log("CREASED ");
                        Vector3 crease = Vector3.Cross(lastNormal, collision.normal);
                        crease.Normalize();
                        velocity = Vector3.Project(velocity, crease);
                        geometryclips |= (1 << 1);
                    }
                    else
                    {
                        // Debug.Log("DID NOT CREASE ");
                        ClipVelocity(ref velocity, collision);
                    }
                }

                break;
            case (1 << 0) | (1 << 1): /* three planes have been detected, our velocity must be cancelled entirely. */
                // Debug.Log("THIRD SWITCH ");
                velocity = Zero3;
                geometryclips |= (1 << 2);
                break;
        }

        lastNormal = collision.normal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProjectVector(ref Vector3 _v, Vector3 _n)
    {
        float _d = Vector3.Dot(_v, _n);
        _v.x = _n.x * _d;
        _v.y = _n.y * _d;
        _v.z = _n.z * _d;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Overlap(Vector3 _pos, Vector3 offset, int _filter, QueryTriggerInteraction _interacttype, Collider[] _colliders, float radius, out int _overlapcount)
    {
        _overlapcount = Physics.OverlapCapsuleNonAlloc(_pos - offset, _pos + offset, radius, _colliders, _filter, _interacttype);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Trace(Vector3 _pos, Vector3 offset, Vector3 _direction, float _len, LayerMask _filter, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, float radius, float tracebias, out int _tracecount)
    {
        GfTools.Minus3(ref _pos, _direction * tracebias);

        _tracecount = Physics.CapsuleCastNonAlloc(_pos - offset, _pos + offset, radius, _direction,
             _hits, _len + tracebias, _filter, _interacttype);
    }

    // Simply a copy of ArchetypeHeader.OverlapFilters.FilterSelf() with trigger checking
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ActorOverlapFilter(ref int _overlapsfound, Collider _self, Collider[] _colliders)
    {
        for (int i = _overlapsfound - 1; i >= 0; i--)
        {
            Collider col = _colliders[i];
            bool filterout = col == _self;

            // we only want to filter out triggers that aren't the actor. Having an imprecise implementation of this filter
            // may lead to unintended consequences for the end-user.
            if (!filterout && col.isTrigger)
            {
                //receiver.OnTriggerHit(ActorHeader.TriggerHitType.Overlapped, col); // invoke a callback to whoever is listening
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MgCollisionStruct ActorTraceFilter(ref int _tracesfound, out int _closestindex, float _bias, Collider _self, RaycastHit[] _hits, Vector3 position)
    {
        float _closestdistance = Mathf.Infinity;
        _closestindex = -1;
        MgCollisionStruct ret = default;

        for (int i = _tracesfound - 1; i >= 0; i--)
        {
            MgCollisionStruct collisionStruct = new MgCollisionStruct(_hits[i].normal, UpVec, _hits[i].collider, _hits[i].distance, _hits[i].point - position, false);
            _hits[i].distance -= _bias;
            RaycastHit _hit = _hits[i];
            Collider _col = _hit.collider;
            float _tracelen = _hit.distance;
            bool filterout = _tracelen < 0F || _col == _self;
            //bool filterout = _col == _self;
            Debug.Log("The name of the collision: " + _hit.transform.name + " with a normal of: " + _hit.normal + " and a distance of: " + _hits[i].distance);

            if (_col != _self)
            {
                // MgOnCollision(collisionStruct);
            }

            // if we aren't already filtering ourselves out, check to see if we're a collider
            if (!filterout && _hit.collider.isTrigger)
            {
                // receiver.OnTriggerHit(ActorHeader.TriggerHitType.Traced, _col);
                filterout = true;
            }

            if (filterout)
            {
                _tracesfound--;

                if (i < _tracesfound)
                    _hits[i] = _hits[_tracesfound];
            }
            else if (_tracelen < _closestdistance)
            {
                _closestdistance = _tracelen;
                _closestindex = i;
                ret = collisionStruct;
            }
        }

        return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void SetMovementDir(Vector3 dir)
    {
        if (!CanFly)
            dir.y = 0;

        m_movementDirMagnitude = dir.magnitude;
        MovementDir = dir;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void SetMovementSpeed(float speed) { this.m_speed = speed; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool GetIsGrounded() { return IsGrounded; }
    protected abstract void MgOnCollision(MgCollisionStruct collision);

}

#region MiscType

public struct MgCollisionStruct
{

    public MgCollisionStruct(Vector3 normal, Vector3 upVec, Collider collider, float distance, Vector3 point, bool pushback)
    {
        this.collider = collider;
        this.normal = normal;
        this.distance = distance;
        this.point = point;
        this.pushback = pushback;
        angle = Vector3.Angle(normal, upVec) + 0.00001F;
    }
    public Collider collider;
    public Vector3 normal;
    public float angle;
    public float distance;
    public Vector3 point;

    public bool pushback;
}

#endregion


