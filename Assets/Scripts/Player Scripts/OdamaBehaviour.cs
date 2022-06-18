using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OdamaBehaviour : MonoBehaviour
{
    private Transform parent = null;

    [SerializeField]
    private  OdamaBehaviourValues odamaValues;

    private const float collisionRadius = 0.7f;

    private float currentBopValue;

    private float currentRotationRelativeToParentRad = 0;
    private Vector3 distanceSmoothVelocity;
    private const float Euler = 2.71828f;

    // private CharacterController
    // Start is called before the first frame update
    void Start()
    {
        currentBopValue = Random.Range(0, 10);

        odamaValues.parentMoveSmoothness *= Random.Range(1f, 2f);
    }

    // Update is called once per frame
    void Update()
    {
        if (parent == null)
            return;

        Vector2 currentRotation = new Vector2(Mathf.Cos(currentRotationRelativeToParentRad),
                                              Mathf.Sin(currentRotationRelativeToParentRad));

        currentRotation *= odamaValues.dstFromParent;
        float verticalBop = Mathf.Sin(currentBopValue) * odamaValues.bopRange;

        Vector3 desiredPosition = parent.position + new Vector3(currentRotation.x, verticalBop, currentRotation.y);

        Vector3 dirFromPlayer = desiredPosition - parent.position;
        float dstFromPlayer = dirFromPlayer.magnitude;
        dirFromPlayer = dirFromPlayer.normalized;

        desiredPosition += odamaValues.positionOffset;

        RaycastHit hit;
        if (Physics.Raycast(parent.position, dirFromPlayer, out hit, dstFromPlayer * 1.2f, ~GfPhysics.IgnoreLayers()))
        {
            Vector3 hitPoint = hit.point;
            desiredPosition = hitPoint;
            dstFromPlayer = (hitPoint - parent.position).magnitude;

            if (dstFromPlayer > collisionRadius)
            {
                desiredPosition -= collisionRadius * dirFromPlayer;
            }
        }

        float smoothTime = odamaValues.parentMoveSmoothness * Mathf.Pow(Euler, dstFromPlayer);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref distanceSmoothVelocity, odamaValues.parentMoveSmoothness);

        //transform.position = desiredPosition;

        currentRotationRelativeToParentRad += Time.deltaTime * odamaValues.rotationSpeed;
        currentBopValue += Time.deltaTime * odamaValues.bopSpeed;
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
