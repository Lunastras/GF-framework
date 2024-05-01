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
        if (Instance) Destroy(Instance);
        Instance = this;
    }

    public static void UpdateVisuals()
    {

    }

    private static IEnumerator<float> _FlushMessagesAndDrawHud()
    {
        float fadeTime = Instance.m_screenFadeTime;

        GfxUiTools.CrossFadeBlackAlpha(1, fadeTime);
        GfxUiTools.RemoveSelectedGameObject();

        yield return Timing.WaitForSeconds(fadeTime);

        UpdateVisuals();

        yield return Timing.WaitUntilDone(GfxUiTools.NotifyMessage(Instance.m_messagesBuffer));
        Instance.m_messagesBuffer.Clear();

        GfxUiTools.CrossFadeBlackAlpha(0, fadeTime);
        yield return Timing.WaitForSeconds(fadeTime); ;
    }

    //should only be called when an action happens
    protected static CoroutineHandle ProgressTime(uint aElapsedHours, List<string> someMessages = null)
    {
        PlayerSaveData playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        uint daysPassed = playerSaveData.ProgressTime(aElapsedHours);

        for (int i = 0; i < daysPassed; ++i)
        {
            someMessages ??= GetMessagesBuffer();

            playerSaveData.CurrentTime %= 24;
            playerSaveData.CurrentDay++;
            string message = "Day " + playerSaveData.CurrentDay;

            bool loseSanity = false;
            bool gainSanity = true;

            for (int resourceIndex = 0; resourceIndex < (int)PlayerResources.COUNT; ++resourceIndex)
            {
                loseSanity |= playerSaveData.Resources[resourceIndex] <= 0;
                gainSanity &= playerSaveData.Resources[resourceIndex] >= 0.5f;
            }

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

        return Timing.RunCoroutine(_FlushMessagesAndDrawHud());
    }

    private static IEnumerator<float> _ExecuteEvent(CornEvent aEvent)
    {
        string message;
        CornEventCostAndRewards eventDetails = CornManagerBalancing.GetEventCostAndRewards(aEvent);
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

        List<string> messagesBuffer = GetMessagesBuffer();
        messagesBuffer.Add(message);

        eventDetails.ApplyModifiersToPlayer(0);

        CoroutineHandle transitionCoroutine = ProgressTime(eventDetails.HoursDuration, messagesBuffer);
        yield return Timing.WaitUntilDone(transitionCoroutine);

        Instance.m_canPlayEvent = true;
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

    public static void ApplyModifier(PlayerResources aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifier(aType, aValue); }
    public static void ApplyModifier(PlayerConsumables aType, float aValue) { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifier(aType, aValue); }

    public static void ApplyModifier(PlayerResourcesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifier(aModifier, aMultiplier, aBonusMultiplier); }
    public static void ApplyModifier(PlayerConsumablesModifier aModifier, float aMultiplier = 1, float aBonusMultiplier = 0) { GfgManagerSaveData.GetActivePlayerSaveData().ApplyModifier(aModifier, aMultiplier, aBonusMultiplier); }

    public static void ApplyModifierResourceList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerResourcesModifier> { if (someModifiers != null) foreach (PlayerResourcesModifier modifier in someModifiers) ApplyModifier(modifier, aMultiplier, aBonusMultiplier); }
    public static void ApplyModifierConsumablesList<T>(T someModifiers, float aMultiplier = 1, float aBonusMultiplier = 0) where T : IEnumerable<PlayerConsumablesModifier> { if (someModifiers != null) foreach (PlayerConsumablesModifier modifier in someModifiers) ApplyModifier(modifier, aMultiplier, aBonusMultiplier); }
    public static int MentalSanity { get { return GfgManagerSaveData.GetActivePlayerSaveData().MentalSanity; } }
}

[System.Serializable]
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
    SLEEP,
    WORK,
    CHORES,
    SOCIAL,
    GYM,
    PERSONAL_TIME,
    CORN,
    RANDOM,
    PERSONAL_GIFT,
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
