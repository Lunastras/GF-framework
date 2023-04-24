using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class WeaponLevelSlider : MonoBehaviour
{
    [SerializeField]
    private Slider m_slider = null;

    [SerializeField]
    private Text m_levelText = null;

    [SerializeField]
    private Text m_weaponCount = null;

    private int m_lastWeaponCount = 1;
    private int m_lastLevel = -999999;

    public void SetWeaponCount(int count)
    {
        if (m_weaponCount && m_lastWeaponCount != count)
        {
            m_lastWeaponCount = count;
            m_weaponCount.text = count.ToString();
        }
    }

    public void SetLevel(int level)
    {
        if (m_lastLevel != level)
        {
            // Debug.Log("I am setting level to be: " + level);
            m_lastLevel = level;
            m_levelText.text = level.ToString();
        }
    }

    public void SetProgress(float value)
    {
        m_slider.value = value;
    }
}
