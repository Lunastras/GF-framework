using UnityEngine;

public class CornMenuProfileSelect : MonoBehaviour
{
    private static CornMenuProfileSelect Instance;
    [SerializeField] private GameObject m_uiButtonPrefab = null;
    [SerializeField] private Transform m_uiButtonParent = null;

    private bool m_initialized = false;

    private GfgPlayerSaveData[] m_saves;

    void Awake()
    {
        this.SetSingleton(ref Instance);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!m_initialized)
        {
            Debug.Assert(m_uiButtonPrefab);
            Debug.Assert(m_uiButtonParent);

            m_saves = GfgManagerSaveData.GetPlayerSaveDatasCopy();
            GfcPooling.DestroyChildren(m_uiButtonParent);

            for (int i = 0; i < m_saves.Length; i++)
            {
                CornPlayerSaveProfileButton button = Instantiate(m_uiButtonPrefab).GetComponent<CornPlayerSaveProfileButton>();
                button.Button.Initialize();
                button.Button.Index = i;
                button.transform.SetParent(m_uiButtonParent);
                button.transform.localPosition = new();
                button.transform.localScale = new(1, 1, 1);
                button.SetSaveData(m_saves[i]);
                button.Button.OnButtonEventCallback += OnButtonEvent;
            }

            m_initialized = true;
        }
    }

    private void OnButtonEvent(GfxButtonCallbackType aCallbackType, GfxButton aButton, bool aState)
    {
        switch (aCallbackType)
        {
            case GfxButtonCallbackType.SELECT:
                break;
            case GfxButtonCallbackType.SUBMIT:
                if (m_saves[aButton.Index] == null)
                    m_saves[aButton.Index] = new("Cool Name(todo add name select)");

                GfgManagerSaveData.SetActivePlayerSaveData(m_saves[aButton.Index], aButton.Index);
                GfgManagerSceneLoader.LoadScene(GfcSceneId.APARTMENT);
                break;
        }
    }
}