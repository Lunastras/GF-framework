using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
public class GfxNotifyPanelChat : GfxNotifyPanelInteractable
{
    [SerializeField] protected RectTransform m_messagesParent;

    [SerializeField] protected GameObject m_messagePrefab;

    [SerializeField] protected float m_messagesPadding = 5;

    [SerializeField] protected bool m_drawIconForProtag = true;

    protected CanvasGroup m_canvasGroup;

    protected GfcTransitionActive m_currentMessageTransitionActive = null;

    void Awake()
    {
        if (m_messagesParent == null) m_messagesParent = transform as RectTransform;
        m_canvasGroup = GetComponent<CanvasGroup>();
        Debug.Assert(m_messagePrefab);
    }

    protected override void OnDrawMessagesStart()
    {
        GfcPooling.DestroyChildren(m_messagesParent);
    }

    protected override void DrawCurrentMessage()
    {
        bool protagMessage = m_messagesBuffer[m_messageBufferIndex].Character == GfxCharacterPortraits.GetProtag();
        bool newName = m_messageBufferIndex == 0 || m_messagesBuffer[m_messageBufferIndex - 1].Name != m_messagesBuffer[m_messageBufferIndex].Name;

        GfxChatMessage chatMessage = GfcPooling.Instantiate(m_messagePrefab).GetComponent<GfxChatMessage>();
        chatMessage.SetMessage(m_messagesBuffer[m_messageBufferIndex], newName && !protagMessage, !protagMessage || m_drawIconForProtag, !newName);
        chatMessage.SetFlipped(protagMessage);

        chatMessage.GetComponent(ref m_currentMessageTransitionActive);


        Canvas.ForceUpdateCanvases();

        RectTransform messageTransform = chatMessage.GetComponent<RectTransform>();
        Vector2 messageSize = messageTransform.sizeDelta;
        messageTransform.SetParent(m_messagesParent, false);
        messageTransform.localPosition =
        new((protagMessage ? 1 : 0) * m_messagesParent.GetProperSize().x + (protagMessage ? -1 : 1) * (chatMessage.GetImageSizeWithPadding() + m_messagesPadding), 0);
    }

    protected override void FlushCurrentMessage()
    {
        if (m_currentMessageTransitionActive) m_currentMessageTransitionActive.Transition.FinishTransition();
    }

    protected override void EraseMessages() { }

    public override bool DrawingMessage()
    {
        return m_currentMessageTransitionActive != null && m_currentMessageTransitionActive.Transition.Transitioning();
    }

    protected override void TransitionBox(float aTimeFactor, bool aFadeIn, bool aHasName)
    {
        m_canvasGroup.alpha = aTimeFactor;
    }
}