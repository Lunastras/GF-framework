using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;
using Unity.Collections;

public class CornManagerEvents : MonoBehaviour
{
    protected static CornManagerEvents Instance;

    [SerializeField] protected float m_screenFadeTime = 0.6f;

    public const int DICE_ROLL_NUM_FACES = 10;

    private bool m_canPlayEvent = true;

    private List<string> m_messagesBuffer = new(4);

    protected static List<string> GetMessagesBuffer()
    {
        if (Instance.m_messagesBuffer.Count > 0)
            Debug.LogError("The messages buffer is not empty, was it not cleared when finished using or is another action happening right now?");

        return Instance.m_messagesBuffer;
    }

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);
        Cursor.visible = true;
    }

    public static void ExecuteEvent(CornEvent aEvent)
    {
        if (Instance.m_canPlayEvent)
        {
            Instance.m_canPlayEvent = false;
            Timing.RunCoroutine(_ExecuteEvent(aEvent));
        }
        else
            Debug.LogWarning("Already in the process of executing an event, could not exeucte event " + aEvent.EventType + " " + aEvent.EventTypeSub);
    }

    private static IEnumerator<float> _ExecuteEvent(CornEvent aEvent)
    {
        string message;
        CornEventCostAndRewards eventDetails = CornManagerBalancing.GetEventCostAndRewards(aEvent);
        if (CanAfford(aEvent))
        {
            bool wasteTime = eventDetails.EventHasCornRoll && MentalSanity < UnityEngine.Random.Range(1, DICE_ROLL_NUM_FACES);

            if (wasteTime)
            {
                eventDetails = CornManagerBalancing.GetEventCostAndRewards(CornEventType.CORN);
                message = "You wasted time and didn't do anything... You watched corn videos obsessively.";
            }
            else //perform the event normally
            {
                //event stuff
                message = "<i>Pretty</i> cool event <color=red>" + aEvent.EventType.ToString() + "</color> was finished.";
            }
        }
        else
        {
            eventDetails = default;
            message = "You cannot perform this action with your current consumables...";
        }

        List<string> messagesBuffer = GetMessagesBuffer();
        messagesBuffer.Add(message);

        eventDetails.ApplyModifiersToPlayer(0);

        CoroutineHandle eventHandle = default;

        GfxUiTools.RemoveSelectedGameObject();

        switch (aEvent.EventType)
        {
            case CornEventType.WORK:
                eventHandle = Timing.RunCoroutine(_ExecuteWorkEvent());
                break;

            case CornEventType.SOCIAL:
                eventHandle = GfgVnScene.StartScene(typeof(TestDialogue)); //test todo
                break;
        }

        if (eventHandle.IsValid) yield return Timing.WaitUntilDone(eventHandle);

        CoroutineHandle apartmentLoadRoutine = GfgManagerSceneLoader.LoadScene(GfcScene.APARTMENT);
        if (apartmentLoadRoutine.IsValid)
        {
            GfgManagerSceneLoader.SkipFinalFade = true;
            GfgManagerSceneLoader.FakeWait = true;
            yield return Timing.WaitUntilDone(apartmentLoadRoutine);
        }

        CoroutineHandle transitionCoroutine = ProgressTime(eventDetails.HoursDuration, messagesBuffer);
        yield return Timing.WaitUntilDone(transitionCoroutine);

        Instance.m_canPlayEvent = true;
    }

    private static IEnumerator<float> _ExecuteWorkEvent()
    {
        var playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        int previousMilestone = playerSaveData.CurrentMilestone;
        GfgManagerSaveData.GetActivePlayerSaveData().GameProgress += CornManagerBalancing.GetGameProgressOnWork();
        int newMilestone = playerSaveData.CurrentMilestone;
        if (previousMilestone < newMilestone)
        {
            //new milestone achieved
            Instance.m_messagesBuffer.Add("New milestone achieved! Currently at milestone " + newMilestone);
        }

        yield return Timing.WaitForOneFrame;
    }

    public static void UpdateVisuals()
    {
        CornMenuApartment.UpdateGraphics();
    }

    //should only be called when an action happens
    protected static CoroutineHandle ProgressTime(int aElapsedHours, List<string> someMessages = null, bool anFadeToBlack = true)
    {
        PlayerSaveData playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        TimeSpan timePassed = playerSaveData.ProgressTime(aElapsedHours);

        for (int i = 0; i < timePassed.Months; ++i)
        {
            CornManagerBalancing.GetEventCostAndRewards(CornEventType.NEW_MONTH).ApplyModifiersToPlayer();
            someMessages.Add("Month passed, take some money lmao");
        }

        for (int i = 0; i < timePassed.Days; ++i)
        {
            someMessages ??= GetMessagesBuffer();

            string message = "Day " + playerSaveData.CurrentDay;

            bool loseSanity = false;
            bool gainSanity = true;

            for (int resourceIndex = 0; resourceIndex < (int)PlayerResources.COUNT; ++resourceIndex)
            {
                gainSanity &= playerSaveData.Resources[resourceIndex] >= 0.5f;
                loseSanity |= playerSaveData.Resources[resourceIndex] <= 0;
            }

            CornManagerBalancing.GetEventCostAndRewards(CornEventType.NEW_DAY).ApplyModifiersToPlayer();

            if (gainSanity)
            {
                message += ", +1 sanity, good job.";
                playerSaveData.MentalSanity++;
            }
            else if (loseSanity)
            {
                message += ", -1 sanity, try to take better care of yourself, otherwise, you will spiral out of control...";
                playerSaveData.MentalSanity--;
            }

            someMessages.Add(message);
            playerSaveData.MentalSanity = Mathf.Clamp(playerSaveData.MentalSanity, 0, DICE_ROLL_NUM_FACES);
        }

        return Timing.RunCoroutine(_FlushMessagesAndDrawHud(anFadeToBlack));
    }

    private static IEnumerator<float> _FlushMessagesAndDrawHud(bool anFadeToBlack = true)
    {
        float fadeTime = Instance.m_screenFadeTime;

        if (anFadeToBlack)
        {
            GfxUiTools.CrossFadeBlackAlpha(1, fadeTime);
            GfxUiTools.RemoveSelectedGameObject();

            yield return Timing.WaitForSeconds(fadeTime);
        }

        GfgManagerSceneLoader.FakeWait = false;

        UpdateVisuals();

        CoroutineHandle handle = GfxUiTools.GetNotifyPanel().DrawMessages(Instance.m_messagesBuffer);

        if (handle.IsValid) yield return Timing.WaitUntilDone(handle);

        Instance.m_messagesBuffer.Clear();

        if (anFadeToBlack)
        {
            GfxUiTools.CrossFadeBlackAlpha(0, fadeTime);
            yield return Timing.WaitForSeconds(fadeTime);
        }
    }

    public static bool CanAfford(CornEvent aEvent, float aBonusMultiplier = 0) { return CornManagerBalancing.GetEventCostAndRewards(aEvent).CanAfford(aBonusMultiplier); }
    public static bool CanAfford(PlayerConsumables aType, float aValue) { return GfgManagerSaveData.GetActivePlayerSaveData().CanAfford(aType, aValue); }
    public static bool CanAfford(PlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { return GfgManagerSaveData.GetActivePlayerSaveData().CanAfford(aModifier, aMultiplier, aBonusMultiplier); }
    public static bool CanAfford<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerConsumablesModifier> { return GfgManagerSaveData.GetActivePlayerSaveData().CanAfford(someModifiers, aMultiplier, aBonusMultiplier); }


    public static void ApplyModifier(PlayerResources aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifier(aType, aValue); }
    public static void ApplyModifier(PlayerConsumables aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifier(aType, aValue); }

    public static void ApplyModifier(PlayerResourcesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifier(aModifier, aMultiplier, aBonusMultiplier); }
    public static void ApplyModifier(PlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifier(aModifier, aMultiplier, aBonusMultiplier); }

    public static void ApplyModifierResourceList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerResourcesModifier> { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifierResourceList(someModifiers, aMultiplier, aBonusMultiplier); }
    public static void ApplyModifierConsumablesList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerConsumablesModifier> { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifierConsumablesList(someModifiers, aMultiplier, aBonusMultiplier); }
    public static int MentalSanity { get { return GfgManagerSaveData.GetActivePlayerSaveData().MentalSanity; } }
}

[Serializable]
public struct CornEvent
{
    public CornEvent(CornEventType aEventType, uint aEventTypeSub = 0)
    {
        EventType = aEventType;
        EventTypeSub = aEventTypeSub;
    }

    public CornEventType EventType;
    public uint EventTypeSub; //the sub event of the EventType, single EventType can have multiple events
}

public enum CornEventType
{
    WORK,
    CHORES,
    PERSONAL_TIME,
    SOCIAL,

    SLEEP,
    CORN,
    RANDOM,
    PERSONAL_GIFT,
    NEW_DAY,
    NEW_MONTH,
    COUNT
}

public enum CornEventTypeWork
{
    WORK_ON_GAME,
    STREAM_WORK,
    CONTRACT_WORK,
    COUNT
}

public enum CornEventTypeSocial
{
    COOL_FRIEND,
    SHADY_FRIEND,
    NO_IDEA_FRIEND,
    COUNT
}
