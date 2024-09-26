using System;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System.Linq;
using System.Reflection;
using UnityEngine.TextCore.Text;

public class _VNC_P0_PROTAG_0 : TestDialogue { }
public class _VNC_P0_GF_0 : TestDialogue { }
public class _VNC_P0_GF_1 : TestDialogue { }
public class _VNC_P0_DUNNO_0 : TestDialogue { }
public class _VNC_P0_TEST_0 : TestDialogue { }

public class _VNC_P1_PROTAG_0 : TestDialogue { }
public class _VNC_P1_GF_0 : TestDialogue { }
public class _VNC_P1_GF_1 : TestDialogue { }
public class _VNC_P1_DUNNO_0 : TestDialogue { }
public class _VNC_P1_TEST_0 : TestDialogue { }

public class _VNC_P2_PROTAG_0 : TestDialogue { }
public class _VNC_P2_GF_0 : TestDialogue { }
public class _VNC_P2_GF_1 : TestDialogue { }
public class _VNC_P2_DUNNO_0 : TestDialogue { }
public class _VNC_P2_TEST_0 : TestDialogue { }

public class _VNC_P3_PROTAG_0 : TestDialogue { }
public class _VNC_P3_GF_0 : TestDialogue { }
public class _VNC_P3_GF_1 : TestDialogue { }
public class _VNC_P3_DUNNO_0 : TestDialogue { }
public class _VNC_P3_TEST_0 : TestDialogue { }

public class _VNC_P4_PROTAG_0 : TestDialogue { }
public class _VNC_P4_GF_0 : TestDialogue { }
public class _VNC_P4_GF_1 : TestDialogue { }
public class _VNC_P4_DUNNO_0 : TestDialogue { }
public class _VNC_P4_TEST_0 : TestDialogue { }


public class CornManagerStory : MonoBehaviour
{
    protected static CornManagerStory Instance;

    [SerializeField] protected CornStoryPhase[] m_storyPhases;

    protected List<StoryCharacter> m_availableCharactersBuffer = new((int)StoryCharacter.COUNT);

    const string VN_SCENE_PREFIX = "_VNC_P";
    private void Start()
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

            if (name.IndexOf(VN_SCENE_PREFIX) != 0)
                continue;

            //Debug.Log("Parsing scene: " + name);

            int phase = 0;
            int currentIndex = VN_SCENE_PREFIX.Length;
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

            if (!Enum.TryParse(nameBuffer, out StoryCharacter storyCharacter))
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

        int characterCount = (int)StoryCharacter.COUNT; //cols

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
                    m_storyPhases[i].Events[nonNullEvents].Character = (StoryCharacter)j;
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
                StoryCharacter storyCharacter = m_storyPhases[i].Events[j].Character;
                for (int x = 0; x < countEvents; x++)
                    GetTypeOfScene(storyCharacter, i, x);
            }
        }
    }

    public static Type GetTypeOfScene(StoryCharacter aCharacter, int aPhase, int aScene, bool aPrintErrors = true)
    {
        Type eventClassType;
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;

        stringBuffer.Append(VN_SCENE_PREFIX);
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

    public static CoroutineHandle StartStoryScene(StoryCharacter aCharacter)
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
            saveData.CurrentStoryPhaseProgress[(int)aCharacter]++;
            CheckIfCurrentPhaseDone();
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

        return GfgManagerVnScene.StartScene(eventClassType, GfgVnSceneHandlerType.DIALOGUE);
    }

    public static List<StoryCharacter> GetAvailableCharactersForEvent()
    {
        var saveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        int currentPhaseIndex = saveData.CurrentStoryPhase;
        CornStoryEventInPhase[] currentPhase = Instance.m_storyPhases[currentPhaseIndex].Events;

        List<StoryCharacter> availableCharacters = Instance.m_availableCharactersBuffer;
        availableCharacters.Clear();

        for (int i = 0; i < currentPhase.Length; i++)
        {
            CornStoryEventInPhase currentEvent = currentPhase[i];
            int progressForCurrentCharacter = saveData.CurrentStoryPhaseProgress[(int)currentEvent.Character];

            if (progressForCurrentCharacter < currentEvent.Count)
            {
                availableCharacters.Add(currentEvent.Character);
            }
        }

        return availableCharacters;
    }

    public static bool CheckIfCurrentPhaseDone()
    {
        var saveData = GfgManagerSaveData.GetActivePlayerSaveData().Data;
        int currentPhaseIndex = saveData.CurrentStoryPhase;
        CornStoryEventInPhase[] currentPhase = Instance.m_storyPhases[currentPhaseIndex].Events;

        bool completedAll = true;
        for (int i = 0; i < currentPhase.Length && completedAll; i++)
        {
            int progressForCurrentCharacter = saveData.CurrentStoryPhaseProgress[(int)currentPhase[i].Character];
            completedAll &= progressForCurrentCharacter == currentPhase[i].Count;
        }

        if (completedAll)
        {
            saveData.CurrentStoryPhase++;
            for (int i = 0; i < saveData.CurrentStoryPhaseProgress.Length; i++)
                saveData.CurrentStoryPhaseProgress[i] = 0;
        }

        return completedAll;
    }
}

[Serializable]
public struct CornStoryEventInPhase
{
    public StoryCharacter Character;
    public int Count;
}

[Serializable]
public struct CornStoryPhase
{
    public CornStoryEventInPhase[] Events;
}

internal struct CornStoryScene
{
    public StoryCharacter Character;
    public int Phase;
    public int Number;
}