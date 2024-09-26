using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MEC;

public class GfgLevelEndScreen : MonoBehaviour
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

    protected void Start()
    {
        m_levelEndGroup.alpha = 0;
    }

    public void EnableEndScreen()
    {
        float levelTimer = GfgManagerLevel.GetSecondsSinceStart();
        int killsCount = GfgManagerLevel.GetCurrentKillCount();
        int resetsCount = GfgManagerLevel.GetResetsCount();

        int levelMinutes = (int)(levelTimer / 60.0f);
        float levelSeconds = levelTimer - 60 * levelMinutes;

        m_killsText.text = killsCount.ToString();
        m_timeSecondsText.text = levelMinutes + ":" + levelSeconds.ToString(TWO_DECIMAL_PRECISSION);
        m_resetsText.text = resetsCount.ToString();
        m_levelEndGroup.CrossFadeAlphaGf(1, m_fadeInTime, true);
    }
}