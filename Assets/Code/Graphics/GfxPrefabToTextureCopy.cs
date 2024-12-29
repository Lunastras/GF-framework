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
    void FixedUpdate() { if (!Application.isPlaying && m_cachedPrefabType != m_prefabType) Initialize(false); UpdateTexture(); }
#endif //UNITY_EDITOR

    void OnDisable()
    {
        if (!m_released)
            GfxManagerPrefabToTexture.ReleaseTemplate(m_prefabType, true);
        m_released = true;
    }

    void Initialize(bool aSkipIfManagerIsNull)
    {
        if (!m_released && m_cachedPrefabType != GfxPrefabToTextureInstanceTemplateType.NONE)
            GfxManagerPrefabToTexture.ReleaseTemplate(m_cachedPrefabType, aSkipIfManagerIsNull);

        if (m_prefabType != GfxPrefabToTextureInstanceTemplateType.NONE)
        {
            Debug.Log("tried aquire");
            m_originalPrefabToTexture = GfxManagerPrefabToTexture.GetTemplate(m_prefabType, aSkipIfManagerIsNull);
            m_released = false;

            if (m_rawImage.texture)
                m_cachedPrefabType = m_prefabType;
            else
                m_cachedPrefabType = GfxPrefabToTextureInstanceTemplateType.NONE;
        }
        else
        {
            m_released = true;
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