using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfxSoftHover : MonoBehaviour
{
    [SerializeField] private Transform m_transform = null;
    [SerializeField] private float m_movementRadius = 30;
    [SerializeField] private float m_smoothTime = 2f;
    [SerializeField] private float m_targeChangeInterval = 1f;
    [SerializeField] private float m_smoothTimeMultiplierOnSnap = 0.1f;
    [SerializeField] private bool m_is2D = false;

    [HideInInspector] public bool m_snapToOrigin = false;

    [HideInInspector] public Vector3 m_originalLocalPosition = Vector3.zero;

    private Vector3 m_movementSmoothRef;

    private Vector3 m_currentPositionTarget = Vector3.zero;

    private float m_timeSinceLastChange = float.MaxValue;

    private Vector3 m_movementAccumulator = Vector3.zero;

    // Start is called before the first frame update
    void Awake()
    {
        if (null == m_transform) m_transform = transform;
        m_originalLocalPosition = m_transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_snapToOrigin && m_timeSinceLastChange >= m_targeChangeInterval)
        {
            if (m_is2D)
            {
                m_currentPositionTarget = Random.insideUnitCircle * m_movementRadius;
            }
            else
            {
                m_currentPositionTarget = Random.insideUnitSphere * m_movementRadius;
            }

            m_timeSinceLastChange = 0;
        }

        m_timeSinceLastChange += Time.deltaTime;

        Vector3 originalLocalPosition = m_transform.localPosition;

        if (m_snapToOrigin)
        {
            //we are using this instead of caching the original local position to not affect any other script that might be changing the local position of the object
            Vector3 originalPosition = m_transform.localPosition - m_movementAccumulator;
            m_transform.localPosition = Vector3.SmoothDamp(m_transform.localPosition, originalPosition, ref m_movementSmoothRef, m_smoothTime * m_smoothTimeMultiplierOnSnap);
        }
        else
        {
            m_transform.localPosition = Vector3.SmoothDamp(m_transform.localPosition, m_currentPositionTarget, ref m_movementSmoothRef, m_smoothTime);
        }

        originalLocalPosition.MinusSelf(m_transform.localPosition);
        m_movementAccumulator.MinusSelf(originalLocalPosition);
    }
}
