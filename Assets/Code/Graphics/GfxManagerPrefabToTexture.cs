using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class GfxManagerPrefabToTexture : MonoBehaviour
{
    private static GfxManagerPrefabToTexture Instance;

    [SerializeField] private EnumSingletons<GfxPrefabToTextureInstanceTemplate, GfxPrefabToTextureInstanceTemplateType> m_templates;
    [SerializeField] private GameObject m_cameraPrefab;
    [SerializeField] private float m_xPositionOffset = 10;

    const float INITIAL_X_POSITION = -65536;
    float m_currentXPosition = INITIAL_X_POSITION;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        this.SetSingleton(ref Instance);
        m_templates.Validate(GfxPrefabToTextureInstanceTemplateType.COUNT);
        m_currentXPosition = INITIAL_X_POSITION;
        Debug.Log("Called, my length is: " + m_templates.Length);
        for (int i = 0; i < m_templates.Length; i++)
        {
            Debug.Log("INITIALIZED THE TEMPLATE FOR " + (GfxPrefabToTextureInstanceTemplateType)i);
            if (m_templates[i].TemplateInstance != null)
                m_templates[i].Initialize();
            else
                Debug.LogError("The template of type " + (GfxPrefabToTextureInstanceTemplateType)i + " is null.");
        }
    }

    public static GfxPrefabToTexture GetTemplate(GfxPrefabToTextureInstanceTemplateType aType, bool aSkipIfInstanceIsNull = false)
    {
        if (aType == GfxPrefabToTextureInstanceTemplateType.NONE)
            return default;

        AccessTemplate(aType, GfxPrefabToTextureInstanceTemplateAccessType.AQUIRE, aSkipIfInstanceIsNull);

        GfxPrefabToTexture instanceData = default;
        if (Instance)
            if (Instance.m_templates.Length > (int)aType && Instance.m_templates[aType].TemplateInstance)
            {
                instanceData = Instance.m_templates[aType].TemplateInstance;
                if (!instanceData) Debug.LogError("Could not retrieve the instance data for " + aType);
            }
            else
                Debug.LogError("Type " + aType + " was not initialized in the inspector.");

        return instanceData;
    }

    public static void ReleaseTemplate(GfxPrefabToTextureInstanceTemplateType aType, bool aSkipIfInstanceIsNull = false) { AccessTemplate(aType, GfxPrefabToTextureInstanceTemplateAccessType.RELEASE, aSkipIfInstanceIsNull); }

    private static void AccessTemplate(GfxPrefabToTextureInstanceTemplateType aType, GfxPrefabToTextureInstanceTemplateAccessType anAccessType, bool aSkipIfInstanceIsNull = false)
    {
        if (aType == GfxPrefabToTextureInstanceTemplateType.NONE)
            return;

        Debug.Assert(aSkipIfInstanceIsNull || Instance);
        if (Instance)
        {
            if (Instance.m_templates.Length > (int)aType)
            {
                var template = Instance.m_templates[aType];
                Debug.Assert(template.TemplateInstance.GetInstanceData().RenderTexture, "The texture is null, something is really wrong");

                if (anAccessType == GfxPrefabToTextureInstanceTemplateAccessType.AQUIRE)
                    template.Aquire();
                else
                    template.Release();
                Instance.m_templates[aType] = template;
            }
        }
    }

#if UNITY_EDITOR
    void Update() { if (!Application.isPlaying) this.SetSingleton(ref Instance); }
#endif //UNITY_EDITOR

    public static GfxPrefabToTextureInstanceData GetInitializedPrefab(GameObject aPrefab, TransformData aTransformData, float anObjectLengthX, float aCameraSizeOrFov, bool anOrthographicProjection, Vector2 aTextureResolution, FilterMode aFilterMode, bool anIgnoreIfInstanceNull)
    {
        GfxPrefabToTextureInstanceData instanceData = default;
        Debug.Assert(Instance || anIgnoreIfInstanceNull);
        if (Instance)
        {
            float objectLengthHalf = anObjectLengthX * 0.5f * aTransformData.Scale.x.Abs();
            GameObject cameraObj = GfcPooling.Instantiate(Instance.m_cameraPrefab);
            Camera camera = cameraObj.GetComponent<Camera>();
            Transform prefab = GfcPooling.Instantiate(aPrefab).transform;
            Transform prefabParent = new GameObject("PrefabParent").transform;

            prefabParent.SetParent(camera.transform, false);
            prefab.SetParent(prefabParent, false);

            prefabParent.gameObject.SetLayerRecursive(camera.gameObject.layer);
            cameraObj.SetHideFlagsRecursive(HideFlags.HideAndDontSave);

            RenderTexture renderTexture = new((int)aTextureResolution.x, (int)aTextureResolution.y, 1);
            renderTexture.filterMode = aFilterMode;

            camera.targetTexture = renderTexture;
            camera.fieldOfView = aCameraSizeOrFov;
            camera.orthographicSize = aCameraSizeOrFov;
            camera.orthographic = anOrthographicProjection;
            camera.transform.position = new(Instance.m_currentXPosition + objectLengthHalf, 0, 0);

            prefabParent.SetLocalPositionAndRotation(aTransformData.Position, aTransformData.Rotation);
            prefabParent.localScale = aTransformData.Scale;

            Instance.m_currentXPosition += Instance.m_xPositionOffset + objectLengthHalf;

            instanceData = new()
            {
                RenderTexture = renderTexture,
                InstantiatedPrefab = prefab.gameObject,
                InstantiatedPrefabParent = prefabParent,
                Camera = camera,
                Prefab = aPrefab,
            };

            SceneManager.MoveGameObjectToScene(cameraObj, SceneManager.GetSceneByBuildIndex((int)GfcSceneId.GF_BASE));
        }

        return instanceData;
    }
}

public enum GfxPrefabToTextureInstanceTemplateAccessType
{
    RELEASE,
    AQUIRE
}

public enum GfxPrefabToTextureInstanceTemplateType
{
    NONE = -1,
    BUTTON_CUBE,
    BUTTON_ROUND_CUBE,
    BUTTON_ROUND,
    COUNT
}

[System.Serializable]
public struct GfxPrefabToTextureInstanceTemplate
{
    public GfxPrefabToTexture TemplateInstance;
    private int m_refCounter;

    public void Initialize()
    {
        TemplateInstance.Initialize(false, true);
        if (m_refCounter == 0)
            TemplateInstance.gameObject.SetActive(false);
    }

    public int Release()
    {
        m_refCounter--;
        if (m_refCounter < 0)
            Debug.LogError("The reference count is " + m_refCounter + ". Object is released more often than it should.");

        if (m_refCounter == 0)
            TemplateInstance.gameObject.SetActive(false);
        return m_refCounter;
    }

    public int Aquire()
    {
        m_refCounter++;
        if (m_refCounter == 1)
            TemplateInstance.gameObject.SetActive(true);
        return m_refCounter;
    }
}