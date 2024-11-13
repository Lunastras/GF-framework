using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class GfxChatMessage : MonoBehaviour
{
    [SerializeField] private RectTransform m_rectTransform;
    [SerializeField] private TextMeshProUGUI m_tmpMain;
    [SerializeField] private TextMeshProUGUI m_tmpName;
    [SerializeField] private Image m_icon;
    [SerializeField] private Image m_iconMask;

    private bool m_useIcon;

    void Awake()
    {
        Debug.Assert(m_tmpMain);
        Debug.Assert(m_tmpName);
        Debug.Assert(m_icon);
        Debug.Assert(m_iconMask);
        TryGetComponent(out m_rectTransform);
        Debug.Assert(m_rectTransform.localScale.x == 1);
    }

    public void SetMessage(GfxTextMessage aMessage, bool anUseName, bool anUseIcon, bool aHiddenIcon)
    {
        m_useIcon = anUseIcon;
        m_tmpMain.text = aMessage.MainText;
        m_tmpName.text = aMessage.Name;

        m_tmpName.gameObject.SetActive(anUseName);
        m_icon.gameObject.SetActive(!aHiddenIcon && anUseIcon);
        m_iconMask.gameObject.SetActive(!aHiddenIcon && anUseIcon);

        m_icon.sprite = GfxCharacterPortraits.GetPortraitData(aMessage.Character).PhoneSprite;
    }

    public void SetFlipped(bool aFlipped)
    {
        Vector3 localScale = m_tmpMain.transform.localScale;
        localScale.x = aFlipped ? -1 : 1;
        m_tmpMain.transform.localScale = localScale;

        localScale = m_tmpName.transform.localScale;
        localScale.x = aFlipped ? -1 : 1;
        m_tmpName.transform.localScale = localScale;

        localScale = transform.localScale;
        localScale.x = aFlipped ? -1 : 1;
        transform.localScale = localScale;
    }

    public float GetImageSizeWithPadding()
    {
        if (!m_useIcon)
            return 0;

        RectTransform imgRect = m_iconMask.GetComponent<RectTransform>();
        return imgRect.GetProperSize().x - 2 * imgRect.anchoredPosition.x;
    }
}