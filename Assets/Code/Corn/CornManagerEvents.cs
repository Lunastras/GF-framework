using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

public class CornManagerEvents : MonoBehaviour
{
    protected static CornManagerEvents Instance;

    [SerializeField] protected float m_screenFadeTime = 0.6f;

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

    void Start()
    {
        GfCommandConsole.RegisterCommand("PassWeek", () => { ProgressTime(7 * 24); });

        GfCommandConsole.RegisterCommand("MentalSanityMinus", () =>
        {
            GfgManagerSaveData.GetActivePlayerSaveData().Data.MentalSanity -= 5;
            CornMenuApartment.UpdateGraphics(true);
        });

        GfCommandConsole.RegisterCommand("MentalSanityPlus", () =>
        {
            GfgManagerSaveData.GetActivePlayerSaveData().Data.MentalSanity += 5;
            CornMenuApartment.UpdateGraphics(true);
        });

        GfCommandConsole.RegisterCommand("hesoyam", () =>
        {
            var data = GfgManagerSaveData.GetActivePlayerSaveData().Data;
            data.MentalSanity = CornManagerBalancing.DICE_ROLL_NUM_FACES;

            for (int i = 0; i < data.Resources.Length; i++)
                data.Resources[i] = 1;

            for (int i = 0; i < data.Consumables.Length; i++)
                data.Consumables[i] = 1;

            data.Consumables[(int)CornPlayerConsumables.MONEY] = 99999999;
            CornMenuApartment.UpdateGraphics(true);
        });
    }

    public static int DICE_ROLL_NUM_FACES { get { return CornManagerBalancing.DICE_ROLL_NUM_FACES; } }

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

    public static int GetEffectiveSanity()
    {
        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        int effectiveSanity = cornSaveData.MentalSanity;
        if (cornSaveData.HadCornEventSinceWakeUp) //double the likelyhood of a failed roll if a corn action has not been performed since waking up
            effectiveSanity = DICE_ROLL_NUM_FACES - 2 * (DICE_ROLL_NUM_FACES - cornSaveData.MentalSanity);
        return effectiveSanity;
    }

    //returns true if the action will executed. False if the corn event can play.
    public static bool GuaranteedCornRollSuccess() { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CornActionsInARow < CornManagerBalancing.MAX_CORN_ACTION_IN_ROW; }

    private static IEnumerator<float> _ExecuteEvent(CornEvent aEvent)
    {
        CornEventCostAndRewards eventRewardsAndCost = CornManagerBalancing.GetEventCostAndRewards(aEvent.EventType, out string message, aEvent.EventTypeSub);
        CoroutineHandle eventHandle = default;

        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;

        List<string> messagesBuffer = GetMessagesBuffer();

        if (CanAfford(aEvent))
        {
            bool wasteTime = eventRewardsAndCost.EventHasCornRoll
            && !GuaranteedCornRollSuccess()
            && GetEffectiveSanity() < UnityEngine.Random.Range(1, DICE_ROLL_NUM_FACES + 1);

            //delme false
            if (wasteTime)
            {
                cornSaveData.CornActionsInARow++;
                cornSaveData.HadCornEventSinceWakeUp = true;
                eventRewardsAndCost = CornManagerBalancing.GetEventCostAndRewards(CornEventType.CORN);
                message = "You wasted time and didn't do anything... You watched corn videos obsessively.";
            }
            else //perform the event normally
            {
                cornSaveData.CornActionsInARow = 0;
                //event stuff
                switch (aEvent.EventType)
                {
                    case CornEventType.WORK:
                        eventHandle = Timing.RunCoroutine(_ExecuteWorkEvent(messagesBuffer));
                        break;

                    case CornEventType.SOCIAL:
                        eventHandle = CornManagerStory.StartStoryScene(aEvent.Scene);
                        break;

                    case CornEventType.CHORES:
                        eventHandle = Timing.RunCoroutine(_ExecuteChoresEvent(messagesBuffer));
                        break;
                }

                message ??= "<i>Pretty</i> cool event <color=red>" + aEvent.EventType.ToString() + "</color> was finished.";
            }
        }
        else
        {
            eventRewardsAndCost = default;
            message = "You cannot perform this action with your current consumables...";
        }

        if (message != null) messagesBuffer.Add(message);

        eventRewardsAndCost.ApplyModifiersToPlayer(0);

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

        CoroutineHandle transitionCoroutine = ProgressTime(eventRewardsAndCost.HoursDuration, messagesBuffer);
        yield return Timing.WaitUntilDone(transitionCoroutine);

        CornManagerPhone.CanTogglePhone = true;


        Instance.m_canPlayEvent = true;
    }

