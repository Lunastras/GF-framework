using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfgNotifyPanelGeneric : GfxNotifyPanelInteractable
{
    [SerializeField] protected GfgDoubleTextWriter m_mainTextWriter;

    [SerializeField] protected GfgDoubleTextWriter m_nameTextWriter;

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
            m_nameTextWriter.WriteString(m_messagesBuffer[m_messageBufferIndex].Name, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);

        m_mainTextWriter.WriteString(m_messagesBuffer[m_messageBufferIndex].MainText, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
    }

    protected override void FlushCurrentMessage()
    {
        m_nameTextWriter.ForceWriteText();
        m_mainTextWriter.ForceWriteText();
    }

    protected override void EraseMessages()
    {
        m_nameTextWriter.EraseText(m_speedMultiplierTextErase);
        m_mainTextWriter.EraseText(m_speedMultiplierTextErase);
    }

    public override bool DrawingMessage() { return m_nameTextWriter.WritingText() || m_mainTextWriter.WritingText(); }
}