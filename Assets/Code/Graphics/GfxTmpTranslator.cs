using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(TextMeshProUGUI))]
public class GfxTmpTranslator : MonoBehaviour
{
    private TextMeshProUGUI m_tmp;
    private GfcLocalizedString m_localizedString;
    [SerializeField] private GfcLocalizationStringTableType m_localizationTable;
    [SerializeField] private string m_localizationKey;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.GetComponent(ref m_tmp);
        m_localizedString = new(m_tmp.text, m_localizationTable, m_localizationKey);
        m_tmp.text = m_localizedString;
        LocalizationSettings.Instance.OnSelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    protected void OnSelectedLocaleChanged(Locale aNewLocale)
    {
        m_tmp.text = m_localizedString;
    }
}
