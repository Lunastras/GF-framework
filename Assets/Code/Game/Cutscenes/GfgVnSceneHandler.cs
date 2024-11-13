using System;
using System.Collections.Generic;
using System.Reflection;
using MEC;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(GfxNotifyPanelTemplate))]
public class GfgVnSceneHandler : MonoBehaviour
{
    [HideInInspector] public GfxNotifyPanelTemplate NotifyPanel;

    [HideInInspector] public int CurrentSceneNameLength = 0;

    [HideInInspector] public List<GfxNotifyOption> OptionsBuffer = new(4);

    [HideInInspector] public List<MethodInfo> CallbackActionsBuffer = new(4);

    [HideInInspector] public List<CornDialogueAction> DialogueActionsBuffer = new(128);

    [HideInInspector] public Stack<GfgVnSceneLabelInfo> LabelStack = new(4);

    [HideInInspector] public GfcStringBuffer DialogueStringBuffer = new(32);

    [HideInInspector] public Action<GfxTextMessage, GfxNotifyPanelTemplate, int> ButtonCallback;

    [HideInInspector] public GfcCoroutineHandle ActionCoroutineHandle = default;

    [HideInInspector] public MethodInfo NextLabel = null;

    [HideInInspector] public CornDialogueSetting CachedDialogueSetting = default;

    [HideInInspector] public bool HasCachedDialogueSetting = false;

    void Awake()
    {
        ButtonCallback = new(OnOptionSubmitCallback);
        this.GetComponent(ref NotifyPanel);
    }

    public CoroutineHandle StartScene<T>() where T : GfgVnScene { return StartScene(typeof(T)); }

    public CoroutineHandle StartScene(Type aSceneType)
    {
        if (aSceneType == null || !aSceneType.IsSubclassOf(typeof(GfgVnScene)))
        {
            LogError("The passed type does not inherit " + typeof(GfgVnScene).Name);
            aSceneType = typeof(TestDialogue);
        }

        Assert(0 == ActionCoroutineHandle.KillCoroutine(), "A coroutine was already being executed.");

        GfgVnScene scene = (GfgVnScene)Activator.CreateInstance(aSceneType);
        return ActionCoroutineHandle.RunCoroutineIfNotRunning(scene._ExecuteActions(this));
    }

