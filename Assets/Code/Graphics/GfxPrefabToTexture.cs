using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;

[ExecuteInEditMode]
public class GfxPrefabToTexture : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Header("")]
    [SerializeField] private GameObject m_prefab;
    [SerializeField] private TransformData m_prefabTransform = new() { Position = new(0, 0, 5), Rotation = Quaternion.identity, Scale = new(1, 1, 1) };
    [SerializeField] private float m_objectLengthX = 10;

    [Header("")]
    [Header("Camera Settings")]
    [Header("")]
    [SerializeField] private float m_cameraSizeOrFov = 70;
    [SerializeField] private bool m_orthographicProjection;

    [Header("")]
    [Header("Render Texture Settings")]
    [Header("")]
    [SerializeField] private FilterMode m_textureFilterMode = FilterMode.Bilinear;

    [Tooltip("Requires texture to be regenerated when changing.")]
    [SerializeField] private Vector2 m_textureResolution = new Vector2(512, 512);
    [Tooltip("Regenerates the texture. Use when generation has errors or the resolution is changed.")]
    [SerializeField] private bool m_generateTexture = false;

    [SerializeField] GfxPrefabToTextureInstanceData m_instanceData; //should not be serialized, debug reasons only, DELME [SerializeField]

    public Action OnTextureUpdated;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GfcBase.InitializeGfBase();
        EditorApplication.playModeStateChanged += ModeChanged;
        Initialize(true, false);
    }

    void Start()
    {
        Debug.Assert(m_prefab || !Application.isPlaying, "The prefab must be assigned in the inspector before playing.");
        Initialize(false, false);
    }

    void ModeChanged(PlayModeStateChange aStateChange)
    {
        if (aStateChange == PlayModeStateChange.ExitingEditMode || aStateChange == PlayModeStateChange.ExitingPlayMode)
            m_instanceData.Destroy();
    }

    private void OnDestroy()
    {
        m_instanceData.Destroy();
        EditorApplication.playModeStateChanged -= ModeChanged;
    }

#if UNITY_EDITOR
    void Update() { if (!Application.isPlaying) UpdateValues(); }
#endif //UNITY_EDITOR

    public void Initialize(bool anIgnoreIfInstanceNull, bool aForceInitialize)
    {
        if (aForceInitialize || !m_instanceData.Valid(m_prefab))
        {
            if (m_prefab)
            {
                m_instanceData.Destroy();
                m_instanceData = GfxManagerPrefabToTexture.GetInitializedPrefab(m_prefab, m_prefabTransform, m_objectLengthX, m_cameraSizeOrFov, m_orthographicProjection, m_textureResolution, m_textureFilterMode, anIgnoreIfInstanceNull);
                m_generateTexture = false;
                OnTextureUpdated?.Invoke();
            }

            if (TryGetComponent(out RawImage image)) image.texture = m_instanceData.RenderTexture;
        }
    }

    private void OnEnable() { m_instanceData.SetActive(true); }
    private void OnDisable() { m_instanceData.SetActive(false); }

#if UNITY_EDITOR
    private void UpdateValues()
    {
        if (m_prefab)
        {
            if (m_generateTexture || !m_instanceData.Valid(m_prefab)) //todo, change when resolution is different
                Initialize(false, true);

            if (m_instanceData.InstantiatedPrefab != null)
            {
                m_instanceData.RenderTexture.filterMode = m_textureFilterMode;
                //m_instanceData.RenderTexture.antiAliasing = m_antialiasing;

                m_instanceData.Camera.fieldOfView = m_cameraSizeOrFov;
                m_instanceData.Camera.orthographicSize = m_cameraSizeOrFov;
                m_instanceData.Camera.orthographic = m_orthographicProjection;

                m_instanceData.InstantiatedPrefabParent.SetLocalPositionAndRotation(m_prefabTransform.Position, m_prefabTransform.Rotation);
                m_instanceData.InstantiatedPrefabParent.localScale = m_prefabTransform.Scale;
            }
        }
    }
#endif //UNITY_EDITOR

    public GfxPrefabToTextureInstanceData GetInstanceData(bool anIgnoreErrors = false) { return m_instanceData; }
}

[System.Serializable]
public struct GfxPrefabToTextureInstanceData
{
    public GameObject Prefab;
    public Camera Camera;
    public RenderTexture RenderTexture;
    public GameObject InstantiatedPrefab;
    public Transform InstantiatedPrefabParent;

    public void Destroy()
    {
        if (Camera)
        {
            Camera.targetTexture = null;
            GameObject.DestroyImmediate(Camera.gameObject);
        }

        Camera = null;
        RenderTexture = null;
        InstantiatedPrefab = null;
        InstantiatedPrefabParent = null;
    }

    public readonly void SetActive(bool aState) { if (Camera) Camera.gameObject.SetActive(aState); }
    public readonly bool Valid() { return Valid(Prefab); }
    public readonly bool Valid(GameObject aPrefab) { return aPrefab == Prefab && Camera != null && RenderTexture != null && InstantiatedPrefab != null && InstantiatedPrefabParent != null; }
}
