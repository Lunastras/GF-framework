using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GfxButton))]
public class CornPlayerSaveDataButton : MonoBehaviour
{
    //[SerializeField] private Image m_iconImage;
    [SerializeField] private TextMeshProUGUI m_textDate;
    [SerializeField] private TextMeshProUGUI m_textSanity;
    [SerializeField] private TextMeshProUGUI m_textMoney;
    [SerializeField] private TextMeshProUGUI m_textEnergy;

    public GfxButton Button;

    void Awake()
    {
        Button = GetComponent<GfxButton>();
    }

    CornSaveData m_saveData;

    public void SetSaveData(CornSaveData aSaveData)
    {
        Debug.Assert(m_textDate);
        Debug.Assert(m_textSanity);
        Debug.Assert(m_textMoney);
        Debug.Assert(m_textEnergy);

        m_saveData = aSaveData;
        if (aSaveData != null)
        {
            GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;

            stringBuffer = stringBuffer + "Day " + (1 + aSaveData.DaysPassed) + " ";

            if (aSaveData.CurrentHour < 10)
                stringBuffer.Append('0');

            stringBuffer.Append(aSaveData.CurrentHour);
            stringBuffer.Append(":00   ");
            stringBuffer.Append(GfcLocalization.GetDateString(aSaveData.CurrentDay + 1, aSaveData.CurrentMonth));

            m_textDate.text = stringBuffer.GetStringCopy();
            m_textSanity.text = (stringBuffer.Clear() + "Sanity " + aSaveData.MentalSanity).GetStringCopy();
            m_textMoney.text = (stringBuffer.Clear() + (int)aSaveData.GetValue(CornPlayerConsumables.MONEY) + "$").GetStringCopy();
            m_textEnergy.text = (stringBuffer.Clear() + "Energy " + (aSaveData.GetValue(CornPlayerConsumables.ENERGY) * 100)).GetStringCopy();
            stringBuffer.Clear();
        }

        GetComponent<GfxButton>().SetInteractable(aSaveData != null);
    }
}