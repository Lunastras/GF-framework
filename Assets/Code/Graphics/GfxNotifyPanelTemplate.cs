using MEC;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

public abstract class GfxNotifyPanelTemplate : MonoBehaviour
{
    protected GfcCoroutineHandle m_currentNotifyCoroutine = default;

    protected List<GfxTextMessage> m_messagesBuffer = new(8);

    public Action<GfxTextMessage, int> OnTextWriteCallback;

    public Action OnNotificationFadeInStart;

    public Action OnNotificationFadeOutStart;

    public Action OnNotificationFadeOutEnd;

    public void QueueMessage(string aMessage) { m_messagesBuffer.Add(new(aMessage)); }
    public void QueueMessage(IEnumerable<string> someMessages) { foreach (string message in someMessages) m_messagesBuffer.Add(new(message)); }
    public void QueueMessage(GfxTextMessage aMessage) { m_messagesBuffer.Add(aMessage); }
    public void QueueMessage(IEnumerable<GfxTextMessage> someMessages) { foreach (GfxTextMessage message in someMessages) m_messagesBuffer.Add(message); }

    public CoroutineHandle DrawMessage(string aMessage) { QueueMessage(aMessage); return DrawMessages(); }
    public CoroutineHandle DrawMessage(IEnumerable<string> someMessages) { QueueMessage(someMessages); return DrawMessages(); }
    public CoroutineHandle DrawMessage(GfxTextMessage aMessage) { QueueMessage(aMessage); return DrawMessages(); }
    public CoroutineHandle DrawMessage(IEnumerable<GfxTextMessage> someMessages) { QueueMessage(someMessages); return DrawMessages(); }

    public bool IsShowingMessages() { return m_currentNotifyCoroutine.CoroutineIsRunning; }

    public GfcCoroutineHandle GetCoroutineHandle() { return m_currentNotifyCoroutine; }

    public virtual CoroutineHandle DrawMessages()
    {
        CoroutineHandle handle = default;
        if (m_messagesBuffer.Count > 0)
            handle = m_currentNotifyCoroutine.RunCoroutineIfNotRunning(_DrawMessages());

        return handle;
    }

    public virtual void ClearVisuals() { }

    protected abstract IEnumerator<float> _DrawMessages();
}