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

    public void SetWeaponCount(int count)
    {
        if (weaponCount != null)
        {
            weaponCount.text = count.ToString();
        }
    }

    public void SetLevelText(int level)
    {
        if (levelText != null)
            levelText.text = level.ToString();
    }

    public void SetProgress(float value)
    {
        if (slider != null)
        {
            slider.value = value;
        }
    }
}
