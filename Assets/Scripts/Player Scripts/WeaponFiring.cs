using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class WeaponFiring : MonoBehaviour
{
    [SerializeField]
    private Transform m_aimTransform;

    [SerializeField]
    private StatsCharacter m_statsCharacter;

    [SerializeField]
    private float m_distanceUpdateInterval = 0.05f;

    [SerializeField]
    private float m_distanceOffset;

    // [SerializeField]
    // private bool canFire = true;

    [SerializeField]
    private float m_maxFireDistance = 100;

    private WeaponBasic[] m_weapons = null;

    private RaycastHit m_lastRayHit;

    private bool m_hitAnObject;

    private int m_numWeapons = 0;

    private double m_timeOflastCheck = 0;

    // Start is called before the first frame update
    void Awake()
    {
        if (null == m_aimTransform)
            m_aimTransform = transform;

        if (m_statsCharacter == null)
            m_statsCharacter = GetComponent<StatsCharacter>();

        m_lastRayHit = new RaycastHit();
    }


    public void SetWeaponArray(WeaponBasic[] weaponArray, int numWeapons = -1)
    {
        m_weapons = weaponArray;

        if (numWeapons < 0)
            numWeapons = m_weapons.Length;

        this.m_numWeapons = numWeapons;
    }

    // Update is called once per frame
    public void Fire()
    {
        Vector3 fireTargetDir = m_aimTransform.forward;
        double currentTime = Time.timeAsDouble;

        if ((currentTime - m_timeOflastCheck) >= m_distanceUpdateInterval)
        {
            m_timeOflastCheck = Time.timeAsDouble;

            Ray ray = new(m_aimTransform.position, fireTargetDir);
            RaycastHit[] rayHits = GfPhysics.GetRaycastHits();

            m_hitAnObject = 0 != Physics.RaycastNonAlloc(ray, rayHits, m_maxFireDistance, ~GfPhysics.IgnoreLayers() - (int)Mathf.Pow(2, (gameObject.layer)));

            if (m_hitAnObject)
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

        for (int i = 0; i < m_numWeapons; ++i)
            m_weapons[i].Fire(m_lastRayHit, m_hitAnObject);

    }

    public void ReleaseFire()
    {
        for (int i = 0; i < m_numWeapons; ++i)
        {
            if (m_weapons[i] != null && m_weapons[i].gameObject.activeSelf)
                m_weapons[i].ReleasedFire(m_lastRayHit, m_hitAnObject);
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
