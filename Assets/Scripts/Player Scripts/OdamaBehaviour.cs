using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OdamaBehaviour : MonoBehaviour
{
    [SerializeField]
    private WeaponBasic m_weaponBasic = null;

    [SerializeField]
    private ShadowProjectorManipulator m_shadowProjector = null;

    [SerializeField]
    private QuadFollowCamera m_followCameraScript = null;

    [SerializeField]
    public float m_dstFromParent = 1;

    [SerializeField]
    public float m_rotationSpeedCoefOnFire = 2;

    [SerializeField]
    public float m_bopCoefOnFire = 0;

    [SerializeField]
    public float m_dstFromParentCoefOnFire = 0.7f;

    [SerializeField]
    public float m_rotationSpeed = 1;

    [SerializeField]
    public float m_bopSpeed = 1;
    [SerializeField]
    public float m_bopRange = 0.5f;
    [SerializeField]
    public float m_parentMoveSmoothness = 0.02f;

    [SerializeField]
    private float m_rotationSpeedSmoothTime = 0.2f;

    [SerializeField]
    private float m_bopValueSmoothTime = 0.2f;

    [SerializeField]
    private float m_distanceSmoothTime = 0.2f;

    [SerializeField]
    public Vector3 m_positionOffset;

    [SerializeField]
    private float m_physCheckInterval = 0.2f;

    private float m_desiredTargetDst = 0;
    private float m_timeUntilPhysCheck;

    private float m_bopCoef = 1;

    private bool m_collidingWithSmth = false;

    [SerializeField]
    private float m_collisionRadius = 0.25f;
    private readonly static Vector3 UPDIR = Vector3.up;

    private float m_currentBopValue = 0;

    private float m_currentRotationRelativeToParentRad = 0;

    private Vector3 m_distanceSmoothVelocity;
    private Vector3 m_desiredPos;

    private float m_currentRotationSpeed;
    private float m_currentTargetDst;

    private float m_rotationSmoothRef = 0;

    private float m_bopSmoothRef = 0;

    private float m_distanceSmoothRef;

    private Transform m_transform;
    private int m_frameOfEnable;

    // private CharacterController
    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
        if (null == m_weaponBasic) m_weaponBasic = GetComponent<WeaponBasic>();
        if (null == m_weaponBasic) Debug.LogWarning("OdamaBehaviour warning: gameobject '" + gameObject.name + "' does not have a WeaponBasic component. Please attach one.");
    }

    public void OnEnable()
    {
        m_desiredTargetDst = m_dstFromParent;
        m_currentTargetDst = m_desiredTargetDst;
        m_currentBopValue = 0;
        m_currentRotationRelativeToParentRad = 0;
        m_frameOfEnable = Time.frameCount;
        m_currentRotationSpeed = m_rotationSpeed;
        m_rotationSmoothRef = 0;
        m_bopCoef = 1;
        m_bopSmoothRef = 0;
        m_distanceSmoothVelocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (m_weaponBasic && m_frameOfEnable != Time.frameCount)
        {
            GfMovementGeneric movementParent = m_weaponBasic.GetMovementParent();
            if (movementParent)
            {
                float rotationCoef = 1;
                float dstCoef = 1;
                float desiredBopCoef = 1;
                if (m_weaponBasic.IsFiring())
                {
                    rotationCoef = m_rotationSpeedCoefOnFire;
                    dstCoef = m_dstFromParentCoefOnFire;
                    desiredBopCoef = m_bopCoefOnFire;
                }

                m_bopCoef = Mathf.SmoothDamp(m_bopCoef, desiredBopCoef, ref m_bopSmoothRef, m_bopValueSmoothTime);

                Vector3 upVec = movementParent.GetUpvecRotation();
                m_transform.rotation = movementParent.GetCurrentRotation();

                if (m_shadowProjector) m_shadowProjector.m_parentMovement = movementParent;
                if (m_followCameraScript) m_followCameraScript.m_defaultUpvec = upVec;

                m_currentRotationSpeed = Mathf.SmoothDamp(m_currentRotationSpeed, m_rotationSpeed * rotationCoef, ref m_rotationSmoothRef, m_rotationSpeedSmoothTime);

                float weaponsCount = m_weaponBasic.GetLoadoutCount();
                float weaponIndex = m_weaponBasic.GetLoadoutWeaponIndex();

                float angleBetweenOdamas = 360.0f / weaponsCount;

                float angleOffset = angleBetweenOdamas * weaponIndex;
                float angle = m_currentRotationRelativeToParentRad + angleOffset;
                float height = System.MathF.Sin(m_currentBopValue + angleOffset * Mathf.Deg2Rad * 2.0f) * m_bopRange * m_bopCoef * 0.5f;

                Vector3 dirFromPlayer = movementParent.GetCurrentRotation() * Vector3.right;
                GfTools.Mult3(ref dirFromPlayer, m_desiredTargetDst);
                GfTools.Add3(ref dirFromPlayer, height * upVec);
                dirFromPlayer = Quaternion.AngleAxis(angle, upVec) * dirFromPlayer;

                m_currentRotationRelativeToParentRad += Time.deltaTime * m_currentRotationSpeed;
                m_currentBopValue += Time.deltaTime * m_bopSpeed;
                m_timeUntilPhysCheck -= Time.deltaTime;

                Vector3 parentPosition = movementParent.transform.position;

                if (m_timeUntilPhysCheck <= 0)
                {
                    m_desiredTargetDst = m_dstFromParent;
                    m_timeUntilPhysCheck = m_physCheckInterval;

                    int layermask = GfPhysics.NonCharacterCollisions();
                    RaycastHit[] raycastHits = GfPhysics.GetRaycastHits();
                    Vector3 dirNormalised = dirFromPlayer;
                    GfTools.Normalize(ref dirNormalised);

                    m_collidingWithSmth = 0 < Physics.SphereCastNonAlloc(parentPosition, m_collisionRadius, dirNormalised, raycastHits, m_desiredTargetDst, layermask, QueryTriggerInteraction.Ignore);

                    if (m_collidingWithSmth)
                        m_desiredTargetDst = raycastHits[0].distance;
                }

                m_currentTargetDst = Mathf.SmoothDamp(m_currentTargetDst, m_desiredTargetDst * dstCoef, ref m_distanceSmoothRef, m_distanceSmoothTime);
                m_desiredPos = m_currentTargetDst * dirFromPlayer + parentPosition;
                m_transform.position = Vector3.SmoothDamp(m_transform.position, m_desiredPos, ref m_distanceSmoothVelocity, m_parentMoveSmoothness);
            }
        }
    }

    public void SetAngle(float angle)
    {
        m_currentRotationRelativeToParentRad = angle * Mathf.Deg2Rad;
    }

    public virtual WeaponBasic GetWeaponBasic() { return m_weaponBasic; }
    public virtual void SetWeaponBasic(WeaponBasic parent) { m_weaponBasic = parent; }
}
