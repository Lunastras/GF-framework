using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OdamaBehaviour : MonoBehaviour
{
    private Transform parent = null;

    [SerializeField]
    private  OdamaBehaviourValues odamaValues;

    [SerializeField]
    private float physCheckInterval = 0.2f;

    private float currentTargetDst = 0;
    private float timeUntilPhysCheck;

    private bool collidingWithSmth = false;

    private RaycastHit raycastHit;

    private const float collisionRadius = 0.7f;

    private float currentBopValue;

    private float currentRotationRelativeToParentRad = 0;

    private Vector3 distanceSmoothVelocity;
    private Vector3 desiredPos;

    // private CharacterController
    // Start is called before the first frame update
    void Start()
    {
        currentTargetDst = odamaValues.dstFromParent;
        currentBopValue = Random.Range(0, 10);

        odamaValues.parentMoveSmoothness *= Random.Range(1f, 2f);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 dirFromPlayer = odamaValues.positionOffset 
                                    + new Vector3(Mathf.Cos(currentRotationRelativeToParentRad) * odamaValues.dstFromParent,
                                                  Mathf.Sin(currentBopValue) * odamaValues.bopRange, 
                                                  Mathf.Sin(currentRotationRelativeToParentRad) * odamaValues.dstFromParent);

        dirFromPlayer = dirFromPlayer.normalized;

        timeUntilPhysCheck -= Time.deltaTime;

        if (timeUntilPhysCheck <= 0)
        {
            timeUntilPhysCheck = physCheckInterval;

            int layermask = GfPhysics.NonCharacterCollisions();
            Collider[] colliders = GfPhysics.GetCollidersArray();
            currentTargetDst = odamaValues.dstFromParent;

            if (collidingWithSmth || 0 < Physics.OverlapSphereNonAlloc(parent.position, collisionRadius, colliders, layermask))
            {
                RaycastHit[] raycastHits = GfPhysics.GetRaycastHits();
                collidingWithSmth = 0 < Physics.SphereCastNonAlloc(parent.position, collisionRadius, dirFromPlayer, raycastHits, odamaValues.dstFromParent, layermask);

                if (collidingWithSmth)
                {
                    collidingWithSmth = true;
                    raycastHit = raycastHits[0];
                    currentTargetDst = raycastHit.distance;
                }
            }     
        }

        desiredPos = currentTargetDst * dirFromPlayer + parent.position;
        
        currentRotationRelativeToParentRad += Time.deltaTime * odamaValues.rotationSpeed;
        currentBopValue += Time.deltaTime * odamaValues.bopSpeed;
    }

    private void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref distanceSmoothVelocity, odamaValues.parentMoveSmoothness);
    }

    public void SetAngle(float angle)
    {
        currentRotationRelativeToParentRad = angle * Mathf.Deg2Rad;
    }

    public void SetParent(Transform parent)
    {
        this.parent = parent;
    }

    public void SetOdamaValues(OdamaBehaviourValues values)
    {
        odamaValues = values;
    }

    public OdamaBehaviourValues GetOdamaValues()
    {
        return odamaValues;
    }
}

[System.Serializable]
public struct OdamaBehaviourValues
{
    public OdamaBehaviourValues(float dstFromParent = 1.5f, float rotationSpeed = 0.8f, float parentMoveSmoothness = 0.25f, float bopSpeed = 3f, float bopRange = 0.4f)
    {
        this.dstFromParent = dstFromParent;
        this.rotationSpeed = rotationSpeed;
        this.parentMoveSmoothness = parentMoveSmoothness;
        this.bopSpeed = bopSpeed;
        this.bopRange = bopRange;

        positionOffset = new Vector3(0, -0.1f, 0);
    }

    public float dstFromParent;
    public float rotationSpeed;
    public float parentMoveSmoothness;
    public float bopSpeed;
    public float bopRange;
    public Vector3 positionOffset;
}
