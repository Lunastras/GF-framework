using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

public abstract class MovementGeneric : MonoBehaviour
{
    [SerializeField]
    protected float speed = 7;
    [SerializeField]
    protected bool canFly;

    [SerializeField]
    protected bool canMove = true;
    [SerializeField]
    protected float mass = 35;
    protected Vector3 velocity;
    public Vector3 movementDir { get; protected set; }
    protected float movementDirMagnitude;

    public bool isGrounded { get; protected set; }

    public bool jumpTrigger { get; set; } = false;

    protected bool jumpTriggerReleased = true;

    [SerializeField]
    private new CapsuleCollider collider;

    //parent position movement
    protected Transform parentTransform;
    private bool hasToDetachFromParent = false;
    // whether or not the velocity was adjusted to that of the parent upon parenting
    private bool adjustedVelocityToParent;

    private Vector3 parentLastRot, parentRotMov, parentLastPos, parentPosMov;
    private float timeOfLastParentRotUpdate, timeOfLastParentPosUpdate;
    protected bool hasToAdjustVelocityWhenParenting = true;

    public abstract void CalculateMovement(float speedMultiplier = 1.0f);

    public virtual void SetMovementDir(Vector3 dir)
    {
        if (!canFly)
            dir.y = 0;

        movementDirMagnitude = dir.magnitude;
        movementDir = dir;
    }

    public virtual void SetCanFly(bool canFly)
    {
        this.canFly = canFly;
    }

    public virtual bool CanFly()
    {
        return canFly;
    }

    public virtual void SetMovementSpeed(float speed)
    {
        this.speed = speed;
    }

    public float SlopeLimit()
    {
        return slopeLimit;
    }

    public float Height()
    {
        return collider.height;
    }

    public float Radius()
    {
        return collider.radius;
    }
    public Vector3 GetVelocity()
    {
        return velocity;
    }

    public void SetVelocity(Vector3 velocity)
    {
        this.velocity = velocity;
    }


    public void SetParentTransform(Transform parent)
    {
        if (parent != parentTransform)
        {
            //  Debug.Log("set parent " + parent.name);
            adjustedVelocityToParent = false;
            timeOfLastParentPosUpdate = timeOfLastParentRotUpdate = Time.time;

            parentRotMov = parentPosMov = Vector3.zero;
            parentLastPos = parent.position;
            hasToDetachFromParent = false;
            parentLastRot = parent.rotation.eulerAngles;
            parentTransform = parent;
        }
    }

    private void PrivateDetachFromParent()
    {
        if (parentTransform != null)
        {
            Vector3 parentMovement = parentPosMov + parentRotMov;
            parentMovement.y = Mathf.Max(0, parentMovement.y);
            velocity += parentMovement;
            // Debug.Log(parentMovement + " added to velocity at time frame ");
            parentTransform = null;
        }

        hasToDetachFromParent = false;
    }

    public void DetachFromParent()
    {
        hasToDetachFromParent = true;
    }

    protected CollisionFlags Move(Vector3 movement)
    {
        // if (controller.enabled)
        // return controller.Move(movement);

        // return default;

        return default;
    }


