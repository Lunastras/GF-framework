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
    private DecalProjector m_projector;

    [SerializeField]
    private Collider m_selfCollider;

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
    private float m_extraDepth = 1.0f;

    [SerializeField]
    private LayerMask m_layerMask;

    [SerializeField]
    private bool m_useInterpolation = true;

    private float m_timeUntiObjCheck = 0;
    private float m_timeUntilDistanceCheck = 0;

    private Collider m_lastCollider;
    private float m_lastCollisionDistance;

    private Transform m_transform;

    private Vector3 m_topPosition;

    private float m_desiredOpacity;
    private float m_lastOpacity;

    private float m_minTimeUntilNextUpdate;

    private float m_timeSinceInterpolationStart;

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
        pivot.z = -0.5f * m_maxProjectionDistance;
    }

    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.deltaTime;
        m_timeUntiObjCheck -= deltaTime;
        m_timeUntilDistanceCheck -= deltaTime;
        m_timeSinceInterpolationStart += deltaTime;

        if (0 >= m_timeUntiObjCheck)
        {
            m_timeUntiObjCheck = m_intervalObjCheck * Random.Range(1.0f - m_updateVariance, 1.0f + m_updateVariance);
            m_timeUntilDistanceCheck = m_intervalDistanceCheck * Random.Range(1.0f - m_updateVariance, 1.0f + m_updateVariance);

            Vector3 topPos = m_transform.position, forward = m_transform.forward;
            Ray ray = new Ray(topPos, -forward);

            RaycastHit[] hits = GfPhysics.GetRaycastHits();
            int traceCount = Physics.RaycastNonAlloc(ray, hits, m_maxProjectionDistance, m_layerMask);
            ActorTraceFilter(ref traceCount, out int closestIndex, hits);

            if (closestIndex > -1)
            {
                m_lastCollider = hits[closestIndex].collider;
                m_lastCollisionDistance = hits[closestIndex].distance;
                m_desiredOpacity = m_opacityMultiplier * (1.0f - m_lastCollisionDistance / m_maxProjectionDistance);
                m_minTimeUntilNextUpdate = System.MathF.Min(m_timeUntilDistanceCheck, m_timeUntiObjCheck);
            }
            else
            {
                m_lastCollider = null;
                m_lastCollisionDistance = m_maxProjectionDistance;
                m_desiredOpacity = 0;
                m_minTimeUntilNextUpdate = m_timeUntilDistanceCheck;
            }

            UpdateValues(m_lastCollisionDistance);
        }

        if (m_lastCollider && 0 >= m_timeUntilDistanceCheck)
        {
            m_timeUntilDistanceCheck = m_intervalDistanceCheck * Random.Range(1.0f - m_updateVariance, 1.0f + m_updateVariance);

            Vector3 projSize = m_projector.size;
            float projectionDepth = projSize.z;
            Vector3 topPos = m_transform.position, forward = m_transform.forward;
            Ray ray = new Ray(topPos, -forward);

            if (m_lastCollider.Raycast(ray, out RaycastHit hitInfo, m_maxProjectionDistance))
            {
                m_lastCollisionDistance = hitInfo.distance;
                m_desiredOpacity = m_opacityMultiplier * (1.0f - m_lastCollisionDistance / m_maxProjectionDistance);
                m_minTimeUntilNextUpdate = System.MathF.Min(m_timeUntilDistanceCheck, m_timeUntiObjCheck);
            }
            else //didn't hit anything
            {
                m_lastCollider = null;
                m_lastCollisionDistance = m_maxProjectionDistance;
                m_desiredOpacity = 0;
                m_minTimeUntilNextUpdate = m_timeUntilDistanceCheck;
            }

            UpdateValues(m_lastCollisionDistance);
        }

        if (m_useInterpolation && m_minTimeUntilNextUpdate > 0)
        {
            float interpolationValue = m_timeSinceInterpolationStart / m_minTimeUntilNextUpdate;
            m_projector.fadeFactor = (1.0f - interpolationValue) * m_lastOpacity + interpolationValue * m_desiredOpacity;
        }
        else
        {
            m_projector.fadeFactor = m_desiredOpacity;
        }
    }

    private void UpdateValues(float projectionDepth)
    {
        m_lastOpacity = m_projector.fadeFactor;
        projectionDepth = m_lastCollisionDistance;
        Vector3 projSize = m_projector.size;
        projSize.z = projectionDepth + m_extraDepth;
        m_projector.size = projSize;
        Vector3 pivot = m_projector.pivot;
        pivot.z = -0.5f * (projectionDepth + m_extraDepth);
        m_projector.pivot = pivot;
        m_timeSinceInterpolationStart = 0;
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
