using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(CanvasGroup), typeof(Image))]
public class GfxCanvasGroupImageAlpha : MonoBehaviour
{
    [SerializeField] bool m_affectRaycast = true;
    private CanvasGroup m_canvasGroup;
    private Image m_image;
    private float m_lastAlpha = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        this.GetComponent(ref m_canvasGroup);
        this.GetComponent(ref m_image);
    }

    void Update()
    {
        float alpha = m_image.color.a;
        if (m_lastAlpha != alpha)
        {
            m_lastAlpha = alpha;
            m_canvasGroup.alpha = m_image.color.a;
            if (m_affectRaycast)
                m_canvasGroup.blocksRaycasts = m_canvasGroup.alpha > 0.0001f;
        }
    }
}