using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyParenting : MonoBehaviour
{
    [SerializeField]
    private PriorityValue<Transform> m_parentTransform = new();

    public bool ParentsPosition = true;

    public bool ParentsRotationMovement = true;

    public bool IgnoresRotation = true;

    private Rigidbody m_rb = null;

    private Vector3 m_parentLastPos;

    private Quaternion m_parentLastRot;

    private Transform m_transform;

    // Start is called before the first frame update
    void Start()
    {
        m_rb = GetComponent<Rigidbody>();
        m_transform = transform;
        if (m_parentTransform.Value)
        {
            m_parentLastRot = m_parentTransform.Value.rotation;
            m_parentLastPos = m_parentTransform.Value.position;
        }
    }

    public void SetParent(Transform parent, uint priority = 0, bool overridePriority = false)
    {
        if (parent != m_parentTransform && m_parentTransform.SetValue(parent, priority, overridePriority))
        {
            m_parentLastRot = parent.rotation;
            m_parentLastPos = parent.position;
        }
    }

    public PriorityValue<Transform> GetParent() { return m_parentTransform; }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (m_parentTransform.Value)
        {
            Vector3 selfPosition = m_transform.position;
            Vector3 parentPosition = m_parentTransform.Value.position;

            Vector3 currentParentPos = m_parentTransform.Value.position;
            if (!currentParentPos.Equals(m_parentLastPos))
            {
                Vector3 parentPosMov = currentParentPos - m_parentLastPos;
                m_parentLastPos = parentPosition;
                GfTools.Add3(ref selfPosition, parentPosMov);
            }

            //Calculate the rotation according to the parent's rotation
            Quaternion currentRot = m_parentTransform.Value.rotation;
            Quaternion parentRotation = Quaternion.identity;
            if (ParentsRotationMovement && !currentRot.Equals(m_parentLastRot)) // .Equals() is different from == (and a bit wrong), but it works better here because of the added accuracy
            {
                Quaternion deltaQuaternion = currentRot * Quaternion.Inverse(m_parentLastRot);
                Vector3 vecFromParent = selfPosition;
                GfTools.Minus3(ref vecFromParent, parentPosition);

                Vector3 newVecFromParent = deltaQuaternion * vecFromParent;
                Vector3 parentRotMov = newVecFromParent - vecFromParent;

                m_parentLastRot = currentRot;
                GfTools.Add3(ref selfPosition, parentRotMov);
                if (!IgnoresRotation) m_rb.MoveRotation(deltaQuaternion * m_transform.rotation);
            }

            m_rb.MovePosition(selfPosition);
        }
    }
}
