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
    }

    public override void CalculateMovement(float speedMultiplier = 1)
    {
        float currentSpeed = rigidbody.velocity.magnitude;
        Vector3 effectiveVelocity = rigidbody.velocity;
        if (!canFly) effectiveVelocity.y = 0;

        Vector3 velDir = effectiveVelocity.normalized;
        float deaccCoef = 1;  

        float speedToMax = System.MathF.Max(0, currentSpeed - maxSpeed * movementDirMagnitude);
        if (0 == speedToMax)
        {
            deaccCoef = System.MathF.Min(1, 1 - Vector3.Dot(movementDir, velDir));
        }

        //make sure it doesn't reduce the speed to less than 0 when not moving
        //+ make sure it doesn't reduce the speed past the desired speed
        float deaccMagn = System.MathF.Min(speedToMax, System.MathF.Min(currentSpeed, deacceleration * deaccCoef * Time.deltaTime));

        Vector3 deaccForce = -velDir * deaccMagn;
        Vector3 accForce = movementDir * acceleration * Time.deltaTime;

        Vector3 vel = deaccForce + accForce;
        rigidbody.AddForce(vel, ForceMode.VelocityChange);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionStay(Collision collision)
    {
        int contactCount = collision.contactCount;
        // collision.GetContact(0).normal;
        // Debug.DrawRay(collision.GetContact(0).point, collision.GetContact(0).normal, Color.red, 0.2f);

    }
}
