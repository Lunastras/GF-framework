using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;
using Unity.Collections;

public class CornManagerEvents : MonoBehaviour
{
    protected static CornManagerEvents Instance;

    [SerializeField] protected float m_screenFadeTime = 0.6f;

    private GfcCoroutineHandle m_eventHandle;
    private GfcCoroutineHandle m_progressTimeHandle;
    private GfcCoroutineHandle m_gameOverHandle;

    private List<string> m_messagesBuffer = new(4);

    public static int DICE_ROLL_NUM_FACES { get { return CornManagerBalancing.DICE_ROLL_NUM_FACES; } }

    public static bool ExecutingEvent { get { return Instance.m_eventHandle.CoroutineIsRunning; } }

    public static CoroutineHandle EventHandle { get { return Instance.m_eventHandle; } }
    public static CoroutineHandle ProgressTimeHandle { get { return Instance.m_progressTimeHandle; } }

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
        GfCommandConsole.RegisterCommand("PassDay", () => { ProgressTime(24); });

        GfCommandConsole.RegisterCommand("PassWeek", () => { ProgressTime(7 * 24); });

        GfCommandConsole.RegisterCommand("kill", () =>
        {
            IncrementMentalSanity(-10, true);
            CornMenuApartment.UpdateGraphics(true);
        });

        GfCommandConsole.RegisterCommand("savebackup", () =>
        {
            GfgManagerSaveData.GetActivePlayerSaveData().MakeBackup();
        });

        GfCommandConsole.RegisterCommand("MentalSanityMinus", () =>
        {
            IncrementMentalSanity(-5, true);
            CornMenuApartment.UpdateGraphics(true);
        });

        GfCommandConsole.RegisterCommand("MentalSanityPlus", () =>
        {
            IncrementMentalSanity(5, true);
            CornMenuApartment.UpdateGraphics(true);
        });

