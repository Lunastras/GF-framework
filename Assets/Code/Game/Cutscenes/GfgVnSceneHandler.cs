using System;
using System.Collections.Generic;
using System.Reflection;
using MEC;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;

public class GfgVnSceneHandler : MonoBehaviour
{
    public GfxNotifyPanelTemplate NotifyPanel;

    [HideInInspector] public int m_currentSceneNameLength = 0;

    [HideInInspector] public List<MessageOption> m_optionsBuffer = new(4);

    [HideInInspector] public List<MethodInfo> m_callbackActionsBuffer = new(4);

    [HideInInspector] public List<CornDialogueAction> m_dialogueActionsBuffer = new(128);

    [HideInInspector] public Stack<GfgVnSceneLabelInfo> m_labelStack = new(4);

    [HideInInspector] public GfcStringBuffer m_dialogueStringBuffer = new(32);

    [HideInInspector] public Action<GfxTextMessage, GfxNotifyPanelTemplate, int> m_buttonCallback;

    [HideInInspector] public GfcCoroutineHandle m_actionCoroutineHandle = default;

    [HideInInspector] public MethodInfo m_nextLabel = null;

    [HideInInspector] public CornDialogueSetting m_cachedDialogueSetting = default;

    [HideInInspector] public bool m_hasCachedDialogueSetting = false;

    void Awake()
    {
        m_buttonCallback = new(OnOptionSubmitCallback);
    }

    public CoroutineHandle StartScene<T>() where T : GfgVnScene { return StartScene(typeof(T)); }

    public CoroutineHandle StartScene(Type aSceneType)
    {
        if (aSceneType == null || !aSceneType.IsSubclassOf(typeof(GfgVnScene)))
        {
            LogError("The passed type does not inherit " + typeof(GfgVnScene).Name);
            aSceneType = typeof(TestDialogue);
        }

        Assert(0 == m_actionCoroutineHandle.KillCoroutine(), "A coroutine was already being executed.");

        Log("Writing scene: " + aSceneType.Name);

        GfgVnScene scene = (GfgVnScene)Activator.CreateInstance(aSceneType);
        return m_actionCoroutineHandle.RunCoroutineIfNotRunning(scene._ExecuteActions(this));
    }

    private void OnOptionSubmitCallback(GfxTextMessage aMessage, GfxNotifyPanelTemplate aNotifyPanel, int aIndex)
    {
        if (aIndex < 0 || aIndex >= m_callbackActionsBuffer.Count)
            LogError("The option index is invalid: " + aIndex + " count of options in buffer is: " + m_callbackActionsBuffer.Count);
        else
            m_nextLabel = m_callbackActionsBuffer[aIndex];
    }
}

public abstract class GfgVnScene
{
    public enum GfgVnScenePlayable
    {
        NO_CONDITIONS,
        PLAYABLE,
        UNPLAYABLE,
    }

    private GfgVnSceneHandler m_handler;

    private const string NEXT_LABEL_ERROR = "Cannot set the next label multiple times in a single label! Next() and Option() calls need to be in different labels!";

    protected abstract void Begin();

    public static GfgVnScenePlayable CanPlayScene<T>() { return CanPlayScene(typeof(T)); }

    private const string CAN_PLAY_FUNC_NAME = "CanPlay";

