using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public abstract class MovementGeneric : MonoBehaviour
{
    [SerializeField]
    public bool CanFly;

    [SerializeField]
    protected float m_speed = 7;
    protected bool m_canMove = true;
    [SerializeField]
    protected float m_mass = 35;
    [SerializeField]
    protected float m_stepOffset = 0.3f;

    protected Transform m_transform;

    protected float m_previousPhysDeltaTime;

    //private new Rigidbody rigidbody;
    private CapsuleCollider m_collider;

    public Vector3 MovementDir { get; protected set; }
    protected float m_movementDirMagnitude;

    public bool JumpTrigger = false;

    protected bool m_jumpTriggerReleased;

    //parent position movement
    [SerializeField]
    protected Transform m_parentTransform;
    // whether or not the velocity was adjusted to that of the parent upon parenting
    private bool m_adjustedVelocityToParent;

    private Vector3 m_parentLastRot, m_parentRotVel, m_parentLastPos, m_parentPosVel;
    private float m_timeOfLastParentRotUpdate, m_timeOfLastParentPosUpdate;
    protected bool m_hasToAdjustVelocityWhenParenting = true;

    [HideInInspector]
    public Vector3 Velocity;

    public Vector3 UpVec { get; set; } = Vector3.up;

    [SerializeField]
    protected float m_slopeLimit = 45;

    [SerializeField]
    protected float m_upperSlopeLimit = 100;

    public bool IsGrounded { get; protected set; } = false;

    public Vector3 SlopeNormal { get; private set; }

    private readonly float SKINEPSILON = 0.002F;
    private readonly float TRACEBIAS = 0.0002F;

    //private Vector3 _previousStep;

    //private Vector3 _currentStep;

    private Vector3 m_movementUntilPhysCheck;

    private float m_previousLerpAlpha;

    //  private Vector3 accumulatedMovement;

    //because velocity isn't changed instantly (due to physchecks running independently from Update()), we need to 
    //add the unparenting velocity in the interpolation phase to avoid stuttering
    private Vector3 m_movementParentInterpolation;

    private float m_accumulatedTimefactor = 0;

    [SerializeField]
    private float m_timeBetweenPhysChecks = 0.02f;

    [SerializeField]
    private bool m_useInterpolation = true;

    private float m_timeUntilPhysChecks = 0;

    static readonly protected Vector3 Zero3 = new Vector3(0, 0, 0);

    private static readonly float MAGIC_JUMP_POWER = 1.5f;
    public const float MIN_GROUNDQUERY = .1F; // distance queried in our ground traces if we weren't grounded the previous simulated step
    public const float MAX_GROUNDQUERY = .5F; // distnace queried in our ground traces if we were grounded in the previous simulation step

    public const int MAX_GROUNDBUMPS = 2; // # of ground snaps/iterations in a SlideMove() 
    public const int MAX_PUSHBACKS = 8; // # of iterations in our Pushback() funcs
    public const int MAX_BUMPS = 6; // # of iterations in our Move() funcs
    public const int MAX_HITS = 12; // # of RaycastHit[] structs allocated to
                                    // a hit buffer.
    public const int MAX_OVERLAPS = 8; // # of Collider classes allocated to a
                                       // overlap buffer.
    public const float MIN_DISPLACEMENT = 0.00001F; // min squared length of a displacement vector required for a Move() to proceed.
    public const float FLY_CREASE_EPSILON = 0.8f; // minimum distance angle during a crease check to disregard any normals being queried.
    public const float INWARD_STEP_DISTANCE = 0.01F; // minimum displacement into a stepping plane
    public const float MIN_HOVER_DISTANCE = 0.025F;
    public const float MIN_PUSHBACK_DEPTH = 0.005F;

    private void Start()
    {

        // QualitySettings.vSyncCount = 0;
        //   Application.targetFrameRate = 30;

        //Time.timeScale = 0.1f;
        m_transform = transform;

        m_collider = GetComponent<CapsuleCollider>();
        SlopeNormal = UpVec;
        InternalStart();
    }
    protected abstract void InternalStart();

    protected virtual void BeforePhysChecks(float deltaTime) { }
    protected virtual void AfterPhysChecks(float delteTime) { }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        m_timeUntilPhysChecks -= deltaTime;
        // Debug.Log("Update called, time until physcheck is: " + m_timeUntilPhysChecks);

        if (m_timeUntilPhysChecks <= 0)
        {

            m_previousPhysDeltaTime = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks + m_timeUntilPhysChecks);
            PhysCheck(m_previousPhysDeltaTime); //actually the current deltatime   
            m_timeUntilPhysChecks += m_timeBetweenPhysChecks;

        }
        else if (m_useInterpolation)
        {
            float timeBetweenChecks = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks);
            float alpha = (timeBetweenChecks - m_timeUntilPhysChecks) / timeBetweenChecks;
            float timefactor = alpha - m_previousLerpAlpha;
            m_previousLerpAlpha = alpha;
            m_accumulatedTimefactor += timefactor;

            m_transform.position += m_movementUntilPhysCheck * timefactor;
        }
    }

    private void LateUpdate()
    {

        //Calculate the movement according to the parent's movement
        if (m_parentTransform != null)
        {
            float deltaTime = Time.deltaTime;
            float currentTime = Time.time;
            float invMaxTimeSinceLastFrame = 1.0f / System.MathF.Max(Time.fixedDeltaTime, deltaTime);

            Vector3 frameParentPosMov = Zero3; //raw movement since last position change
            Vector3 currentPos = m_parentTransform.position;

            if (m_parentLastPos != currentPos)
            {

                float invTimeSinceLastFrame = 1.0f / System.MathF.Max(deltaTime, currentTime - m_timeOfLastParentPosUpdate);

                frameParentPosMov.x = currentPos.x - m_parentLastPos.x;
                frameParentPosMov.y = currentPos.y - m_parentLastPos.y;
                frameParentPosMov.z = currentPos.z - m_parentLastPos.z;

                if (invTimeSinceLastFrame >= invMaxTimeSinceLastFrame)
                {
                    m_parentPosVel.x = frameParentPosMov.x * invTimeSinceLastFrame;
                    m_parentPosVel.y = frameParentPosMov.y * invTimeSinceLastFrame;
                    m_parentPosVel.z = frameParentPosMov.z * invTimeSinceLastFrame;
                }
                else
                    m_parentPosVel = Zero3;


                m_timeOfLastParentPosUpdate = currentTime;
                m_parentLastPos = currentPos;
            }

            //Calculate the rotation according to the parent's rotation
            Vector3 frameParentRotMov = Zero3; //raw movement since last position change
            Vector3 currentRot = m_parentTransform.rotation.eulerAngles;
            if (m_parentLastRot != currentRot)
            {
                Vector3 parentRotation = (currentRot - m_parentLastRot);
                float invTimeSinceLastFrame = 1.0f / System.MathF.Max(deltaTime, currentTime - m_timeOfLastParentRotUpdate);

                Vector3 dif = m_transform.position - m_parentTransform.position;

                if (m_parentLastRot.x != currentRot.x)
                    frameParentRotMov += Quaternion.AngleAxis(parentRotation.x, Vector3.right) * dif;

                if (m_parentLastRot.y != currentRot.y)
                    frameParentRotMov += Quaternion.AngleAxis(parentRotation.y, Vector3.up) * dif;

                if (m_parentLastRot.z != currentRot.z)
                    frameParentRotMov += Quaternion.AngleAxis(parentRotation.z, Vector3.forward) * dif;

                frameParentRotMov = (m_parentTransform.position + frameParentRotMov) - m_transform.position;

                if (MovementDir == Zero3)
                    m_transform.Rotate(Vector3.up * parentRotation.y);

                if (invTimeSinceLastFrame >= invMaxTimeSinceLastFrame)
                {
                    m_parentRotVel.x = frameParentRotMov.x * invTimeSinceLastFrame;
                    m_parentRotVel.y = frameParentRotMov.y * invTimeSinceLastFrame;
                    m_parentRotVel.z = frameParentRotMov.z * invTimeSinceLastFrame;
                }
                else
                    m_parentRotVel = Zero3;

                m_timeOfLastParentRotUpdate = currentTime;
                m_parentLastRot = currentRot;
            }

            //adjust the player's velocity to the parent's
            if (!m_adjustedVelocityToParent)
            {
                Vector3 parentVelocity = m_parentRotVel + m_parentPosVel;
                m_adjustedVelocityToParent = true;

                AdjustVector(ref Velocity, parentVelocity);
                GfTools.Mult3(ref parentVelocity, m_previousPhysDeltaTime);
                AdjustVector(ref m_movementUntilPhysCheck, parentVelocity); //adjust interpolation 
            }

            GfTools.Add3(ref frameParentRotMov, frameParentPosMov);
            m_transform.position += frameParentRotMov;
        }
    }

    private void PhysCheck(float deltaTime)
    {
        //  Debug.Log("deltaTime: " + deltaTime + " real deltatime is: " + Time.deltaTime);
        BeforePhysChecks(deltaTime);

        m_previousLerpAlpha = 0;

        /*
            Steps:

            1. Discrete Overlap Resolution
            2. Continuous Collision Prevention    
        */

        /* actor transform values */

        Vector3 movementCorrection = m_movementUntilPhysCheck * (1.0f - m_accumulatedTimefactor);
        //  Debug.Log("accumulated time factor was + " + accumulatedTimefactor);

        Vector3 position = m_transform.position + movementCorrection;
        m_transform.position = position;

        Vector3 immediateMovement = Zero3;
        Vector3 velocity = Velocity;
        Quaternion orientation = m_transform.rotation;
        m_accumulatedTimefactor = 0;
        IsGrounded = false;

        /* archetype buffers & references */
        Vector3 lastNormal = Zero3;

        QueryTriggerInteraction querytype = QueryTriggerInteraction.Ignore;
        Collider self = m_collider;

        Collider[] colliderbuffer = GfPhysics.GetCollidersArray();
        int layermask = GfPhysics.GetLayerMask(gameObject.layer);
        RaycastHit[] tracesbuffer = GfPhysics.GetRaycastHits();

        int numbumps = 0;
        int numpushbacks = 0;
        int geometryclips = 0;
        int numoverlaps = -1;
        MgCollisionStruct collision;

        /* attempt an overlap pushback at this current position */
        while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)
        {
            Overlap(position, orientation, layermask, 0, querytype, colliderbuffer, out numoverlaps);

            /* filter ourselves out of the collider buffer */
            ActorOverlapFilter(ref numoverlaps, self, colliderbuffer);

            //  Debug.Log("Num of overlaps is " + numoverlaps + " num of colliders size is: " + colliderbuffer.Length);
            if (numoverlaps > 0)
            {
                /* pushback against the first valid penetration found in our collider buffer */
                for (int ci = 0; ci < numoverlaps; ci++)
                {
                    Collider otherc = colliderbuffer[ci];
                    Transform othert = otherc.transform;

                    if (Physics.ComputePenetration(self, position, orientation, otherc,
                        othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                    {
                        Debug.Log("Found collision with object");
                        /* resolve pushback using closest exit distance */
                        collision = new MgCollisionStruct(normal, UpVec, otherc, mindistance, position - normal * mindistance);
                        MgOnCollision(collision);

                        Vector3 collisionPush = normal * (mindistance + MIN_PUSHBACK_DEPTH);
                        position += collisionPush;
                        immediateMovement += collisionPush;

                        /* only consider normals that we are technically penetrating into */
                        if (Vector3.Dot(velocity, normal) < 0F)
                            PM_FlyDetermineImmediateGeometry(ref velocity, ref lastNormal, collision, ref geometryclips, ref position);

                        break;
                    }
                }
            }
        }

        /* tracing values */
        float timefactor = 1F;
        float skin = SKINEPSILON;

        int _tracecount = 0;

        // We must assume that our position is valid.
        // actor.SetPosition(position);

        while (numbumps++ <= MAX_BUMPS && timefactor > 0)
        {
            // Begin Trace
            Vector3 _trace = velocity * deltaTime * timefactor;
            float _tracelen = _trace.magnitude;

            // IF unable to trace any further, break and end
            if (_tracelen <= MIN_DISPLACEMENT)
            {
                break;
            }
            else
            {
                Trace(position, _trace / _tracelen, _tracelen + skin, /* prevent tunneling by using this skin length */
                      orientation, layermask, _interacttype: querytype,
                      tracesbuffer, out _tracecount);
                //
                ActorTraceFilter(ref _tracecount, out int _i0, TRACEBIAS, self, tracesbuffer);

                if (_i0 <= -1) /* Nothing was discovered in our trace */
                {
                    position += _trace;
                    break;
                }
                else /* Discovered an obstruction along our linear path */
                {
                    RaycastHit _closest = tracesbuffer[_i0];
                    float _dist = Mathf.Max(_closest.distance - skin, 0F);

                    timefactor -= _dist / _tracelen;
                    position += (_trace / _tracelen) * _dist; /* Move back! position += trace direction * distance from hit */

                    collision = new MgCollisionStruct(_closest.normal, UpVec, _closest.collider, _closest.distance, _closest.point - position);

                    MgOnCollision(collision);

                    /* determine our topology state */
                    PM_FlyDetermineImmediateGeometry(ref velocity, ref lastNormal, collision, ref geometryclips, ref position);
                }
            }
        }

        //int safetycount = 0;
        /* Safety check to prevent multiple actors phasing through each other... Feel free to disable this for performance if you'd like*/
        Overlap(position, orientation, layermask, 0F, _interacttype: querytype, colliderbuffer, out int safetycount);

        /* filter ourselves out of the collider buffer, no need to check for triggers */
        FilterSelf(ref safetycount, self, colliderbuffer);

        //don't move object if collision was found
        if (safetycount != 0)
            position = m_transform.position;

        // m_transform.position = position;
        m_transform.position += immediateMovement;
        m_movementUntilPhysCheck = position - m_transform.position;

        // Debug.Log("immediate movement is: " + immediateMovement + " and the interpoltion movement is: " + movementUntilFixedUpdate);
        //transform.position = position;

        if (!IsGrounded) SlopeNormal = UpVec;



        Velocity = velocity;
        AfterPhysChecks(deltaTime);
    }

    public void SetParentTransform(Transform parent)
    {
        if (parent != m_parentTransform)
        {
            //  Debug.Log("set parent " + parent.name);
            m_adjustedVelocityToParent = false;
            m_timeOfLastParentPosUpdate = m_timeOfLastParentRotUpdate = Time.time;

            m_parentRotVel = m_parentPosVel = Zero3;
            m_parentLastPos = parent.position;
            m_parentLastRot = parent.rotation.eulerAngles;
            m_parentTransform = parent;

        }
    }

    public void DetachFromParent(bool addVelocity = true)
    {
        //  addVelocity = false;
        if (m_parentTransform != null)
        {
            Debug.Log("Deparented from parent, addVelocity is: " + addVelocity);

            if (addVelocity)
            {
                Vector3 parentVelocity = Zero3; //m_parentPosMov + m_parentRotMov

                float maxTimeSinceLastFrame = System.MathF.Max(Time.fixedDeltaTime, Time.deltaTime);
                if (Time.time - m_timeOfLastParentPosUpdate <= maxTimeSinceLastFrame) GfTools.Add3(ref parentVelocity, m_parentPosVel);
                if (Time.time - m_timeOfLastParentRotUpdate <= maxTimeSinceLastFrame) GfTools.Add3(ref parentVelocity, m_parentRotVel);

                //remove velocity that is going down
                float verticalFallSpeed = System.MathF.Max(0, -Vector3.Dot(UpVec, parentVelocity));

                parentVelocity.x += UpVec.x * verticalFallSpeed;
                parentVelocity.y += UpVec.y * verticalFallSpeed;
                parentVelocity.z += UpVec.z * verticalFallSpeed;

                GfTools.Add3(ref m_movementUntilPhysCheck, parentVelocity * m_previousPhysDeltaTime);
                GfTools.Add3(ref Velocity, parentVelocity);
            }

            m_parentTransform = null;
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
    private static void ProjectVector(ref Vector3 _v, Vector3 _n)
    {
        float _d = Vector3.Dot(_v, _n);
        _v.x = _n.x * _d;
        _v.y = _n.y * _d;
        _v.z = _n.z * _d;
    }

    private void PM_FlyClipVelocity(ref Vector3 velocity, MgCollisionStruct collision)
    {
        float len = velocity.magnitude;
        if (len <= 0F) // preventing NaN generation
            return;
        else if (Vector3.Dot(velocity / len, collision.normal) < 0F && len > 0.005f)
        {
            // only clip if we're piercing into the infinite plane 
            //ClipVector(ref velocity, plane);



            float dotSlopeVel = Vector3.Dot(SlopeNormal, velocity.normalized);

            if (collision.angle > m_upperSlopeLimit)
            {
                velocity -= SlopeNormal * (len * System.MathF.Max(0, dotSlopeVel));
            }

            if (IsGrounded)
            {
                velocity -= SlopeNormal * (len * dotSlopeVel);
                SlopeNormal = collision.normal;
            }

            Vector3 velocityChange = velocity.normalized.magnitude * (-Vector3.Dot(velocity, collision.normal)) * collision.normal;
            velocity += velocityChange;


        }
    }

    protected abstract void MgOnCollision(MgCollisionStruct collision);

    private static float GetStepHeight(ref RaycastHit hit, ref CapsuleCollider collider, Vector3 UpVec, ref Vector3 position)
    {
        Vector3 bottomPos = -(collider.height * 0.5f) * UpVec;
        Vector3 localStepHitPos = (hit.point - position);

        return Vector3.Dot(UpVec, localStepHitPos) - Vector3.Dot(UpVec, bottomPos);
    }

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
                    PM_FlyClipVelocity(ref velocity, collision);
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
                        ProjectVector(ref velocity, crease);
                        geometryclips |= (1 << 1);
                    }
                    else
                    {
                        // Debug.Log("DID NOT CREASE ");
                        PM_FlyClipVelocity(ref velocity, collision);
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

    public void Overlap(Vector3 _pos, Quaternion _orient, int _filter, float _inflate, QueryTriggerInteraction _interacttype, Collider[] _colliders, out int _overlapcount)
    {
        Vector3 offset = (_inflate + m_collider.height * 0.5f - m_collider.radius) * UpVec;
        Vector3 _p0 = _pos - offset;
        Vector3 _p1 = _pos + offset;



        _overlapcount = Physics.OverlapCapsuleNonAlloc(_p0, _p1, m_collider.radius + _inflate, _colliders, _filter, _interacttype);

        //Debug.Log("I DID AN OVERLP CHECK AAAAAAAAAAAAAAAAAAAAAA and i found numObjects: " + _overlapcount);
    }

    // Simply a copy of ArchetypeHeader.TraceFilters.FindClosestFilterInvalids() with added trigger functionality
    public static void ActorTraceFilter(ref int _tracesfound, out int _closestindex, float _bias, Collider _self, RaycastHit[] _hits)
    {
        int nb_found = _tracesfound;
        float _closestdistance = Mathf.Infinity;
        _closestindex = -1;

        for (int i = nb_found - 1; i >= 0; i--)
        {
            _hits[i].distance -= _bias;
            RaycastHit _hit = _hits[i];
            Collider _col = _hit.collider;
            float _tracelen = _hit.distance;
            bool filterout = false;

            // if our dist is less than zero OR our collider is ourselves
            if (_tracelen <= 0F || _col == _self)
                filterout = true;

            // if we aren't already filtering ourselves out, check to see if we're a collider
            if (!filterout && _hit.collider.isTrigger)
            {
                // receiver.OnTriggerHit(ActorHeader.TriggerHitType.Traced, _col);
                filterout = true;
            }

            if (filterout)
            {
                nb_found--;

                if (i < nb_found)
                    _hits[i] = _hits[nb_found];
            }
            else
            {
                if (_tracelen < _closestdistance)
                {
                    _closestdistance = _tracelen;
                    _closestindex = i;
                }

                continue;
            }
        }

        _tracesfound = nb_found;
    }

    public void Trace(Vector3 _pos, Vector3 _direction, float _len, Quaternion _orient, LayerMask _filter, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, out int _tracecount)
    {
        // _pos += _orient * collider.center;
        //_pos -= _direction * TRACEBIAS[ARCHETYPE_CAPSULE];
        _pos -= _direction * TRACEBIAS;
        //Debug.Log("DIRECTION IS " + _direction + " TRACE LEN IS " + _len);

        Vector3 offset = (m_collider.height * 0.5f - m_collider.radius) * UpVec;
        Vector3 _p0 = _pos - offset;
        Vector3 _p1 = _pos + offset;

        _tracecount = Physics.CapsuleCastNonAlloc(_p0, _p1, m_collider.radius, _direction,
             _hits, _len + TRACEBIAS, _filter, _interacttype);
    }

    // Simply a copy of ArchetypeHeader.OverlapFilters.FilterSelf() with trigger checking
    public static void ActorOverlapFilter(ref int _overlapsfound, Collider _self, Collider[] _colliders)
    {
        int nb_found = _overlapsfound;
        for (int i = nb_found - 1; i >= 0; i--)
        {
            bool filterout = false;
            Collider col = _colliders[i];
            if (col == _self) // if we are the actor's collider
                filterout = true;

            // we only want to filter out triggers that aren't the actor. Having an imprecise implementation of this filter
            // may lead to unintended consequences for the end-user.
            if (!filterout && col.isTrigger)
            {
                //receiver.OnTriggerHit(ActorHeader.TriggerHitType.Overlapped, col); // invoke a callback to whoever is listening
                filterout = true;
            }

            if (filterout)
            {
                nb_found--;

                if (i < nb_found)
                    _colliders[i] = _colliders[nb_found];
            }
        }

        _overlapsfound = nb_found;
    }

    public static void FindClosestFilterInvalids(ref int _tracesfound, out int _closestindex, float _bias, Collider _self, RaycastHit[] _hits)
    {
        int nb_found = _tracesfound;
        float _closestdistance = Mathf.Infinity;
        _closestindex = -1;

        for (int i = nb_found - 1; i >= 0; i--)
        {
            _hits[i].distance -= _bias;
            RaycastHit _hit = _hits[i];
            float _tracelen = _hit.distance;

            if (_tracelen > 0F &&
                !_hit.collider.Equals(_self))
            {
                if (_tracelen < _closestdistance)
                {
                    _closestdistance = _tracelen;
                    _closestindex = i;
                }
            }
            else
            {
                nb_found--;

                if (i < nb_found)
                    _hits[i] = _hits[nb_found];
            }
        }
    }

    public static void FilterSelf(ref int _overlapsfound, Collider _self, Collider[] _colliders)
    {
        int nb_found = _overlapsfound;
        for (int i = nb_found - 1; i >= 0; i--)
        {
            if (_colliders[i] == _self)
            {
                nb_found--;

                if (i < nb_found)
                    _colliders[i] = _colliders[nb_found];
            }
            else
                continue;
        }

        _overlapsfound = nb_found;
    }

    public static void FindClosestFilterInvalidsList(ref int _tracesfound, out int _closestindex, float _bias, List<Collider> _invalids, RaycastHit[] _hits)
    {
        int nb_found = _tracesfound;
        float _closestdistance = Mathf.Infinity;
        _closestindex = -1;

        for (int i = nb_found - 1; i >= 0; i--)
        {
            _hits[i].distance -= _bias;
            RaycastHit _hit = _hits[i];
            float _tracelen = _hit.distance;

            if (_tracelen > 0F && !_invalids.Contains(_hit.collider))
            {
                if (_tracelen < _closestdistance)
                {
                    _closestdistance = _tracelen;
                    _closestindex = i;
                }
            }
            else
            {
                nb_found--;

                if (i < nb_found)
                    _hits[i] = _hits[nb_found];
            }
        }
    }

    public virtual void SetMovementDir(Vector3 dir)
    {
        if (!CanFly)
            dir.y = 0;

        m_movementDirMagnitude = dir.magnitude;
        MovementDir = dir;
    }

    public virtual void SetMovementSpeed(float speed)
    {
        this.m_speed = speed;
    }
}

public struct MgCollisionStruct
{

    public MgCollisionStruct(Vector3 normal, Vector3 upVec, Collider collider, float distance, Vector3 point)
    {
        this.collider = collider;
        this.normal = normal;
        this.distance = distance;
        this.point = point;
        angle = Vector3.Angle(normal, upVec) + 0.00001F;
    }

    public Collider collider;
    public Vector3 normal;
    public float angle;
    public float distance;
    public Vector3 point;
}
