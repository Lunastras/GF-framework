using System;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System.Linq;

public class _VNC_P0_PROTAG_0 : TestDialoguePhone { }
public class _VNC_P0_GF_0 : TestDialoguePhone { }
public class _VNC_P0_GF_1 : TestDialoguePhone { }
public class _VNC_P0_DUNNO_0 : TestDialoguePhone { }
public class _VNC_P0_TEST_0 : TestDialoguePhone { }

public class _VNC_P1_PROTAG_0 : TestDialoguePhone { }
public class _VNC_P1_GF_0 : TestDialoguePhone { }
public class _VNC_P1_GF_1 : TestDialoguePhone { }
public class _VNC_P1_DUNNO_0 : TestDialoguePhone { }
public class _VNC_P1_TEST_0 : TestDialoguePhone { }

public class _VNC_P2_PROTAG_0 : TestDialoguePhone { }
public class _VNC_P2_GF_0 : TestDialoguePhone { }
public class _VNC_P2_GF_1 : TestDialoguePhone { }
public class _VNC_P2_DUNNO_0 : TestDialoguePhone { }
public class _VNC_P2_TEST_0 : TestDialoguePhone { }

public class _VNC_P3_PROTAG_0 : TestDialoguePhone { }
public class _VNC_P3_GF_0 : TestDialoguePhone { }
public class _VNC_P3_GF_1 : TestDialoguePhone { }
public class _VNC_P3_DUNNO_0 : TestDialoguePhone { }
public class _VNC_P3_TEST_0 : TestDialoguePhone { }

public class _VNC_P4_PROTAG_0 : TestDialoguePhone { }
public class _VNC_P4_GF_0 : TestDialoguePhone { }
public class _VNC_P4_GF_1 : TestDialoguePhone { }
public class _VNC_P4_DUNNO_0 : TestDialoguePhone { }
public class _VNC_P4_TEST_0 : TestDialoguePhone { }

public class CornManagerStory : MonoBehaviour
{
    protected static CornManagerStory Instance;

    [SerializeField] protected CornStoryPhase[] m_storyPhases;

    protected List<CornStoryVnSceneDetails> m_nonPhaseSpecificEvents = new(8);

    protected List<CornStoryVnSceneDetails> m_availableEventsBuffer = new((int)GfcStoryCharacter.COUNT);

    const string VN_SCENE_PHASE_PREFIX = "_VNC_P";

    const string VN_SCENE_NON_PHASE_PREFIX = "_VNC_NP_";

    private void Awake()
    {
        this.SetSingleton(ref Instance);
        ParseEvents();
    }

