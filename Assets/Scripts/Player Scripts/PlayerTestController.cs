using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTestController : MovementGeneric
{
    [SerializeField]
    private float acceleration = 10;

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
        float movementDirMagnitude = movementDir.magnitude;
        Vector3 vel = movementDir * acceleration * Time.deltaTime;
        rigidbody.AddForce(vel);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
