using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GfxLoadingScreenVisuals : MonoBehaviour
{
    public static GfxLoadingScreenVisuals Instance { get; protected set; }

    [SerializeField] private CanvasGroup m_canvasGroup;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        if (m_canvasGroup == null) m_canvasGroup = GetComponent<CanvasGroup>();
        SetGroupAlpha(0);
    }

    public virtual void SetGroupAlpha(float anAlpha)
    {
        m_canvasGroup.alpha = anAlpha;
        m_canvasGroup.gameObject.SetActive(anAlpha > 0.0001f);
    }
}
