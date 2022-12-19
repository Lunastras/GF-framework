using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class ArchetypeCollision
{
    public virtual void UpdateValues() { }

    public virtual Vector3 GetBottomPosLocal() { return Vector3.zero; }

    public virtual void Trace(Vector3 _pos, Vector3 _direction, float _len, LayerMask _filter, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, float bias, out int _tracecount) { _tracecount = 0; }

    public virtual void Overlap(Vector3 _pos, int _filter, QueryTriggerInteraction _interacttype, Collider[] _colliders, out int _overlapcount) { _overlapcount = 0; }
}

internal class ArchetypeCapsule : ArchetypeCollision
{
    private CapsuleCollider m_collider;
    private Vector3 m_topOffset;
    private Vector3 m_topOffsetOverlap;
    private float m_radius;

    private float m_radiusOverlap;

    private static readonly float OVERLAP_INFLATE = 0.01F;
    private static readonly float OVERLAP_SKIN = 0.01F;

    public ArchetypeCapsule(CapsuleCollider collider)
    {
        m_collider = collider;
    }

    public override void UpdateValues()
    {
        Vector3 scale;
        Vector3 factor3 = new Vector3(1, 1, 1);
        Transform transform = m_collider.transform;
        while (transform)
        {
            scale = transform.localScale;
            factor3.x *= scale.x;
            factor3.y *= scale.y;
            factor3.z *= scale.z;

            transform = transform.parent;
        }

        int dir = m_collider.direction;
        Vector3 topDir;
        float radiusScale;
        float heightScale;

        switch (dir)
        {
            case (0):
                heightScale = factor3.x;
                topDir = new Vector3(1, 0, 0);
                radiusScale = System.MathF.Max(factor3.z, factor3.y);
                break;
            case (2):
                heightScale = factor3.z;
                topDir = new Vector3(0, 0, 1);
                radiusScale = System.MathF.Max(factor3.x, factor3.y);
                break;
            default: //most likely 1, aka y axis
                heightScale = factor3.y;
                topDir = new Vector3(0, 1, 0);
                radiusScale = System.MathF.Max(factor3.z, factor3.x);
                break;
        }

        topDir = m_collider.transform.rotation * topDir;
        m_radius = m_collider.radius * radiusScale;

        float halfHeight = System.MathF.Max(0, (m_collider.height * heightScale * 0.5f - m_radius));
        m_topOffset = halfHeight * topDir;
        m_topOffsetOverlap = (halfHeight + OVERLAP_INFLATE) * topDir;
        m_radiusOverlap = m_radius + OVERLAP_SKIN;
    }

    public override void Trace(Vector3 _pos, Vector3 _direction, float _len, LayerMask _filter, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, float bias, out int _tracecount)
    {
        GfTools.Minus3(ref _pos, _direction * bias);

        _tracecount = Physics.CapsuleCastNonAlloc(_pos - m_topOffset, _pos + m_topOffset, m_radius, _direction,
             _hits, _len + bias, _filter, _interacttype);
    }

    public override void Overlap(Vector3 _pos, int _filter, QueryTriggerInteraction _interacttype, Collider[] _colliders, out int _overlapcount)
    {
        _overlapcount = Physics.OverlapCapsuleNonAlloc(_pos - m_topOffset, _pos + m_topOffset, m_radiusOverlap, _colliders, _filter, _interacttype);
    }
}

internal class ArchetypeSphere : ArchetypeCollision
{
    private SphereCollider m_collider;
    private float m_radius;
    private float m_radiusOverlap;
    private static readonly float OVERLAP_SKIN = 0.02F;

    public ArchetypeSphere(SphereCollider collider)
    {
        m_collider = collider;
    }

    public override void UpdateValues()
    {
        Vector3 scale;
        Vector3 factor3 = new Vector3(1, 1, 1);
        Transform transform = m_collider.transform;
        while (transform)
        {
            scale = transform.localScale;
            factor3.x *= scale.x;
            factor3.y *= scale.y;
            factor3.z *= scale.z;

            transform = transform.parent;
        }

        m_radius = m_collider.radius * System.MathF.Max(factor3.x, System.MathF.Max(factor3.y, factor3.z));
        m_radiusOverlap = m_radius + OVERLAP_SKIN;
    }

    public override void Trace(Vector3 _pos, Vector3 _direction, float _len, LayerMask _filter, QueryTriggerInteraction _interacttype, RaycastHit[] _hits, float bias, out int _tracecount)
    {
        GfTools.Minus3(ref _pos, _direction * bias);

        _tracecount = Physics.SphereCastNonAlloc(_pos, m_radius, _direction, _hits, _len + bias, _filter, _interacttype);
    }

    public override void Overlap(Vector3 _pos, int _filter, QueryTriggerInteraction _interacttype, Collider[] _colliders, out int _overlapcount)
    {
        _overlapcount = Physics.OverlapSphereNonAlloc(_pos, m_radiusOverlap, _colliders, _filter, _interacttype);
    }
}