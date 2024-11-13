using MEC;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

public abstract class GfxNotifyPanelInteractable : GfxNotifyPanelTemplate
{
    [SerializeField] protected GameObject m_optionsButtonPrefab;

    [SerializeField] protected Transform m_optionsButtonsParent;

    [SerializeField] protected float m_transitionTime = 0.3f;

    [SerializeField] protected float m_transitionTimeNoName = 0.3f;

    [SerializeField] protected float m_speedMultiplierTextWrite = 1;

    [SerializeField] protected float m_speedMultiplierTextErase = 1;

    [SerializeField] protected float m_speedMultiplierFinish = 2;

    [SerializeField] protected GfcInputType m_inputSubmit = GfcInputType.SUBMIT;

    [SerializeField] protected GfcInputType m_inputSkip = GfcInputType.RUN;

    [SerializeField] protected float m_messageWaitSecondsOnSkip = 0.2f;

    [SerializeField] protected bool m_forceWriteOnSubmit = false;

    [SerializeField] protected BoxTextTransitionMode m_transitionMixMode = BoxTextTransitionMode.MIX_NONE;

    protected int m_lastWrittenMessageIndex = -1;

    protected int m_messageBufferIndex = 0;

    private bool m_waitingForOptionSelect = true;

    public override void ClearVisuals()
    {
        Debug.Assert(m_currentNotifyCoroutine.CoroutineIsRunning, "Clear cannot be called while the routine is still running");
        TransitionBox(0, true, false);
    }

    protected virtual CoroutineHandle RemoveOptionButtons()
    {
        GfcPooling.DestroyChildren(m_optionsButtonsParent);
        return default;
    }

    public virtual CoroutineHandle InitializeOptionButton(GfxTextMessage aTextMessage, GfxButton anInstantiatedButton, GfxNotifyOption aOption, int aIndex)
    {
        GfxPanel panel = anInstantiatedButton as GfxPanel;

        if (panel)
        {
            GfxPanelCreateData createData = GfxUiTools.GetDefaultPanelCreateData();
            createData.ButtonCreateData.Parent = m_optionsButtonsParent;
            createData.IndecesColumnRow = new(0, aIndex);

            panel.SetCreateData(createData, true);
            panel.SetTextOnly(aOption.OptionText);
        }
        else
        {
            Debug.LogError("The spawned option button does not have a GfxPanel component. If it shouldn't, then override this function and initialize the button properly");
        }

        return default;
    }

    public virtual IEnumerator<float> _InitializeOptionButtons(GfxTextMessage aTextMessage)
    {
        for (int optionIndex = 0; optionIndex < aTextMessage.Options.Count; ++optionIndex)
        {
            GfxButton button = GfcPooling.Instantiate(m_optionsButtonPrefab).GetComponent<GfxButton>();
            CoroutineHandle initializeButtonHandle = InitializeOptionButton(aTextMessage, button, aTextMessage.Options[optionIndex], optionIndex);
            if (initializeButtonHandle.IsValid) yield return Timing.WaitUntilDone(initializeButtonHandle);

            button.OnButtonEventCallback += OnOptionCallback;
            button.Index = optionIndex;
            button.SetDisabled(aTextMessage.Options[optionIndex].IsDisabled, aTextMessage.Options[optionIndex].DisabledReason);
        }
    }

    protected virtual void OnOptionCallbackInternal(GfxButtonCallbackType aType, GfxButton aButton, bool aState) { }

    private void OnOptionCallback(GfxButtonCallbackType aType, GfxButton aButton, bool aState)
    {
        if (aType == GfxButtonCallbackType.SUBMIT)
        {
            if (m_messagesBuffer.Count <= m_messageBufferIndex || m_messageBufferIndex < 0)
            {
                Debug.LogError("Bad index for some reason " + m_messagesBuffer + " but messages in buffer are " + m_messagesBuffer.Count);
                m_messageBufferIndex = m_messagesBuffer.Count - 1; //some weird bug happened where this idex was all messed up
            }

            OnOptionCallbackInternal(aType, aButton, aState);
            m_messagesBuffer[m_messageBufferIndex].OptionCallback.Invoke(m_messagesBuffer[m_messageBufferIndex], this, aButton.Index);
            m_waitingForOptionSelect = false;//I need to figure out if some signal thing exists
        }
    }

    public GfxTextMessage GetTextMessage(int anIndex) { return m_messagesBuffer[anIndex]; }

    public GfxTextMessage GetCurrentTextMessage() { return m_messagesBuffer[m_messageBufferIndex]; }

    public int GetCurrentTextMessageIndex() { return m_messageBufferIndex; }

    protected virtual void OnDrawMessagesStart() { }

    protected virtual void OnDrawMessagesEnd() { }

    protected abstract void TransitionBox(float aTimeFactor, bool aFadeIn, bool aHasName);

    protected abstract void DrawCurrentMessage();

    protected abstract void FlushCurrentMessage();

    protected abstract void EraseMessages();

    private void DrawCurrentMessageInternal()
    {
        if (m_lastWrittenMessageIndex != m_messageBufferIndex)
        {
            //messageAudioSource?.Stop();
            // messageAudioSource = m_messagesBuffer[m_messageBufferIndex].Sound?.PlaySingleInstance();
            m_lastWrittenMessageIndex = m_messageBufferIndex;
            OnDrawMessageCallback?.Invoke(m_messagesBuffer[m_messageBufferIndex], m_messageBufferIndex);
            DrawCurrentMessage();
        }
    }

