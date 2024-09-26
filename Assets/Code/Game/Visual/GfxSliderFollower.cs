using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GfxSliderFollower : MonoBehaviour
{
    public Slider Slider;

    [SerializeField] private CanvasGroup m_canvasGroup;

    public Transform Target;

    public Vector2 m_targetPositionOffset;

    [SerializeField] private float m_movementSmoothTime = 0.05f;

    [SerializeField] private float m_alphaSmoothTime = 0.05f;

    private Transform m_transform;

    private Vector3 m_movementSmoothRef;

    private float m_alphaSmoothRef;

    public bool Visible = false;

    // Start is called before the first frame update
    void Awake()
    {
        this.GetComponentIfNull(ref Slider);
        this.GetComponentIfNull(ref m_canvasGroup);

        if (m_canvasGroup == null)
            m_canvasGroup = GetComponent<CanvasGroup>();

        m_canvasGroup.alpha = 0;

        m_transform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (Target && GfgCameraController.Instance)
        {
            Vector2 targetScreenPosition = GfgCameraController.Instance.Camera.WorldToScreenPoint(Target.position);
            GfcTools.Add(ref targetScreenPosition, m_targetPositionOffset);

            m_canvasGroup.alpha = Mathf.SmoothDamp(m_canvasGroup.alpha, Visible ? 1 : 0, ref m_alphaSmoothRef, m_alphaSmoothTime);
            m_transform.position = Vector3.SmoothDamp(m_transform.position, targetScreenPosition, ref m_movementSmoothRef, m_movementSmoothTime);
        }
    }
}
