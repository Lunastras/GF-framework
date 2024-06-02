using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GfxDoubleTextWriter))]
public abstract class GfxNotifyPanelGeneric : MonoBehaviour
{
    [SerializeField] protected GfxDoubleTextWriter m_mainTextWriter;

    [SerializeField] protected GfxDoubleTextWriter m_nameTextWriter;

    [SerializeField] protected GameObject m_optionsButtonPrefab;

    [SerializeField] protected Transform m_optionsButtonsParent;

    [SerializeField] protected float m_transitionTime;

    [SerializeField] protected float m_transitionTimeNoName;

    [SerializeField] protected float m_speedMultiplierTextWrite = 1;

    [SerializeField] protected float m_speedMultiplierTextErase = 1;

    [SerializeField] protected float m_speedMultiplierFinish = 2;

    [SerializeField] protected GfcInputTrackerShared m_submitTracker;

    [SerializeField] protected GfcInputType m_skipInput;

    [SerializeField] protected float m_messageWaitSecondsOnSkip = 0.2f;

    [SerializeField] protected bool m_forceWriteOnSubmit = false;

    [SerializeField] protected BoxTextTransitionMode m_transitionMixMode = BoxTextTransitionMode.MIX_NONE;

    private int m_messageBufferIndex = 0;

    protected GfcCoroutineHandle m_currentNotifyCoroutine = default;

    protected List<GfxTextMessage> m_messagesBuffer = new(8);

    public Action<GfxTextMessage, int, bool> OnTextWriteCallback;

    public Action OnNotificationFadeInStart;

    public Action OnNotificationFadeOutStart;

    public Action OnNotificationFadeOutEnd;

    public CoroutineHandle DrawMessages(string aMessage)
    {
        m_messagesBuffer.Add(new(aMessage));
        return DrawMessages();
    }

    public CoroutineHandle DrawMessages(IEnumerable<string> someMessages)
    {
        foreach (string message in someMessages) m_messagesBuffer.Add(new(message, "Notify"));
        return DrawMessages();
    }

    public CoroutineHandle DrawMessages(GfxTextMessage aMessage)
    {
        m_messagesBuffer.Add(aMessage);
        return DrawMessages();
    }

    public CoroutineHandle DrawMessages(IEnumerable<GfxTextMessage> someMessages)
    {
        foreach (GfxTextMessage message in someMessages) m_messagesBuffer.Add(message);
        return DrawMessages();
    }

    private CoroutineHandle DrawMessages()
    {
        if (m_messagesBuffer.Count > 0)
            return m_currentNotifyCoroutine.RunCoroutineIfNotRunning(_DrawMessages());
        else
            return m_currentNotifyCoroutine;
    }

    public bool IsShowingMessages() { return m_currentNotifyCoroutine.CoroutineIsRunning; }

    public CoroutineHandle GetCoroutineHandle() { return m_currentNotifyCoroutine; }

    protected abstract IEnumerator<float> _AnimateContinueGraphics();

    protected abstract void TransitionBox(float aTimeFactor, bool aFadeIn, bool aHasName, bool aSkipping);

    protected abstract void OnTextWrite(GfxTextMessage aMessage, int aMessageIndex, bool aSkipping);

    private void OnTextWriteInternal(GfxTextMessage aMessage, int aMessageIndex, bool aSkipping)
    {
        OnTextWrite(aMessage, aMessageIndex, aSkipping);
        OnTextWriteCallback?.Invoke(aMessage, aMessageIndex, aSkipping);
    }

    protected abstract void SubmitOnText(int aMessageIndex, bool aHasName, bool aSkipping);

    private bool m_waitingForOptionSelect = true;

    public virtual CoroutineHandle InitializeOptionButton(GfxTextMessage aTextMessage, GfxButton anInstantiatedButton, MessageOption aOption, int aIndex) { return default; }

    public virtual CoroutineHandle InitializeOptionButtons(GfxTextMessage aTextMessage)
    {
        for (int optionIndex = 0; optionIndex < aTextMessage.Options.Count; ++optionIndex)
        {
            GfxButton button = Instantiate(m_optionsButtonPrefab).GetComponent<GfxButton>();
            CoroutineHandle initializeButtonHandle = InitializeOptionButton(aTextMessage, button, aTextMessage.Options[optionIndex], optionIndex);

            button.OnButtonEventCallback += OnOptionCallback;
            button.Index = optionIndex;
            button.SetDisabled(aTextMessage.Options[optionIndex].IsDisabled);
        }

        return default;
    }

    public virtual CoroutineHandle RemoveOptionButtons()
    {
        GfcPooling.DestroyChildren(m_optionsButtonsParent);
        return default;
    }

