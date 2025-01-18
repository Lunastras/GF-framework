using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(GfxButton))]
public class CornPlayerSaveProfileButton : MonoBehaviour
{
    //[SerializeField] private Image m_iconImage;
    [SerializeField] private TextMeshProUGUI m_textName;
    [SerializeField] private TextMeshProUGUI m_textCreationDate;

    [SerializeField] private TextMeshProUGUI m_textDate;
    [SerializeField] private TextMeshProUGUI m_textSanity;
    [SerializeField] private TextMeshProUGUI m_textMoney;
    [SerializeField] private TextMeshProUGUI m_textEnergy;
    [SerializeField] private TextMeshProUGUI m_textPlayTime;
    [SerializeField] private TextMeshProUGUI m_textPlayTimeElapsedTime;

    public GfxButton Button { get; private set; }

    void Awake()
    {
        Button = GetComponent<GfxButton>();
    }

    public void SetSaveData(GfgPlayerSaveData aSaveData)
    {
        if (aSaveData == null)
        {
            m_textName.text = "New save";
            m_textCreationDate.text = null;

            m_textDate.text = null;
            m_textMoney.text = null;
            m_textEnergy.text = null;
            m_textSanity.text = null;
            m_textPlayTimeElapsedTime.text = null;
            m_textPlayTime.gameObject.SetActive(false);
        }
        else
        {
            m_textPlayTime.gameObject.SetActive(true);
            var cornData = aSaveData.Data;
            m_textName.text = aSaveData.GetName();
            DateTime timeOfCreation = GfcTools.GetLocalDateTimeFromUnixUtc(aSaveData.GetUnixTimeOfCreation());
            m_textCreationDate.text = timeOfCreation.ToString();

            if (aSaveData != null)
            {
                ShortTimeSpan timeSpan = default;
                timeSpan.AddSeconds((int)aSaveData.SecondsPlayed);
                m_textPlayTimeElapsedTime.text = timeSpan.ToString();

                GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;

                stringBuffer = stringBuffer.Clear() + "Day " + (1 + cornData.DaysPassed) + " ";

                if (cornData.CurrentHour < 10)
                    stringBuffer.Append('0');

                stringBuffer.Append(cornData.CurrentHour);
                stringBuffer.Append(":00   ");
                stringBuffer.Append(GfcLocalization.GetDateString(cornData.CurrentDay + 1, cornData.CurrentMonth));

                m_textDate.text = stringBuffer.GetStringCopy();
                m_textSanity.text = (stringBuffer.Clear() + "Sanity " + cornData.MentalSanity).GetStringCopy();
                m_textMoney.text = (stringBuffer.Clear() + (int)cornData.GetValue(CornPlayerConsumables.MONEY) + "$").GetStringCopy();
                m_textEnergy.text = (stringBuffer.Clear() + "Energy " + (cornData.GetValue(CornPlayerConsumables.ENERGY) * 100)).GetStringCopy();
                stringBuffer.Clear();
            }
        }
    }
}

public struct ShortTimeSpan
{
    public void AddSeconds(int anElapsedSeconds)
    {
        int hours = anElapsedSeconds / 3600;
        anElapsedSeconds -= 3600 * hours;
        int minutes = anElapsedSeconds / 60;
        anElapsedSeconds -= 60 * minutes;

        Seconds += anElapsedSeconds;
        Minutes += minutes;
        Hours += hours;
    }

    public void AddMinutes(int anElapsedMinutes)
    {
        int hours = anElapsedMinutes / 60;
        anElapsedMinutes -= 60 * hours;

        Minutes += anElapsedMinutes;
        Hours += hours;
    }

    public int Seconds;
    public int Minutes;
    public int Hours;

    public readonly override string ToString()
    {
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;

        if (Hours < 10)
            stringBuffer += 0;

        stringBuffer = stringBuffer + Hours + ':';

        if (Minutes < 10)
            stringBuffer += 0;

        stringBuffer = stringBuffer + Minutes + ':';

        if (Seconds < 10)
            stringBuffer += 0;

        stringBuffer += Seconds;

        string finalString = stringBuffer.GetStringCopy();
        stringBuffer.Clear();
        return finalString;
    }
}