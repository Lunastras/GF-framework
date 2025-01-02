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
        if (aEvent.EventType == CornEventType.SLEEP)
            aEvent.EventTypeSub = (int)CornSleepType.INTERRUPTED;

        CornEventCostAndRewards eventRewardsAndCost = CornManagerBalancing.GetEventCostAndRewards(aEvent.EventType, out string message, aEvent.EventTypeSub);
        CoroutineHandle eventHandle = default;

        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;

        List<string> messagesBuffer = GetMessagesBuffer();

        if (CanAffordMoney(aEvent))
        {
            bool wasteTime = eventRewardsAndCost.EventHasCornRoll
            && !GuaranteedCornRollSuccess()
            && GetEffectiveSanity() < UnityEngine.Random.Range(1, DICE_ROLL_NUM_FACES + 1);

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

                    case CornEventType.SLEEP:
                        eventHandle = Timing.RunCoroutine(_ExecuteSleepEvent(messagesBuffer));
                        eventRewardsAndCost = default;
                        break;
                }

                message ??= "<i>Pretty</i> cool event <color=red>" + aEvent.EventType.ToString() + "</color> was finished.";
            }
        }
        else
        {
            message = "You do not have enough money for this action.";
            eventRewardsAndCost = default;
        }

        if (!eventRewardsAndCost.CanAfford())
        {
            messagesBuffer.Add("Your fatigue mists your judgement and affects your sanity. You should go to sleep soon.");
            cornSaveData.MentalSanity--;
            CheckGameOver();
        }

        if (!message.IsEmpty()) messagesBuffer.Add(message);

        GfcCursor.RemoveSelectedGameObject();

        if (eventHandle.IsValid)
            yield return Timing.WaitUntilDone(eventHandle);

        if (aEvent.EventType == CornEventType.SLEEP)
            eventRewardsAndCost = SleepingMinigameData.GetFinalRewards();
        eventRewardsAndCost.ApplyModifiersToPlayer(0);

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
            CornManagerStory.CheckIfCurrentPhaseDone();
        }

        yield return Timing.WaitForOneFrame;
    }

    private static IEnumerator<float> _ExecuteChoresEvent(List<string> aMessageBuffer)
    {
        var playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        var cornSaveData = playerSaveData.Data;

        yield return Timing.WaitUntilDone(GfgManagerSceneLoader.LoadScene(GfcSceneId.IRISU));
        yield return Timing.WaitUntilDone(IrisuManagerGame.GetCoroutineHandle());
        float level = IrisuManagerGame.GetDifficulty();
        float choresExtraPoints = IrisuManagerGame.GetDifficulty() * (CornManagerBalancing.GetBaseChoresMiniGameExtraPoints() + cornSaveData.GetValue(CornPlayerSkillsStats.HANDICRAFT));

        aMessageBuffer.Add("You got " + choresExtraPoints * 100 + " extra Chores points!");
        cornSaveData.ApplyModifier(CornPlayerResources.CHORES, choresExtraPoints);

        yield return Timing.WaitForOneFrame;
    }

    private static CornHoursSleepData SleepingMinigameData;
    private static IEnumerator<float> _ExecuteSleepEvent(List<string> aMessageBuffer)
    {
        var playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        SleepingMinigameData.Initialize();

        if (SleepingMinigameData.Sleep() == CornHourSleepResult.DAY_TIME)
        {
            yield return Timing.WaitUntilDone(GfgManagerSceneLoader.LoadScene(GfcSceneId.WHACK_A_WOLF));
            CornWhackAWolf.Instance.OnGameEnd += OnWhackAWolfGameEnd;
            yield return Timing.WaitUntilDone(CornWhackAWolf.GetCoroutineHandle());
        }

        aMessageBuffer.Add(SleepingMinigameData.GetSleepMessage());

        yield return Timing.WaitForOneFrame;
    }

    private static void OnWhackAWolfGameEnd(CornWhackAWolfGameResult aResult)
    {
        if (aResult == CornWhackAWolfGameResult.WIN)
            if (SleepingMinigameData.Sleep() == CornHourSleepResult.DAY_TIME)
                CornWhackAWolf.RequestOneMoreGame();
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
            CheckGameOver();
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

    public static IEnumerator<float> CheckGameOver()
    {
        var playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        playerSaveData.Data.MentalSanity.MaxSelf(0);

        if (playerSaveData.Data.MentalSanity == 0)
            Debug.LogError("Game over, but not implemented yet");

        yield return Timing.WaitForOneFrame;
    }

    public static bool CanAfford(CornEvent aEvent, float aBonusMultiplier = 0) { return CornManagerBalancing.GetEventCostAndRewards(aEvent).CanAfford(aBonusMultiplier); }
    public static bool CanAffordMoney(CornEvent aEvent, float aBonusMultiplier = 0) { return CornManagerBalancing.GetEventCostAndRewards(aEvent).CanAffordMoney(aBonusMultiplier); }

    public static bool CanAfford(CornPlayerConsumables aType, float aValue) { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(aType, aValue); }
    public static bool CanAfford(CornPlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(aModifier, aMultiplier, aBonusMultiplier); }
    public static bool CanAfford<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<CornPlayerConsumablesModifier> { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAfford(someModifiers, aMultiplier, aBonusMultiplier); }
    public static bool CanAffordMoney<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<CornPlayerConsumablesModifier> { return GfgManagerSaveData.GetActivePlayerSaveData().Data.CanAffordMoney(someModifiers, aMultiplier, aBonusMultiplier); }

    public static void ApplyModifier(CornPlayerResources aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aType, aValue); }
    public static void ApplyModifier(CornPlayerConsumables aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aType, aValue); }

    public static void ApplyModifier(CornPlayerResourcesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aModifier, aMultiplier, aBonusMultiplier); }
    public static void ApplyModifier(CornPlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifier(aModifier, aMultiplier, aBonusMultiplier); }

    public static void ApplyModifierResourceList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<CornPlayerResourcesModifier> { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifierResourceList(someModifiers, aMultiplier, aBonusMultiplier); }
    public static void ApplyModifierConsumablesList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<CornPlayerConsumablesModifier> { GfgManagerSaveData.GetActivePlayerSaveData().Data.ApplyModifierConsumablesList(someModifiers, aMultiplier, aBonusMultiplier); }
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

