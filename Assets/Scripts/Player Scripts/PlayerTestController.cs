using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTestController : MovementGeneric
{
    [SerializeField]
    private float acceleration = 10;

    [SerializeField]
    private float maxSpeed = 10;

    [SerializeField]
    private float deacceleration = 10;

    [SerializeField]
    private new Rigidbody rigidbody;

    // Start is called before the first frame update
    void Start()
    {
        if (!rigidbody)
            rigidbody = GetComponent<Rigidbody>();

       rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public override void CalculateMovement(float speedMultiplier = 1)
    {
        float currentSpeed = rigidbody.velocity.magnitude;

        Vector3 velDir = rigidbody.velocity.normalized;

        //will be 1 if over speed limit, 0 if under
        float deaccCoef;

        if (maxSpeed - currentSpeed > 0)
            deaccCoef = Mathf.Max(0, Vector3.Dot(-velDir, movementDir));
        else
            deaccCoef = 1;

        float deaccMagn = Mathf.Min(currentSpeed, deacceleration * deaccCoef);
        Debug.Log("Deacc magnitude is " + deaccMagn);

        Vector3 deaccForce = -velDir * deaccMagn;

        Vector3 accForce = movementDir * acceleration;

        Vector3 vel = (deaccForce + accForce) * Time.deltaTime;
        rigidbody.AddForce(vel, ForceMode.VelocityChange);

        //Debug.Log("velocity rn is " + rigidbody.velocity);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
