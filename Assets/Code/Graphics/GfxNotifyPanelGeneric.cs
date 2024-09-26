using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfxNotifyPanelGeneric : GfxNotifyPanelTemplate
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

    [SerializeField] protected GfcInputType m_inputSubmit;

    [SerializeField] protected GfcInputType m_inputSkip;

    [SerializeField] protected float m_messageWaitSecondsOnSkip = 0.2f;

    [SerializeField] protected bool m_forceWriteOnSubmit = false;

    [SerializeField] protected BoxTextTransitionMode m_transitionMixMode = BoxTextTransitionMode.MIX_NONE;

    protected int m_lastWrittenMessageIndex = -1;
    protected int m_messageBufferIndex = 0;

    protected abstract IEnumerator<float> _AnimateContinueGraphics();

    protected abstract void TransitionBox(float aTimeFactor, bool aFadeIn, bool aHasName);

    protected abstract void OnTextWrite(GfxTextMessage aMessage, int aMessageIndex);

    void Awake()
    {
        Debug.Assert(m_mainTextWriter);
        Debug.Assert(m_nameTextWriter);
    }

    private void OnTextWriteInternal(GfxTextMessage aMessage, int aMessageIndex)
    {
        OnTextWrite(aMessage, aMessageIndex);
        OnTextWriteCallback?.Invoke(aMessage, aMessageIndex);
    }

    protected abstract void SubmitOnText(int aMessageIndex, bool aHasName);

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

    public override void ClearVisuals()
    {
        Debug.Assert(m_currentNotifyCoroutine.CoroutineIsRunning, "Clear cannot be called while the routine is still running");
        TransitionBox(0, true, false);
    }

    public virtual CoroutineHandle RemoveOptionButtons()
    {
        GfcPooling.DestroyChildren(m_optionsButtonsParent);
        return default;
    }

    protected void WriteCurrentMessage(ref GfcAudioSource messageAudioSource)
    {
        messageAudioSource?.Stop();
        messageAudioSource = m_messagesBuffer[m_messageBufferIndex].Sound?.PlaySingleInstance();
        m_lastWrittenMessageIndex = m_messageBufferIndex;
        OnTextWriteInternal(m_messagesBuffer[m_messageBufferIndex], m_messageBufferIndex);
        m_nameTextWriter.WriteString(m_messagesBuffer[m_messageBufferIndex].Name, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
        m_mainTextWriter.WriteString(m_messagesBuffer[m_messageBufferIndex].MainText, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
    }

    protected override IEnumerator<float> _DrawMessages()
    {
        if (m_messagesBuffer.Count == 0)
            yield break;

        const int FADE_IN = 0;

        m_nameTextWriter.RemoveText();
        m_mainTextWriter.RemoveText();

        m_messageBufferIndex = 0;
        m_lastWrittenMessageIndex = -1;
        bool hasNameBox = false;
        float invTransitionTime = 1;
        GfxTextMessage currentMessage = default;

        GfcInputTracker trackerSubmit = new(GfcInputType.SUBMIT);
        GfcAudioSource messageAudioSource = null;

        for (int transitionIndex = 0; transitionIndex < 2; ++transitionIndex)
        {
            if (m_messageBufferIndex < m_messagesBuffer.Count)
                currentMessage = m_messagesBuffer[m_messageBufferIndex];

            transitionIndex %= 2;

            if (transitionIndex == FADE_IN)
            {
                OnNotificationFadeInStart?.Invoke();
                m_mainTextWriter.ForceWriteText();
                hasNameBox = !m_messagesBuffer[m_messageBufferIndex].Name.IsEmpty();
                float effectiveTransitionTime = hasNameBox ? m_transitionTime : m_transitionTimeNoName;
                invTransitionTime = effectiveTransitionTime > 0.00001f ? 1.0f / effectiveTransitionTime : 999999999;

                if (BoxTextTransitionMode.MIX_BEGIN == m_transitionMixMode || BoxTextTransitionMode.MIX_BOTH == m_transitionMixMode)
                    WriteCurrentMessage(ref messageAudioSource);
            }
            else //fading out
            {
                m_nameTextWriter.EraseText();
                OnNotificationFadeOutStart?.Invoke();
            }

            for (float progress = 0; progress < GfcTools.EPSILON; progress += Time.deltaTime * invTransitionTime)
            {
                if (GfcInput.GetInput(m_inputSkip) || trackerSubmit.PressedSinceLastCheck())
                {
                    m_mainTextWriter.ForceWriteText();
                    m_nameTextWriter.ForceWriteText();
                    break;
                }

                TransitionBox(transitionIndex == FADE_IN ? progress : 1f - progress, transitionIndex == FADE_IN, hasNameBox);
                yield return Timing.WaitForOneFrame;
            }

            Time.timeScale = 1.0f;
            TransitionBox(transitionIndex == FADE_IN ? 1 : 0, transitionIndex == FADE_IN, hasNameBox);

            if (transitionIndex == FADE_IN && m_messagesBuffer.Count > 0) //perform this only in the first transition
            {
                string previousStringName = m_messagesBuffer[m_messageBufferIndex].Name;

                for (; m_messageBufferIndex < m_messagesBuffer.Count; ++m_messageBufferIndex)
                {
                    currentMessage = m_messagesBuffer[m_messageBufferIndex];

                    if (m_messageBufferIndex != m_lastWrittenMessageIndex)
                    {
                        OnTextWriteInternal(currentMessage, m_messageBufferIndex);
                        //m_nameTextWriter.FinishTranslation();

                        if (currentMessage.Name != previousStringName)
                            m_nameTextWriter.WriteString(currentMessage.Name, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);

                        m_mainTextWriter.WriteString(currentMessage.MainText, m_speedMultiplierTextWrite, m_speedMultiplierTextErase);
                        m_lastWrittenMessageIndex = m_messageBufferIndex;
                    }

                    if (m_mainTextWriter.WritingText())
                        yield return Timing.WaitUntilDone(m_mainTextWriter.WaitUntilTextFinishes(trackerSubmit, m_inputSkip, m_speedMultiplierFinish, m_forceWriteOnSubmit));

                    Debug.Log("Finished writing the text I assume");

                    trackerSubmit.PressedSinceLastCheck(); //reset the submit in case it was pressed while waiting for the text

                    if (!currentMessage.Options.IsEmpty())
                    {
                        Debug.Assert(currentMessage.OptionCallback != null, "There are options in the message, but the callback function is null.");
                        m_waitingForOptionSelect = true;

                        //draw options here
                        yield return Timing.WaitUntilDone(InitializeOptionButtons(currentMessage), false);

                        while (m_waitingForOptionSelect)
                            yield return Timing.WaitForSeconds(0.0333f); //check at an fps of 30

                        yield return Timing.WaitUntilDone(RemoveOptionButtons(), false);

                        trackerSubmit.PressedSinceLastCheck();
                    }
                    else
                    {
                        CoroutineHandle continueAnimationHandle = Timing.RunCoroutine(_AnimateContinueGraphics());

                        if (GfcInput.GetInput(m_inputSkip))
                            yield return Timing.WaitForSeconds(m_messageWaitSecondsOnSkip);
                        else while (!trackerSubmit.PressedSinceLastCheck() && !GfcInput.GetInput(m_inputSkip))
                                yield return Timing.WaitForOneFrame;

                        if (continueAnimationHandle.IsValid) Timing.KillCoroutines(continueAnimationHandle);
                    }

                    SubmitOnText(m_messageBufferIndex, hasNameBox);

                    previousStringName = m_messagesBuffer[m_messageBufferIndex].Name;
                } //for (; m_messageBufferIndex < m_messagesBuffer.Count; ++m_messageBufferIndex)

                Debug.Log("Erasing text I assume");
                m_mainTextWriter.EraseText(m_speedMultiplierTextErase);
            }
            else OnNotificationFadeOutEnd?.Invoke();

            if (transitionIndex == FADE_IN && BoxTextTransitionMode.MIX_END > m_transitionMixMode && m_mainTextWriter.WritingText())
                yield return Timing.WaitUntilDone(m_mainTextWriter.WaitUntilTextFinishes(trackerSubmit, m_inputSkip, m_speedMultiplierFinish, m_forceWriteOnSubmit));
        }

        //m_mainTextWriter.FinishTranslation();
        //m_nameTextWriter.FinishTranslation();

        messageAudioSource?.Stop();
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
    public GfxTextMessage(string aMainText = null, string aName = null, StoryCharacter aCharacter = StoryCharacter.NONE, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL)
    {
        MainText = aMainText;
        Name = aName;
        OptionCallback = null;
        Options = null;
        Character = aCharacter;
        Emotion = anEmotion;
        Sound = null;
    }

    public GfxTextMessage(string aMainText, List<MessageOption> someOptions, Action<GfxTextMessage, GfxNotifyPanelGeneric, int> aOptionCallback, string aName = null, StoryCharacter aCharacter = StoryCharacter.NONE, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL)
    {
        Options = someOptions;
        OptionCallback = aOptionCallback;
        MainText = aMainText;
        Name = aName;
        Character = aCharacter;
        Emotion = anEmotion;
        Sound = null;
    }

    public List<MessageOption> Options;
    public Action<GfxTextMessage, GfxNotifyPanelGeneric, int> OptionCallback;

    public string MainText;
    public string Name;

    public StoryCharacter Character;
    public CharacterEmotion Emotion;

    public GfcSound Sound;
}

public enum BoxTextTransitionMode
{
    MIX_NONE,
    MIX_BEGIN,
    MIX_END,
    MIX_BOTH,
}