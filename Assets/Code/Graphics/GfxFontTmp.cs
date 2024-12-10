using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class GfxFontTmp : MonoBehaviour
{
    [SerializeField] private GfxFontType m_fontType = GfxFontType.PARAGRAPH;
    [SerializeField] private GfxFontFamily m_fontFamily = GfxFontFamily.MAIN;

    void Start()
    {
        GetComponent<TextMeshProUGUI>().font = GfxFont.GetFont(m_fontType, m_fontFamily);
        Destroy(this);
    }
}
