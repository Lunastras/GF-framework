using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OdamaBehaviour : MonoBehaviour
{
    [SerializeField]
    private Transform m_parent = null;
    [SerializeField]
    public float m_dstFromParent = 1;
    [SerializeField]
    public float m_rotationSpeed = 1;
    [SerializeField]
    public float m_parentMoveSmoothness = 0.02f;
    [SerializeField]
    public float m_bopSpeed = 1;
    [SerializeField]
    public float m_bopRange = 0.5f;
    [SerializeField]
    public Vector3 m_positionOffset;

    [SerializeField]
    private float m_physCheckInterval = 0.2f;

    [SerializeField]
    private GfMovementGeneric m_parentMovement;

    private float m_currentTargetDst = 0;
    private float m_timeUntilPhysCheck;

    private bool m_collidingWithSmth = false;

    private const float RADIUS = 0.7f;
    private readonly static Vector3 UPDIR = Vector3.up;

    private float m_currentBopValue;

    private float m_currentRotationRelativeToParentRad = 0;

    private Vector3 m_distanceSmoothVelocity;
    private Vector3 m_desiredPos;

    // private CharacterController
    // Start is called before the first frame update
    void Start()
    {
        m_currentTargetDst = m_dstFromParent;
        m_currentBopValue = Random.Range(0, 10);

        m_parentMoveSmoothness *= Random.Range(1f, 2f);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 dirFromPlayer = m_positionOffset
                                    + new Vector3(Mathf.Cos(m_currentRotationRelativeToParentRad) * m_dstFromParent,
                                                  Mathf.Sin(m_currentBopValue) * m_bopRange,
                                                  Mathf.Sin(m_currentRotationRelativeToParentRad) * m_dstFromParent);

        dirFromPlayer = dirFromPlayer.normalized;

        if (m_parentMovement && m_parentMovement.UpvecRotation() != UPDIR)
        {
            dirFromPlayer = Quaternion.FromToRotation(UPDIR, m_parentMovement.UpvecRotation()) * dirFromPlayer;
        }

        m_timeUntilPhysCheck -= Time.deltaTime;

        if (m_timeUntilPhysCheck <= 0)
        {
            m_timeUntilPhysCheck = m_physCheckInterval;

            int layermask = GfPhysics.NonCharacterCollisions();
            Collider[] colliders = GfPhysics.GetCollidersArray();
            m_currentTargetDst = m_dstFromParent;

            if (m_collidingWithSmth || 0 < Physics.OverlapSphereNonAlloc(transform.position, RADIUS, colliders, layermask))
            {
                RaycastHit[] raycastHits = GfPhysics.GetRaycastHits();
                m_collidingWithSmth = 0 < Physics.SphereCastNonAlloc(m_parent.position, RADIUS, dirFromPlayer, raycastHits, m_dstFromParent, layermask);

                if (m_collidingWithSmth)
                {
                    m_currentTargetDst = raycastHits[0].distance;
                }
            }
        }

        m_currentTargetDst = m_dstFromParent;

        m_desiredPos = m_currentTargetDst * dirFromPlayer + m_parent.position;

        m_currentRotationRelativeToParentRad += Time.deltaTime * m_rotationSpeed;
        m_currentBopValue += Time.deltaTime * m_bopSpeed;
    }

    private void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, m_desiredPos, ref m_distanceSmoothVelocity, m_parentMoveSmoothness);
    }

    public void SetAngle(float angle)
    {
        m_currentRotationRelativeToParentRad = angle * Mathf.Deg2Rad;
    }

    public void SetParent(GfMovementGeneric parent)
    {
        this.m_parent = parent.transform;
        m_parentMovement = parent;
    }
}
