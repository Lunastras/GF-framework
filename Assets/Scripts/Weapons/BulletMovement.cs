using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [SerializeField]
    protected BulletMovementValues bulletValues;

    //[SerializeField]
    // private float mass = 0.0f;

    //  [SerializeField]
    //private float maxFallSpeed = 6.0f;

    public float multiplier { get; set; } = 1.0f;

    private float currentSpeed;

    private float yVelocity = 0;

    public Transform target { get; set; } = null;

    // Start is called before the first frame update
    void Start()
    {
    }

    private void Initialize()
    {
        currentSpeed = 0;
        multiplier = 1;
        yVelocity = 0;
        currentSpeed = bulletValues.speed;
    }

    void OnEnable()
    {
        Initialize();
    }

    // Update is called once per frame
    // Update is called once per frame
    void FixedUpdate()
    {
        //yVelocity -= Time.deltaTime * mass;
        currentSpeed += Time.deltaTime * bulletValues.acceleration;

        //error correction for currentSpeed
        currentSpeed += Mathf.Sign(bulletValues.acceleration) * Mathf.Min(0, bulletValues.maxSpeedChange - Mathf.Abs(bulletValues.speed - currentSpeed));

        //error correction for vertical velocity
        //float currentYVelocity = yVelocity + transform.forward.y;
    }
    void Update()
    {
        transform.position += ((Vector3.up * yVelocity + transform.forward * currentSpeed) * multiplier) * Time.deltaTime;
        transform.Rotate(Vector3.up * bulletValues.yRotation * Time.deltaTime);
    }

    public void SetSpeed(float speed)
    {
        currentSpeed = speed;
    }

    public void SetAcceleration(float acceleration)
    {
        bulletValues.acceleration = acceleration;
    }

    public void SetMaxSpeedChange(float maxSpeedChange)
    {
        bulletValues.maxSpeedChange = maxSpeedChange;
    }

    [System.Serializable]
    public class BulletMovementValues
    {
        public BulletMovementValues(float speed = 20, float acceleration = 1.0f, float maxSpeedChange = 6, float yRotation = 0)
        {
            this.speed = speed;
            this.acceleration = acceleration;
            this.maxSpeedChange = maxSpeedChange;
            this.yRotation = yRotation;
        }

        public float yRotation;

        public float speed;

        public float acceleration;

        public float maxSpeedChange;
    }

    public void SetBulletMovementValues(BulletMovementValues bulletValues)
    {
        this.bulletValues = bulletValues;
        Initialize();
    }

    public BulletMovementValues GetBulletMovementValues()
    {
        return bulletValues;
    }

}
