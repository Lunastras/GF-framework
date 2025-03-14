using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GfxButton))]
public class CornShopItemButton : MonoBehaviour
{
    [SerializeField] private Image m_iconImage;
    [SerializeField] private TextMeshProUGUI m_textName;
    [SerializeField] private TextMeshProUGUI m_textDescription;
    [SerializeField] private TextMeshProUGUI m_textPrice;
    [SerializeField] private TextMeshProUGUI m_textDeliveryTime;
    [SerializeField] private TextMeshProUGUI m_textPersonalNeeds;
    [SerializeField] private TextMeshProUGUI m_textBonuses;
    private CornShopItem m_item;
    private GameObject m_instantiatedPreviewPrefab;

    public GfxButton Button { get; private set; }
    private bool m_printedNullError = false;
    public CornShopItem GetShopItem() { return m_item; }

    void Awake()
    {
        Button = GetComponent<GfxButton>();
    }

    public void SetShopItem(CornShopItem anItem)
    {
        m_item = anItem;
        CornShopItemsData itemData = CornManagerBalancing.GetShopItemData(anItem);
        m_iconImage.sprite = itemData.Icon;
        m_textName.text = new GfcLocalizedString(itemData.Name, GfcLocalizationStringTableType.MISC).String;
        //m_textDescription.text = new GfcLocalizedString(itemData.Description, GfcLocalizationStringTable.MISC).String;
        m_textDeliveryTime.text = itemData.DeliveryDays.ToString();
        m_textPersonalNeeds.text = (itemData.PersonalNeedsPoints * 100).Round().ToString();
        m_textPrice.text = itemData.Price.ToString();
        m_textBonuses.text = null; //todo
        UpdateCanAfford();
    }

    public void UpdateCanAfford()
    {
        Button.Initialize();
        CornShopItemsData itemData = CornManagerBalancing.GetShopItemData(m_item);
        bool canAfford = GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(CornPlayerConsumables.MONEY, -itemData.Price);
        Button.SetInteractable(canAfford, "Not enough money");
    }

    public void SetPreview(bool anActive)
    {
        if (anActive)
        {
            if (m_instantiatedPreviewPrefab == null)
            {
                CornShopItemsData itemData = CornManagerBalancing.GetShopItemData(m_item);
                if (itemData.Prefab)
                {
                    Debug.Assert(itemData.PreviewScale.Abs() > 0.0001f);
                    m_instantiatedPreviewPrefab = Instantiate(itemData.Prefab);
                    m_instantiatedPreviewPrefab.transform.SetParent(CornManagerShop.GetPrefabPreviewParent(), false);
                    m_instantiatedPreviewPrefab.transform.SetLocalPositionAndRotation(new(), Quaternion.identity);
                    m_instantiatedPreviewPrefab.transform.localScale = new(itemData.PreviewScale, itemData.PreviewScale, itemData.PreviewScale);
                }
                else if (!m_printedNullError)
                {
                    m_printedNullError = true;
                    Debug.LogError("Tried to instantiate the prefab for item shop " + m_item + ", but it's null.");
                }
            }
            else
            {
                m_instantiatedPreviewPrefab.SetActive(true);
            }
        }
        else if (m_instantiatedPreviewPrefab)
            m_instantiatedPreviewPrefab.SetActive(false);
    }

    void OnDestroy()
    {
        Destroy(m_instantiatedPreviewPrefab);
    }
}