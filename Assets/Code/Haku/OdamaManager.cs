using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class OdamaManager : GfgLoadoutManager
{
    [SerializeField]
    public float m_rotationSpeedCoefOnFire = 5;

    [SerializeField]
    public float m_bopCoefOnFire = 0;

    [SerializeField]
    public float m_dstFromParentCoefOnFire = 0.7f;

    [SerializeField]
    public float m_rotationSpeed = 10;

    [SerializeField]
    public float m_bopSpeed = 1;
    [SerializeField]
    public float m_bopRange = 0.5f;

    [SerializeField]
    private float m_rotationSpeedSmoothTime = 0.2f;

    [SerializeField]
    private float m_bopValueSmoothTime = 0.2f;

    [SerializeField]
    public Vector3 m_positionOffset;

    private float m_bopCoef = 1;

    private NetworkVariable<float> m_currentBopValue = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<float> m_currentRotationRelativeToParentRad = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private float m_currentRotationSpeed;

    private float m_rotationSmoothRef = 0;

    private float m_bopSmoothRef = 0;

    private const float DEG_2_RAD_MULT_2 = Mathf.Deg2Rad * 2F;

    private const float PI_MULT_2 = Mathf.PI * 2F;

    private List<OdamaBehaviour> m_odamaList = new(4);

    protected Transform m_transform = null;

    protected void Awake()
    {
        m_currentRotationSpeed = m_rotationSpeed;
        m_transform = transform;
    }

    private static readonly Vector3 RIGHT3 = Vector3.right;

    protected override void OnWeaponSet(WeaponGeneric weapon)
    {
        OdamaBehaviour odama = weapon.GetComponent<OdamaBehaviour>();
        if (odama) m_odamaList.Add(odama);
    }

    protected override void OnWeaponsCleared()
    {
        m_odamaList.Clear();
        if (IsOwner)
        {
            m_currentBopValue.Value = Random.Range(0, PI_MULT_2);
            m_currentRotationRelativeToParentRad.Value = Random.Range(0, PI_MULT_2);
        }
    }

    public override void Respawned()
    {
        base.Respawned();
        Vector3 pos = m_transform.position;
        for (int i = 0; i < m_odamaList.Count; ++i)
        {
            m_odamaList[i].transform.position = pos;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        int odamaCount = m_odamaList.Count;
        if (odamaCount > 0 && m_parentMovement)
        {
            float dstCoef = 1;
            float rotationCoef = 1;
            float desiredBopCoef = 1;
            float deltaTime = Time.deltaTime * m_statsCharacter.GetDeltaTimeCoef();
            Vector3 upVec = m_parentMovement.GetUpvecRotation();

            if (m_weaponFiring && m_weaponFiring.IsFiring)
            {
                desiredBopCoef = m_bopCoefOnFire;
                dstCoef = m_dstFromParentCoefOnFire;
                rotationCoef = m_rotationSpeedCoefOnFire;
            }

            m_bopCoef = Mathf.SmoothDamp(m_bopCoef, desiredBopCoef, ref m_bopSmoothRef, m_bopValueSmoothTime, int.MaxValue, deltaTime);
            m_currentRotationSpeed = Mathf.SmoothDamp(m_currentRotationSpeed, m_rotationSpeed * rotationCoef, ref m_rotationSmoothRef, m_rotationSpeedSmoothTime, int.MaxValue, deltaTime);

            if (IsOwner)
            {
                m_currentBopValue.Value += deltaTime * m_bopSpeed;
                m_currentRotationRelativeToParentRad.Value += deltaTime * m_currentRotationSpeed;
            }

            OdamaBehaviour odama;
            float angleOffset, angle, height;
            Vector3 dirFromPlayer = Vector3.zero;
            float angleBetweenOdamasRad = PI_MULT_2 / odamaCount;
            float heightCoef = m_bopRange * m_bopCoef * 0.5f;

            for (int i = 0; i < odamaCount; ++i)
            {
                odama = m_odamaList[i];
                angleOffset = angleBetweenOdamasRad * i;
                angle = m_currentRotationRelativeToParentRad.Value + angleOffset;
                height = System.MathF.Sin((m_currentBopValue.Value + angleOffset) * 2F) * heightCoef;

                dirFromPlayer = m_parentMovement.GetCurrentRotation() * RIGHT3;
                GfcTools.Mult3(ref dirFromPlayer, odama.GetDesiredDst());
                GfcTools.Add3(ref dirFromPlayer, height * upVec);
                dirFromPlayer = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, upVec) * dirFromPlayer;

                odama.UpdateMovement(deltaTime, dirFromPlayer, transform.position, dstCoef, m_parentMovement);
            }
        }

    }
}