    private void ParseEvents()
    {
        IEnumerable<Type> vnScenes = GfcTools.GetSubclasses<GfgVnScene>(GetType());
        Span<CornStoryScene> storyScenes = stackalloc CornStoryScene[vnScenes.Count()];

        int countStoryScenes = 0;
        int highestPhase = 0;

        foreach (Type type in vnScenes)
        {
            string name = type.Name;

            if (name.IndexOf(VN_SCENE_PHASE_PREFIX) == 0)
            {
                int phase = 0;
                int currentIndex = VN_SCENE_PHASE_PREFIX.Length;
                while (currentIndex < name.Length)
                {
                    char c = name[currentIndex];
                    if (c >= '0' && c <= '9')
                        phase = phase * 10 + c - '0';
                    else
                        break;

                    currentIndex++;
                }

                if (name[currentIndex] != '_')
                {
                    Debug.LogError("Error parsing vn corn scene: " + name + ", expected '_' at index " + currentIndex + ", found: " + name[currentIndex]);
                    break;
                }

                currentIndex++;
                int startCharacterNameIndex = currentIndex;

                GfcStringBuffer nameBuffer = GfcPooling.GfcStringBuffer;

                while (currentIndex < name.Length && name[currentIndex] != '_')
                    nameBuffer.Append(name[currentIndex++]);

                if (!Enum.TryParse(nameBuffer, out GfcStoryCharacter storyCharacter))
                {
                    Debug.LogError("Error parsing story character name '" + nameBuffer.StringBuffer + "' in scene: " + name);
                    break;
                }

                nameBuffer.Clear();

                if (name[currentIndex] != '_')
                {
                    Debug.LogError("Error parsing vn corn scene: " + name + ", expected '_' at index " + currentIndex + ", found: " + name[currentIndex]);
                    break;
                }

                currentIndex++;

                int number = 0;
                while (currentIndex < name.Length)
                {
                    char c = name[currentIndex];
                    if (c >= '0' && c <= '9')
                        number = number * 10 + c - '0';
                    else
                        break;

                    currentIndex++;
                }

                if (currentIndex != name.Length)
                {
                    Debug.LogError("Error parsing vn corn scene: " + name + ", expected end at index: " + currentIndex);
                }

                CornStoryScene storyScene = new()
                {
                    Number = number,
                    Character = storyCharacter,
                    Phase = phase,
                };

                //Debug.Log("Parsed event at phase: " + phase + " for character " + storyCharacter + " at scene: " + number);
                storyScenes[countStoryScenes++] = storyScene;
                highestPhase.MaxSelf(phase);
            }
            else if (name.IndexOf(VN_SCENE_NON_PHASE_PREFIX) == 0)
            {
                GfcStringBuffer nameBuffer = GfcPooling.GfcStringBuffer;

                int currentIndex = VN_SCENE_NON_PHASE_PREFIX.Length;
                while (currentIndex < name.Length && name[currentIndex] != '_')
                    nameBuffer.Append(name[currentIndex++]);

                if (!Enum.TryParse(nameBuffer, out GfcStoryCharacter storyCharacter))
                {
                    Debug.LogError("Error parsing story character name '" + nameBuffer.StringBuffer + "' in scene: " + name);
                    break;
                }

                nameBuffer.Clear();

                CornStoryVnSceneDetails sceneDetails;
                sceneDetails.PhaseSpecific = false;
                sceneDetails.Scene = type;
                sceneDetails.Character = storyCharacter;

                m_nonPhaseSpecificEvents.Add(sceneDetails);
            }
        }

        int characterCount = (int)GfcStoryCharacter.COUNT; //cols

        //phases are rows
        highestPhase++;

        Span<int> characterEventsInPhase = stackalloc int[highestPhase * characterCount];

        for (int i = 0; i < countStoryScenes; i++)
            characterEventsInPhase[storyScenes[i].Phase * characterCount + (int)storyScenes[i].Character]++;

        m_storyPhases = new CornStoryPhase[highestPhase];

        for (int i = 0; i < highestPhase; i++)
        {
            int nonNullEvents = 0;
            for (int j = 0; j < characterCount; j++)
                if (characterEventsInPhase[i * characterCount + j] != 0)
                    nonNullEvents++;

            m_storyPhases[i].Events = new CornStoryEventInPhase[nonNullEvents];
            nonNullEvents = 0;
            for (int j = 0; j < characterCount; j++)
            {
                int eventsInPhase = characterEventsInPhase[i * characterCount + j];
                if (eventsInPhase != 0)
                {
                    m_storyPhases[i].Events[nonNullEvents].Character = (GfcStoryCharacter)j;
                    m_storyPhases[i].Events[nonNullEvents].Count = eventsInPhase;
                    nonNullEvents++;
                }
            }
        }

        //validate the scenes to make sure we do not skip anything
        for (int i = 0; i < m_storyPhases.Length; i++)
        {
            int countCharacters = m_storyPhases[i].Events.Length;
            for (int j = 0; j < countCharacters; j++)
            {
                int countEvents = m_storyPhases[i].Events[j].Count;
                GfcStoryCharacter storyCharacter = m_storyPhases[i].Events[j].Character;
                for (int x = 0; x < countEvents; x++)
                    GetTypeOfScene(storyCharacter, i, x);
            }
        }
    }

    public static Type GetTypeOfScene(GfcStoryCharacter aCharacter, int aPhase, int aScene, bool aPrintErrors = true)
    {
        Type eventClassType;
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;

        stringBuffer.Append(VN_SCENE_PHASE_PREFIX);
        stringBuffer.Append(aPhase);
        stringBuffer.Append('_');
        stringBuffer.Append(aCharacter.ToString());
        stringBuffer.Append('_');
        stringBuffer.Append(aScene);

        eventClassType = Type.GetType(stringBuffer);

        if (aPrintErrors && eventClassType == null)
            Debug.LogError("Could not find the vn story scene " + stringBuffer.StringBuffer);

        stringBuffer.Clear();

        return eventClassType;
    }

    private static Type GetStoryScene(GfcStoryCharacter aCharacter)
    {
        var saveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        int currentPhaseIndex = saveData.CurrentStoryPhase;
        CornStoryEventInPhase[] currentPhase = Instance.m_storyPhases[currentPhaseIndex].Events;
        int currentProgressForCharacter = saveData.CurrentStoryPhaseProgress[(int)aCharacter];

        int indexOfEvent = 0;
        for (; indexOfEvent < currentPhase.Length && currentPhase[indexOfEvent].Character != aCharacter; indexOfEvent++) { }

        Type eventClassType = null;

        if (currentProgressForCharacter < currentPhase[indexOfEvent].Count)
        {
            eventClassType = GetTypeOfScene(aCharacter, currentPhaseIndex, currentProgressForCharacter);
        }
        else
        {
            Debug.LogError("Already executed all the events for character " + aCharacter + " in the phase " + currentPhaseIndex);
        }

        if (eventClassType == null)
        {
            Debug.LogError("There is no story scene for character: " + aCharacter + " ");
            eventClassType = typeof(TestDialogue); //return default dialogue scene
        }

        return eventClassType;
    }