    private void OnOptionSubmitCallback(GfxTextMessage aMessage, GfxNotifyPanelTemplate aNotifyPanel, int aIndex)
    {
        if (aIndex < 0 || aIndex >= CallbackActionsBuffer.Count)
            LogError("The option index is invalid: " + aIndex + " count of options in buffer is: " + CallbackActionsBuffer.Count);
        else
            NextLabel = CallbackActionsBuffer[aIndex];
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

    public enum GfgVnSceneSkipable
    {
        NO_CONDITIONS,
        SKIPABLE,
        UNSKIPABLE,
    }

    private GfgVnSceneHandler m_handler;

    private const string NEXT_LABEL_ERROR = "Cannot set the next label multiple times in a single label! Next() and Option() calls need to be in different labels!";

    protected abstract void Begin();


    private const string CAN_PLAY_FUNC_NAME = "CanPlay";

    private const string CAN_SKIP_FUNC_NAME = "CanSkip";

    public static GfgVnScenePlayable CanPlayScene<T>() { return CanPlayScene(typeof(T)); }

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

    public static GfgVnSceneSkipable CanSkipScene<T>() { return CanSkipScene(typeof(T)); }

    public static GfgVnSceneSkipable CanSkipScene(Type aScene)
    {
        GfgVnSceneSkipable ret = GfgVnSceneSkipable.NO_CONDITIONS;
        MethodInfo canPlayFunc = aScene.GetMethod(CAN_SKIP_FUNC_NAME, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (canPlayFunc != null)
        {
            bool skipable = (bool)canPlayFunc.Invoke(null, null);
            ret = skipable ? GfgVnSceneSkipable.SKIPABLE : GfgVnSceneSkipable.UNSKIPABLE;
        }

        return ret;
    }

    public void EndCurrentScene()
    {
        m_handler.ActionCoroutineHandle.KillCoroutine();
        //go to menu or something dunno, todo
    }

    private void ExecuteLabel(MethodInfo aLabelMethod)
    {
        Assert(aLabelMethod != null, "The method info passed is null!");

        m_handler.NextLabel = null;
        m_handler.HasCachedDialogueSetting = false;

        m_handler.LabelStack.Clear();
        m_handler.OptionsBuffer.Clear();
        m_handler.DialogueActionsBuffer.Clear();
        m_handler.CallbackActionsBuffer.Clear();

        m_handler.DialogueStringBuffer.SetLength(m_handler.CurrentSceneNameLength);

        PushLabel(aLabelMethod.Name);
        aLabelMethod?.Invoke(this, null);
    }

    public IEnumerator<float> _ExecuteActions(GfgVnSceneHandler aHandler)
    {
        m_handler = aHandler;
        m_handler.DialogueStringBuffer.Clear();
        m_handler.DialogueStringBuffer.Append(GetType().Name);
        m_handler.CurrentSceneNameLength = m_handler.DialogueStringBuffer.Length;
        GfxNotifyPanelTemplate notifyPanel = m_handler.NotifyPanel;

        Next(Begin);

    StartLabel:

        while (m_handler.NextLabel == null && !m_handler.OptionsBuffer.IsEmpty())
            yield return Timing.WaitForOneFrame;

        ExecuteLabel(m_handler.NextLabel);

        for (int i = 0; i < m_handler.DialogueActionsBuffer.Count; ++i)
        {
            CornDialogueAction dialogueAction = m_handler.DialogueActionsBuffer[i];

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

        if (m_handler.NextLabel == null && m_handler.OptionsBuffer.IsEmpty())
        {
            CoroutineHandle writerHandle = notifyPanel.GetCoroutineHandle();
            if (writerHandle.IsValid)
                yield return Timing.WaitUntilDone(writerHandle);

            m_handler.ActionCoroutineHandle.Finished();
            EndCurrentScene();
        }
        else
            goto StartLabel;
    }

    protected void PushLabel(string aLabelName)
    {
        //remove any postfix from the buffer
        if (m_handler.LabelStack.Count > 0)
            m_handler.DialogueStringBuffer.SetLength(m_handler.LabelStack.Peek().DialogueStringPrefixLength);

        m_handler.DialogueStringBuffer.Append(aLabelName);

        m_handler.LabelStack.Push(new()
        {
            LabelStatementCount = 0,
            DialogueStringPrefixLength = m_handler.DialogueStringBuffer.Length,
        });
    }

    protected void PopLabel()
    {
        if (m_handler.LabelStack.Count > 1)
            m_handler.LabelStack.Pop();
        else
            LogError("Cannot pop the main label! Only secondary labels created with 'PushLabel' can be popped!");
    }

    private string GetTranslatedText(string aText)
    {
        if (aText == null)
            return null;

        GfgVnSceneLabelInfo currentLabel = m_handler.LabelStack.Peek();

        m_handler.DialogueStringBuffer.SetLength(currentLabel.DialogueStringPrefixLength);
        m_handler.DialogueStringBuffer.Append(currentLabel.LabelStatementCount);
        currentLabel.LabelStatementCount++;

        return new GfcLocalizedString(aText, GfcLocalizationStringTable.DIALOGUE, m_handler.DialogueStringBuffer);
    }

    protected void Wait(float aSeconds)
    {
        CornDialogueAction action = new()
        {
            ActionType = CornDialogueActionType.WAIT,
            WaitTime = aSeconds,
        };

        m_handler.DialogueActionsBuffer.Add(action);
    }

    protected void Environment(GfcSceneId aScene)
    {
        CornDialogueAction action = new()
        {
            ActionType = CornDialogueActionType.ENVIRONMENT,
            Scene = aScene,
        };

        m_handler.DialogueActionsBuffer.Add(action);
    }

    private void CheckForNextLabel() { Assert(m_handler.NextLabel == null, NEXT_LABEL_ERROR); }

    protected void SayKey(string aUniqueKeyPostfix) { SayKey(aUniqueKeyPostfix, m_handler.HasCachedDialogueSetting ? m_handler.CachedDialogueSetting : default); }
    protected void SayKey(string aUniqueKeyPostfix, GfcStoryCharacter aCharacter, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL, string aNameOverride = null) { SayKey(aUniqueKeyPostfix, new(aCharacter, anEmotion, aNameOverride)); }

    protected void SayKey(string aUniqueKeyPostfix, CornDialogueSetting aSetting = default)
    {
        GfgVnSceneLabelInfo currentLabel = m_handler.LabelStack.Peek();

        m_handler.DialogueStringBuffer.SetLength(currentLabel.DialogueStringPrefixLength);
        m_handler.DialogueStringBuffer.Append(aUniqueKeyPostfix);

        SayUntranslated(GfcLocalization.GetString(GfcLocalizationStringTable.DIALOGUE, m_handler.DialogueStringBuffer), aSetting);
    }

    private string GetCharacterName(GfcStoryCharacter aCharacter)
    {
        //todo
        return aCharacter.ToString();
    }

    private GfcSound GetCharacterSound(GfcStoryCharacter aCharacter, CharacterSound aCharacterSound)
    {
        //todo
        return null;
    }

    protected void Say(string aText) { SayUntranslated(aText, m_handler.HasCachedDialogueSetting ? m_handler.CachedDialogueSetting : default); }
    protected void Say(string aText, GfcStoryCharacter aCharacter, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL, string aNameOverride = null) { SayUntranslated(aText, new(aCharacter, anEmotion, aNameOverride)); }
    protected void Say(string aText, CornDialogueSetting aSetting) { SayUntranslated(GetTranslatedText(aText), aSetting); }

    protected void SayUntranslated(string aText, CornDialogueSetting aSetting)
    {
        m_handler.HasCachedDialogueSetting = true;
        m_handler.CachedDialogueSetting = aSetting;

        if (aText != null)
        {
            string name = aSetting.NameOverride;
            if (name == null && aSetting.Character != GfcStoryCharacter.NONE)
                name = GetCharacterName(aSetting.Character);

            GfcSound sound = aSetting.SoundOverride;
            if (sound == null && aSetting.Character != GfcStoryCharacter.NONE && aSetting.CharacterSound != CharacterSound.NONE)
                sound = GetCharacterSound(aSetting.Character, aSetting.CharacterSound);

            GfxTextMessage message = default;
            message.MainText = aText;
            message.OptionCallback = m_handler.ButtonCallback;
            message.Name = name;
            message.Character = aSetting.Character;
            message.Emotion = aSetting.CharacterEmotion;
            message.Sound = sound;

            CornDialogueAction action = new()
            {
                ActionType = CornDialogueActionType.TEXT,
                Message = message,
            };

            m_handler.DialogueActionsBuffer.Add(action);
        }
    }

    protected void Action(Action anAction)
    {
        CornDialogueAction action = new()
        {
            ActionType = CornDialogueActionType.ACTION,
            Action = anAction,
        };

        m_handler.DialogueActionsBuffer.Add(action);
    }

    private void CheckTypeOfLastAction(CornDialogueActionType aType)
    {
        if (m_handler.DialogueActionsBuffer.Count > 0)
        {
            CornDialogueActionType lastActionType = m_handler.DialogueActionsBuffer[^1].ActionType;
            Assert(aType == lastActionType, "The last action performed needs to be of type " + aType + " but it is of type " + lastActionType);
        }
        else
            LogError("The last action performed needs to be of type " + aType + ", but there is no previous action!");
    }

    protected void Append(string aTextToAppend)
    {
        CheckTypeOfLastAction(CornDialogueActionType.TEXT);

        CornDialogueAction dialogueAction = m_handler.DialogueActionsBuffer[^1];
        GfxTextMessage message = dialogueAction.Message;

        if (message.Options.IsEmpty())
        {
            message.MainText += aTextToAppend;
            dialogueAction.Message = message;
            m_handler.DialogueActionsBuffer[^1] = dialogueAction;
        }
        else
        {
            int lastOptionIndex = message.Options.Count - 1;
            GfxNotifyOption option = message.Options[lastOptionIndex];
            option.OptionText += aTextToAppend;
            message.Options[lastOptionIndex] = option;
        }
    }

    protected void Next(Action aNextLabel)
    {
        CheckForNextLabel();
        Assert(m_handler.OptionsBuffer.IsEmpty(), NEXT_LABEL_ERROR);
        Assert(aNextLabel != null, "The label passed is invalid!");
        m_handler.NextLabel = aNextLabel.GetMethodInfo();
    }

    protected void Option(string aText, Action aJumpLabel, bool anIsDisabled = false, string aDisabledReason = null)
    {
        OptionUntranslated(GetTranslatedText(aText), aJumpLabel, anIsDisabled, GetTranslatedText(aDisabledReason));
    }

    protected void OptionUntranslated(string aText, Action aJumpLabel, bool anIsDisabled = false, string aDisabledReason = null)
    {
        CheckForNextLabel();
        CheckTypeOfLastAction(CornDialogueActionType.TEXT);

        m_handler.CallbackActionsBuffer.Add(aJumpLabel.GetMethodInfo());

        GfxNotifyOption option = new(aText, anIsDisabled, aDisabledReason);
        m_handler.OptionsBuffer.Add(option);

        CornDialogueAction dialogueAction = m_handler.DialogueActionsBuffer[^1];
        GfxTextMessage message = dialogueAction.Message;
        message.Options = m_handler.OptionsBuffer;

        dialogueAction.Message = message;
        m_handler.DialogueActionsBuffer[^1] = dialogueAction;
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
    public CornDialogueSetting(GfcStoryCharacter aCharacter = GfcStoryCharacter.NONE, CharacterEmotion anEmotion = CharacterEmotion.NEUTRAL, string aNameOverride = null)
    {
        Character = aCharacter;
        CharacterEmotion = anEmotion;
        NameOverride = aNameOverride;
        SoundOverride = null;
        CharacterSound = CharacterSound.NONE;
    }

    public GfcStoryCharacter Character;
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