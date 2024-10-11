
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CornActionButton : MonoBehaviour
{
    public GfxSliderText SliderText;

    [SerializeField] private GfxSoftHover m_softHover = null;

    public Transform WorldPointOfReference = null;

    [SerializeField] private bool m_optionalFields = false;

    [HideInInspector] public GfxButton2D Button;

    private Transform m_transform;

    private bool m_initialized = false;
    public void Initialize()
    {
        if (!m_initialized)
        {
            Button = GetComponent<GfxButton2D>();
            Button?.Initialize();

            if (SliderText == null) SliderText = GetComponent<GfxSliderText>();
            if (m_softHover == null) m_softHover = GetComponent<GfxSoftHover>();
            Debug.Assert(SliderText && m_softHover || m_optionalFields);
            m_transform = transform;

            m_initialized = true;
        }
    }

    void Start()
    {
        Initialize();
        Debug.Assert(WorldPointOfReference, "The world reference point was not assigned.");
    }

    void Update()
    {
        if (WorldPointOfReference) m_transform.position = GfcCamera.MainCamera.WorldToScreenPoint(WorldPointOfReference.position);
    }

    private void OnButtonEvent(GfxButtonCallbackType aType, GfxButton aButton, bool aState)
    {
        switch (aType)
        {
            case GfxButtonCallbackType.SELECT:
                if (aState) m_transform.SetAsLastSibling();
                m_softHover.m_snapToOrigin = aState;
                break;
        }
    }

    public void SetPanelColor(Color aColor, ColorBlendMode aBlendMode = ColorBlendMode.REPLACE) { Button.SetPanelColor(aColor, aBlendMode); }

    public void SetContentColor(Color aColor, ColorBlendMode aBlendMode = ColorBlendMode.REPLACE) { Button.SetContentColor(aColor, aBlendMode); }
}
