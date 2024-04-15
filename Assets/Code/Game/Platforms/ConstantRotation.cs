using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ConstantRotation : MonoBehaviour
{
    [HideInInspector]
    public Quaternion Rotation = Quaternion.identity;
    private Transform m_transform;

    // Start is called before the first frame update
    void Awake()
    {
        m_transform = transform;
        Rotation = m_transform.rotation;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //Setting the rotation will rebuild the transform matrix of the object. We wish to avoid this as much as possible, so we first check if the values are identical
        if (!m_transform.rotation.Equals(Rotation))
            m_transform.rotation = Rotation;
    }
}
