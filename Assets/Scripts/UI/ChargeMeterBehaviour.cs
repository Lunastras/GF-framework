using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class ChargeMeterBehaviour : MonoBehaviour
{
    [SerializeField]
    protected Slider m_sliderCharge = null;

    [SerializeField]
    protected Image m_imageColourBackground = null;

    [SerializeField]
    protected Text m_chargeText = null;

    [SerializeField]
    protected Vector2 m_chargePositionOffset = new Vector3(50, 50, 0);

    [SerializeField]
    protected float m_opacityFadeSmoothtime = 0.05f;

    [SerializeField]
    protected float m_positionSmoothTime = 0.2f;

    [SerializeField]
    protected Color[] m_levelColors;

    [SerializeField]
    protected Color m_topLevelColor1;

    [SerializeField]
    protected Color m_topLevelColor2;

    [SerializeField]
    protected float m_topLevelBlinkInterval = 0.1f;

    private bool m_showingTopColor2 = false;
    private float m_timeUntilTopLevelBlink = 0;

    protected Vector2 m_positionSmoothRef;

    protected CanvasGroup m_canvasGroup = null;

    protected bool m_wasShowingMeterLastFrame = false;

    protected Transform m_transform = null;
    protected int m_lastLevel = 0;

    protected Image m_fillImage = null;

    protected Color m_defaultBackgroundColour;

    protected Vector2 m_currentPosition;

    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
        m_canvasGroup = GetComponent<CanvasGroup>();
        if (null == m_sliderCharge) m_sliderCharge = GetComponent<Slider>();
        gameObject.SetActive(true);
        m_fillImage = m_sliderCharge.fillRect.GetComponent<Image>();
        m_canvasGroup.alpha = 0;
        m_defaultBackgroundColour = m_imageColourBackground.color;
        m_currentPosition = m_transform.position;

        m_sliderCharge.value = 0;
        m_chargeText.text = "0";
        m_fillImage.color = m_levelColors[0];
    }

    void LateUpdate()
    {
        List<WeaponGeneric> weapons = HudManager.GetWeapons();

        if (null != weapons && weapons.Count > 0)
        {
            WeaponChargeLevels weapon = weapons[0] as WeaponChargeLevels;
            if (weapon)
            {
                float nextLevelProgress = weapon.NextLevelProgress(1);
                int level = weapon.CurrentLevel(1);
                bool showChargeMeter = level > 0 || nextLevelProgress > 0;

                if (showChargeMeter != m_wasShowingMeterLastFrame)
                {
                    float opacity = 0;
                    if (showChargeMeter) opacity = 1;
                    GfUiTools.CrossFadeAlphaGroup(m_canvasGroup, opacity, m_opacityFadeSmoothtime);
                }

                m_wasShowingMeterLastFrame = showChargeMeter;

                Vector2 screenPosPlayer = GfLevelManager.GetPlayerPositionOnScreen();
                GfTools.Add2(ref screenPosPlayer, m_chargePositionOffset);
                m_currentPosition = Vector2.SmoothDamp(m_currentPosition, screenPosPlayer, ref m_positionSmoothRef, m_positionSmoothTime);

                if (m_canvasGroup.alpha > 0.02f)
                {

                    m_transform.position = m_currentPosition;
                    m_sliderCharge.value = nextLevelProgress;
                    if (m_chargeText && m_lastLevel != level)
                    {
                        m_chargeText.text = level.ToString();
                    }

                    m_lastLevel = level;

                    Color fillColor = m_fillImage.color;
                    Color backgroundColor = m_defaultBackgroundColour;

                    if (weapon.IsMaxLevel(WeaponPointsTypes.CHARGE))
                    {
                        m_timeUntilTopLevelBlink -= Time.unscaledDeltaTime;

                        if (m_timeUntilTopLevelBlink <= 0)
                        {
                            m_timeUntilTopLevelBlink = m_topLevelBlinkInterval;
                            m_showingTopColor2 = !m_showingTopColor2;
                        }

                        fillColor = m_topLevelColor1;
                        if (m_showingTopColor2) fillColor = m_topLevelColor2;
                    }
                    else
                    {
                        int colorIndex = System.Math.Min(m_levelColors.Length - 1, level);
                        fillColor = m_levelColors[colorIndex];
                        if (--colorIndex >= 0)
                            backgroundColor = m_levelColors[colorIndex];
                    }

                    m_fillImage.color = fillColor;
                    m_imageColourBackground.color = backgroundColor;
                }
            }
            else
            {
                m_sliderCharge.value = 0;
                m_chargeText.text = "0";
            }
        }
    }
}