    public static CoroutineHandle StartDialogueScene<T>() { return StartDialogueScene(typeof(T)); }
    public static CoroutineHandle StartDialogueScene(Type aScene)
    {
        if (aScene == null)
        {
            aScene = typeof(TestDialogue);
            Debug.LogError("The passed scene was null, executing the test scene instead.");
        }

        return GfgManagerVnScene.StartScene(aScene, GfgVnSceneHandlerType.DIALOGUE);
    }

    public static List<CornStoryVnSceneDetails> GetAvailableEvents()
    {
        var saveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        int currentPhaseIndex = saveData.CurrentStoryPhase;
        CornStoryEventInPhase[] currentPhase = Instance.m_storyPhases[currentPhaseIndex].Events;

        var availableCharacters = Instance.m_availableEventsBuffer;
        availableCharacters.Clear();

        for (int i = 0; i < Instance.m_nonPhaseSpecificEvents.Count; i++)
        {
            Type scene = Instance.m_nonPhaseSpecificEvents[i].Scene;
            string sceneName = scene.Name;

            if (!saveData.FinishedNonSpecificScenes.Contains(sceneName))
            {
                if (GfgVnScene.CanSkipScene(scene) == GfgVnScene.GfgVnSceneSkipable.SKIPABLE)
                {
                    saveData.FinishedNonSpecificScenes.Add(scene.Name);
                }
                else if (GfgVnScene.CanPlayScene(scene) != GfgVnScene.GfgVnScenePlayable.UNPLAYABLE)
                {
                    availableCharacters.Add(Instance.m_nonPhaseSpecificEvents[i]);
                }
            }
        }

        for (int i = 0; i < currentPhase.Length; i++)
        {
            CornStoryEventInPhase currentEvent = currentPhase[i];
            int progressForCurrentCharacter = saveData.CurrentStoryPhaseProgress[(int)currentEvent.Character];

            if (progressForCurrentCharacter < currentEvent.Count)
            {
                //make sure we don't have this character in the buffer already
                bool characterAlreadyPresent = false;
                for (int j = 0; j < availableCharacters.Count && !characterAlreadyPresent; j++)
                    characterAlreadyPresent = currentEvent.Character == availableCharacters[j].Character;

                if (!characterAlreadyPresent)
                {
                    Type scene = GetStoryScene(currentEvent.Character);
                    if (GfgVnScene.CanPlayScene(scene) != GfgVnScene.GfgVnScenePlayable.UNPLAYABLE)
                    {
                        CornStoryVnSceneDetails storyEvent;
                        storyEvent.Character = currentEvent.Character;
                        storyEvent.Scene = scene;
                        storyEvent.PhaseSpecific = true;
                        availableCharacters.Add(storyEvent);
                    }
                }
            }
        }

        return availableCharacters;
    }

    public static void IncrementStoryProgress(CornStoryVnSceneDetails aScene)
    {
        var saveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        if (aScene.PhaseSpecific)
        {
            saveData.CurrentStoryPhaseProgress[(int)aScene.Character]++;
            CheckIfCurrentPhaseDone();
        }
        else
        {
            saveData.FinishedNonSpecificScenes.Add(aScene.Scene.Name);
        }
    }

    public static bool CheckIfCurrentPhaseDone()
    {
        var saveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        int currentPhaseIndex = saveData.CurrentStoryPhase;
        CornStoryEventInPhase[] currentPhase = Instance.m_storyPhases[currentPhaseIndex].Events;

        bool completedAll = saveData.CurrentStoryPhaseGameLevelFinished;
        for (int i = 0; i < currentPhase.Length && completedAll; i++)
        {
            int progressForCurrentCharacter = saveData.CurrentStoryPhaseProgress[(int)currentPhase[i].Character];
            completedAll &= progressForCurrentCharacter >= currentPhase[i].Count;
            if (progressForCurrentCharacter > currentPhase[i].Count)
            {
                Debug.LogError("The progress in the save file for character " + currentPhase[i].Character + " is " + progressForCurrentCharacter + ", but the max progress should be " + currentPhase[i].Count);
            }
        }

        if (completedAll)
        {
            saveData.CurrentStoryPhase++;
            saveData.CurrentStoryPhaseGameTriviasFinished = 0;
            saveData.CurrentStoryPhaseGameLevelFinished = false;

            for (int i = 0; i < saveData.CurrentStoryPhaseProgress.Length; i++)
                saveData.CurrentStoryPhaseProgress[i] = 0;
        }

        return completedAll;
    }
}

