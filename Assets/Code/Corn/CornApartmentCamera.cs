using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CornApartmentCamera : MonoBehaviour
{
    [SerializeField] private float m_distanceFromTarget = 16;

    [SerializeField] private float m_angleFromTargetPlane = 30;

    [SerializeField] private float m_currentRotation = 45;

    [SerializeField] private float m_rotationMaxSpeed = 45;

    [SerializeField] private float m_rotationAcceleration = 5;

    [SerializeField] private float m_rotationDeacceleration = 5;

    [SerializeField] private Vector3 m_targetOffset = new(0, 3, 0);

    public static CornApartmentCamera Instance { get { return ourInstance; } }

    protected static CornApartmentCamera ourInstance = null;

    public Camera Camera { get; private set; } = null;

    private Transform m_transform;

    private float m_currentRotationSpeed;

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref ourInstance);

        DontDestroyOnLoad(gameObject);
        Camera = GetComponent<Camera>();
        m_transform = transform;
    }

    void LateUpdate()
    {
        if (CornMenuApartment.Instance)
        {
            float xAxisMovement = GfcInput.GetAxisRaw(GfcInputType.MOVEMENT_X);

            if (xAxisMovement.Abs() > 0.001f)
            {
                m_currentRotationSpeed += m_rotationAcceleration * xAxisMovement * Time.deltaTime;
                if (m_currentRotationSpeed.Abs() > m_rotationMaxSpeed)
                    m_currentRotationSpeed = m_currentRotationSpeed.Sign() * m_rotationMaxSpeed;
            }
            else
            {
                int originalSpeedSign = m_currentRotationSpeed.Sign();
                m_currentRotationSpeed -= originalSpeedSign * m_rotationDeacceleration * Time.deltaTime;

                if (originalSpeedSign > 0)
                    m_currentRotationSpeed.MaxSelf(0);
                else
                    m_currentRotationSpeed.MinSelf(0);
            }

            m_currentRotation += Time.deltaTime * m_currentRotationSpeed;

            Vector3 cameraForward = -(Quaternion.AngleAxis(m_currentRotation, Vector3.up) * (Quaternion.AngleAxis(m_angleFromTargetPlane, Vector3.forward) * new Vector3(1, 0, 0))).normalized;
            Vector3 cameraPos = m_targetOffset + CornMenuApartment.Instance.CameraTarget.position - cameraForward * m_distanceFromTarget;

            m_transform.position = cameraPos;
            m_transform.rotation = Quaternion.LookRotation(cameraForward);
        }
    }
}