    protected void CalculateParentMovement()
    {
        if (hasToDetachFromParent && !isGrounded)
        {
            PrivateDetachFromParent();
        }

        //Calculate the movement according to the parent's movement
        if (parentTransform == null) return;

        Vector3 frameParentMovement = Vector3.zero;

        Vector3 frameParentPosMov = Vector3.zero; //raw movement since last position change
        Vector3 currentPos = parentTransform.position;
        float currentTime = Time.time;

        if (parentLastPos != currentPos)
        {
            parentPosMov = currentPos - parentLastPos;

            frameParentPosMov = parentPosMov;

            float timeSinceLastFrame = currentTime - timeOfLastParentPosUpdate;

            if (timeSinceLastFrame < Time.deltaTime * 2.5f)
            {
                parentPosMov = (timeSinceLastFrame >= Time.deltaTime * 0.8f) ? (parentPosMov /= timeSinceLastFrame) : Vector3.zero;
            }

            timeOfLastParentPosUpdate = currentTime;
            parentLastPos = currentPos;
        }

        //Calculate the rotation according to the parent's rotation

        Vector3 frameParentRotMov = Vector3.zero; //raw movement since last position change
        Vector3 currentRot = parentTransform.rotation.eulerAngles;
        if (parentLastRot != currentRot)
        {
            Vector3 parentRotation = (currentRot - parentLastRot);
            float timeSinceLastFrame = currentTime - timeOfLastParentRotUpdate;

            if (timeSinceLastFrame < Time.deltaTime * 2.5f)
            {
                Vector3 rotMovement = Vector3.zero;
                Vector3 dif = transform.position - parentTransform.position;

                if (parentLastRot.x != currentRot.x)
                {
                    Quaternion xRot = Quaternion.AngleAxis(parentRotation.x, Vector3.right);
                    rotMovement += xRot * dif;
                }
                if (parentLastRot.y != currentRot.y)
                {
                    Quaternion yRot = Quaternion.AngleAxis(parentRotation.y, Vector3.up);
                    rotMovement += yRot * dif;
                }
                if (parentLastRot.z != currentRot.z)
                {
                    Quaternion zRot = Quaternion.AngleAxis(parentRotation.z, Vector3.forward);
                    rotMovement += zRot * dif;
                }

                parentRotMov = (parentTransform.position + rotMovement) - transform.position;

                // Debug.DrawRay(transform.position, parentRotMov * 20f, Color.green, 0.1f);

                if (movementDir == Vector3.zero)
                {
                    transform.Rotate(Vector3.up * parentRotation.y);
                }

                frameParentRotMov = parentRotMov;
                parentRotMov = (timeSinceLastFrame >= Time.deltaTime * 0.8f) ? (parentRotMov /= timeSinceLastFrame) : Vector3.zero;
            }

            timeOfLastParentRotUpdate = currentTime;
            parentLastRot = currentRot;
        }

        //adjust the player's velocity to the parent's and calculate the velocity of the parent object 
        frameParentMovement = frameParentRotMov + frameParentPosMov;
        if (frameParentMovement != Vector3.zero)
        {
            if (!adjustedVelocityToParent)
            {
                Vector3 parentVelocity = parentRotMov + parentPosMov;
                adjustedVelocityToParent = !hasToAdjustVelocityWhenParenting;

                if (parentVelocity != Vector3.zero && hasToAdjustVelocityWhenParenting)
                {
                    adjustedVelocityToParent = true;
                    Vector3 velocityNorm = velocity.normalized;
                    float velocityDot = Vector3.Dot(parentVelocity.normalized, velocityNorm);

                    if (velocityDot > 0)
                    {
                        float speedToDecrease = velocityDot * parentVelocity.magnitude;
                        velocity = (velocity.magnitude > speedToDecrease) ? (velocity - velocityNorm * speedToDecrease) : Vector3.zero;
                    }
                }
            }

            //controller.Move(frameParentMovement);
        }


    }

    public const float MIN_GROUNDQUERY = .1F; // distance queried in our ground traces if we weren't grounded the previous simulated step
    public const float MAX_GROUNDQUERY = .5F; // distnace queried in our ground traces if we were grounded in the previous simulation step

    public const int MAX_GROUNDBUMPS = 2; // # of ground snaps/iterations in a SlideMove() 
    public const int MAX_PUSHBACKS = 8; // # of iterations in our Pushback() funcs
    public const int MAX_BUMPS = 6; // # of iterations in our Move() funcs
    public const int MAX_HITS = 12; // # of RaycastHit[] structs allocated to
                                    // a hit buffer.
    public const int MAX_OVERLAPS = 8; // # of Collider classes allocated to a
                                       // overlap buffer.
    public const float MIN_DISPLACEMENT = 0.001F; // min squared length of a displacement vector required for a Move() to proceed.
    public const float FLY_CREASE_EPSILON = 0.8f; // minimum distance angle during a crease check to disregard any normals being queried.
    public const float INWARD_STEP_DISTANCE = 0.01F; // minimum displacement into a stepping plane
    public const float MIN_HOVER_DISTANCE = 0.025F;
    public const float MIN_PUSHBACK_DEPTH = 0.005F;

    public Vector3 Velocity;

    public Vector3 UpVec { get; set; } = Vector3.up;

    [SerializeField]
    private float slopeLimit = 45;

    [SerializeField]
    private new Rigidbody rigidbody;

    public bool IsGrounded { get; private set; } = false;

    public Vector3 SlopeNormal { get; private set; }

    private readonly float SKINEPSILON = 0.02F;
    private readonly float TRACEBIAS = 0.02F;

    // Start is called before the first frame update
    void Start()
    {
        //  Time.timeScale = 0.2f;
        if (null == collider)
            collider = GetComponent<CapsuleCollider>();

        if (null == rigidbody)
            rigidbody = GetComponent<Rigidbody>();
    }


