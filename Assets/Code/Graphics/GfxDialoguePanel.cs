using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfxDialoguePanel : MonoBehaviour
{
    public GfxImageInterpolator Portrait;

    public GfxNotifyPanelGeneric PanelGeneric;

    void Start()
    {
        PanelGeneric.OnTextWriteCallback += OnTextWriteInternal;
        PanelGeneric.OnNotificationFadeOutEnd += OnNotificationFadeOutEnd;
        PanelGeneric.OnNotificationFadeInStart += OnNotificationFadeInStart;
    }

    private void OnTextWriteInternal(GfxTextMessage aMessage, int aMessageIndex)
    {
        Portrait.SetSprite(GfxCharacterPortraits.GetPortrait(aMessage.Character));
    }

    private void OnNotificationFadeOutEnd()
    {
        Portrait.SetSprite(null);
    }

    public void OnNotificationFadeInStart()
    {
        Portrait.SetSprite(GfxCharacterPortraits.GetPortrait(PanelGeneric.GetTextMessage(0).Character), true);
    }
}
