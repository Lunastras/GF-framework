using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;


/*
This class is used to simulate collision on the URP decal projetor. It overrides any value set on the projector's pivot poin. 
The world position of the object will always be the top of the projector, as in the place where the projector starts
*/
public class ShadowProjectorManipulator : MonoBehaviour
{
    [SerializeField]
    private DecalProjector m_projector = null;

    public GfMovementGeneric m_parentMovement = null;

    public Collider m_selfCollider = null;

    public float m_intervalObjCheck = 0.5f;

    public float m_intervalDistanceCheck = 0.1f;

    public float m_updateVariance = 0.1f;

    public float m_maxProjectionDistance = 20.0f;

    public float m_extraDepth = 1.0f;

    public Vector2 m_opacityStartEnd = new Vector2(1, 0);

    public Vector2 m_sizeStart = new Vector2(2, 2);

    public Vector2 m_sizeEnd = new Vector2(0, 0);

    public float m_smoothTimeOpacity = 0.1f;

    public float m_smoothTimeSize = 0.1f;

    public bool m_overrideZaxisRotation = true;

    private Vector2 m_sizeDesired;

    private float m_timeUntiObjCheck = 0;
    private float m_timeUntilDistanceCheck = 0;

    private Collider m_lastCollider;
    private float m_lastCollisionDistance;

    private Transform m_transform;

    private float m_desiredOpacity;

    private float m_smoothRefOpacity;

    private Vector2 m_smoothRefSize;

    private float m_projectionDepthCurrent;

    [SerializeField]
    private Vector3 m_upDir = new Vector3(0, 1, 0);

    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
        if (null == m_projector) m_projector = GetComponent<DecalProjector>();
        if (null == m_selfCollider) m_selfCollider = GetComponent<Collider>();

        Vector3 projSize = m_projector.size;
        projSize.z = m_maxProjectionDistance;
        m_projector.size = projSize;

        Vector3 pivot = m_projector.pivot;
        pivot.z = 0.5f * m_maxProjectionDistance;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (m_overrideZaxisRotation && m_parentMovement)
        {
            m_transform.rotation = (m_parentMovement.GetCurrentRotation() * Quaternion.AngleAxis(90, Vector3.right));
        }

        float deltaTime = Time.deltaTime;
        m_timeUntiObjCheck -= deltaTime;
        m_timeUntilDistanceCheck -= deltaTime;

        if (0 >= m_timeUntiObjCheck)
        {
            m_timeUntiObjCheck = m_intervalObjCheck * Random.Range(1.0f - m_updateVariance, 1.0f + m_updateVariance);
            m_timeUntilDistanceCheck = m_intervalDistanceCheck * Random.Range(1.0f - m_updateVariance, 1.0f + m_updateVariance);

            Vector3 topPos = m_transform.position, forward = m_transform.forward;
            Ray ray = new Ray(topPos, forward);

            RaycastHit[] hits = GfPhysics.GetRaycastHits();
            int traceCount = Physics.RaycastNonAlloc(ray, hits, m_maxProjectionDistance, GfPhysics.NonCharacterCollisions(), QueryTriggerInteraction.Ignore);
            ActorTraceFilter(ref traceCount, out int closestIndex, hits);

            RaycastHit hitInfo = default;
            bool hitSomething = closestIndex > -1;
            if (hitSomething) hitInfo = hits[closestIndex];

            UpdateValues(hitSomething, hitInfo, m_lastCollisionDistance);
        }

        if (m_lastCollider && 0 >= m_timeUntilDistanceCheck)
        {
            m_timeUntilDistanceCheck = m_intervalDistanceCheck * Random.Range(1.0f - m_updateVariance, 1.0f + m_updateVariance);

            Vector3 topPos = m_transform.position, forward = m_transform.forward;
            Ray ray = new Ray(topPos, forward);

            bool hitSomething = m_lastCollider.Raycast(ray, out RaycastHit hitInfo, m_maxProjectionDistance);
            UpdateValues(hitSomething, hitInfo, m_lastCollisionDistance);
        }


        m_projector.fadeFactor = Mathf.SmoothDamp(m_projector.fadeFactor, m_desiredOpacity, ref m_smoothRefOpacity, m_smoothTimeOpacity);

        Vector3 projSize = m_projector.size;
        Vector2 widthHeight = new Vector2(projSize.x, projSize.y);
        widthHeight = Vector2.SmoothDamp(widthHeight, m_sizeDesired, ref m_smoothRefSize, m_smoothTimeSize);

        projSize.x = widthHeight.x;
        projSize.y = widthHeight.y;
        projSize.z = m_projectionDepthCurrent;
        m_projector.size = projSize;

    }

    private void UpdateValues(bool hitSomething, RaycastHit hitInfo, float projectionDepth)
    {
        if (hitSomething)
        {
            m_lastCollider = hitInfo.collider;
            m_lastCollisionDistance = hitInfo.distance;
            float distanceCoef = m_lastCollisionDistance / m_maxProjectionDistance;
            float oneMinusDistanceCoef = (1.0f - distanceCoef);

            m_desiredOpacity = m_opacityStartEnd.x * oneMinusDistanceCoef + m_opacityStartEnd.y * distanceCoef;
            m_sizeDesired.x = m_sizeStart.x * oneMinusDistanceCoef + m_sizeEnd.x * distanceCoef;
            m_sizeDesired.y = m_sizeStart.y * oneMinusDistanceCoef + m_sizeEnd.y * distanceCoef;
        }
        else //didn't hit anything
        {
            m_lastCollider = null;
            m_lastCollisionDistance = m_maxProjectionDistance;
            m_desiredOpacity = m_opacityStartEnd.y;
            m_sizeDesired = m_sizeEnd;
        }

        m_projectionDepthCurrent = m_lastCollisionDistance + m_extraDepth;
        Vector3 pivot = m_projector.pivot;
        pivot.z = 0.5f * m_projectionDepthCurrent;
        m_projector.pivot = pivot;
    }

    private void ActorTraceFilter(ref int _tracesfound, out int _closestindex, RaycastHit[] _hits)
    {
        float _closestdistance = Mathf.Infinity;
        _closestindex = -1;

        for (int i = _tracesfound - 1; i >= 0; i--)
        {
            RaycastHit _hit = _hits[i];
            Collider _col = _hit.collider;
            float _tracelen = _hit.distance;

            if (_col == m_selfCollider) //filterOut
            {
                --_tracesfound;

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
}