    protected IEnumerator<float> _DrawMessages()
    {
        if (m_messagesBuffer.Count == 0)
            yield break;

        const int FADE_IN = 0;

        m_nameTextWriter.RemoveText();
        m_mainTextWriter.RemoveText();

        m_messageBufferIndex = 0;
        int lastWrittenMessageIndex = -1;
        bool skipTransition = false;
        bool hasNameBox = false;
        float invTransitionTime = 1;
        GfxTextMessage currentMessage = default;

        for (int transitionIndex = 0; transitionIndex < 2; ++transitionIndex)
        {
            if (m_messageBufferIndex < m_messagesBuffer.Count)
                currentMessage = m_messagesBuffer[m_messageBufferIndex];

            transitionIndex %= 2;
            float transitionProgress = 0;

            if (transitionIndex == FADE_IN)
            {
                OnNotificationFadeInStart?.Invoke();
                m_mainTextWriter.ForceWriteText();
                hasNameBox = !m_messagesBuffer[m_messageBufferIndex].Name.IsEmpty();
                hasNameBox = true;
                float effectiveTransitionTime = hasNameBox ? m_transitionTime : m_transitionTimeNoName;
                invTransitionTime = effectiveTransitionTime > 0.00001f ? 1.0f / effectiveTransitionTime : 999999999;

                if (BoxTextTransitionMode.MIX_BEGIN <= m_transitionMixMode && m_transitionMixMode != BoxTextTransitionMode.MIX_END)
                {
                    lastWrittenMessageIndex = m_messageBufferIndex;
                    OnTextWriteInternal(currentMessage, m_messageBufferIndex, GfcInput.GetAxisInput(m_skipInput));
                    m_nameTextWriter.WriteString(m_messagesBuffer[m_messageBufferIndex].Name, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
                    m_mainTextWriter.WriteString(m_messagesBuffer[m_messageBufferIndex].MainText, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
                }
            }
            else
            {
                m_nameTextWriter.EraseText();
                OnNotificationFadeOutStart?.Invoke();
            }

            bool skippedTransition = false;
            float auxTransitionProgress;
            const float EPSILON_ONE = 0.9999f;
            while (transitionProgress < 1)
            {
                if (GfcInput.GetAxisInput(m_skipInput) || m_submitTracker.PressedSinceLastCheck() || skipTransition)
                {
                    transitionProgress = 1;
                    if (transitionIndex == FADE_IN && lastWrittenMessageIndex != m_messageBufferIndex)
                    {
                        skippedTransition = true;
                        lastWrittenMessageIndex = m_messageBufferIndex;
                        m_nameTextWriter.WriteString(m_messagesBuffer[m_messageBufferIndex].Name, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
                        m_mainTextWriter.WriteString(m_messagesBuffer[m_messageBufferIndex].MainText, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
                    }

                    m_mainTextWriter.ForceWriteText();
                    m_nameTextWriter.ForceWriteText();

                    skipTransition = transitionIndex != FADE_IN;
                }

                auxTransitionProgress = transitionIndex == FADE_IN ? transitionProgress : 1.0f - transitionProgress;
                TransitionBox(auxTransitionProgress, transitionIndex == FADE_IN, hasNameBox, skippedTransition);

                if (transitionProgress < EPSILON_ONE)
                    yield return Timing.WaitForOneFrame;

                transitionProgress += Time.deltaTime * invTransitionTime;
            }

            auxTransitionProgress = transitionIndex == FADE_IN ? transitionProgress : 1.0f - transitionProgress;
            TransitionBox(auxTransitionProgress, transitionIndex == FADE_IN, hasNameBox, skippedTransition);

            if (transitionIndex == FADE_IN && m_messagesBuffer.Count > 0) //perform this only in the first transition
            {
                string previousStringName = m_messagesBuffer[m_messageBufferIndex].Name;

                for (; m_messageBufferIndex < m_messagesBuffer.Count; ++m_messageBufferIndex)
                {
                    currentMessage = m_messagesBuffer[m_messageBufferIndex];

                    if (m_messageBufferIndex != lastWrittenMessageIndex)
                    {
                        OnTextWriteInternal(currentMessage, m_messageBufferIndex, GfcInput.GetAxisInput(m_skipInput));
                        m_nameTextWriter.FinishTranslation();

                        if (currentMessage.Name != previousStringName)
                            m_nameTextWriter.WriteString(currentMessage.Name, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);

                        m_mainTextWriter.WriteString(currentMessage.MainText, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
                    }

                    lastWrittenMessageIndex = m_messageBufferIndex;

                    if (m_mainTextWriter.WritingText())
                        yield return Timing.WaitUntilDone(m_mainTextWriter.WaitUntilTextFinishes(m_submitTracker, m_skipInput, m_speedMultiplierFinish, m_forceWriteOnSubmit));

                    bool skip = false;

                    if (currentMessage.Options != null && currentMessage.Options.Count > 0)
                    {
                        Debug.Assert(currentMessage.OptionCallback != null, "There are options in the message but the callback function is null.");
                        m_waitingForOptionSelect = true;

                        //draw options here
                        yield return Timing.WaitUntilDone(InitializeOptionButtons(currentMessage), false);

                        while (m_waitingForOptionSelect)
                            yield return Timing.WaitForSeconds(0.0333f); //check at an fps of 30

                        yield return Timing.WaitUntilDone(RemoveOptionButtons(), false);

                        m_submitTracker.PressedSinceLastCheck();
                    }
                    else
                    {
                        CoroutineHandle continueAnimationHandle = Timing.RunCoroutine(_AnimateContinueGraphics());

                        skip = GfcInput.GetAxisInput(m_skipInput);
                        if (skip)
                            yield return Timing.WaitForSeconds(m_messageWaitSecondsOnSkip);
                        else while (!m_submitTracker.PressedSinceLastCheck() && !GfcInput.GetAxisInput(m_skipInput))
                                yield return Timing.WaitForOneFrame;

                        if (continueAnimationHandle.IsValid) Timing.KillCoroutines(continueAnimationHandle);
                    }

                    SubmitOnText(m_messageBufferIndex, hasNameBox, skip);

                    previousStringName = m_messagesBuffer[m_messageBufferIndex].Name;
                }

                m_mainTextWriter.EraseText(m_speedMultiplierTextErase);
            }
            else OnNotificationFadeOutEnd?.Invoke();

            if (transitionIndex == FADE_IN && BoxTextTransitionMode.MIX_END > m_transitionMixMode && m_mainTextWriter.WritingText())
                yield return Timing.WaitUntilDone(m_mainTextWriter.WaitUntilTextFinishes(m_submitTracker, m_skipInput, m_speedMultiplierFinish, m_forceWriteOnSubmit));
        }

        m_messageBufferIndex = 0;
        m_messagesBuffer.Clear();
        m_currentNotifyCoroutine.Finished();
    }

    protected virtual void OnOptionCallbackInternal(GfxButtonCallbackType aType, GfxButton aButton, bool aState) { }

    private void OnOptionCallback(GfxButtonCallbackType aType, GfxButton aButton, bool aState)
    {
        if (aType == GfxButtonCallbackType.SUBMIT)
        {
            OnOptionCallbackInternal(aType, aButton, aState);
            m_messagesBuffer[m_messageBufferIndex].OptionCallback.Invoke(m_messagesBuffer[m_messageBufferIndex], this, aButton.Index);
            m_waitingForOptionSelect = false;//I need to figure out if some signal thing exists
        }
    }

    public GfxTextMessage GetTextMessage(int anIndex) { return m_messagesBuffer[anIndex]; }

    public GfxTextMessage GetCurrentTextMessage() { return m_messagesBuffer[m_messageBufferIndex]; }

    public int GetCurrentTextMessageIndex() { return m_messageBufferIndex; }
}

public struct MessageOption
{
    public MessageOption(string aOptionText, bool anIsDisabled = false)
    {
        OptionText = aOptionText;
        IsDisabled = anIsDisabled;
    }

    public string OptionText;

    public bool IsDisabled;
}

public struct GfxTextMessage
{
    public GfxTextMessage(string aMainText = null, string aName = null, StoryCharacter aCharacter = StoryCharacter.NONE)
    {
        MainText = aMainText;
        Name = aName;
        OptionCallback = null;
        Options = null;
        Character = aCharacter;
    }

    public GfxTextMessage(string aMainText, List<MessageOption> someOptions, Action<GfxTextMessage, GfxNotifyPanelGeneric, int> aOptionCallback, string aName = null, StoryCharacter aCharacter = StoryCharacter.NONE)
    {
        Options = someOptions;
        OptionCallback = aOptionCallback;
        MainText = aMainText;
        Name = aName;
        Character = aCharacter;
    }

    public StoryCharacter Character;
    public string MainText;
    public string Name;
    public List<MessageOption> Options;
    public Action<GfxTextMessage, GfxNotifyPanelGeneric, int> OptionCallback;

}

public enum BoxTextTransitionMode
{
    MIX_NONE,
    MIX_BEGIN,
    MIX_END,
    MIX_BOTH,
}