    private static bool WaitingForEndOfLevel = false;

    private static void OnLevelEndSubmit() { WaitingForEndOfLevel = false; }

    private static IEnumerator<float> _ExecuteWorkEvent(List<string> aMessageBuffer)
    {
        var playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        if (!playerSaveData.Data.CurrentStoryPhaseGameLevelFinished)
        {
            var cornSaveData = playerSaveData.Data;
            //new milestone achieved
            WaitingForEndOfLevel = true;
            aMessageBuffer.Add("New milestone achieved! Currently at milestone " + (cornSaveData.CurrentStoryPhase + 1));

            GfgManagerSceneLoader.LoadScene(GfcSceneId.CORN_LEVEL_0 + cornSaveData.CurrentStoryPhase);

            GfgManagerLevel.OnLevelEndSubmit += OnLevelEndSubmit;
            while (WaitingForEndOfLevel) yield return Timing.WaitForSeconds(0.033f);
            GfgManagerLevel.OnLevelEndSubmit -= OnLevelEndSubmit;

            cornSaveData.CurrentStoryPhaseGameLevelFinished = true;
            playerSaveData.Data = cornSaveData;
        }

        yield return Timing.WaitForOneFrame;
    }

    private static IEnumerator<float> _ExecuteChoresEvent(List<string> aMessageBuffer)
    {
        var playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        var cornSaveData = playerSaveData.Data;

        yield return Timing.WaitUntilDone(GfgManagerSceneLoader.LoadScene(GfcSceneId.IRISU));
        yield return Timing.WaitUntilDone(IrisuManagerGame.GetGameHandle());
        float level = IrisuManagerGame.GetDifficulty();
        float choresExtraPoints = IrisuManagerGame.GetDifficulty() * (CornManagerBalancing.GetBaseChoresMiniGameExtraPoints() + cornSaveData.GetValue(CornPlayerSkillsStats.HANDICRAFT));

        aMessageBuffer.Add("You got " + choresExtraPoints * 100 + " extra Chores points!");
        cornSaveData.ApplyModifier(CornPlayerResources.CHORES, choresExtraPoints);
        playerSaveData.Data = cornSaveData;

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
        var playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        TimeSpan timePassed = playerSaveData.ProgressTime(aElapsedHours);

        someMessages ??= GetMessagesBuffer();

        for (int i = 0; i < timePassed.Weeks; ++i)
        {
            CornManagerBalancing.GetEventCostAndRewards(CornEventType.NEW_WEEK).ApplyModifiersToPlayer();
            playerSaveData.MaxMentalSanity--;
            playerSaveData.MentalSanity = Mathf.Clamp(playerSaveData.MentalSanity, 0, playerSaveData.MaxMentalSanity);
            someMessages.Add("New week passed, take some money lmao, but -1 sanity.");
        }

        for (int i = 0; i < timePassed.Days; ++i)
        {
            string message = "Day " + playerSaveData.CurrentDay;

            bool loseSanity = false;
            bool gainSanity = true;

            for (int resourceIndex = 0; resourceIndex < (int)CornPlayerResources.COUNT; ++resourceIndex)
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
            playerSaveData.MentalSanity = Mathf.Clamp(playerSaveData.MentalSanity, 0, playerSaveData.MaxMentalSanity);
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
    public static bool CanAfford(CornPlayerConsumables aType, float aValue) { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(aType, aValue); }
    public static bool CanAfford(PlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(aModifier, aMultiplier, aBonusMultiplier); }
    public static bool CanAfford<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerConsumablesModifier> { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(someModifiers, aMultiplier, aBonusMultiplier); }


    public static void ApplyModifier(CornPlayerResources aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aType, aValue); }
    public static void ApplyModifier(CornPlayerConsumables aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aType, aValue); }

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
    NEW_WEEK,
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
