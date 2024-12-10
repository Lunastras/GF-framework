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

    public Action<GfxTextMessage, int> OnDrawMessageCallback;

    public Action OnNotificationFadeInStart;

    public Action OnNotificationFadeOutStart;

    public Action OnNotificationFadeOutEnd;

    protected void Start()
    {
        ClearVisuals();
    }

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
            handle = m_currentNotifyCoroutine.RunCoroutineIfNotRunning(_DrawMessages(), Segment.LateUpdate);

        return handle;
    }

    public virtual void ClearVisuals() { }

    protected abstract IEnumerator<float> _DrawMessages();
}


public struct GfxTextMessage
{
    public GfxTextMessage(string aMainText = null, string aName = null, GfcStoryCharacter aCharacter = GfcStoryCharacter.NONE, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL)
    {
        MainText = aMainText;
        Name = aName;
        OptionCallback = null;
        Options = null;
        Character = aCharacter;
        Emotion = anEmotion;
        Sound = null;
    }

    public GfxTextMessage(string aMainText, List<GfxNotifyOption> someOptions, Action<GfxTextMessage, GfxNotifyPanelTemplate, int> aOptionCallback, string aName = null, GfcStoryCharacter aCharacter = GfcStoryCharacter.NONE, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL)
    {
        Options = someOptions;
        OptionCallback = aOptionCallback;
        MainText = aMainText;
        Name = aName;
        Character = aCharacter;
        Emotion = anEmotion;
        Sound = null;
    }

    public List<GfxNotifyOption> Options;
    public Action<GfxTextMessage, GfxNotifyPanelTemplate, int> OptionCallback;

    public string MainText;
    public string Name;

    public GfcStoryCharacter Character;
    public CharacterEmotion Emotion;

    public GfcSound Sound;
}

public struct GfxNotifyOption
{
    public GfxNotifyOption(string aOptionText, bool anInteractable = true, string aDisabledReason = null)
    {
        OptionText = aOptionText;
        Interactable = anInteractable;
        DisabledReason = aDisabledReason;
    }

    public string OptionText;
    public string DisabledReason;

    public bool Interactable;
}