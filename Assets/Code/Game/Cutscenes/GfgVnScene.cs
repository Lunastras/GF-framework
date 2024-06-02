using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MEC;
using static UnityEngine.Debug;

public class GfgVnScene
{
    private static int CurrentSceneNameLength = 0;

    private static bool OurHasOptions = false;

    private static List<GfxTextMessage> OurMessagesBuffer = new(128);

    private static List<MessageOption> OurOptionsBuffer = new(4);

    private static List<MethodInfo> OurCallbackActionsBuffer = new(4);

    private static List<CornDialogueAction> OurDialogueActionsBuffer = new(128);

    private static GfcStringBuffer OurDialogueStringBuffer = new(32);

    private static Stack<LabelInfo> OurLabelStack = new(4);

    private static readonly Action<GfxTextMessage, GfxNotifyPanelGeneric, int> CALLBACK = new(OnOptionSubmitCallback);

    private const string START_FUNC = "Start";

    private static GfcCoroutineHandle OurActionCoroutineHandle = default;

    private static MethodInfo NextLabel = null;

    private const string NEXT_LABEL_ERROR = "Cannot set the next label multiple times in a single label! Next() and Option() calls need to be in different labels!";

    public static CoroutineHandle StartScene(Type aSceneType)
    {
        OurDialogueStringBuffer.Clear();
        OurDialogueStringBuffer.Concatenate(aSceneType.Name);
        CurrentSceneNameLength = OurDialogueStringBuffer.Length;

        Log("Writing scene " + aSceneType);

        Assert(0 == OurActionCoroutineHandle.KillCoroutine(), "A coroutine was already being executed.");
        Assert(typeof(GfgVnScene) != aSceneType && aSceneType.IsSubclassOf(typeof(GfgVnScene)), "The passed type does not inherit " + typeof(GfgVnScene).Name);

        NextLabel = aSceneType.GetMethod(START_FUNC, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert(NextLabel != null, "The function " + START_FUNC + "() could not be found in the class " + aSceneType.Name);

        return OurActionCoroutineHandle.RunCoroutineIfNotRunning(_ExecuteActions());
    }

    private static void ExecuteLabel(MethodInfo aLabelMethod)
    {
        Assert(aLabelMethod != null, "The method info passed is null!");

        NextLabel = null;
        OurHasOptions = false;

        OurLabelStack.Clear();
        OurOptionsBuffer.Clear();
        OurMessagesBuffer.Clear();
        OurDialogueActionsBuffer.Clear();
        OurCallbackActionsBuffer.Clear();

        OurDialogueStringBuffer.SetLength(CurrentSceneNameLength);

        PushLabel(aLabelMethod.Name);
        aLabelMethod?.Invoke(null, null);
    }

    private static IEnumerator<float> _ExecuteActions()
    {
        GfxNotifyPanelGeneric notifyPanel = GfxUiTools.GetDialoguePanel();

    StartLabel:

        while (NextLabel == null && OurHasOptions)
            yield return Timing.WaitForOneFrame;

        ExecuteLabel(NextLabel);

        for (int i = 0; i < OurDialogueActionsBuffer.Count; ++i)
        {
            CornDialogueAction dialogueAction = OurDialogueActionsBuffer[i];

            if (dialogueAction.ActionType == CornDialogueActionType.TEXT)
            {
                OurMessagesBuffer.Add(dialogueAction.Message);
            }
            else
            {
                CoroutineHandle notifyHandle = notifyPanel.DrawMessages(OurMessagesBuffer);
                if (notifyHandle.IsValid) yield return Timing.WaitUntilDone(notifyHandle);

                OurMessagesBuffer.Clear();
                switch (dialogueAction.ActionType)
                {
                    case CornDialogueActionType.ENVIRONMENT:
                        yield return Timing.WaitUntilDone(GfgManagerSceneLoader.LoadScene(dialogueAction.Scene));
                        break;

                    case CornDialogueActionType.SOUND:
                        break;

                    case CornDialogueActionType.WAIT:
                        yield return Timing.WaitForSeconds(dialogueAction.WaitTime);
                        break;
                }
            }
        }

        if (OurMessagesBuffer.Count > 0)
            notifyPanel.DrawMessages(OurMessagesBuffer);

        OurMessagesBuffer.Clear();

        if (NextLabel == null && !OurHasOptions)
        {
            CoroutineHandle writerHandle = notifyPanel.GetCoroutineHandle();
            if (writerHandle.IsValid)
                yield return Timing.WaitUntilDone(writerHandle);

            OurActionCoroutineHandle.Finished();
            EndCurrentScene();
        }
        else
            goto StartLabel;
    }

    protected static void PushLabel(string aLabelName)
    {
        //remove any postfix from the buffer
        if (OurLabelStack.Count > 0)
            OurDialogueStringBuffer.SetLength(OurLabelStack.Peek().DialogueStringPrefixLength);

        OurDialogueStringBuffer.Concatenate(aLabelName);

        OurLabelStack.Push(new()
        {
            LabelStatementCount = 0,
            DialogueStringPrefixLength = OurDialogueStringBuffer.Length,
        });
    }

    protected static void PopLabel()
    {
        Assert(OurLabelStack.Count > 1, "Cannot pop the main label! Only secondary labels created with 'PushLabel' can be popped!");
        OurLabelStack.Pop();
    }

    private static string GetLocalizedText(string aKey)
    {
        //todo
        return string.Copy(aKey); ;
    }

    private static string GetCurrentLocalizedString()
    {
        LabelInfo currentLabel = OurLabelStack.Pop();

        OurDialogueStringBuffer.SetLength(currentLabel.DialogueStringPrefixLength);
        OurDialogueStringBuffer.Concatenate(currentLabel.LabelStatementCount);
        currentLabel.LabelStatementCount++;

        OurLabelStack.Push(currentLabel);
        return GetLocalizedText(OurDialogueStringBuffer);
    }

    protected static void Wait(float aSeconds)
    {
        CornDialogueAction action = new()
        {
            ActionType = CornDialogueActionType.WAIT,
            WaitTime = aSeconds,
        };

        OurDialogueActionsBuffer.Add(action);
    }

    protected static void Environment(GfcScene aScene)
    {
        CornDialogueAction action = new()
        {
            ActionType = CornDialogueActionType.ENVIRONMENT,
            Scene = aScene,
        };

        OurDialogueActionsBuffer.Add(action);
    }

    private static void CheckForNextLabel() { Assert(NextLabel == null, NEXT_LABEL_ERROR); }

    protected static void Say(CornDialogueSetting aSetting = default, string aNameOverride = null) { Say(GetCurrentLocalizedString(), aSetting, aNameOverride); }

    protected static void SayKey(string aUniqueKeyPostfix, CornDialogueSetting aSetting = default, string aNameOverride = null)
    {
        LabelInfo currentLabel = OurLabelStack.Peek();

        OurDialogueStringBuffer.SetLength(currentLabel.DialogueStringPrefixLength);
        OurDialogueStringBuffer.Concatenate(aUniqueKeyPostfix);

        Say(GetLocalizedText(OurDialogueStringBuffer), aSetting, aNameOverride);
    }

    protected static void Say(string aText, CornDialogueSetting aSetting = default, string aNameOverride = null)
    {
        string name = aNameOverride;
        if (name.IsEmpty() && aSetting.Character != StoryCharacter.NONE)
        {
            //todo
            name = aSetting.Character.ToString();
        }

        GfxTextMessage message = default;
        message.MainText = aText;
        message.OptionCallback = CALLBACK;
        message.Name = name;
        message.Character = aSetting.Character;

        CornDialogueAction action = new()
        {
            ActionType = CornDialogueActionType.TEXT,
            Message = message,
        };

        OurDialogueActionsBuffer.Add(action);
    }

    private static void CheckTypeOfLastAction(CornDialogueActionType aType)
    {
        if (OurDialogueActionsBuffer.Count > 0)
        {
            CornDialogueActionType lastActionType = OurDialogueActionsBuffer[^1].ActionType;
            Assert(aType == lastActionType, "The last action performed needs to be of type " + aType + " but it is of type " + lastActionType);
        }
        else
            LogError("The last action performed needs to be of type " + aType + ", but there is no previous action!");
    }

    protected static void Append() { Append(GetCurrentLocalizedString()); }

    protected static void Append(string aTextToAppend)
    {
        CheckTypeOfLastAction(CornDialogueActionType.TEXT);

        CornDialogueAction dialogueAction = OurDialogueActionsBuffer[^1];
        GfxTextMessage message = dialogueAction.Message;

        if (message.Options.IsEmpty())
        {
            message.MainText += aTextToAppend;
            dialogueAction.Message = message;
            OurDialogueActionsBuffer[^1] = dialogueAction;
        }
        else
        {
            int lastOptionIndex = message.Options.Count - 1;
            MessageOption option = message.Options[lastOptionIndex];
            option.OptionText += aTextToAppend;
            message.Options[lastOptionIndex] = option;
        }
    }

    protected static void Next(Action aNextLabel)
    {
        CheckForNextLabel();
        Assert(!OurHasOptions, NEXT_LABEL_ERROR);
        Assert(aNextLabel != null, "The label passed is invalid!");
        NextLabel = aNextLabel.GetMethodInfo();
    }

    protected static void Option(Action aJumpLabel, bool anIsDisabled = false) { Option(GetCurrentLocalizedString(), aJumpLabel, anIsDisabled); }

    protected static void Option(string aText, Action aJumpLabel, bool anIsDisabled = false)
    {
        CheckForNextLabel();
        CheckTypeOfLastAction(CornDialogueActionType.TEXT);

        OurCallbackActionsBuffer.Add(aJumpLabel.GetMethodInfo());

        MessageOption option = new(aText, anIsDisabled);
        OurOptionsBuffer.Add(option);

        CornDialogueAction dialogueAction = OurDialogueActionsBuffer[^1];
        GfxTextMessage message = dialogueAction.Message;
        message.Options = OurOptionsBuffer;

        dialogueAction.Message = message;
        OurDialogueActionsBuffer[^1] = dialogueAction;
        OurHasOptions = true;
    }

    public static void EndCurrentScene()
    {
        OurActionCoroutineHandle.KillCoroutine();
        //go to menu or something dunno, todo
    }

    private static void OnOptionSubmitCallback(GfxTextMessage aMessage, GfxNotifyPanelGeneric aNotifyPanel, int aIndex) { NextLabel = OurCallbackActionsBuffer[aIndex]; }
}

public struct CornDialogueAction
{
    public CornDialogueActionType ActionType;

    public GfxTextMessage Message;

    public GfcScene Scene;

    public float WaitTime;
}

public struct CornDialogueSetting
{
    public CornDialogueSetting(StoryCharacter aCharacter = StoryCharacter.NONE)
    {
        Character = aCharacter;
    }

    public StoryCharacter Character;
}

public enum CornDialogueActionType
{
    TEXT,
    SOUND,
    ENVIRONMENT,
    WAIT,
}

internal struct LabelInfo
{
    public int LabelStatementCount;
    public int DialogueStringPrefixLength;
}