using UnityEngine;

public class CornMenuSaveData : MonoBehaviour
{
    private static CornMenuSaveData Instance;
    [SerializeField] private GameObject m_uiButtonPrefab = null;
    [SerializeField] private Transform m_uiButtonParent = null;

    private bool m_initialized = false;

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

            CornSaveData[] backups = GfgManagerSaveData.GetActivePlayerSaveData().DataBackups;
            GfcPooling.DestroyChildren(m_uiButtonParent);

            for (int i = 0; i < backups.Length; i++)
            {
                CornPlayerSaveDataButton button = Instantiate(m_uiButtonPrefab).GetComponent<CornPlayerSaveDataButton>();
                button.Button.Initialize();
                button.Button.Index = i;
                button.transform.SetParent(m_uiButtonParent);
                button.transform.localPosition = new();
                button.transform.localScale = new(1, 1, 1);

                button.Button.OnButtonEventCallback += OnButtonEvent;
            }

            m_initialized = true;
        }
    }

    private void UpdateButtons()
    {
        if (!m_initialized)
            return;

        var playerProfile = GfgManagerSaveData.GetActivePlayerSaveData();
        //playerProfile.ValidateSaveFile(false);
        CornSaveData[] backups = playerProfile.DataBackups;

        int index = 0;
        foreach (Transform child in m_uiButtonParent)
        {
            if (backups[index] != null)
            {
                child.gameObject.SetActive(true);
                child.GetComponent<CornPlayerSaveDataButton>().SetSaveData(backups[index]);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
            index++;
        }
    }

    private void OnEnable()
    {
        UpdateButtons();
    }

    public static void UpdateCanAffordButtons()
    {
        Instance.Start();
        foreach (Transform child in Instance.m_uiButtonParent)
            child.GetComponent<CornShopItemButton>().UpdateCanAfford();
    }

    private void OnButtonEvent(GfxButtonCallbackType aCallbackType, GfxButton aButton, bool aState)
    {
        switch (aCallbackType)
        {
            case GfxButtonCallbackType.SELECT:
                break;
            case GfxButtonCallbackType.SUBMIT:
                GfgManagerSaveData.GetActivePlayerSaveData().SetBackupDataUsed(aButton.Index);
                GfgManagerSceneLoader.LoadScene(GfcSceneId.APARTMENT);
                break;
        }
    }
}