    protected void PM_FlyMove()
    {
        SlopeNormal = UpVec;
        IsGrounded = false;
        /*
            Steps:

            1. Discrete Overlap Resolution
            2. Continuous Collision Prevention    
        */

        /* actor transform values */
        Vector3 position = transform.position;
        Vector3 velocity = Velocity;
        Quaternion orientation = transform.rotation;

        /* archetype buffers & references */
        Vector3 lastplane = Vector3.zero;

        QueryTriggerInteraction querytype = QueryTriggerInteraction.Ignore;
        Collider self = collider;

        Collider[] colliderbuffer = GfPhysics.GetCollidersArray();
        int layermask = GfPhysics.GetLayerMask(gameObject.layer);
        RaycastHit[] tracesbuffer = GfPhysics.GetRaycastHits();

        /* tracing values */
        float timefactor = 1F;
        float skin = SKINEPSILON;

        int numbumps = 0;
        int numpushbacks = 0;
        int geometryclips = 0;
        int numoverlaps = -1;

        /* attempt an overlap pushback at this current position */
        while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)
        {
            float inflate = 1.01f;
            Overlap(position, orientation, layermask, inflate, querytype, colliderbuffer, out numoverlaps);

            if (numoverlaps > 0)
            {
               // Debug.Log("I MIGHT HAVE HIT SOMETHING " + colliderbuffer[0].name);
            }

            /* filter ourselves out of the collider buffer */
            ActorOverlapFilter(ref numoverlaps, self, colliderbuffer);        

            //  Debug.Log("Num of overlaps is " + numoverlaps + " num of colliders size is: " + colliderbuffer.Length);
            if (numoverlaps > 0)
            {
               // Debug.Log("I HIT SOMETHING " + colliderbuffer[0].name);
                /* pushback against the first valid penetration found in our collider buffer */
                for (int ci = 0; ci < numoverlaps; ci++)
                {
                    Collider otherc = colliderbuffer[ci];
                    Transform othert = otherc.transform;

                    if (Physics.ComputePenetration(self, position, orientation, otherc,
                        othert.position, othert.rotation, out Vector3 normal, out float mindistance))
                    {
                        /* resolve pushback using closest exit distance */
                        position += normal * (mindistance + MIN_PUSHBACK_DEPTH);
                        float angle = 0.01f + Mathf.Round(Vector3.Angle(Vector3.up, normal));
                        IsGrounded |= slopeLimit > angle;

                        /* only consider normals that we are technically penetrating into */
                        if (Vector3.Dot(velocity, normal) < 0F)
                            PM_FlyDetermineImmediateGeometry(ref velocity, ref lastplane, normal, ref geometryclips);

                        break;
                    }
                }
            }
        }

        // We must assume that our position is valid.
        // actor.SetPosition(position);

