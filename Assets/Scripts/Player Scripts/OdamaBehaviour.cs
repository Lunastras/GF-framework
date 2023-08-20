using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OdamaBehaviour : MonoBehaviour
{
    [SerializeField]
    private float m_collisionRadius = 0.25f;

    [SerializeField]
    private WeaponGeneric m_weaponBasic = null;

    [SerializeField]
    private ShadowProjectorManipulator m_shadowProjector = null;

    [SerializeField]
    private QuadFollowCamera m_followCameraScript = null;

    [SerializeField]
    public float m_movementSmoothTime = 0.02f;

    [SerializeField]
    private float m_distanceSmoothTime = 0.2f;

    [SerializeField]
    public float m_dstFromParent = 1;

    [SerializeField]
    private float m_physCheckInterval = 0.2f;


    private float m_desiredTargetDst = 0;

    private float m_timeUntilPhysCheck;

    private bool m_collidingWithSmth = false;

    private readonly static Vector3 UPDIR = Vector3.up;

    private Vector3 m_distanceSmoothVelocity;
    private Vector3 m_desiredPos;

    private float m_currentTargetDst;


    private float m_distanceSmoothRef;

    private Transform m_transform;

    // private CharacterController
    // Start is called before the first frame update
    void Awake()
    {
        m_transform = transform;
        if (null == m_weaponBasic) m_weaponBasic = GetComponent<WeaponGeneric>();
        if (null == m_weaponBasic) Debug.LogWarning("OdamaBehaviour warning: gameobject '" + gameObject.name + "' does not have a WeaponBasic component. Please attach one.");
    }

    public void OnEnable()
    {
        m_desiredTargetDst = m_dstFromParent;
        m_currentTargetDst = m_desiredTargetDst;
        m_distanceSmoothVelocity = Vector3.zero;
    }

    public void UpdateMovement(float deltaTime, Vector3 dirFromPlayer, Vector3 parentPosition, float distanceCoef, GfMovementGeneric parentMovement)
    {
        m_timeUntilPhysCheck -= deltaTime;

        if (parentMovement)
        {
            //m_shadowProjector.transform.rotation = parentMovement.GetCurrentRotation();
            if (m_shadowProjector) m_shadowProjector.m_parentMovement = parentMovement;
            if (m_followCameraScript) m_followCameraScript.m_defaultUpvec = parentMovement.GetUpvecRotation();
        }

        if (m_timeUntilPhysCheck <= 0)
        {
            m_desiredTargetDst = m_dstFromParent;
            m_timeUntilPhysCheck = m_physCheckInterval;

            Vector3 dirNormalised = dirFromPlayer;
            GfTools.Normalize(ref dirNormalised);

            int layermask = GfPhysics.NonCharacterCollisions();
            RaycastHit[] raycastHits = GfPhysics.GetRaycastHits();

            m_collidingWithSmth = 0 < Physics.SphereCastNonAlloc(parentPosition, m_collisionRadius, dirNormalised, raycastHits, m_desiredTargetDst, layermask, QueryTriggerInteraction.Ignore);

            if (m_collidingWithSmth)
                m_desiredTargetDst = raycastHits[0].distance;
        }

        m_currentTargetDst = Mathf.SmoothDamp(m_currentTargetDst, m_desiredTargetDst * distanceCoef, ref m_distanceSmoothRef, m_distanceSmoothTime);
        m_desiredPos = m_currentTargetDst * dirFromPlayer + parentPosition;
        m_transform.position = Vector3.SmoothDamp(m_transform.position, m_desiredPos, ref m_distanceSmoothVelocity, m_movementSmoothTime);
    }

    public float GetDesiredDst()
    {
        return m_desiredTargetDst;
    }

    public virtual WeaponGeneric GetWeaponBasic() { return m_weaponBasic; }
    public virtual void SetWeaponBasic(WeaponGeneric parent) { m_weaponBasic = parent; }
}
