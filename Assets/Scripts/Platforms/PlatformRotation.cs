using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformRotation : MonoBehaviour
{
    public Vector3 rotation;
    public float speedMultiplier = 1;

    private Rigidbody m_rb;
    private Transform m_transform;

    // Start is called before the first frame update
    void Start()
    {
        m_rb = GetComponent<Rigidbody>();
        if (null == m_rb)
        {
            m_rb = gameObject.AddComponent<Rigidbody>();
        }

        m_rb.isKinematic = true;
        m_rb.interpolation = RigidbodyInterpolation.Interpolate;
        m_transform = transform;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        m_rb.MoveRotation(Quaternion.Euler(rotation * (speedMultiplier * Time.deltaTime)) * m_transform.rotation);
    }
}
