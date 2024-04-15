
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;

using System;
using System.Diagnostics;
using System.Threading;


public class GfgFiringWeapons : NetworkBehaviour
{
    [SerializeField]
    private Transform m_aimTransform = null;

    [SerializeField]
    private GfgStatsCharacter m_statsCharacter = null;

    [SerializeField]
    private float m_distanceUpdateInterval = 0.01f;

    [SerializeField]
    private float m_distanceOffset = 0;

    // [SerializeField]
    // private bool canFire = true;

    [SerializeField]
    private float m_maxFireDistance = 100;

    private List<WeaponGeneric> m_weapons = null;

    protected float m_timeUntilPhys = 0;

    public void SetAimTransform(Transform transform)
    {
        m_aimTransform = transform;
    }

    public Transform GetAimTransform()
    {
        return m_aimTransform;
    }

    private RaycastHit m_lastRayHit;

    private double m_timeUntilNextCheck = 0;

    protected NetworkVariable<Vector3> m_lastPoint = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<Vector3> m_lastNormal = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<ulong> m_lastCollisionNetId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    public bool IsFiring { get; private set; } = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (null == m_aimTransform) m_aimTransform = transform;

        if (m_statsCharacter == null)
            m_statsCharacter = GetComponent<GfgStatsCharacter>();

        m_lastRayHit = new RaycastHit();
        m_weapons = new(1);
    }


    public void SetWeapon(WeaponGeneric weapon, int weaponIndex)
    {
        if (m_weapons.Count > weaponIndex)
            m_weapons[weaponIndex] = weapon;
        else
            m_weapons.Add(weapon);
    }

    public void ClearWeapons()
    {
        m_weapons.Clear();
    }

    public List<WeaponGeneric> GetWeapons()
    {
        return m_weapons;
    }

    List<ParticleRayHit> particleHits = new(28);

    public void FixedUpdate()
    {
        if (IsOwner)
        {
            Vector3 fireTargetDir = m_aimTransform.forward;
            m_timeUntilNextCheck -= Time.deltaTime;

            if (m_timeUntilNextCheck <= 0)
            {
                m_timeUntilNextCheck = m_distanceUpdateInterval;

                RaycastHit[] rayHits = GfcPhysics.GetRaycastHits();
                Ray ray = new(m_aimTransform.position, fireTargetDir);
                int collisionMask = GfcPhysics.TargetableCollisions();

                int countHits = Physics.RaycastNonAlloc(ray, rayHits, m_maxFireDistance, collisionMask, QueryTriggerInteraction.Ignore);
                int closestIndex = -1;

                for (int i = 0; i < countHits; ++i)//find the closest collision that isn't itself
                {
                    if (rayHits[i].collider.gameObject != gameObject)
                    {
                        closestIndex = i;
                        break;
                    }
                }

                if (-1 != closestIndex)
                {
                    m_lastRayHit = rayHits[closestIndex];
                    m_lastRayHit.distance += m_distanceOffset;
                    var hitNetworkBehaviour = m_lastRayHit.collider.GetComponent<NetworkBehaviour>();
                    if (hitNetworkBehaviour)
                        m_lastCollisionNetId.Value = hitNetworkBehaviour.NetworkObjectId;
                    else
                        m_lastCollisionNetId.Value = 0;

                    //Debug.DrawRay(ray.origin, ray.direction * 100, Color.green, 5);
                    // Debug.DrawRay(ray.origin, m_lastRayHit.point - ray.origin, Color.cyan, 5);
                }
                else
                {
                    m_lastRayHit.distance = m_maxFireDistance;
                    m_lastCollisionNetId.Value = 0;
                }
            }

            m_lastRayHit.point = m_aimTransform.position + fireTargetDir * m_lastRayHit.distance;
            m_lastPoint.Value = m_lastRayHit.point;
            m_lastNormal.Value = m_lastRayHit.normal;
        }

        for (int i = 0; null != m_weapons && i < m_weapons.Count; ++i)
        {
            m_weapons[i].transform.rotation = Quaternion.LookRotation((m_lastRayHit.point - m_weapons[i].transform.position).normalized);
        }

        Stopwatch timePerParse = Stopwatch.StartNew();

        Ray ray1 = new(m_aimTransform.position, m_aimTransform.forward);
        int collisionMask1 = GfcPhysics.TargetableCollisions();
        //ParticleInterCollision.Raycast(ray1, collisionMask1, particleHits);

        timePerParse.Stop();

        //UnityEngine.Debug.Log("I hit " + particleHits.Count + " particles, elapsed time in ms: " + timePerParse.Elapsed.TotalMilliseconds);
    }

    // Update is called once per frame
    public void Fire(FireType fireType = FireType.MAIN)
    {
        IsFiring = true;

        FireHit hit = new FireHit
        {
            point = m_lastPoint.Value,
            normal = m_lastNormal.Value,
            collisionNetId = m_lastCollisionNetId.Value,
        };

        for (int i = 0; null != m_weapons && i < m_weapons.Count; ++i)
            m_weapons[i].Fire(hit, fireType);
    }

    public void ReleaseFire(FireType fireType = FireType.MAIN)
    {
        IsFiring = false;

        FireHit hit = new FireHit
        {
            point = m_lastPoint.Value,
            normal = m_lastNormal.Value,
            collisionNetId = m_lastCollisionNetId.Value,
        };

        for (int i = 0; null != m_weapons && i < m_weapons.Count; ++i)
        {
            if (m_weapons[i] != null && m_weapons[i].gameObject.activeSelf)
                m_weapons[i].ReleasedFire(hit, fireType);
        }
    }

    public GfgStatsCharacter GetGfgStatsCharacter()
    {
        return m_statsCharacter;
    }

    public void SetStatsCharacter(GfgStatsCharacter character)
    {
        m_statsCharacter = character;
    }
}
