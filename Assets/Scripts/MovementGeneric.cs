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
    [SerializeField]
    protected float m_slopeLimit = 45;
    [SerializeField]
    protected float m_upperSlopeLimit = 100;
    [SerializeField]
    private float m_timeBetweenPhysChecks = 0.02f;

    [SerializeField]
    private bool m_useInterpolation = true;

    protected Transform m_transform;

    protected float m_previousPhysDeltaTime;

    //private new Rigidbody rigidbody;
    private CapsuleCollider m_collider;

    public Vector3 MovementDir { get; protected set; }
    protected float m_movementDirMagnitude;

    public bool JumpTrigger = false;

    protected bool m_jumpTriggerReleased;
    protected Transform m_parentTransform;
    private bool m_adjustedVelocityToParent;  // whether or not the velocity was adjusted to that of the parent upon parenting

    private Vector3 m_parentLastRot, m_parentRotVel, m_parentLastPos, m_parentPosVel;
    private float m_timeOfLastParentRotUpdate, m_timeOfLastParentPosUpdate;
    
    private float m_parentDeltaTimePos, m_parentDeltaTimeRot; //delta of the last movement from the parent
    protected bool m_hasToAdjustVelocityWhenParenting = true;

    [HideInInspector]
    public Vector3 Velocity;

    public Vector3 UpVec = Vector3.up;

    protected bool IsGrounded = false;

    public Vector3 SlopeNormal { get; private set; }

    private readonly float SKINEPSILON = 0.002F;
    private readonly float TRACEBIAS = 0.0002F;

    private Vector3 m_movementUntilPhysCheck;

    private float m_previousLerpAlpha;

    //because velocity isn't changed instantly (due to physchecks running independently from Update()), we need to 
    //add the unparenting velocity in the interpolation phase to avoid stuttering
    private Vector3 m_movementParentInterpolation;

    private float m_accumulatedTimefactor = 0;

    private float m_timeUntilPhysChecks = 0;

    static readonly protected Vector3 Zero3 = new Vector3(0, 0, 0);

    public const int MAX_PUSHBACKS = 8; // # of iterations in our Pushback() funcs
    public const int MAX_BUMPS = 6; // # of iterations in our Move() funcs                      // a hit buffer.
    public const float MIN_DISPLACEMENT = 0.00001F; // min squared length of a displacement vector required for a Move() to proceed.
    public const float MIN_PUSHBACK_DEPTH = 0.005F;

    private void Start()
    {
        m_transform = transform;

        Time.timeScale = 0.1f;
        m_collider = GetComponent<CapsuleCollider>();
        SlopeNormal = UpVec;
        InternalStart();
    }
    protected abstract void InternalStart();

    protected virtual void BeforePhysChecks(float deltaTime) { }
    protected virtual void AfterPhysChecks(float delteTime) { }

    private void Update()
    {
        //Debug.Log("the current rotation is: " + m_transform.rotation);
        float deltaTime = Time.deltaTime;
        m_timeUntilPhysChecks -= deltaTime;

        if (m_timeUntilPhysChecks <= 0)
        {
            m_previousPhysDeltaTime = System.MathF.Max(deltaTime, m_timeBetweenPhysChecks + m_timeUntilPhysChecks);
            PhysCheck(m_previousPhysDeltaTime); //actually the current deltatime   
            m_timeUntilPhysChecks += m_timeBetweenPhysChecks;
        }
        else if (m_useInterpolation) //Interpolation calculations
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
       // Debug.Log("LATEUPDATE was called");
        //Calculate the movement according to the parent's movement
        if (m_parentTransform != null)
        {
            float deltaTime = Time.deltaTime;
            float currentTime = Time.time;

            Vector3 frameParentPosMov = Zero3; //raw movement since last position change
            Vector3 currentPos = m_parentTransform.position;
            if (m_parentLastPos != currentPos)
            {
                frameParentPosMov.x = currentPos.x - m_parentLastPos.x;
                frameParentPosMov.y = currentPos.y - m_parentLastPos.y;
                frameParentPosMov.z = currentPos.z - m_parentLastPos.z;

                m_parentDeltaTimeRot = currentTime - m_timeOfLastParentPosUpdate;
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

                if (MovementDir == Zero3)
                    m_transform.Rotate(Vector3.up * parentRotation.y);

                m_parentDeltaTimeRot = currentTime - m_timeOfLastParentRotUpdate;
                m_timeOfLastParentRotUpdate = currentTime;
                m_parentLastRot = currentRot;
                m_parentRotVel = frameParentRotMov;
            }

            //adjust the player's velocity to the parent's
            if (!m_adjustedVelocityToParent)
            {
                Vector3 parentVelocity = GetParentVelocity(currentTime, deltaTime);
                m_adjustedVelocityToParent = true;

                GfTools.ProjectOnPlane(ref Velocity, parentVelocity);
                GfTools.Mult3(ref parentVelocity, m_previousPhysDeltaTime);
                GfTools.ProjectOnPlane(ref m_movementUntilPhysCheck, parentVelocity); //adjust interpolation 
            }

            GfTools.Add3(ref frameParentRotMov, frameParentPosMov);
            m_transform.localPosition += frameParentRotMov;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PhysCheck(float deltaTime)
    {
        if(m_parentTransform)
            return;

        BeforePhysChecks(deltaTime);

        Debug.Log("The velocity is: " + Velocity);

        m_previousLerpAlpha = 0;
        Vector3 movementCorrection = m_movementUntilPhysCheck * (1.0f - m_accumulatedTimefactor);
        m_transform.localPosition += movementCorrection;
        Vector3 position = m_transform.position;

        Vector3 immediateMovement = Zero3;
        Vector3 velocity = Velocity;
        Quaternion orientation = m_transform.rotation;
        m_accumulatedTimefactor = 0;
        IsGrounded = false;
        MgCollisionStruct collision;
        Vector3 upDir = transform.up;
        Vector3 offset = (m_collider.height * 0.5f - m_collider.radius) * upDir; //todo

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
            Overlap(position, offset, layermask, querytype, colliderbuffer, out numoverlaps);
            ActorOverlapFilter(ref numoverlaps, self, colliderbuffer); /* filter ourselves out of the collider buffer */

            if (numoverlaps > 0)
            {
                for (int ci = 0; ci < numoverlaps; ci++)/* pushback against the first valid penetration found in our collider buffer */
                {
                    Collider otherc = colliderbuffer[ci];
                    Transform othert = otherc.transform;

                    if (Physics.ComputePenetration(self, position, orientation, otherc,
                        othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                    {
                        /* resolve pushback using closest exit distance */
                        collision = new MgCollisionStruct(normal, UpVec, otherc, mindistance, position - normal * mindistance);
                        MgOnCollision(collision);

                        Vector3 collisionPush = normal * (mindistance + MIN_PUSHBACK_DEPTH);
                        immediateMovement += collisionPush;
                        position += collisionPush;

                        /* only consider normals that we are technically penetrating into */
                        if (Vector3.Dot(velocity, normal) < 0F)
                            PM_FlyDetermineImmediateGeometry(ref velocity, ref lastNormal, collision, ref geometryclips, ref position);

                        break;
                    }
                } // for (int ci = 0; ci < numoverlaps; ci++)
            } // if (numoverlaps > 0)
        }// while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)

        /* OVERLAP SECTION END*/

        /* TRACING SECTION START*/
        float timefactor = 1F;
        float skin = SKINEPSILON;

        int _tracecount = 0;

        while (numbumps++ <= MAX_BUMPS && timefactor > 0)
        {
            Vector3 _trace = velocity * deltaTime * timefactor;
            Debug.Log("The velocity is: " + velocity);
            float _tracelen = _trace.magnitude;

            // IF unable to trace any further, break and end
            if (_tracelen > MIN_DISPLACEMENT)
            {
                Vector3 traceDir = _trace;
                GfTools.Div3(ref traceDir, _tracelen);
                Trace(position, offset, traceDir, _tracelen + skin , layermask, querytype, tracesbuffer, out _tracecount);/* prevent tunneling by using this skin length */                    
                ActorTraceFilter(ref _tracecount, out int _i0, TRACEBIAS, self, tracesbuffer);
                Debug.Log("The trace length was " + _tracelen);

                if (_i0 > -1) /* Nothing was discovered in our trace */
                {
                    RaycastHit _closest = tracesbuffer[_i0];
                    float _dist = Mathf.Max(_closest.distance - skin, 0F);

                    timefactor -= _dist / _tracelen;

                    GfTools.Mult3(ref traceDir, _dist);
                    GfTools.Add3(ref position, traceDir);

                    collision = new MgCollisionStruct(_closest.normal, UpVec, _closest.collider, _closest.distance, _closest.point - position);
                    MgOnCollision(collision);

                    /* determine our topology state */
                    PM_FlyDetermineImmediateGeometry(ref velocity, ref lastNormal, collision, ref geometryclips, ref position);
                }
                else /* Discovered an obstruction along our linear path */
                {          
                    Debug.Log("FOUND NOTHING ");                              
                    GfTools.Add3(ref position, _trace);
                    break;
                }
            } else break; //if (_tracelen > MIN_DISPLACEMENT)        
        } // while (numbumps++ <= MAX_BUMPS && timefactor > 0)

        /* TRACING SECTION END*/

        int safetycount = 0;
        if(numbumps > MAX_BUMPS) {
            Overlap(position, offset, layermask, querytype, colliderbuffer, out safetycount); /* Safety check to prevent multiple actors phasing through each other... Feel free to disable this for performance if you'd like*/
            ActorOverlapFilter(ref safetycount, self, colliderbuffer); /* filter ourselves out of the collider buffer, no need to check for triggers */

            if (safetycount != 0) //don't move object if collision was found
                position = m_transform.position;
        }

        m_transform.localPosition += immediateMovement;
        GfTools.Add3(ref position, immediateMovement);
        m_movementUntilPhysCheck = position - m_transform.position;
        if (!IsGrounded) SlopeNormal = UpVec;
        Velocity = velocity;
        AfterPhysChecks(deltaTime);
    }

    float timeOfParenting;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetParentTransform(Transform parent)
    {
        if (parent != m_parentTransform)
        {
            Debug.Log("set parent " + parent.name);
            m_adjustedVelocityToParent = false;
            m_timeOfLastParentPosUpdate = m_timeOfLastParentRotUpdate = Time.time;

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
        //  addVelocity = false;
        if (m_parentTransform != null)
        {
            Debug.Log("Deparented from parent, addVelocity is: " + addVelocity + " Time since parenting was: " + (Time.time - timeOfParenting));

            if (addVelocity)
            {
                Debug.Break();
                Vector3 parentVelocity = GetParentVelocity(Time.time, Time.deltaTime);

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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetParentVelocity(float time, float deltaTime) {
        Vector3 parentVelocity = Zero3; //m_parentPosMov + m_parentRotMov

        float maxTimeSinceLastFrame = System.MathF.Max(Time.fixedDeltaTime, deltaTime);
        if (time - m_timeOfLastParentPosUpdate <= maxTimeSinceLastFrame) {
            float invTimeSinceLastFrame = 1.0f / System.MathF.Max(deltaTime, m_parentDeltaTimePos);

            parentVelocity.x = m_parentPosVel.x * invTimeSinceLastFrame;
            parentVelocity.y = m_parentPosVel.y * invTimeSinceLastFrame;
            parentVelocity.z = m_parentPosVel.z * invTimeSinceLastFrame;
        }

        if (time - m_timeOfLastParentRotUpdate <= maxTimeSinceLastFrame) {
            float invTimeSinceLastFrame = 1.0f / System.MathF.Max(deltaTime, m_parentDeltaTimeRot);

            parentVelocity.x += m_parentRotVel.x * invTimeSinceLastFrame;
            parentVelocity.y += m_parentRotVel.y * invTimeSinceLastFrame;
            parentVelocity.z += m_parentRotVel.z * invTimeSinceLastFrame;
        }

        return parentVelocity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PM_FlyClipVelocity(ref Vector3 velocity, MgCollisionStruct collision)
    {
        if (Vector3.Dot(velocity, collision.normal) < 0F) //only do these calculations if the normal is facing away from the velocity
        {
            float dotSlopeVel = Vector3.Dot(SlopeNormal, velocity); //dot of the previous slope

            if (collision.angle > m_upperSlopeLimit)
                GfTools.Minus3(ref velocity, SlopeNormal * System.MathF.Max(0, dotSlopeVel));
            

            if (IsGrounded)
            {
                GfTools.Minus3(ref velocity, SlopeNormal * dotSlopeVel);
                SlopeNormal = collision.normal;
            }

            GfTools.Minus3(ref velocity, (Vector3.Dot(velocity, collision.normal)) * collision.normal);
        }
    }

    protected abstract void MgOnCollision(MgCollisionStruct collision);

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
                        GfTools.Project(ref velocity, crease);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Overlap(Vector3 _pos, Vector3 offset, int _filter, QueryTriggerInteraction _interacttype, Collider[] _colliders, out int _overlapcount)
    {
        _overlapcount = Physics.OverlapCapsuleNonAlloc(_pos - offset, _pos + offset, m_collider.radius, _colliders, _filter, _interacttype);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Trace(Vector3 _pos, Vector3 offset, Vector3 _direction, float _len, LayerMask _filter, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, out int _tracecount)
    {
        GfTools.Minus3(ref _pos, _direction * TRACEBIAS);

        _tracecount = Physics.CapsuleCastNonAlloc(_pos - offset, _pos + offset, m_collider.radius, _direction,
             _hits, _len + TRACEBIAS, _filter, _interacttype);
    }

    // Simply a copy of ArchetypeHeader.OverlapFilters.FilterSelf() with trigger checking
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ActorOverlapFilter(ref int _overlapsfound, Collider _self, Collider[] _colliders)
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
    public static void ActorTraceFilter(ref int _tracesfound, out int _closestindex, float _bias, Collider _self, RaycastHit[] _hits)
    {
        float _closestdistance = Mathf.Infinity;
        _closestindex = -1;

        for (int i = _tracesfound - 1; i >= 0; i--)
        {
            _hits[i].distance -= _bias;
            RaycastHit _hit = _hits[i];
            Collider _col = _hit.collider;
            float _tracelen = _hit.distance;
            bool filterout = _tracelen <= 0F || _col == _self;

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
            }
        }
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
    public virtual bool GetIsGrounded()  { return IsGrounded; }
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
