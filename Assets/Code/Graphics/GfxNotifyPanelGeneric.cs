using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfxNotifyPanelGeneric : GfxNotifyPanelInteractable
{
    [SerializeField] protected GfxDoubleTextWriter m_mainTextWriter;

    [SerializeField] protected GfxDoubleTextWriter m_nameTextWriter;

    void Awake()
    {
        Debug.Assert(m_mainTextWriter);
        Debug.Assert(m_nameTextWriter);
    }

    protected override void OnDrawMessagesStart()
    {
        m_nameTextWriter.RemoveText();
        m_mainTextWriter.RemoveText();
    }

    protected override void DrawCurrentMessage()
    {
        if (m_messageBufferIndex == 0 || m_messagesBuffer[m_messageBufferIndex - 1].Name != m_messagesBuffer[m_messageBufferIndex].Name)
            m_nameTextWriter.WriteStringAnimation(m_messagesBuffer[m_messageBufferIndex].Name, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);

        m_mainTextWriter.WriteStringAnimation(m_messagesBuffer[m_messageBufferIndex].MainText, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
    }

    protected override void FlushCurrentMessage()
    {
        m_nameTextWriter.ForceWriteText();
        m_mainTextWriter.ForceWriteText();
    }

    protected override void EraseMessages()
    {
        m_nameTextWriter.EraseTextAnimation(m_speedMultiplierTextErase);
        m_mainTextWriter.EraseTextAnimation(m_speedMultiplierTextErase);
    }

    public override bool DrawingMessage() { return m_nameTextWriter.WritingText() || m_mainTextWriter.WritingText(); }
}