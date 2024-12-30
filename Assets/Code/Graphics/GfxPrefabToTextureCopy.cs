using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class GfxPrefabToTextureCopy : MonoBehaviour
{
    [SerializeField] private GfxPrefabToTextureInstanceTemplateType m_prefabType = GfxPrefabToTextureInstanceTemplateType.BUTTON_CUBE;
    private RawImage m_rawImage;
    private GfxPrefabToTextureInstanceTemplateType m_cachedPrefabType = GfxPrefabToTextureInstanceTemplateType.NONE;

    private GfxPrefabToTexture m_originalPrefabToTexture;
    bool m_released = true;

    void Awake()
    {
        this.GetComponent(ref m_rawImage);
        GfcBase.InitializeGfBase();
    }
    void OnEnable() { Initialize(true); }
    void Start() { Initialize(false); }


#if UNITY_EDITOR
    void Update()
    {
        if (m_cachedPrefabType != m_prefabType)
            Initialize(false);
    }
#endif //UNITY_EDITOR

    void ReleaseTemplate(GfxPrefabToTextureInstanceTemplateType aType, bool aSkipIfManagerIsNull)
    {
        if (!m_released)
        {
            Debug.Assert(aType != GfxPrefabToTextureInstanceTemplateType.NONE);
            GfxManagerPrefabToTexture.ReleaseTemplate(aType, aSkipIfManagerIsNull);
            m_originalPrefabToTexture.OnTextureUpdated -= UpdateTexture;
            m_originalPrefabToTexture = null;
        }
        m_released = true;
    }

    void OnDisable() { ReleaseTemplate(m_prefabType, true); }

    void Initialize(bool aSkipIfManagerIsNull)
    {
        ReleaseTemplate(m_cachedPrefabType, aSkipIfManagerIsNull);

        m_released = true;
        if (m_prefabType != GfxPrefabToTextureInstanceTemplateType.NONE)
        {
            m_originalPrefabToTexture = GfxManagerPrefabToTexture.GetTemplate(m_prefabType, aSkipIfManagerIsNull);

            if (m_originalPrefabToTexture)
            {
                m_originalPrefabToTexture.OnTextureUpdated += UpdateTexture;
                m_cachedPrefabType = m_prefabType;
                m_released = false;
            }
            else m_cachedPrefabType = GfxPrefabToTextureInstanceTemplateType.NONE;
        }
        else
        {
            m_cachedPrefabType = GfxPrefabToTextureInstanceTemplateType.NONE;
            m_originalPrefabToTexture = null;
        }

        UpdateTexture();
    }

    void UpdateTexture()
    {
        m_rawImage.texture = m_originalPrefabToTexture ? m_originalPrefabToTexture.GetInstanceData().RenderTexture : null;
    }
}