    public abstract bool DrawingMessage();

    protected override IEnumerator<float> _DrawMessages()
    {
        if (m_messagesBuffer.Count == 0)
            yield break;

        const int FADE_IN = 0;

        OnDrawMessagesStart();

        m_messageBufferIndex = 0;
        m_lastWrittenMessageIndex = -1;
        bool hasNameBox = false;
        float invTransitionTime = 1;
        GfxTextMessage currentMessage = default;

        GfcInputTracker trackerSubmit = new(m_inputSubmit, gameObject);
        GfcInputTracker trackerSkip = new(m_inputSkip, gameObject);

        for (int transitionIndex = FADE_IN; transitionIndex < 2; ++transitionIndex)
        {
            if (m_messageBufferIndex < m_messagesBuffer.Count)
                currentMessage = m_messagesBuffer[m_messageBufferIndex];

            if (transitionIndex == FADE_IN)
            {
                OnNotificationFadeInStart?.Invoke();
                hasNameBox = !m_messagesBuffer[m_messageBufferIndex].Name.IsEmpty();
                float effectiveTransitionTime = hasNameBox ? m_transitionTime : m_transitionTimeNoName;
                invTransitionTime = effectiveTransitionTime > 0.00001f ? 1.0f / effectiveTransitionTime : 999999999;

                if (BoxTextTransitionMode.MIX_BEGIN == m_transitionMixMode || BoxTextTransitionMode.MIX_BOTH == m_transitionMixMode)
                    DrawCurrentMessageInternal();
            }
            else //fading out
            {
                OnNotificationFadeOutStart?.Invoke();
            }

            for (float progress = 0; progress < GfcTools.EPSILON; progress += Time.deltaTime * invTransitionTime)
            {
                if (trackerSkip.Pressed() || trackerSubmit.PressedSinceLastCheck())
                {
                    FlushCurrentMessage();
                    break;
                }

                TransitionBox(transitionIndex == FADE_IN ? progress : 1f - progress, transitionIndex == FADE_IN, hasNameBox);
                yield return Timing.WaitForOneFrame;
            }

            TransitionBox(transitionIndex == FADE_IN ? 1 : 0, transitionIndex == FADE_IN, hasNameBox);

            if (transitionIndex == FADE_IN && m_messagesBuffer.Count > 0) //perform this only in the first transition
            {
                for (; m_messageBufferIndex < m_messagesBuffer.Count; ++m_messageBufferIndex)
                {
                    currentMessage = m_messagesBuffer[m_messageBufferIndex];

                    DrawCurrentMessageInternal();

                    while (DrawingMessage() && !trackerSubmit.PressedSinceLastCheck() && !trackerSkip.Pressed())
                        yield return Timing.WaitForOneFrame;

                    FlushCurrentMessage();

                    trackerSubmit.PressedSinceLastCheck(); //reset the submit in case it was pressed while waiting for the text

                    if (!currentMessage.Options.IsEmpty())
                    {
                        Debug.Assert(currentMessage.OptionCallback != null, "There are options in the message, but the callback function is null.");
                        m_waitingForOptionSelect = true;

                        //draw options here
                        yield return Timing.WaitUntilDone(Timing.RunCoroutine(_InitializeOptionButtons(currentMessage)));

                        while (m_waitingForOptionSelect)
                            yield return Timing.WaitForOneFrame;

                        yield return Timing.WaitUntilDone(RemoveOptionButtons(), false);

                        trackerSubmit.PressedSinceLastCheck();
                    }
                    else
                    {
                        CoroutineHandle continueAnimationHandle = Timing.RunCoroutine(_AnimateContinueGraphics());

                        if (trackerSkip.Pressed())
                            yield return Timing.WaitForSeconds(m_messageWaitSecondsOnSkip);
                        else while (!trackerSubmit.PressedSinceLastCheck() && !trackerSkip.Pressed())
                                yield return Timing.WaitForOneFrame;

                        if (continueAnimationHandle.IsValid) Timing.KillCoroutines(continueAnimationHandle);
                    }
                } //for (; m_messageBufferIndex < m_messagesBuffer.Count; ++m_messageBufferIndex)

                EraseMessages();
            }
            else OnNotificationFadeOutEnd?.Invoke();

            if (transitionIndex == FADE_IN && BoxTextTransitionMode.MIX_END > m_transitionMixMode)
                while (DrawingMessage() && !trackerSubmit.PressedSinceLastCheck() && !trackerSkip.Pressed())
                    yield return Timing.WaitForOneFrame;

            FlushCurrentMessage();
        }

        //messageAudioSource?.Stop();
        m_messageBufferIndex = 0;
        m_messagesBuffer.Clear();
        OnDrawMessagesEnd();
        m_currentNotifyCoroutine.Finished();
    }

    protected virtual IEnumerator<float> _AnimateContinueGraphics() { yield break; }
}

public enum BoxTextTransitionMode
{
    MIX_NONE,
    MIX_BEGIN,
    MIX_END,
    MIX_BOTH,
}