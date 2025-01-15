using System;
using UnityEngine;

public class CornManagerShop : MonoBehaviour
{
    private static CornManagerShop Instance;
    [SerializeField] private GameObject m_shopItemPrefab = null;
    [SerializeField] private Transform m_shopItemsParent = null;
    [SerializeField] private Transform m_prefabPreviewParent;
    [SerializeField] private float m_previewRotationSpeed = 5;

    private float m_currentRotation = 0;
    private bool m_initialized = false;
    void Awake()
    {
        this.SetSingleton(ref Instance);
    }

    void Update()
    {
        m_currentRotation += m_previewRotationSpeed * Time.deltaTime;
        m_prefabPreviewParent.localRotation = Quaternion.AngleAxis(m_currentRotation, Vector3.up);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!m_initialized)
        {
            Debug.Assert(m_shopItemPrefab);
            Debug.Assert(m_shopItemsParent);

            int shopItemsNotOwnedLength = CornManagerBalancing.GetShopItemsNotOwnedCount();
            Span<CornShopItem> shopItemsNotOwned = stackalloc CornShopItem[shopItemsNotOwnedLength];
            CornManagerBalancing.GetShopItemsNotOwned(ref shopItemsNotOwned);

            GfcPooling.DestroyChildren(m_shopItemsParent);

            for (int i = 0; i < shopItemsNotOwnedLength; i++)
            {
                CornShopItemButton button = Instantiate(m_shopItemPrefab).GetComponent<CornShopItemButton>();
                button.Button.Initialize();
                button.SetShopItem(shopItemsNotOwned[i]);
                button.Button.Index = i;
                button.transform.SetParent(m_shopItemsParent);
                button.transform.localPosition = new();
                button.transform.localScale = new(1, 1, 1);

                button.Button.OnButtonEventCallback += OnButtonEvent;
            }

            m_initialized = true;
        }
    }

    public static void UpdateCanAffordButtons()
    {
        Instance.Start();
        foreach (Transform child in Instance.m_shopItemsParent)
            child.GetComponent<CornShopItemButton>().UpdateCanAfford();
    }

    public static Transform GetPrefabPreviewParent() { return Instance.m_prefabPreviewParent; }

    private void OnButtonEvent(GfxButtonCallbackType aCallbackType, GfxButton aButton, bool aState)
    {
        CornShopItemButton shopButton = aButton.GetComponent<CornShopItemButton>();
        CornShopItem item = shopButton.GetShopItem();

        switch (aCallbackType)
        {
            case GfxButtonCallbackType.SELECT:
                if (aState)
                {
                    shopButton.SetPreview(true);
                    int price = CornManagerBalancing.GetShopItemData(item).Price;
                    CornMenuApartment.Instance.PreviewChange(CornPlayerConsumables.MONEY, price);
                }
                else
                {
                    shopButton.SetPreview(false);
                    CornMenuApartment.EndStatsPreview();
                }

                break;
            case GfxButtonCallbackType.SUBMIT:
                Destroy(aButton.gameObject);
                CornManagerEvents.PurchaseShopItem(item);
                break;
        }
    }
}