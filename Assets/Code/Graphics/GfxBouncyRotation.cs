using UnityEngine;

public class GfxBouncyRotation : MonoBehaviour
{
    public float BounceIntervalSeconds = 2;
    public Vector3 RotationOffsetsRange = new(10, 10, 10);
    [HideInInspector] public Quaternion OriginalRotation;

    private Transform m_transform;
    private float m_timeLeftUntilBounce = 0;
    private Quaternion m_refSmoothQuat;

    private Quaternion m_desiredQuat;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_transform = transform;
        OriginalRotation = m_transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_timeLeftUntilBounce <= 0)
        {
            m_timeLeftUntilBounce = BounceIntervalSeconds;
            Quaternion xRotation = Quaternion.AngleAxis(Random.Range(-RotationOffsetsRange.x, RotationOffsetsRange.x), Vector3.right);
            Quaternion yRotation = Quaternion.AngleAxis(Random.Range(-RotationOffsetsRange.y, RotationOffsetsRange.y), Vector3.up);
            Quaternion zRotation = Quaternion.AngleAxis(Random.Range(-RotationOffsetsRange.z, RotationOffsetsRange.z), Vector3.forward);

            m_desiredQuat = OriginalRotation * xRotation * yRotation * zRotation;
        }

        float deltaTime = Time.deltaTime;
        m_transform.rotation = GfcTools.QuatSmoothDamp(m_transform.rotation, m_desiredQuat, ref m_refSmoothQuat, BounceIntervalSeconds, deltaTime);
        m_timeLeftUntilBounce -= deltaTime;
    }
}