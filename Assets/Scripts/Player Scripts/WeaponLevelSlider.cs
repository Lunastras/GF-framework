using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class WeaponLevelSlider : MonoBehaviour
{
    public WeaponBasic weaponBasic { get; set; }

    [SerializeField]
    private Slider slider = null;

    [SerializeField]
    private Text levelText = null;

    [SerializeField]
    private Text weaponCount = null;

    private int m_lastWeaponCount = 1;
    private int m_lastLevel = -999999;

    public void SetWeaponCount(int count)
    {
        if (weaponCount && m_lastWeaponCount != count)
        {
            m_lastWeaponCount = count;
            weaponCount.text = count.ToString();
        }
    }

    public void SetLevel(int level)
    {
        if (m_lastLevel != level)
        {
            // Debug.Log("I am setting level to be: " + level);
            m_lastLevel = level;
            levelText.text = level.ToString();
        }
    }

    public void SetProgress(float value)
    {
        if (slider.value != value)
        {
            // Debug.Log("I am setting PROGRESS to: " + value);
            slider.value = value;
        }
    }
}
