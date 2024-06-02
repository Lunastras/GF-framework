using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GfxSliderText : MonoBehaviour
{
    [SerializeField] private Slider m_sliderMain;
    [SerializeField] private TextMeshProUGUI m_textValue;

    [SerializeField] private float m_previewFlashInterval = 2;

    [SerializeField] private float m_previewHold = 1;

    [SerializeField] private float m_valueTextMultiplier = 100; //used to not write a value between 0 and 1 on the text

    [SerializeField] private Color m_sliderPositiveColor = Color.green;

    [SerializeField] private Color m_sliderNegativeColor = Color.red;


    [Header("Optional Preview Values")]
    [Tooltip(" If the user wants to auto duplicate the slider and text (leaving these fields empty), then the slider and text need to be sepparate, they can't be under each other's hierarchy")][SerializeField] private bool m_autoGenerateIfNull = true;
    [SerializeField] private Slider m_sliderBackground;

    [SerializeField] private TextMeshProUGUI m_textValuePreview;

    [SerializeField] private TextMeshProUGUI m_textName;

    private Image m_sliderBackgroundImage = null;

    private Image m_sliderMainImage = null;

    private Color m_originalTextColor;

    private readonly Color TRANSPARENT = new Color(0, 0, 0, 0);

    private float m_lastValue = 0;

    private bool m_isPreviewing = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (m_sliderMain == null) m_sliderMain = GetComponent<Slider>();
        if (m_textValue == null) m_textValue = GetComponent<TextMeshProUGUI>();
        Debug.Assert(m_valueTextMultiplier != 0, "Some schmuck face passed 0 wtf " + gameObject.name);

        if (m_sliderMain)
        {
            m_sliderMainImage = m_sliderMain.fillRect.GetComponent<Image>();
            m_lastValue = m_sliderMain.value;
        }

        if (m_sliderBackground == null && m_autoGenerateIfNull && m_sliderMain)
        {
            Transform sliderParent = m_sliderMain.transform.parent;
            m_sliderBackground = Instantiate(m_sliderMain.gameObject, sliderParent, false).GetComponent<Slider>();

            m_sliderBackgroundImage = m_sliderBackground.fillRect.GetComponent<Image>();
            m_sliderBackgroundImage.color = m_sliderPositiveColor;

            m_sliderBackground.transform.SetAsFirstSibling();

            m_originalTextColor = m_textValue.color;

            Debug.Assert(m_sliderBackground.handleRect == null, "I am not sure the auto duplicate works with sliders that have a handle...");
        }

        if (m_textValuePreview == null && m_autoGenerateIfNull && m_textValue)
        {
            Transform textParent = m_textValue.transform.parent;
            m_textValuePreview = Instantiate(m_textValue.gameObject, textParent, false).GetComponent<TextMeshProUGUI>();
        }
    }

    void Start()
    {
        if (m_sliderMain) SetSliderValue(m_sliderMain.value);
    }

    public void EndPreview()
    {
        m_isPreviewing = false;

        if (m_sliderMain)
        {
            m_sliderMain.value = m_lastValue;
        }

        if (m_textValue) m_textValue.color = m_originalTextColor;

        if (m_textValuePreview) m_textValuePreview.text = null;
        if (m_sliderBackground) m_sliderBackground.value = 0;
    }

    public void SetSliderValue(float aValue)
    {
        m_sliderMain.value = aValue;

        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
        m_textValue.text = ((int)(aValue * m_valueTextMultiplier).Round()).ToString();
        stringBuffer.Clear();
        m_lastValue = m_sliderMain.value;

        EndPreview();
    }

    public void Preview(float aPreviewValue)
    {
        if (!Mathf.Approximately(aPreviewValue, m_sliderMain.value))
        {
            EndPreview();

            if (aPreviewValue > m_sliderMain.value)
            {
                m_sliderBackground.value = aPreviewValue;
                m_sliderBackgroundImage.color = m_sliderPositiveColor;
            }
            else //if (aValue < m_sliderMain.value), will always be true at this point
            {
                m_sliderBackgroundImage.color = m_sliderNegativeColor;
                m_sliderBackground.value = m_sliderMain.value;
                m_sliderMain.value = aPreviewValue;
            }

            int aNum = (int)(aPreviewValue * m_valueTextMultiplier).Round().Clamp(0, m_valueTextMultiplier);
            m_textValuePreview.text = aNum.ToString();
            m_textValuePreview.color = m_sliderBackgroundImage.color;
            m_textValue.color = TRANSPARENT;
            m_isPreviewing = true;
        }
        //do nothing if they are equal
    }

    public void PreviewChange(float aChange) { Preview(m_sliderMain.value + aChange); }

    public void SetName(string aName)
    {
        m_textName.text = aName;
    }
}
