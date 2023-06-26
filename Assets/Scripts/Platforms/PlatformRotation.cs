using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlatformRotation : MonoBehaviour
{
    public Vector3 Rotation;
    public float SpeedMultiplier = 1;

    public bool RotatesLocally = false;

    private Rigidbody m_rb;
    private Transform m_transform;

    // Start is called before the first frame update
    void Start()
    {
        m_rb = GetComponent<Rigidbody>();

        m_rb.isKinematic = true;
        m_rb.interpolation = RigidbodyInterpolation.Interpolate;
        m_transform = transform;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Vector3 auxRotation = rotation;
        //auxRotation.x = rotation.z;
        //auxRotation.z = rotation.x;

        if (RotatesLocally)
            m_rb.MoveRotation(m_transform.rotation * Quaternion.Euler(Rotation * (SpeedMultiplier * Time.deltaTime)));

        else
            m_rb.MoveRotation(Quaternion.Euler(Rotation * (SpeedMultiplier * Time.deltaTime)) * m_transform.rotation);
    }
}
