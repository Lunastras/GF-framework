using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MEC;

public class LevelEndScreen : MonoBehaviour
{
    [SerializeField]
    protected float m_fadeInTime = 1;

    [SerializeField]
    protected CanvasGroup m_levelEndGroup = null;

    [SerializeField]
    protected TMP_Text m_killsText = null;

    [SerializeField]
    protected TMP_Text m_timeSecondsText = null;

    [SerializeField]
    protected TMP_Text m_resetsText = null;

    const string TWO_DECIMAL_PRECISSION = "F2";

    public void EnableEndScreen()
    {
        float levelTimer = GfManagerLevel.GetSecondsSinceStart();
        int killsCount = GfManagerLevel.GetCurrentKillCount();
        int resetsCount = GfManagerLevel.GetResetsCount();

        int levelMinutes = (int)(levelTimer / 60.0f);
        float levelSeconds = levelTimer - 60 * levelMinutes;

        m_killsText.text = killsCount.ToString();
        m_timeSecondsText.text = levelMinutes + ":" + levelSeconds.ToString(TWO_DECIMAL_PRECISSION);
        m_resetsText.text = resetsCount.ToString();

        GfUiTools.CrossFadeAlphaGroup(m_levelEndGroup, 1, m_fadeInTime, true);
    }
}
