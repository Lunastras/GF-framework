using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class OdamaManager : LoadoutManager
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

    private List<OdamaBehaviour> m_list = new(4);

    protected override void InternalStart()
    {
        m_currentRotationSpeed = m_rotationSpeed;
    }

    protected override void OnWeaponSet(WeaponBasic weapon)
    {
        OdamaBehaviour odama = weapon.GetComponent<OdamaBehaviour>();
        if (odama) m_list.Add(odama);
    }

    protected override void OnWeaponsCleared()
    {
        m_list.Clear();
        if (IsOwner)
        {
            m_currentBopValue.Value = Random.Range(0, 6.283185f);
            m_currentRotationRelativeToParentRad.Value = Random.Range(0, 6.283185f);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        int length = m_list.Count;

        if (length > 0)
        {
            float deltaTime = Time.deltaTime;
            Vector3 upVec = m_parentMovement.GetUpvecRotation();

            float rotationCoef = 1;
            float dstCoef = 1;
            float desiredBopCoef = 1;
            if (m_weaponFiring && m_weaponFiring.IsFiring)
            {
                rotationCoef = m_rotationSpeedCoefOnFire;
                dstCoef = m_dstFromParentCoefOnFire;
                desiredBopCoef = m_bopCoefOnFire;
            }

            m_bopCoef = Mathf.SmoothDamp(m_bopCoef, desiredBopCoef, ref m_bopSmoothRef, m_bopValueSmoothTime);
            m_currentRotationSpeed = Mathf.SmoothDamp(m_currentRotationSpeed, m_rotationSpeed * rotationCoef, ref m_rotationSmoothRef, m_rotationSpeedSmoothTime);

            if (IsOwner)
            {
                m_currentRotationRelativeToParentRad.Value += deltaTime * m_currentRotationSpeed;
                m_currentBopValue.Value += deltaTime * m_bopSpeed;
            }

            OdamaBehaviour odama;
            float angleBetweenOdamas = 360.0f / length;

            //Debug.Log("The current rotation is: ")

            for (int i = 0; i < length; ++i)
            {
                odama = m_list[i];

                if (m_parentMovement)
                {
                    float angleOffset = angleBetweenOdamas * i;
                    float angle = m_currentRotationRelativeToParentRad.Value + angleOffset;
                    float height = System.MathF.Sin(m_currentBopValue.Value + angleOffset * Mathf.Deg2Rad * 2.0f) * m_bopRange * m_bopCoef * 0.5f;

                    Vector3 dirFromPlayer = m_parentMovement.GetCurrentRotation() * Vector3.right;
                    GfTools.Mult3(ref dirFromPlayer, odama.GetDesiredDst());
                    GfTools.Add3(ref dirFromPlayer, height * upVec);
                    dirFromPlayer = Quaternion.AngleAxis(angle, upVec) * dirFromPlayer;

                    odama.UpdateMovement(deltaTime, dirFromPlayer, transform.position, dstCoef, m_parentMovement);
                }
            }
        }

    }
}
