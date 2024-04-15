using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUIBehaviour : MonoBehaviour
{
    [SerializeField]
    private Slider m_slider = null;

    [SerializeField]
    private Text m_healthText = null;

    private float m_lastHp = -999999;

    private float m_lastMaxHp = -999999;

    public void SetMaxHealth(float maxHp)
    {
        if (m_lastMaxHp != maxHp)
        {
            m_lastMaxHp = maxHp;
            if (maxHp != 0)
                SetHpPercent(m_lastHp / maxHp);
            else
                SetHpPercent(0);
        }
    }

    public void SetHealthPoints(float hp)
    {
        if (m_lastHp != hp)
        {
            m_lastHp = hp;

            if (m_lastMaxHp != 0)
                SetHpPercent(m_lastHp / m_lastMaxHp);
            else
                SetHpPercent(0);

            m_healthText.text = hp.ToString();
        }
    }

    public void SetHealthText(string text)
    {
        m_healthText.text = text;
    }

    public void SetHpPercent(float value)
    {
        m_slider.value = value;
    }
}