[Serializable]
public struct CornStoryEventInPhase
{
    public GfcStoryCharacter Character;
    public int Count;
}

[Serializable]
public struct CornStoryPhase
{
    public CornStoryEventInPhase[] Events;
}

internal struct CornStoryScene
{
    public GfcStoryCharacter Character;
    public int Phase;
    public int Number;
}

public struct CornStoryVnSceneDetails
{
    public GfcStoryCharacter Character;
    public Type Scene;
    public bool PhaseSpecific;
}

public class TestDialoguePhone : GfgVnScene
{
    protected override void Begin()
    {
        bool canAffordEvent = CornManagerEvents.CanAffordMoney(new CornEvent(CornEventType.SOCIAL));

        Say("Hello loser...", new(GfcStoryCharacter.PROTAG));
        Say("Let's see, do you wish to see my cock?");

        Say("...", new(GfcStoryCharacter.GF));
        Say("..Bad.", new(GfcStoryCharacter.GF));
        Say("...Sponge", new(GfcStoryCharacter.GF));
        Say("...", new(GfcStoryCharacter.GF));

        Say("............Well... this is awkward...", new CornDialogueSetting(GfcStoryCharacter.PROTAG));

        Say("Bruh.", new CornDialogueSetting(GfcStoryCharacter.GF));

        Say("Gimme an Answer");
        {
            Append(" Cool dick");

            Option("Hell yeee", Yes, canAffordEvent, "You don't have enough money for this event");
            Option("Lmao Nope", No);
        }
    }

    void Yes()
    {
        Say("Yee", new CornDialogueSetting(GfcStoryCharacter.GF));
        Say("Pls gibe", new CornDialogueSetting(GfcStoryCharacter.GF));

        Say("Goody...", new CornDialogueSetting(GfcStoryCharacter.PROTAG));
        CornManagerPhone.QueueSocialEventAfterMessages(typeof(TestDialogue));
    }

    void No()
    {
        Say("Hell nahh shit is too small for me ", new CornDialogueSetting(GfcStoryCharacter.GF));
        Say("Zannen desuyo", new CornDialogueSetting(GfcStoryCharacter.PROTAG));
    }
}

public class DialogueCornRoll : GfgVnScene
{
    private bool m_rollWin = false;
    private int m_rollCount = 0;

    protected override void Begin()
    {
        bool canAffordEvent = CornManagerEvents.CanAffordMoney(new CornEvent(CornEventType.SOCIAL));

        Say("Hello loser...", new(GfcStoryCharacter.DUNNO));
        Say("Let's see how fucked you are...");
        Play();
    }

    void Play()
    {
        Say("Let's test your luck...");
        WaitForCoroutine(_RollDice());
        Next(GameResult);
    }

    void GameResult()
    {
        if (m_rollWin)
        {
            if (m_rollCount == 2)
            {
                Say("Good job boy... Do you wish to take your final roll?");
                CornOptions(true);
            }
            else //m_rollCount here should be either 1 or 3, which are both wins
            {
                Say("Congratulations... For now...");
                Next(EndWin);
            }
        }
        else
        {
            if (m_rollCount == 1)
            {
                Say("Unlucky boy... Do you wish to try your luck again?");
                Say("If you win twice in a row, you get to leave.");
                Say("If you lose, you will be my plaything for longer.");
                CornOptions(true);
            }
            else
            {
                Say("Aww... Looks like you might have to spend some time with me, or do you have an ace up your sleeve?");
                CornOptions(false);
            }
        }
    }

    void CornOptions(bool aCanPlayAgain)
    {
        if (aCanPlayAgain) Option("Yes (6 hours on fail)", Play);
        Option("No (Waste 3 hours)", EndLose); //3 hour corn roll
        Option("Skip Roll (-20 energy)", EndWin);
    }

    void EndWin()
    {

    }

    void EndLose()
    {
        int multiplier = m_rollCount == 1 ? 1 : 2;
        CornManagerEvents.HoursToWasteThisEvent = multiplier * CornManagerBalancing.GetEventCostAndRewards(CornEventType.CORN).HoursDuration;
    }

    IEnumerator<float> _RollDice()
    {
        yield return Timing.WaitForOneFrame;
        int rollResultValue = UnityEngine.Random.Range(1, CornManagerEvents.DICE_ROLL_NUM_FACES + 1);
        m_rollWin = CornManagerEvents.GetEffectiveSanity() >= rollResultValue;
        //Debug.Log("SANITY: " + CornManagerEvents.GetEffectiveSanity() + ", VALUE " + rollResultValue + " RESULT " + m_rollWin);
        m_rollCount++;
    }
}