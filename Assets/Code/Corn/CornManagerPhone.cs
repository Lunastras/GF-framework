using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class CornManagerPhone : MonoBehaviour
{
    private static CornManagerPhone Instance;
    [SerializeField] private GfcInputTracker m_openPhoneInput;

    [SerializeField] private GameObject m_prefabMessagesNotifyPanel;

    [SerializeField] private GameObject m_prefabCharacterInHub;

    [SerializeField] private GfcGameState[] m_statesWhereToggleEnabled;

    private List<CornMessageEventInstance> m_messageScenes;

    private int m_currentlyShownEventIndex = -1;

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);
    }

    void Start()
    {
        GfgManagerGame.Instance.OnGameStateChanged += OnGameStateChanged;
    }

    public static void CheckAvailableStoryScenes()
    {
        AddMessageScenes(CornManagerStory.GetAvailableEvents());
    }

    public static bool AddMessageScenes(CornStoryVnSceneDetails aScene, bool anUpdateScenes)
    {
        bool success = false;
        Debug.Assert(aScene.Scene != null);

        bool canAdd = true;
        int currIndex = 0;
        int count = Instance.m_messageScenes.Count;

        for (; currIndex < count && canAdd; currIndex++)
            canAdd = Instance.m_messageScenes[currIndex].SceneDetails.Character != aScene.Character;

        if (canAdd)
        {
            success = true;
            Instance.m_messageScenes.Add(new(aScene));

            UpdateMessages();
        }
        else
        {
            Debug.LogError("The scene (" + aScene.Scene + ") could not be added, there is already an event registered for the character " + aScene.Character + ", (" + Instance.m_messageScenes[--currIndex].SceneDetails.Scene + ")");
        }

        return success;
    }

    public static bool AddMessageScenes(IEnumerable<CornStoryVnSceneDetails> aScene)
    {
        bool success = true;
        foreach (CornStoryVnSceneDetails vnScene in aScene)
            success &= AddMessageScenes(vnScene, false);

        if (success)
            UpdateMessages();

        return success;
    }

    public static bool RemoveMessageScenes(Type aScene)
    {
        Debug.Assert(aScene != null);

        bool foundItem = false;
        int currIndex = 0;
        int count = Instance.m_messageScenes.Count;

        for (; currIndex < count && !foundItem; currIndex++)
            foundItem = Instance.m_messageScenes[currIndex].SceneDetails.Scene == aScene;

        if (foundItem)
        {
            CloseMessageScene(--currIndex);
            Instance.m_messageScenes[currIndex] = Instance.m_messageScenes[^1];
            Instance.m_messageScenes.RemoveAt(Instance.m_messageScenes.Count - 1);
            UpdateMessages();
        }

        return foundItem;
    }

    public static void CloseMessageScene(int aSceneIndex)
    {
        CornMessageEventInstance messageEvent = Instance.m_messageScenes[aSceneIndex];
        if (Instance.m_currentlyShownEventIndex == aSceneIndex)
        {
            Debug.Assert(false, "Not implemented");
        }

        if (Instance.m_messageScenes[aSceneIndex].EventHandle.IsValid)
        {
            Timing.KillCoroutines(messageEvent.EventHandle);
            GfcPooling.Destroy(messageEvent.PanelTemplate.gameObject);

            messageEvent.EventHandle = default;
            messageEvent.PanelTemplate = null;
            Instance.m_messageScenes[aSceneIndex] = messageEvent;
        }
    }

    private static void UpdateMessages()
    {
        Debug.Log("Events available: " + Instance.m_messageScenes.Count);
    }

    private void OnGameStateChanged(GfcGameState aNewState, GfcGameState anOldState)
    {
        if (aNewState == GfcGameState.APARTMENT && (anOldState == GfcGameState.INVALID || anOldState == GfcGameState.LOADING))
        {
            CheckAvailableStoryScenes();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (m_openPhoneInput.PressedSinceLastCheck())
        {
            bool validState = false;
            GfcGameState gameState = GfgManagerGame.GetGameState();

            for (int i = 0; !validState && i < m_statesWhereToggleEnabled.Length; i++)
            {
                validState = gameState == m_statesWhereToggleEnabled[i];
            }

            if (validState)
            {
                GfcGameState gameStateToSet = gameState == GfcGameState.PHONE ? GfgManagerGame.GetPreviousGameState() : GfcGameState.PHONE;
                GfgManagerGame.SetGameState(gameStateToSet);
            }
        }
    }

    public static void PressedCharacter(int anEventIndex)
    {

    }

    public static void PressedBack()
    {
        CloseMessageScene(Instance.m_currentlyShownEventIndex);
    }
}

internal struct CornMessageEventInstance
{
    public CornMessageEventInstance(CornStoryVnSceneDetails aSceneDetails)
    {
        SceneDetails = aSceneDetails;
        PanelTemplate = null;
        EventHandle = default;
    }

    public CornStoryVnSceneDetails SceneDetails;
    public GfxNotifyPanelTemplate PanelTemplate;
    public CoroutineHandle EventHandle;
}