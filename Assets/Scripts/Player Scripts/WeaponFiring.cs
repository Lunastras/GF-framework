
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;


public class WeaponFiring : NetworkBehaviour
{
    [SerializeField]
    private Transform m_aimTransform = null;

    [SerializeField]
    private StatsCharacter m_statsCharacter = null;

    [SerializeField]
    private float m_distanceUpdateInterval = 0.01f;

    [SerializeField]
    private float m_distanceOffset = 0;

    // [SerializeField]
    // private bool canFire = true;

    [SerializeField]
    private float m_maxFireDistance = 100;

    private List<WeaponBasic> m_weapons = null;

    private RaycastHit m_lastRayHit;

    private double m_timeOflastCheck = 0;

    protected NetworkVariable<Vector3> m_lastPoint = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<Vector3> m_lastNormal = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<Vector3> m_lastCollisionId = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    public bool IsFiring { get; private set; } = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (null == m_aimTransform)
            m_aimTransform = Camera.main.transform;

        if (m_statsCharacter == null)
            m_statsCharacter = GetComponent<StatsCharacter>();

        m_lastRayHit = new RaycastHit();
        m_weapons = new(1);
    }


    public void SetWeapon(WeaponBasic weapon, int weaponIndex)
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

    public List<WeaponBasic> GetWeapons()
    {
        return m_weapons;
    }

    // Update is called once per frame
    public void Fire(FireType fireType = FireType.MAIN)
    {
        IsFiring = true;

        if (IsOwner)
        {
            Vector3 fireTargetDir = m_aimTransform.forward;
            double currentTime = Time.timeAsDouble;

            if ((currentTime - m_timeOflastCheck) >= m_distanceUpdateInterval)
            {
                m_timeOflastCheck = Time.timeAsDouble;

                Ray ray = new(m_aimTransform.position, fireTargetDir);
                RaycastHit[] rayHits = GfPhysics.GetRaycastHits();

                bool hitAnObject = 0 != Physics.RaycastNonAlloc(ray, rayHits, m_maxFireDistance, GfPhysics.CharacterCollisions() - (int)Mathf.Pow(2, (gameObject.layer)));

                if (hitAnObject)
                {
                    m_lastRayHit = rayHits[0];
                    m_lastRayHit.distance += m_distanceOffset;
                }
                else
                {
                    m_lastRayHit.distance = m_maxFireDistance;
                }
            }

            m_lastRayHit.point = m_aimTransform.position + fireTargetDir * m_lastRayHit.distance;

            m_lastPoint.Value = m_lastRayHit.point;
            m_lastNormal.Value = m_lastRayHit.normal;
        }

        FireHit hit = new FireHit
        {
            point = m_lastPoint.Value,
            normal = m_lastNormal.Value,
            collisionId = -1,
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
            collisionId = -1,
        };

        for (int i = 0; null != m_weapons && i < m_weapons.Count; ++i)
        {
            if (m_weapons[i] != null && m_weapons[i].gameObject.activeSelf)
                m_weapons[i].ReleasedFire(hit, fireType);
        }
    }

    public StatsCharacter GetStatsCharacter()
    {
        return m_statsCharacter;
    }

    public void SetStatsCharacter(StatsCharacter character)
    {
        m_statsCharacter = character;
    }
}

public struct FireHit
{
    public Vector3 point;
    public Vector3 normal;
    public int collisionId;
}
