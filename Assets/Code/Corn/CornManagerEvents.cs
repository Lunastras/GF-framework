using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class CornManagerEvents : MonoBehaviour
{
    protected static CornManagerEvents Instance;

    [SerializeField] protected float m_cornEnergyCost = 20;

    [SerializeField] protected uint m_cornTimeCost = 2;

    [SerializeField] protected float m_screenFadeTime = 0.6f;

    [SerializeField] protected CornPlayerResources m_resourcesModifierDaily = default;

    public const int DICE_ROLL_NUM_FACES = 10;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance) Destroy(Instance);
        Instance = this;
    }

    public static void ExecuteEvent(CornEvent aEvent)
    {
        Timing.RunCoroutine(_ExecuteEvent(aEvent));
    }

    private static IEnumerator<float> _ExecuteEvent(CornEvent aEvent)
    {
        float fadeTime = Instance.m_screenFadeTime;
        PlayerSaveData playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();

        bool wasteTime = aEvent.EventHasCornRoll && playerSaveData.Resources.MentalSanity < UnityEngine.Random.Range(1, DICE_ROLL_NUM_FACES);

        if (wasteTime)
        {
            ExecuteCornEvent();
        }
        else //perform the event normally
        {
            //event stuff
            _ExecuteTransition("Event " + aEvent.EventType.ToString() + " finished.");
            ProgressTime(aEvent.HoursDuration);
        }

        yield return Timing.WaitForOneFrame;
    }

    private static IEnumerator<float> _ExecuteTransition(string aMessage = null, List<string> someMessages = null)
    {
        float fadeTime = Instance.m_screenFadeTime;

        GfUiTools.CrossFadeAlpha(1, fadeTime);
        yield return Timing.WaitForSeconds(fadeTime);

        UpdateHud();

        GfUiTools.NotifyMessage(aMessage);
        while (GfUiTools.NotifyIsActive())
            yield return Timing.WaitForSeconds(0.03f);

        GfUiTools.NotifyMessage(someMessages);
        while (GfUiTools.NotifyIsActive())
            yield return Timing.WaitForSeconds(0.03f);

        GfUiTools.CrossFadeAlpha(0, fadeTime);
        yield return Timing.WaitForSeconds(fadeTime); ;
    }

    public static void ExecuteCornEvent()
    {
        PlayerSaveData playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        playerSaveData.Consumables.Energy -= Instance.m_cornEnergyCost;
        ProgressTime(Instance.m_cornTimeCost);

        Timing.RunCoroutine(_ExecuteTransition("You wasted time and didn't do anything... You watched corn videos obsessively."));
    }

    public static void ProgressTime(uint aElapsedHours)
    {
        PlayerSaveData playerSaveData = GfgManagerSaveData.GetActivePlayerSaveData();
        playerSaveData.CurrentTime += aElapsedHours;
        string message = null;

        if (playerSaveData.CurrentTime >= 24)
        {
            playerSaveData.CurrentTime %= 24;
            playerSaveData.CurrentDay++;
            message = "Day " + playerSaveData.CurrentDay;

            playerSaveData.Resources -= Instance.m_resourcesModifierDaily;

            bool loseSanity = playerSaveData.Resources.Chores <= 0
                            || playerSaveData.Resources.Groceries <= 0
                            || playerSaveData.Resources.PersonalNeeds <= 0
                            || playerSaveData.Resources.PhysicalHealth <= 0
                            || playerSaveData.Resources.Productivity <= 0
                            || playerSaveData.Resources.Relantionship <= 0
                            || playerSaveData.Resources.SocialLife <= 0;

            bool gainSanity = playerSaveData.Resources.Chores >= 0.5
                        && playerSaveData.Resources.Groceries >= 0.5
                        && playerSaveData.Resources.PersonalNeeds >= 0.5
                        && playerSaveData.Resources.PhysicalHealth >= 0.5
                        && playerSaveData.Resources.Productivity >= 0.5
                        && playerSaveData.Resources.Relantionship >= 0.5
                        && playerSaveData.Resources.SocialLife >= 0.5;

            if (gainSanity)
            {
                message += ", +1 sanity, good job.";
                playerSaveData.Resources.MentalSanity++;
            }
            else if (loseSanity)
            {
                message += ", -1 sanity, try to take better care of yourself, otherwise, you will spiral out of control...";
                playerSaveData.Resources.MentalSanity--;
            }

            playerSaveData.Resources.MentalSanity = Mathf.Clamp(playerSaveData.Resources.MentalSanity, 0, DICE_ROLL_NUM_FACES);
        }

        Timing.RunCoroutine(_ExecuteTransition(message));
    }

    public static void UpdateHud()
    {

    }
}

[System.Serializable]
public struct CornEvent
{
    public CornPlayerConsumables ConsumablesModifier;
    public CornPlayerResources ResourcesModifier;
    public string NotificationText;
    public CornEventType EventType;
    public bool EventHasCornRoll;
    public uint HoursDuration;
}

public enum CornEventType
{
    NONE,
    WORK,
    GROCERIES,
    CHORES,
    SOCIAL,
    RELATIONSHIP,
    GYM,
    PERSONAL_LEISURE,
    PERSONAL_BUY,
    RANDOM,
}
