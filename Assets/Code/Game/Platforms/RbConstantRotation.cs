using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RbConstantRotation : MonoBehaviour
{
    [HideInInspector]
    public Quaternion Rotation = Quaternion.identity;
    private Transform m_transform;
    private Rigidbody m_rb;

    // Start is called before the first frame update
    void Awake()
    {
        m_transform = transform;
        Rotation = m_transform.rotation;

        m_rb = GetComponent<Rigidbody>();

        m_rb.isKinematic = true;
        m_rb.interpolation = RigidbodyInterpolation.Interpolate;
        m_transform = transform;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Setting the rotation will rebuild the transform matrix of the object. We wish to avoid this as much as possible, so we first check if the values are identical
        if (!m_transform.rotation.Equals(Rotation))
            m_rb.MoveRotation(Rotation);
    }
}