public struct CornHoursSleepData
{
    public int HourseSlept;
    public int DesiredHoursSleep;
    public float EnergyPerHour;
    public bool CaughtNight;
    public int CurrentHour;

    public void Initialize()
    {
        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;

        HourseSlept = 0;
        var eventRewardsAndCost = CornManagerBalancing.GetEventCostAndRewardsRaw(CornEventType.SLEEP);
        Debug.Assert(eventRewardsAndCost.ConsumablesModifier.Length == 1 && eventRewardsAndCost.ConsumablesModifier[0].Type == CornPlayerConsumables.ENERGY, " Because of the new design, the only consumable sleep should replenish is energy.");
        DesiredHoursSleep = eventRewardsAndCost.HoursDuration - 1; //-1 because one hour has no benefits to the stats
        Debug.Assert(DesiredHoursSleep > 0);

        EnergyPerHour = eventRewardsAndCost.ConsumablesModifier[0].Value / DesiredHoursSleep;
        CurrentHour = cornSaveData.CurrentHour;
        CaughtNight = false;
        DesiredHoursSleep.MinSelf((int)(1.001f + ((1.0f - cornSaveData.GetValue(CornPlayerConsumables.ENERGY)) / EnergyPerHour)));
    }

    public CornHourSleepResult Sleep()
    {
        while (HourseSlept < DesiredHoursSleep)
        {
            ++HourseSlept;
            CurrentHour = ++CurrentHour % 24;
            bool nightTime = IsSleepHour();
            CaughtNight |= nightTime;
            if (!nightTime) return CornHourSleepResult.DAY_TIME;
        }

        return CornHourSleepResult.FINISHED_SLEEP;
    }

    public void IncrementHour()
    {
        HourseSlept++;
        CurrentHour = ++CurrentHour % 24;
        CaughtNight |= IsSleepHour();
    }

    public CornEventCostAndRewards GetFinalRewards()
    {
        var rewards = CornManagerBalancing.Instance.SleepRewardsTemp;
        rewards.ConsumablesModifier[0].Value = HourseSlept * EnergyPerHour;
        rewards.HoursDuration = HourseSlept + 1;
        return rewards;
    }

    public readonly string GetSleepMessage()
    {
        string message;
        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        if (HourseSlept == DesiredHoursSleep || 1 <= cornSaveData.GetValue(CornPlayerConsumables.ENERGY) + HourseSlept * EnergyPerHour)
            message = "You woke up feeling well rested!";
        else if (CaughtNight)
            message = "You woke up early because of the sunlight, might as well start your day early, right?";
        else
            message = "You couldn't sleep that well because of the sunlight. Go to sleep at night for a good night's rest.";

        return message;
    }

    public bool IsSleepHour() { return CornManagerBalancing.IsSleepHour(CurrentHour); }
}

public enum CornHourSleepResult
{
    FINISHED_SLEEP,
    DAY_TIME,
}