        int _tracecount = 0;
        while (numbumps++ <= MAX_BUMPS && timefactor > 0)
        {
            // Begin Trace
            Vector3 _trace = velocity * Time.deltaTime;
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

                ActorTraceFilter(ref _tracecount, out int _i0, TRACEBIAS, self, tracesbuffer);

                if (_i0 <= -1) /* Nothing was discovered in our trace */
                {
                    position += _trace;
                    break;
                }
                else /* Discovered an obstruction along our linear path */
                {
                    RaycastHit _closest = tracesbuffer[_i0];
                    float _rto = _closest.distance / _tracelen;
                    timefactor -= _rto;

                    float _dis = Mathf.Max(_closest.distance - skin, 0F);
                    position += (_trace / _tracelen) * _dis; /* Move back! */

                    /* determine our topology state */
                    PM_FlyDetermineImmediateGeometry(ref velocity, ref lastplane, _closest.normal, ref geometryclips);
                }
            }
        }

        /* Safety check to prevent multiple actors phasing through each other... Feel free to disable this for performance if you'd like*/
        Overlap(position, orientation, layermask, 0F, _interacttype: querytype, colliderbuffer, out int safetycount);

        /* filter ourselves out of the collider buffer, no need to check for triggers */
        FilterSelf(ref safetycount, self, colliderbuffer);

        if (safetycount == 0)
            rigidbody.MovePosition(position);
        //transform.position = position;

        Velocity = velocity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProjectVector(ref Vector3 _v, Vector3 _n)
    {
        float _d = Vector3.Dot(_v, _n);
        _v.x = _n.x * _d;
        _v.y = _n.y * _d;
        _v.z = _n.z * _d;
    }

    private void PM_FlyClipVelocity(ref Vector3 velocity, Vector3 normal, float dotOffset)
    {
        float len = velocity.magnitude;
        if (len <= 0F) // preventing NaN generation
            return;
        else if (Vector3.Dot(velocity / len, normal) < 0F && len > 0.005f)
        {
            // only clip if we're piercing into the infinite plane 
            //ClipVector(ref velocity, plane);

            float angle = 0.01f + Mathf.Round(Vector3.Angle(Vector3.up, normal));
            IsGrounded |= slopeLimit > angle;

            if (IsGrounded)
            {
                SlopeNormal = normal;
                velocity -= SlopeNormal * (len * Vector3.Dot(SlopeNormal, velocity.normalized));
            }

            //Debug.Log("the given velocity dir was " + velocity.normalized + " with a velocity of " + velocity + " and a slope of " + SlopeNormal);
            float velNormMagn = velocity.normalized.magnitude;
            Vector3 velocityChange = velNormMagn * (-Vector3.Dot(velocity, normal) + dotOffset) * normal;
            velocity += velocityChange;

            Debug.Log("Velocity change is " + velocityChange);

        }
    }

    private void PM_FlyDetermineImmediateGeometry(ref Vector3 velocity, ref Vector3 lastplane, Vector3 plane, ref int geometryclips)
    {

        //  Debug.Log("Geometry clips num is " + geometryclips);
        switch (geometryclips)
        {
            case 0: /* the first penetration plane has been identified in the feedback loop */
                // Debug.Log("FIRST SWITCH ");
                PM_FlyClipVelocity(ref velocity, plane, 0);
                geometryclips |= 1 << 0;
                break;
            case (1 << 0): /* two planes have been discovered, which potentially result in a crease */

                float creaseEpsilon = System.MathF.Cos(Mathf.Deg2Rad * slopeLimit);
                //Debug.Log("SECOND SWITCH ");
                if (Vector3.Dot(lastplane, plane) < creaseEpsilon)
                {
                    // Debug.Log("CREASED ");
                    Vector3 crease = Vector3.Cross(lastplane, plane);
                    crease.Normalize();


                    ProjectVector(ref velocity, crease);
                    geometryclips |= (1 << 1);
                }
                else
                {
                    // Debug.Log("DID NOT CREASE ");
                    PM_FlyClipVelocity(ref velocity, plane, 0.5f);
                }

                break;
            case (1 << 0) | (1 << 1): /* three planes have been detected, our velocity must be cancelled entirely. */
                // Debug.Log("THIRD SWITCH ");
                velocity = Vector3.zero;
                geometryclips |= (1 << 2);
                break;
        }

        lastplane = plane;
    }

    public void Overlap(Vector3 _pos, Quaternion _orient, int _filter, float _inflate, QueryTriggerInteraction _interacttype, Collider[] _colliders, out int _overlapcount)
    {
        Vector3 offset = (_inflate + collider.height * 0.5f - collider.radius) * UpVec;
        Vector3 _p0 = _pos - offset;
        Vector3 _p1 = _pos + offset;

        

        _overlapcount = Physics.OverlapCapsuleNonAlloc(_p0, _p1, collider.radius + _inflate, _colliders, _filter, _interacttype);

        //Debug.Log("I DID AN OVERLP CHECK AAAAAAAAAAAAAAAAAAAAAA and i found numObjects: " + _overlapcount);
    }

    // Simply a copy of ArchetypeHeader.TraceFilters.FindClosestFilterInvalids() with added trigger functionality
    public static void ActorTraceFilter(
           ref int _tracesfound,
           out int _closestindex,
           float _bias,
           Collider _self,
           RaycastHit[] _hits)
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
        Debug.Log("DIRECTION IS " + _direction + " TRACE LEN IS " + _len);

        Vector3 offset = (collider.height * 0.5f - collider.radius) * UpVec;
        Vector3 _p0 = _pos - offset;
        Vector3 _p1 = _pos + offset;

        _tracecount = Physics.CapsuleCastNonAlloc(_p0, _p1, collider.radius, _direction,
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

    public static void FindClosestFilterInvalids(
        ref int _tracesfound,
        out int _closestindex,
        float _bias,
        Collider _self,
        RaycastHit[] _hits)
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

    public static void FilterSelf(
        ref int _overlapsfound,
        Collider _self,
        Collider[] _colliders)
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

    public static void FindClosestFilterInvalidsList(
        ref int _tracesfound,
        out int _closestindex,
        float _bias,
        List<Collider> _invalids,
        RaycastHit[] _hits)
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
}