        GfCommandConsole.RegisterCommand("hesoyam", () =>
        {
            var data = GfgManagerSaveData.GetActivePlayerSaveData().Data;
            data.MentalSanity = data.MaxMentalSanity;

            for (int i = 0; i < data.Resources.Length; i++)
                data.Resources[i] = 1;

            for (int i = 0; i < data.Consumables.Length; i++)
                data.Consumables[i] = 1;

            data.Consumables[(int)CornPlayerConsumables.MONEY] = 99999999;
            CornMenuApartment.UpdateGraphics(true);
        });
    }

    public static CoroutineHandle ExecuteEvent(CornEvent anEvent)
    {
        CoroutineHandle eventHandle = default;
        if (!Instance.m_eventHandle.CoroutineIsRunning)
        {
            Debug.Log("Executing event " + anEvent.EventType + " with subtype " + anEvent.EventTypeSub);
            eventHandle = Instance.m_eventHandle.RunCoroutine(Instance._ExecuteEvent(anEvent));
        }
        else
            Debug.LogWarning("Already in the process of executing an event, could not exeucte event " + anEvent.EventType + " " + anEvent.EventTypeSub);
        return eventHandle;
    }

    public static int GetEffectiveSanity()
    {
        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        int effectiveSanity = cornSaveData.MentalSanity;
        /*
        if (cornSaveData.HadCornEventSinceWakeUp) //double the likelyhood of a failed roll if a corn action has not been performed since waking up
            effectiveSanity = DICE_ROLL_NUM_FACES - 2 * (DICE_ROLL_NUM_FACES - cornSaveData.MentalSanity);*/
        return effectiveSanity;
    }

    //returns true if the action will executed. False if the corn event can play.
    public static bool GuaranteedCornRollSuccess() { return GetEffectiveSanity() == DICE_ROLL_NUM_FACES; }// || GfgManagerSaveData.GetActivePlayerSaveData().Data.CornActionsInARow < CornManagerBalancing.MAX_CORN_ACTION_IN_ROW; }

    private IEnumerator<float> _ExecuteCornRoll()
    {
        yield return Timing.WaitUntilDone(GfgManagerGame.SetGameState(GfcGameState.CUTSCENE));
        yield return Timing.WaitUntilDone(CornManagerStory.StartDialogueScene<DialogueCornRoll>());
    }

    public static int HoursToWasteThisEvent;

    private IEnumerator<float> _ExecuteEvent(CornEvent anEvent)
    {
        while (GfgManagerGame.GameStateTransitioning()) yield return Timing.WaitForOneFrame;

        CornEventCostAndRewards eventRewardsAndCost = CornManagerBalancing.GetEventCostAndRewards(anEvent.EventType, out string message, anEvent.EventTypeSub);
        CoroutineHandle eventHandle = default;

        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;

        List<string> messagesBuffer = GetMessagesBuffer();

        if (CanAffordMoney(anEvent))
        {
            HoursToWasteThisEvent = 0;
            if (eventRewardsAndCost.EventHasCornRoll && !GuaranteedCornRollSuccess())
                yield return Timing.WaitUntilDone(Timing.RunCoroutine(Instance._ExecuteCornRoll()));

            if (HoursToWasteThisEvent > 0)
            {
                cornSaveData.CornActionsInARow++;
                cornSaveData.HadCornEventSinceWakeUp = true;
                eventRewardsAndCost = CornManagerBalancing.GetEventCostAndRewards(CornEventType.CORN);
                eventRewardsAndCost.HoursDuration = HoursToWasteThisEvent;
                message = "You wasted time and didn't do anything... You watched corn videos obsessively.";
            }
            else //perform the event normally
            {
                cornSaveData.CornActionsInARow = 0;
                //event stuff
                switch (anEvent.EventType)
                {
                    case CornEventType.WORK:
                        eventHandle = Timing.RunCoroutine(_ExecuteWorkEvent(messagesBuffer));
                        break;

                    case CornEventType.SOCIAL:
                        eventHandle = CornManagerStory.StartDialogueScene(anEvent.Scene);
                        break;

                    case CornEventType.CHORES:
                        eventHandle = Timing.RunCoroutine(_ExecuteChoresEvent(messagesBuffer));
                        break;

                    case CornEventType.SLEEP:
                        eventHandle = Timing.RunCoroutine(_ExecuteSleepEvent(messagesBuffer));
                        eventRewardsAndCost = default;
                        break;
                    case CornEventType.STUDY:
                        Debug.Log("Incrementing point for " + (CornPlayerSkillsStats)anEvent.EventTypeSub);
                        cornSaveData.ApplyModifier((CornPlayerSkillsStats)anEvent.EventTypeSub, 0.01f);
                        break;
                }

                message ??= "<i>Pretty</i> cool event <color=red>" + anEvent.EventType.ToString() + "</color> was finished.";
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
            IncrementMentalSanity(-1);
        }

        if (!message.IsEmpty()) messagesBuffer.Add(message);

        GfcCursor.RemoveSelectedGameObject();

        if (eventHandle.IsValid)
            yield return Timing.WaitUntilDone(eventHandle);

        if (anEvent.EventType == CornEventType.SLEEP)
            eventRewardsAndCost = SleepingMinigameData.GetFinalRewards();
        eventRewardsAndCost.ApplyModifiersToPlayer(0);

        if (anEvent.EventType == CornEventType.CORN)
            cornSaveData.CornActionsExecuted++;

        CornManagerPhone.CanTogglePhone = false;
        CoroutineHandle apartmentLoadRoutine = default;
        if (GfgScene.GetSceneState(GfcSceneId.APARTMENT) == GfcSceneLoadState.UNLOADED)
            apartmentLoadRoutine = GfgManagerSceneLoader.LoadScene(GfcSceneId.APARTMENT, GfcGameState.APARTMENT);

        if (apartmentLoadRoutine.IsValid)
        {
            GfgManagerSceneLoader.FakeWait = true;
            yield return Timing.WaitUntilDone(apartmentLoadRoutine);
        }

        CoroutineHandle transitionCoroutine = ProgressTime(eventRewardsAndCost.HoursDuration, messagesBuffer);
        yield return Timing.WaitUntilDone(transitionCoroutine);

        CornManagerPhone.CanTogglePhone = true;

        m_eventHandle.Finished();
    }

    public static CoroutineHandle _FlushDeliveredShopItems(List<string> aMessageBuffer)
    {
        CoroutineHandle handle = default; //should play an animation
        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        List<CornShopItemPurchased> deliveryItems = cornSaveData.PurchasedItems;
        for (int j = 0; j < deliveryItems.Count; j++)
        {
            if (!deliveryItems[j].Arrived && deliveryItems[j].DaysLeft == 0)
            {
                CornShopItemPurchased purchaseData = deliveryItems[j];
                CornShopItemsData itemData = CornManagerBalancing.GetShopItemData(deliveryItems[j].Item);
                aMessageBuffer.Add(itemData.Name + " arrived!");
                cornSaveData.ApplyModifierSkillStatsList(itemData.Modifiers);
                cornSaveData.ApplyModifier(CornPlayerResources.PERSONAL_NEEDS, itemData.PersonalNeedsPoints);
                purchaseData.Arrived = true;
                deliveryItems[j] = purchaseData;
            }
        }
        return handle;
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
    protected static CoroutineHandle ProgressTime(int anElapsedHours, List<string> someMessages = null, bool aFadeToBlack = true)
    {
        CoroutineHandle progressTimeHandle = default;
        if (!Instance.m_progressTimeHandle.CoroutineIsRunning)
        {
            var activePlayerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
            var playerSaveData = activePlayerSaveData.Data;
            TimeSpan timePassed = playerSaveData.ProgressTime(anElapsedHours);

            someMessages ??= GetMessagesBuffer();

            for (int i = 0; i < timePassed.Weeks; ++i)
            {
                CornManagerBalancing.GetEventCostAndRewards(CornEventType.NEW_WEEK).ApplyModifiersToPlayer();
                playerSaveData.MaxMentalSanity--;
                IncrementMentalSanity(0); //clamp sanity
                someMessages.Add("New week passed, take some money lmao, but -1 sanity.");
            }

            List<CornShopItemPurchased> deliveryItems = playerSaveData.PurchasedItems;
            for (int j = 0; j < deliveryItems.Count; j++)
            {
                var deliveryData = deliveryItems[j];
                if (!deliveryData.Arrived)
                {
                    deliveryData.DaysLeft = (deliveryData.DaysLeft - timePassed.Days).Max(0);
                    deliveryItems[j] = deliveryData;
                }

            }

            for (int i = 0; i < timePassed.Days; ++i)
            {
                playerSaveData.DaysPassed++;
                string message = "Day " + (playerSaveData.DaysPassed + 1);

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
                    IncrementMentalSanity(1);
                }
                else if (loseSanity)
                {
                    message += ", -1 sanity, try to take better care of yourself, otherwise, you will spiral out of control...";
                    IncrementMentalSanity(-1);
                }

                someMessages.Add(message);
            }

            if (timePassed.Weeks > 0)
            {
                if (timePassed.Weeks > 1) Debug.LogWarning("The game was not designed to have more than one week pass at a time, backup saves will have a huge gap between them");
                activePlayerSaveData.MakeBackup();
            }

            progressTimeHandle = Instance.m_progressTimeHandle.RunCoroutine(Instance._FlushMessagesAndDrawHud(aFadeToBlack));
        }
        else
        {
            Debug.LogWarning("Already passing time, cannot progress time before the current routine is over.");
        }

        return progressTimeHandle;
    }

    private IEnumerator<float> _FlushMessagesAndDrawHud(bool aFadeToBlack = true)
    {
        while (GfgManagerGame.GameStateTransitioning()) yield return Timing.WaitForOneFrame;

        float fadeTime = Instance.m_screenFadeTime;

        GfcTransitionParent transition = GfxTransitions.Instance.GetSingleton(GfxTransitionType.BLACK_FADE);
        if (aFadeToBlack)
        {
            GfcCursor.RemoveSelectedGameObject();
            yield return Timing.WaitUntilDone(transition.StartFadeIn());
            //GfxUiTools.FadeOverlayAlpha(1, fadeTime);
            //yield return Timing.WaitForSeconds(fadeTime);
        }

        GfgManagerSceneLoader.FakeWait = false;

        GfgManagerSceneLoader.RequestGameStateAfterLoad(GfcGameState.APARTMENT, false);

        CoroutineHandle deliveryCoroutine = _FlushDeliveredShopItems(Instance.m_messagesBuffer);
        if (deliveryCoroutine.IsValid) Timing.WaitUntilDone(deliveryCoroutine);

        UpdateVisuals();

        CoroutineHandle handle = GfxUiTools.GetNotifyPanel().DrawMessage(Instance.m_messagesBuffer);
        if (handle.IsValid) yield return Timing.WaitUntilDone(handle);
        Instance.m_messagesBuffer.Clear();

        if (!CheckGameOver())
        {
            GfgManagerSaveData.SaveGame();
            CornManagerPhone.LoadAvailableStoryScenes();

            if (aFadeToBlack)
            {
                yield return Timing.WaitUntilDone(transition.StartFadeOut());
                //GfxUiTools.FadeOverlayAlpha(0, fadeTime);
                //yield return Timing.WaitForSeconds(fadeTime);
            }
        }

        m_progressTimeHandle.Finished();
    }

    public static void IncrementMentalSanity(int aModifier, bool aCheckGameOver = false)
    {
        var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        cornSaveData.MentalSanity = (cornSaveData.MentalSanity + aModifier).Clamp(0, cornSaveData.MaxMentalSanity);
        if (aCheckGameOver)
            CheckGameOver();
    }

    public static bool CheckGameOver()
    {
        if (!Instance.m_gameOverHandle.CoroutineIsRunning)
        {
            var cornSaveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
            if (cornSaveData.MentalSanity <= 0)
            {
                //kill any relevant system running
                //should also close dialogue system in case the mental sanity is reduced there
                Instance.m_eventHandle.KillCoroutine();
                Instance.m_progressTimeHandle.KillCoroutine();

                Instance.m_eventHandle = default;
                Instance.m_progressTimeHandle = default;
                Instance.m_gameOverHandle.RunCoroutine(Instance._GameOver());
            }
        }
        else
        {
            Debug.LogWarning("Already executing GameOver routine, cannot increment mental sanity at this moment.");
        }

        return Instance.m_gameOverHandle.CoroutineIsRunning;
    }

    public IEnumerator<float> _GameOver()
    {
        Debug.Log("GAME OVER BOOOO");
        CoroutineHandle loadLevelHandle = GfgManagerSceneLoader.LoadScene(GfcSceneId.GAME_OVER, GfcGameState.MAIN_MENU);

        CoroutineHandle handle = GfxUiTools.GetNotifyPanel().DrawMessage(Instance.m_messagesBuffer);
        if (handle.IsValid) yield return Timing.WaitUntilDone(handle);

        Debug.Assert(loadLevelHandle.IsValid); //this should never be invalid
        yield return Timing.WaitUntilDone(loadLevelHandle);

        yield return Timing.WaitForSeconds(1);
        GfgManagerGame.SetGameState(GfcGameState.SAVE_SELECT);
        m_gameOverHandle.Finished();
    }

    public static void PurchaseShopItem(CornShopItem anItem) { GfgManagerSaveData.GetActivePlayerSaveData().Data.PurchaseShopItem(anItem); }

    public static bool CanAfford(CornEvent anEvent, float aBonusMultiplier = 0) { return CornManagerBalancing.GetEventCostAndRewards(anEvent).CanAfford(aBonusMultiplier); }
    public static bool CanAffordMoney(CornEvent anEvent, float aBonusMultiplier = 0) { return CornManagerBalancing.GetEventCostAndRewards(anEvent).CanAffordMoney(aBonusMultiplier); }

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
    public CornEvent(CornEventType anEventType, uint anEventTypeSub = 0, Type aScene = null)
    {
        EventType = anEventType;
        EventTypeSub = anEventTypeSub;
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
        var rewards = CornManagerBalancing.Instance.AuxCostAndRewardsCopy[(int)CornEventType.SLEEP];
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