using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

public class CharacterMovement : MonoBehaviour
{
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
    
    public static readonly int ARCHETYPE_SPHERE = 0;
    public static readonly int ARCHETYPE_CAPSULE = 1;
    public static readonly int ARCHETYPE_BOX = 2;
    public static readonly int ARCHETYPE_LINE = 3;

    public Vector3 Velocity;

    private readonly Vector3 upVec = Vector3.up;

    [SerializeField]
    private new CapsuleCollider collider;

    [SerializeField]
    private float slopeLimit = 45;

    [SerializeField]
    private new Rigidbody rigidbody;

    public bool IsGrounded { get; private set; } = false;

    private Vector3 lastPosition;



    private static readonly float[] SKINEPSILON = new float[3]
    {
        0.002F, // sphere
        0.002F, // capsule
        0.02F // box
    };

    private static readonly float[] TRACEBIAS = new float[4]
    {
        0.0002F, // sphere
        0.0002F, // capsule
        0.01F, // box
        0.0F // line
    };

    public static float GET_SKINEPSILON(int _i0) => SKINEPSILON[_i0];
    public static float GET_TRACEBIAS(int _i0) => TRACEBIAS[_i0];
    // Start is called before the first frame update
    void Start()
    {
      //  Time.timeScale = 0.2f;
        lastPosition = transform.position;

        if(null == collider)
            collider = GetComponent<CapsuleCollider>();

        if(null == rigidbody)
            rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
   // void Update()
    //{
        
   // }

    void FixedUpdate() {

        PM_FlyMove();     
    }


    public void PM_FlyMove()
    {
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
        float skin = SKINEPSILON[ARCHETYPE_CAPSULE];

        int numbumps = 0;
        int numpushbacks = 0;
        int geometryclips = 0;
        int numoverlaps = -1;

        /* attempt an overlap pushback at this current position */
        while (numpushbacks++ < MAX_PUSHBACKS && numoverlaps != 0)
        {
            Overlap(position, orientation, layermask, /* inflate */ 0F, querytype, colliderbuffer, out numoverlaps);

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
                        /* resolve pushback using closest exit distance */
                        position += normal * (mindistance + MIN_PUSHBACK_DEPTH);

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
                      orientation, layermask, 0F, _interacttype: querytype,
                      tracesbuffer, out _tracecount);

                ActorTraceFilter(ref _tracecount, out int _i0, GET_TRACEBIAS(ARCHETYPE_CAPSULE), self, tracesbuffer);

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

        if(safetycount == 0)
            rigidbody.MovePosition(position);
            //transform.position = position;
        
        Velocity = velocity;

        lastPosition = position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProjectVector(ref Vector3 _v, Vector3 _n)
    {
        float _d = Vector3.Dot(_v, _n);
        _v.x = _n.x * _d;
        _v.y = _n.y * _d;
        _v.z = _n.z * _d;
    }

    private void PM_FlyClipVelocity(ref Vector3 velocity, Vector3 normal)
    {
        float len = velocity.magnitude;
        if (len <= 0F) // preventing NaN generation
            return;
        else if (Vector3.Dot(velocity / len, normal) < 0F && len > 0.005f) {
            // only clip if we're piercing into the infinite plane 
            //ClipVector(ref velocity, plane);

            float angle = 0.01f + Mathf.Round(Vector3.Angle(Vector3.up, normal));
            IsGrounded = slopeLimit > angle;
      
            if(IsGrounded) {
                velocity.y = 0;
            }

            float velNormMagn = velocity.normalized.magnitude;
            Debug.Log("The current velocity is: " + velocity.normalized.magnitude + " isGrounded is: " + IsGrounded);
      
            Vector3 velocityChange = velNormMagn * (-Vector3.Dot(velocity, normal) + 0.5f) * normal;

            Debug.Log("velocity change is " + velocityChange);

          //if(IsGrounded) {

          //    Vector3 velDir = velocity.normalized;
          //    //velocity += velocityChange ;
          //    velocity *= System.MathF.Max(0, Vector3.Dot(velDir, -normal));

          //} else {
          //    velocity += velocityChange;
          //}
            
            velocity += velocityChange;
        } 
    }

    private void PM_FlyDetermineImmediateGeometry(ref Vector3 velocity, ref Vector3 lastplane, Vector3 plane, ref int geometryclips)
    {

      //  Debug.Log("Geometry clips num is " + geometryclips);
        switch (geometryclips)
        {
            case 0: /* the first penetration plane has been identified in the feedback loop */
              // Debug.Log("FIRST SWITCH ");
                PM_FlyClipVelocity(ref velocity, plane);
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
                else {
                   // Debug.Log("DID NOT CREASE ");
                    PM_FlyClipVelocity(ref velocity, plane);
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
        Vector3 offset = (_inflate + collider.height * 0.5f - collider.radius) * upVec;
        Vector3 _p0 = _pos - offset;
        Vector3 _p1 = _pos + offset;

       // Debug.Log("The positions used for the physCheck " + _p0 + " " + _p1 + " The radius used is: " + (collider.radius + _inflate) + " the layermask used is: " + _filter);

        _overlapcount = Physics.OverlapCapsuleNonAlloc(_p0, _p1, collider.radius + _inflate, _colliders, _filter, _interacttype);
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

    public void Trace(Vector3 _pos, Vector3 _direction, float _len, Quaternion _orient, LayerMask _filter, float _inflate, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, out int _tracecount)
    {
       // _pos += _orient * collider.center;
        _pos -= _direction * TRACEBIAS[ARCHETYPE_CAPSULE];

        Vector3 offset = (_inflate + collider.height * 0.5f - collider.radius) * upVec;
        Vector3 _p0 = _pos - offset;
        Vector3 _p1 = _pos + offset;

        _tracecount = Physics.CapsuleCastNonAlloc(_p0, _p1, collider.radius + _inflate, _direction,
             _hits, _len + TRACEBIAS[ARCHETYPE_CAPSULE], _filter, _interacttype);
    }

    // Simply a copy of ArchetypeHeader.OverlapFilters.FilterSelf() with trigger checking
    public static void ActorOverlapFilter(
        ref int _overlapsfound,
            Collider _self,
            Collider[] _colliders)
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
            else
                continue;
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
