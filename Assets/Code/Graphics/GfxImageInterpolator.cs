using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class GfxImageInterpolator : MonoBehaviour
{
    public AnimationCurve AnimationCurve = null;

    public Image MainImage = null;

    private Image m_secondaryImage = null;

    public float FadeTime = 0.5f;

    public bool IgnoreTimeScale = false;

    private void Start()
    {
        m_secondaryImage = Instantiate(MainImage.gameObject, MainImage.transform.parent, false).GetComponent<Image>();
        SetSprite(null, true);
    }

    public void SnapToDesiredState()
    {
        m_secondaryImage.SetAlpha(0);
        MainImage.SetAlpha(MainImage.sprite ? 1 : 0);
    }

    public void SetSprite(Sprite aSprite, bool aSnapToDesiredState = false)
    {
        if (aSnapToDesiredState)
        {
            MainImage.sprite = aSprite;
            SnapToDesiredState();
        }
        else if (aSprite != MainImage.sprite)
        {
            m_secondaryImage.color = MainImage.color;
            m_secondaryImage.sprite = MainImage.sprite;
            MainImage.sprite = aSprite;

            if (m_secondaryImage.sprite != null)
            {
                m_secondaryImage.SetAlpha(m_secondaryImage.sprite ? 1 : 0);
                m_secondaryImage.CrossFadeAlphaGf(0, FadeTime, IgnoreTimeScale, AnimationCurve);
            }
            else
            {
                m_secondaryImage.SetAlpha(0);
            }

            MainImage.SetAlpha(0);

            if (MainImage.sprite != null)
                MainImage.CrossFadeAlphaGf(1, FadeTime, IgnoreTimeScale, AnimationCurve);
        }
    }
}
