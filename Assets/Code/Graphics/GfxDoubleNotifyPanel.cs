using MEC;
using System.Collections.Generic;
using UnityEngine;

public class GfxDoubleNotifyPanel : GfxNotifyPanelTemplate
{
    [SerializeField] private GfxNotifyPanelTemplate m_mainNotifyPanel;

    [SerializeField] private bool m_mainNotifyPanelIsPrefab;

    private GfxNotifyPanelTemplate m_secondaryNotifyPanel;

    private int m_messageBufferIndex = 0;

    private List<GfxTextMessage> m_messagesCopyBuffer = new(16);

    private bool m_waitingForNotifyPanelToEnd = false;

    void Awake()
    {
        if (m_mainNotifyPanelIsPrefab)
        {
            Instantiate(m_mainNotifyPanel.gameObject).GetComponent(ref m_mainNotifyPanel);
            m_mainNotifyPanel.transform.SetParent(transform, false);
        }

        Instantiate(m_mainNotifyPanel).GetComponent(ref m_secondaryNotifyPanel);
        m_secondaryNotifyPanel.transform.SetParent(m_mainNotifyPanel.transform.parent, false);
    }

    protected override IEnumerator<float> _DrawMessages()
    {
        if (m_messagesBuffer.Count == 0)
            yield break;

        m_messageBufferIndex = 0;

        while (m_messageBufferIndex < m_messagesBuffer.Count)
        {
            m_messagesCopyBuffer.Clear();

            do
            {
                m_messagesCopyBuffer.Add(m_messagesBuffer[m_messageBufferIndex]);
                m_messageBufferIndex++;
            }
            while (m_messageBufferIndex < m_messagesBuffer.Count && m_messagesBuffer[m_messageBufferIndex].Name == m_messagesBuffer[(m_messageBufferIndex - 1).Max(0)].Name);

            m_mainNotifyPanel.OnNotificationFadeOutStart += OnCurrentNotifyPanelFadeOutStart;
            m_mainNotifyPanel.DrawMessage(m_messagesCopyBuffer);

            m_waitingForNotifyPanelToEnd = true;
            while (m_waitingForNotifyPanelToEnd)
                yield return Timing.WaitForOneFrame;

            m_mainNotifyPanel.OnNotificationFadeOutStart -= OnCurrentNotifyPanelFadeOutStart;

            (m_mainNotifyPanel, m_secondaryNotifyPanel) = (m_secondaryNotifyPanel, m_mainNotifyPanel); //swap items
            m_mainNotifyPanel.transform.SetAsFirstSibling();
        }

        m_messageBufferIndex = 0;
        m_messagesBuffer.Clear();
        m_currentNotifyCoroutine.Finished();
    }

    private void OnCurrentNotifyPanelFadeOutStart()
    {
        m_waitingForNotifyPanelToEnd = false;
    }
}