    public static GfgVnScenePlayable CanPlayScene(Type aScene)
    {
        GfgVnScenePlayable ret = GfgVnScenePlayable.NO_CONDITIONS;
        MethodInfo canPlayFunc = aScene.GetMethod(CAN_PLAY_FUNC_NAME, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (canPlayFunc != null)
        {
            bool playable = (bool)canPlayFunc.Invoke(null, null);
            ret = playable ? GfgVnScenePlayable.PLAYABLE : GfgVnScenePlayable.UNPLAYABLE;
        }

        return ret;
    }

    public void EndCurrentScene()
    {
        m_handler.m_actionCoroutineHandle.KillCoroutine();
        //go to menu or something dunno, todo
    }

    private void ExecuteLabel(MethodInfo aLabelMethod)
    {
        Assert(aLabelMethod != null, "The method info passed is null!");

        m_handler.m_nextLabel = null;
        m_handler.m_hasCachedDialogueSetting = false;

        m_handler.m_labelStack.Clear();
        m_handler.m_optionsBuffer.Clear();
        m_handler.m_dialogueActionsBuffer.Clear();
        m_handler.m_callbackActionsBuffer.Clear();

        m_handler.m_dialogueStringBuffer.SetLength(m_handler.m_currentSceneNameLength);

        PushLabel(aLabelMethod.Name);
        aLabelMethod?.Invoke(this, null);
    }

    public IEnumerator<float> _ExecuteActions(GfgVnSceneHandler aHandler)
    {
        m_handler = aHandler;
        m_handler.m_dialogueStringBuffer.Clear();
        m_handler.m_dialogueStringBuffer.Append(GetType().Name);
        m_handler.m_currentSceneNameLength = m_handler.m_dialogueStringBuffer.Length;
        GfxNotifyPanelTemplate notifyPanel = m_handler.NotifyPanel;

        Next(Begin);

    StartLabel:

        while (m_handler.m_nextLabel == null && !m_handler.m_optionsBuffer.IsEmpty())
            yield return Timing.WaitForOneFrame;

        ExecuteLabel(m_handler.m_nextLabel);

        for (int i = 0; i < m_handler.m_dialogueActionsBuffer.Count; ++i)
        {
            CornDialogueAction dialogueAction = m_handler.m_dialogueActionsBuffer[i];

            if (dialogueAction.ActionType == CornDialogueActionType.TEXT)
            {
                notifyPanel.QueueMessage(dialogueAction.Message);
            }
            else
            {
                CoroutineHandle notifyHandle = notifyPanel.DrawMessages();
                if (notifyHandle.IsValid) yield return Timing.WaitUntilDone(notifyHandle);

                switch (dialogueAction.ActionType)
                {
                    case CornDialogueActionType.ACTION:
                        dialogueAction.Action?.Invoke();
                        break;

                    case CornDialogueActionType.ENVIRONMENT:
                        yield return Timing.WaitUntilDone(GfgManagerSceneLoader.LoadScene(dialogueAction.Scene, GfcGameState.CUTSCENE));
                        break;

                    case CornDialogueActionType.SOUND:
                        break;

                    case CornDialogueActionType.WAIT:
                        yield return Timing.WaitForSeconds(dialogueAction.WaitTime);
                        break;
                }
            }
        }

        notifyPanel.DrawMessages();

        if (m_handler.m_nextLabel == null && m_handler.m_optionsBuffer.IsEmpty())
        {
            CoroutineHandle writerHandle = notifyPanel.GetCoroutineHandle();
            if (writerHandle.IsValid)
                yield return Timing.WaitUntilDone(writerHandle);

            m_handler.m_actionCoroutineHandle.Finished();
            EndCurrentScene();
        }
        else
            goto StartLabel;
    }

    protected void PushLabel(string aLabelName)
    {
        //remove any postfix from the buffer
        if (m_handler.m_labelStack.Count > 0)
            m_handler.m_dialogueStringBuffer.SetLength(m_handler.m_labelStack.Peek().DialogueStringPrefixLength);

        m_handler.m_dialogueStringBuffer.Append(aLabelName);

        m_handler.m_labelStack.Push(new()
        {
            LabelStatementCount = 0,
            DialogueStringPrefixLength = m_handler.m_dialogueStringBuffer.Length,
        });
    }

    protected void PopLabel()
    {
        if (m_handler.m_labelStack.Count > 1)
            m_handler.m_labelStack.Pop();
        else
            LogError("Cannot pop the main label! Only secondary labels created with 'PushLabel' can be popped!");
    }

    private string GetLocalizedText(string aKey)
    {
        //todo
        return string.Copy(aKey); ;
    }

    private string GetCurrentLocalizedString()
    {
        GfgVnSceneLabelInfo currentLabel = m_handler.m_labelStack.Pop();

        m_handler.m_dialogueStringBuffer.SetLength(currentLabel.DialogueStringPrefixLength);
        m_handler.m_dialogueStringBuffer.Append(currentLabel.LabelStatementCount);
        currentLabel.LabelStatementCount++;

        m_handler.m_labelStack.Push(currentLabel);
        return GetLocalizedText(m_handler.m_dialogueStringBuffer);
    }

    protected void Wait(float aSeconds)
    {
        CornDialogueAction action = new()
        {
            ActionType = CornDialogueActionType.WAIT,
            WaitTime = aSeconds,
        };

        m_handler.m_dialogueActionsBuffer.Add(action);
    }

    protected void Environment(GfcSceneId aScene)
    {
        CornDialogueAction action = new()
        {
            ActionType = CornDialogueActionType.ENVIRONMENT,
            Scene = aScene,
        };

        m_handler.m_dialogueActionsBuffer.Add(action);
    }

    private void CheckForNextLabel() { Assert(m_handler.m_nextLabel == null, NEXT_LABEL_ERROR); }

    protected void SayKey(string aUniqueKeyPostfix) { SayKey(aUniqueKeyPostfix, m_handler.m_hasCachedDialogueSetting ? m_handler.m_cachedDialogueSetting : default); }
    protected void SayKey(string aUniqueKeyPostfix, StoryCharacter aCharacter, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL, string aNameOverride = null) { SayKey(aUniqueKeyPostfix, new(aCharacter, anEmotion, aNameOverride)); }

    protected void SayKey(string aUniqueKeyPostfix, CornDialogueSetting aSetting = default)
    {
        GfgVnSceneLabelInfo currentLabel = m_handler.m_labelStack.Peek();

        m_handler.m_dialogueStringBuffer.SetLength(currentLabel.DialogueStringPrefixLength);
        m_handler.m_dialogueStringBuffer.Append(aUniqueKeyPostfix);

        Say(GetLocalizedText(m_handler.m_dialogueStringBuffer), aSetting);
    }

    private string GetCharacterName(StoryCharacter aCharacter)
    {
        //todo
        return aCharacter.ToString();
    }

    private GfcSound GetCharacterSound(StoryCharacter aCharacter, CharacterSound aCharacterSound)
    {
        //todo
        return null;
    }

    protected void Say(int aCountRepeats) { while (--aCountRepeats >= 0) Say(); }

    protected void Say() { Say(GetCurrentLocalizedString(), m_handler.m_hasCachedDialogueSetting ? m_handler.m_cachedDialogueSetting : default); }
    protected void Say(CornDialogueSetting aSetting) { Say(GetCurrentLocalizedString(), aSetting); }
    protected void Say(StoryCharacter aCharacter, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL, string aNameOverride = null) { Say(GetCurrentLocalizedString(), new(aCharacter, anEmotion, aNameOverride)); }

    protected void Say(string aText) { Say(aText, m_handler.m_hasCachedDialogueSetting ? m_handler.m_cachedDialogueSetting : default); }
    protected void Say(string aText, StoryCharacter aCharacter, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL, string aNameOverride = null) { Say(aText, new(aCharacter, anEmotion, aNameOverride)); }

    protected void Say(string aText, CornDialogueSetting aSetting)
    {
        m_handler.m_hasCachedDialogueSetting = true;
        m_handler.m_cachedDialogueSetting = aSetting;

        if (aText != null)
        {
            string name = aSetting.NameOverride;
            if (name == null && aSetting.Character != StoryCharacter.NONE)
                name = GetCharacterName(aSetting.Character);

            GfcSound sound = aSetting.SoundOverride;
            if (sound == null && aSetting.Character != StoryCharacter.NONE && aSetting.CharacterSound != CharacterSound.NONE)
                sound = GetCharacterSound(aSetting.Character, aSetting.CharacterSound);

            GfxTextMessage message = default;
            message.MainText = aText;
            message.OptionCallback = m_handler.m_buttonCallback;
            message.Name = name;
            message.Character = aSetting.Character;
            message.Emotion = aSetting.CharacterEmotion;
            message.Sound = sound;

            CornDialogueAction action = new()
            {
                ActionType = CornDialogueActionType.TEXT,
                Message = message,
            };

            m_handler.m_dialogueActionsBuffer.Add(action);
        }
    }

    protected void Action(Action anAction)
    {
        CornDialogueAction action = new()
        {
            ActionType = CornDialogueActionType.ACTION,
            Action = anAction,
        };

        m_handler.m_dialogueActionsBuffer.Add(action);
    }

    private void CheckTypeOfLastAction(CornDialogueActionType aType)
    {
        if (m_handler.m_dialogueActionsBuffer.Count > 0)
        {
            CornDialogueActionType lastActionType = m_handler.m_dialogueActionsBuffer[^1].ActionType;
            Assert(aType == lastActionType, "The last action performed needs to be of type " + aType + " but it is of type " + lastActionType);
        }
        else
            LogError("The last action performed needs to be of type " + aType + ", but there is no previous action!");
    }

    protected void Append(string aTextToAppend)
    {
        CheckTypeOfLastAction(CornDialogueActionType.TEXT);

        CornDialogueAction dialogueAction = m_handler.m_dialogueActionsBuffer[^1];
        GfxTextMessage message = dialogueAction.Message;

        if (message.Options.IsEmpty())
        {
            message.MainText += aTextToAppend;
            dialogueAction.Message = message;
            m_handler.m_dialogueActionsBuffer[^1] = dialogueAction;
        }
        else
        {
            int lastOptionIndex = message.Options.Count - 1;
            MessageOption option = message.Options[lastOptionIndex];
            option.OptionText += aTextToAppend;
            message.Options[lastOptionIndex] = option;
        }
    }

    protected void Next(Action aNextLabel)
    {
        CheckForNextLabel();
        Assert(m_handler.m_optionsBuffer.IsEmpty(), NEXT_LABEL_ERROR);
        Assert(aNextLabel != null, "The label passed is invalid!");
        m_handler.m_nextLabel = aNextLabel.GetMethodInfo();
    }

    protected void Option(Action aJumpLabel, bool anIsDisabled = false) { Option(GetCurrentLocalizedString(), aJumpLabel, anIsDisabled); }

    protected void Option(string aText, Action aJumpLabel, bool anIsDisabled = false)
    {
        CheckForNextLabel();
        CheckTypeOfLastAction(CornDialogueActionType.TEXT);

        m_handler.m_callbackActionsBuffer.Add(aJumpLabel.GetMethodInfo());

        MessageOption option = new(aText, anIsDisabled);
        m_handler.m_optionsBuffer.Add(option);

        CornDialogueAction dialogueAction = m_handler.m_dialogueActionsBuffer[^1];
        GfxTextMessage message = dialogueAction.Message;
        message.Options = m_handler.m_optionsBuffer;

        dialogueAction.Message = message;
        m_handler.m_dialogueActionsBuffer[^1] = dialogueAction;
    }
}

public struct CornDialogueAction
{
    public CornDialogueActionType ActionType;

    public GfxTextMessage Message;

    public Action Action;

    public GfcSceneId Scene;

    public float WaitTime;
}

public struct CornDialogueSetting
{
    public CornDialogueSetting(StoryCharacter aCharacter = StoryCharacter.NONE, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL, string aNameOverride = null)
    {
        Character = aCharacter;
        CharacterEmotion = anEmotion;
        NameOverride = aNameOverride;
        SoundOverride = null;
        CharacterSound = CharacterSound.NONE;
    }

    public StoryCharacter Character;
    public CharacterEmotion CharacterEmotion;
    public CharacterSound CharacterSound;

    public string NameOverride;
    public GfcSound SoundOverride;
}

public enum CornDialogueActionType
{
    ACTION,
    TEXT,
    SOUND,
    ENVIRONMENT,
    WAIT,
}

public struct GfgVnSceneLabelInfo
{
    public int LabelStatementCount;
    public int DialogueStringPrefixLength;
}