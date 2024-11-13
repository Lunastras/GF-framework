using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

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

    public static bool ExecutingEvent { get { return !Instance.m_canPlayEvent; } }

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
        CoroutineHandle eventHandle = default;

        if (CanAfford(aEvent))
        {
            bool wasteTime = eventDetails.EventHasCornRoll && MentalSanity < UnityEngine.Random.Range(1, DICE_ROLL_NUM_FACES);

            if (wasteTime && false) //delme
            {
                eventDetails = CornManagerBalancing.GetEventCostAndRewards(CornEventType.CORN);
                message = "You wasted time and didn't do anything... You watched corn videos obsessively.";
            }
            else //perform the event normally
            {
                //event stuff
                switch (aEvent.EventType)
                {
                    case CornEventType.WORK:
                        eventHandle = Timing.RunCoroutine(_ExecuteWorkEvent());
                        break;

                    case CornEventType.SOCIAL:
                        eventHandle = CornManagerStory.StartStoryScene(aEvent.Scene);
                        break;
                }

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

        GfcCursor.RemoveSelectedGameObject();

        if (eventHandle.IsValid) yield return Timing.WaitUntilDone(eventHandle);

        CornManagerPhone.CanTogglePhone = false;

        CoroutineHandle apartmentLoadRoutine = GfgManagerSceneLoader.LoadScene(GfcSceneId.APARTMENT, GfcGameState.APARTMENT);
        if (apartmentLoadRoutine.IsValid)
        {
            GfgManagerSceneLoader.FakeWait = true;
            yield return Timing.WaitUntilDone(apartmentLoadRoutine);
        }

        CornManagerPhone.LoadAvailableStoryScenes();

        CoroutineHandle transitionCoroutine = ProgressTime(eventDetails.HoursDuration, messagesBuffer);
        yield return Timing.WaitUntilDone(transitionCoroutine);

        CornManagerPhone.CanTogglePhone = true;


        Instance.m_canPlayEvent = true;
    }

    private static bool WaitingForEndOfLevel = false;

    private static void OnLevelEndSubmit() { WaitingForEndOfLevel = false; }

    private static IEnumerator<float> _ExecuteWorkEvent()
    {
        var playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        int previousMilestone = playerSaveData.CurrentMilestone;
        GfgManagerSaveData.GetActivePlayerSaveData().Data.GameProgress += CornManagerBalancing.GetGameProgressOnWork();
        int newMilestone = playerSaveData.CurrentMilestone;
        if (previousMilestone < newMilestone)
        {
            //new milestone achieved
            WaitingForEndOfLevel = true;
            Instance.m_messagesBuffer.Add("New milestone achieved! Currently at milestone " + newMilestone);

            GfgManagerSceneLoader.LoadScene(GfcSceneId.CORN_LEVEL_0 + previousMilestone);

            GfgManagerLevel.OnLevelEndSubmit += OnLevelEndSubmit;
            while (WaitingForEndOfLevel) yield return Timing.WaitForSeconds(0.033f);
            GfgManagerLevel.OnLevelEndSubmit -= OnLevelEndSubmit;
        }

        yield return Timing.WaitForOneFrame;
    }

    protected void OnDestroy()
    {
        GfgManagerLevel.OnLevelEndSubmit -= OnLevelEndSubmit;
    }

    public static void UpdateVisuals()
    {
        CornMenuApartment.UpdateGraphics();
    }

    //should only be called when an action happens
    protected static CoroutineHandle ProgressTime(int aElapsedHours, List<string> someMessages = null, bool anFadeToBlack = true)
    {
        PlayerSaveData playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        TimeSpan timePassed = playerSaveData.Data.ProgressTime(aElapsedHours);

        for (int i = 0; i < timePassed.Months; ++i)
        {
            CornManagerBalancing.GetEventCostAndRewards(CornEventType.NEW_MONTH).ApplyModifiersToPlayer();
            someMessages.Add("Month passed, take some money lmao");
        }

        for (int i = 0; i < timePassed.Days; ++i)
        {
            someMessages ??= GetMessagesBuffer();

            string message = "Day " + playerSaveData.Data.CurrentDay;

            bool loseSanity = false;
            bool gainSanity = true;

            for (int resourceIndex = 0; resourceIndex < (int)PlayerResources.COUNT; ++resourceIndex)
            {
                gainSanity &= playerSaveData.Data.Resources[resourceIndex] >= 0.5f;
                loseSanity |= playerSaveData.Data.Resources[resourceIndex] <= 0;
            }

            CornManagerBalancing.GetEventCostAndRewards(CornEventType.NEW_DAY).ApplyModifiersToPlayer();

            if (gainSanity)
            {
                message += ", +1 sanity, good job.";
                playerSaveData.Data.MentalSanity++;
            }
            else if (loseSanity)
            {
                message += ", -1 sanity, try to take better care of yourself, otherwise, you will spiral out of control...";
                playerSaveData.Data.MentalSanity--;
            }

            someMessages.Add(message);
            playerSaveData.Data.MentalSanity = Mathf.Clamp(playerSaveData.Data.MentalSanity, 0, DICE_ROLL_NUM_FACES);
        }

        return Timing.RunCoroutine(_FlushMessagesAndDrawHud(anFadeToBlack));
    }

    private static IEnumerator<float> _FlushMessagesAndDrawHud(bool anFadeToBlack = true)
    {
        float fadeTime = Instance.m_screenFadeTime;

        if (anFadeToBlack)
        {
            GfxUiTools.FadeOverlayAlpha(1, fadeTime);
            GfcCursor.RemoveSelectedGameObject();

            yield return Timing.WaitForSeconds(fadeTime);
        }

        GfgManagerSceneLoader.FakeWait = false;

        UpdateVisuals();

        CoroutineHandle handle = GfxUiTools.GetNotifyPanel().DrawMessage(Instance.m_messagesBuffer);

        if (handle.IsValid) yield return Timing.WaitUntilDone(handle);

        Instance.m_messagesBuffer.Clear();

        if (anFadeToBlack)
        {
            GfxUiTools.FadeOverlayAlpha(0, fadeTime);
            yield return Timing.WaitForSeconds(fadeTime);
        }
    }

    public static bool CanAfford(CornEvent aEvent, float aBonusMultiplier = 0) { return CornManagerBalancing.GetEventCostAndRewards(aEvent).CanAfford(aBonusMultiplier); }
    public static bool CanAfford(PlayerConsumables aType, float aValue) { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(aType, aValue); }
    public static bool CanAfford(PlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(aModifier, aMultiplier, aBonusMultiplier); }
    public static bool CanAfford<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerConsumablesModifier> { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(someModifiers, aMultiplier, aBonusMultiplier); }


    public static void ApplyModifier(PlayerResources aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aType, aValue); }
    public static void ApplyModifier(PlayerConsumables aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aType, aValue); }

    public static void ApplyModifier(PlayerResourcesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aModifier, aMultiplier, aBonusMultiplier); }
    public static void ApplyModifier(PlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aModifier, aMultiplier, aBonusMultiplier); }

    public static void ApplyModifierResourceList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerResourcesModifier> { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifierResourceList(someModifiers, aMultiplier, aBonusMultiplier); }
    public static void ApplyModifierConsumablesList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerConsumablesModifier> { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifierConsumablesList(someModifiers, aMultiplier, aBonusMultiplier); }
    public static int MentalSanity { get { return GfgManagerSaveData.GetActivePlayerSaveData().Data.MentalSanity; } }
}

[Serializable]
public struct CornEvent
{
    public CornEvent(CornEventType aEventType, uint aEventTypeSub = 0, Type aScene = null)
    {
        EventType = aEventType;
        EventTypeSub = aEventTypeSub;
        Scene = aScene;
    }

    public CornEventType EventType;
    public uint EventTypeSub; //the sub event of the EventType, single EventType can have multiple events. For story scenes, this can be the ID of the character the player is meeting with
    public Type Scene;
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
