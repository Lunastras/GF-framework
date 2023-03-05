using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShadowProjectorManipulator : MonoBehaviour
{
    [SerializeField]
    private DecalProjector m_projector;

    [SerializeField]
    private Collider m_selfCollider;
    [SerializeField]
    private Vector3 m_pivot;

    [SerializeField]
    private float m_intervalObjCheck = 0.5f;

    [SerializeField]
    private float m_intervalDistanceCheck = 0.1f;

    [SerializeField]
    private float m_updateVariance = 0.1f;

    [SerializeField]
    private float m_maxProjectionDistance = 20.0f;

    [SerializeField]
    private float m_opacityMultiplier = 1.0f;

    [SerializeField]
    private LayerMask m_layerMask;

    private float m_timeUntiObjCheck = 0;
    private float m_timeUntilDistanceCheck = 0;

    private Collider m_lastCollider;
    private float m_lastCollisionDistance;

    private Transform m_transform;

    private float m_desiredOpacity;
    private float m_lastOpacity;
    
    private void GetValues(float projectionDepth, out Vector3 topPos, out Vector3 forward)
    {
        forward = m_transform.forward;
        topPos = m_transform.position;
        GfTools.Minus3(ref topPos, forward * (0.5f * projectionDepth));
        GfTools.Add3(ref topPos, m_projector.pivot);
    }

    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
        if (null == m_projector) m_projector = GetComponent<DecalProjector>();
        if (null == m_selfCollider) m_selfCollider = GetComponent<Collider>();

        Vector3 projSize = m_projector.size;
        projSize.z = m_maxProjectionDistance;
        m_projector.size = projSize;
    }

    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.deltaTime;
        m_timeUntiObjCheck -= deltaTime;
        m_timeUntilDistanceCheck -= deltaTime;

        if (0 >= m_timeUntiObjCheck)
        {
            m_timeUntiObjCheck = m_intervalObjCheck * Random.Range(1.0f - m_updateVariance,  1.0f + m_updateVariance);
            m_timeUntilDistanceCheck = m_intervalDistanceCheck * Random.Range(1.0f - m_updateVariance, 1.0f + m_updateVariance);

            Vector3 projSize = m_projector.size;
            float projectionDepth = projSize.z;
            Vector3 topPos, forward;
            GetValues(projectionDepth, out topPos, out forward);
            Ray ray = new Ray(topPos, forward);

            RaycastHit[] hits = GfPhysics.GetRaycastHits();
            int traceCount = Physics.RaycastNonAlloc(ray, hits, projectionDepth, m_layerMask);
            ActorTraceFilter(ref traceCount, out int closestIndex, hits);

            if(closestIndex > -1)
            {
                m_lastCollider = hits[closestIndex].collider;
                m_lastCollisionDistance = hits[closestIndex].distance;
                m_desiredOpacity = m_opacityMultiplier * (m_lastCollisionDistance / projectionDepth);
            } 
            else
            {
                m_lastCollider = null;
                m_lastCollisionDistance = m_maxProjectionDistance;
                m_desiredOpacity = m_opacityMultiplier;
            }

            m_lastOpacity = m_projector.fadeFactor;
            projectionDepth = m_lastCollisionDistance;
            projSize.z = projectionDepth;
            m_projector.size = projSize;
        }

        if (m_lastCollider && 0 >= m_timeUntilDistanceCheck)
        {
            m_timeUntilDistanceCheck = m_intervalDistanceCheck * Random.Range(1.0f - m_updateVariance, 1.0f + m_updateVariance);

            Vector3 projSize = m_projector.size;
            float projectionDepth = projSize.z;
            Vector3 topPos, forward;
            GetValues(projectionDepth, out topPos, out forward);
            Ray ray = new Ray(topPos, forward);

            if(m_lastCollider.Raycast(ray, out RaycastHit hitInfo, projectionDepth))
            {
                m_lastCollisionDistance = hitInfo.distance;
                m_desiredOpacity = m_opacityMultiplier * (m_lastCollisionDistance / projectionDepth);
            }
            else //didn't hit anything
            {
                m_lastCollider = null;
                m_lastCollisionDistance = m_maxProjectionDistance;
                m_desiredOpacity = m_opacityMultiplier;
            }

            m_lastOpacity = m_projector.fadeFactor;
            projectionDepth = m_lastCollisionDistance;
            projSize.z = projectionDepth;
            m_projector.size = projSize;
        }

        m_projector.fadeFactor = m_desiredOpacity;
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
