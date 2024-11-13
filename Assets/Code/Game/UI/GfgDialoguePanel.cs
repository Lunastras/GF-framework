using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgDialoguePanel : MonoBehaviour
{
    public GfxImageInterpolator Portrait;

    public GfxNotifyPanelGeneric PanelGeneric;

    void Start()
    {
        PanelGeneric.OnDrawMessageCallback += OnTextWriteInternal;
        PanelGeneric.OnNotificationFadeOutEnd += OnNotificationFadeOutEnd;
        PanelGeneric.OnNotificationFadeInStart += OnNotificationFadeInStart;
    }

    private void OnTextWriteInternal(GfxTextMessage aMessage, int aMessageIndex)
    {
        Portrait.SetSprite(GfxCharacterPortraits.GetPortraitData(aMessage.Character).MainSprite);
    }

    private void OnNotificationFadeOutEnd()
    {
        Portrait.SetSprite(null);
    }

    public void OnNotificationFadeInStart()
    {
        Portrait.SetSprite(GfxCharacterPortraits.GetPortraitData(PanelGeneric.GetTextMessage(0).Character).MainSprite, true);
    }
}
