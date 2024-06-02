using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfxDialoguePanel : MonoBehaviour
{
    public GfxImageInterpolator Portrait;

    public GfxNotifyPanelGeneric PanelGeneric;

    private bool m_firstMessageSinceFadeIn = false;

    void Start()
    {
        PanelGeneric.OnTextWriteCallback += OnTextWriteInternal;
        PanelGeneric.OnNotificationFadeOutEnd += OnNotificationFadeOutEnd;
        PanelGeneric.OnNotificationFadeInStart += OnNotificationFadeInStart;
    }

    private void OnTextWriteInternal(GfxTextMessage aMessage, int aMessageIndex, bool aSkipping)
    {
        Portrait.SetSprite(GfxCharacterPortraits.GetPortrait(aMessage.Character), m_firstMessageSinceFadeIn);
        m_firstMessageSinceFadeIn = false;
    }

    private void OnNotificationFadeOutEnd()
    {
        Portrait.SetSprite(null);
    }

    public void OnNotificationFadeInStart()
    {
        m_firstMessageSinceFadeIn = true;
    }
}
