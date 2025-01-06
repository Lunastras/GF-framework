using System;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class CornManagerPhone : MonoBehaviour
{
    private static CornManagerPhone Instance;

    [SerializeField] private GfcInputType m_openPhoneInput;

    [SerializeField] private GameObject m_prefabMessagesNotifyPanel;

    [SerializeField] private GameObject m_prefabAvatarsInHub;

    [SerializeField] private Transform m_sceneHandleParent;

    [SerializeField] private GfcGameState[] m_statesWhereToggleEnabled;

    [SerializeField] private Transform m_avatarsParent;

    private List<CornMessageEventInstance> m_messageScenes = new(8);

    private CornAvatarInteract m_currentlyShownAvatarEvent = null;

    private Type m_queuedSocialEventAfterMessages = null;

    GfcInputTracker m_openPhoneInputTracker;

    protected bool m_canTogglePhone = true;

    public static bool CanTogglePhone { get { return Instance.m_canTogglePhone; } set { Instance.m_canTogglePhone = value; } }

    // Start is called before the first frame update
    void Awake()
    {
        m_openPhoneInputTracker = new(m_openPhoneInput);
        m_openPhoneInputTracker.DisplayPromptString = new("Open Phone");
        this.SetSingleton(ref Instance);
    }

    // Update is called once per frame
    void Update()
    {
        if (CanTogglePhone)
        {
            bool validState = m_statesWhereToggleEnabled.Length == 0;
            GfcGameState gameState = GfgManagerGame.GetGameState();

            for (int i = 0; !validState && i < m_statesWhereToggleEnabled.Length; i++)
                validState = gameState == m_statesWhereToggleEnabled[i];

            if (validState && !GfgManagerGame.GameStateTransitioningFadeInPhase() && m_openPhoneInputTracker.PressedSinceLastCheck())
                TogglePhone();
        }
    }

    public static void TogglePhone()
    {
        Instance.TogglePhoneInternal();
    }

    protected void TogglePhoneInternal()
    {
        if (CanTogglePhone)
        {
            GfcGameState gameStateToSet = GfgManagerGame.GetGameState() == GfcGameState.PHONE ? GfgManagerGame.GetPreviousGameState() : GfcGameState.PHONE;
            GfgManagerGame.SetGameState(gameStateToSet);
        }
    }

    public static void LoadAvailableStoryScenes()
    {
        ClearAllMessageScenes();
        AddMessageScenes(CornManagerStory.GetAvailableEvents());
    }

    public static void QueueSocialEventAfterMessages(Type aScene)
    {
        Debug.Assert(Instance.m_queuedSocialEventAfterMessages == null);
        Instance.m_queuedSocialEventAfterMessages = aScene;
    }

    protected static void StartSocialEventIfEventWasQueued()
    {
        if (Instance.m_queuedSocialEventAfterMessages != null)
        {
            CornManagerStory.IncrementStoryProgress(Instance.m_messageScenes[Instance.m_currentlyShownAvatarEvent.PhoneEventIndex].SceneDetails);
            ClearAllMessageScenes();
            CornEvent cornEvent = new(CornEventType.SOCIAL);
            cornEvent.Scene = Instance.m_queuedSocialEventAfterMessages;
            CornManagerEvents.ExecuteEvent(cornEvent);
            Instance.m_queuedSocialEventAfterMessages = null;
        }
    }

    public static bool AddMessageScenes(CornStoryVnSceneDetails aScene)
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
            CornAvatarInteract avatar = GfcPooling.Instantiate(Instance.m_prefabAvatarsInHub).GetComponent<CornAvatarInteract>();
            avatar.PhoneEventIndex = Instance.m_messageScenes.Count;
            avatar.SetStoryCharacter(aScene.Character);
            avatar.transform.SetParent(Instance.m_avatarsParent);

            Vector3 spawnPosition = UnityEngine.Random.insideUnitSphere * 1;
            spawnPosition.y = 0;
            spawnPosition.Normalize();
            spawnPosition *= UnityEngine.Random.Range(3, 7);
            spawnPosition.y = 1;

            avatar.transform.localPosition = spawnPosition;
            avatar.transform.localRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up);

            CornMessageEventInstance messageEvent = new(aScene);
            messageEvent.Avatar = avatar;

            Instance.m_messageScenes.Add(messageEvent);
        }
        else
        {
            Debug.LogError("The scene (" + aScene.Scene + ") could not be added, there is already an event registered for the character " + aScene.Character + ", (" + Instance.m_messageScenes[--currIndex].SceneDetails.Scene + ")");
        }

        return success;
    }

    public static void AddMessageScenes(IEnumerable<CornStoryVnSceneDetails> someScenes)
    {
        foreach (CornStoryVnSceneDetails vnScene in someScenes)
            AddMessageScenes(vnScene);
    }

    public static void ClearAllMessageScenes()
    {
        while (Instance.m_messageScenes.Count > 0)
            RemoveMessageScene(0);
    }

    public static void RemoveMessageScene(int aSceneIndex)
    {
        if (aSceneIndex < Instance.m_messageScenes.Count)
        {
            CornMessageEventInstance messageEvent = Instance.m_messageScenes[aSceneIndex];

            if (messageEvent.EventHandle.CoroutineHandle.IsValid)
            {
                Timing.KillCoroutines(messageEvent.EventHandle);
                GfcPooling.Destroy(messageEvent.PanelTemplate.gameObject);

                messageEvent.EventHandle = default;
                messageEvent.PanelTemplate = null;
                Instance.m_messageScenes[aSceneIndex] = messageEvent;
            }

            CloseMessageScene(aSceneIndex);
            Instance.m_messageScenes[aSceneIndex].EventHandle.KillCoroutine();
            Instance.m_messageScenes[aSceneIndex].Avatar.DestroySelf();
            Instance.m_messageScenes[aSceneIndex] = Instance.m_messageScenes[^1];
            Instance.m_messageScenes[^1].Avatar.PhoneEventIndex = aSceneIndex;
            Instance.m_messageScenes.RemoveAt(Instance.m_messageScenes.Count - 1);
        }
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
            RemoveMessageScene(--currIndex);

        return foundItem;
    }

    public static CornAvatarInteract GetCurrentlyShownAvatar() { return Instance.m_currentlyShownAvatarEvent; }

    public static void CloseMessageScene(int aSceneIndex)
    {
        CornMessageEventInstance messageEvent = Instance.m_messageScenes[aSceneIndex];

        if (Instance.m_currentlyShownAvatarEvent && Instance.m_currentlyShownAvatarEvent.PhoneEventIndex == aSceneIndex)
        {
            if (messageEvent.PanelTemplate) messageEvent.PanelTemplate.gameObject.SetActiveGf(false);
            Instance.m_currentlyShownAvatarEvent = null;
        }
    }

    public static void PressedAvatar(int anEventIndex)
    {
        if (Instance.m_currentlyShownAvatarEvent == null)
        {
            CornMessageEventInstance eventInstance = Instance.m_messageScenes[anEventIndex];
            Instance.m_currentlyShownAvatarEvent = Instance.m_messageScenes[anEventIndex].Avatar;

            if (eventInstance.PanelTemplate == null)
            {
                eventInstance.PanelTemplate = GfcPooling.Instantiate(Instance.m_prefabMessagesNotifyPanel).GetComponent<GfgVnSceneHandler>();
                eventInstance.PanelTemplate.transform.SetParent(Instance.m_sceneHandleParent, false);
            }

            eventInstance.EventHandle.RunCoroutineIfNotRunning(Instance._ExecuteMessageEvent(eventInstance.Avatar, eventInstance.PanelTemplate));
            Instance.m_messageScenes[anEventIndex] = eventInstance;

            eventInstance.PanelTemplate.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Requested event at index " + anEventIndex + ", but the event at index " + Instance.m_currentlyShownAvatarEvent.PhoneEventIndex + " is already being executed.");
        }
    }

    private IEnumerator<float> _ExecuteMessageEvent(CornAvatarInteract anAvatar, GfgVnSceneHandler aSceneHandler)
    {
        CornMessageEventInstance eventInstance = Instance.m_messageScenes[anAvatar.PhoneEventIndex];

        yield return Timing.WaitForOneFrame; //wait for the scene handle to initialize
        yield return Timing.WaitUntilDone(aSceneHandler.StartScene(eventInstance.SceneDetails.Scene));

        StartSocialEventIfEventWasQueued();
        RemoveMessageScene(anAvatar.PhoneEventIndex);
    }

    public static void PressedBack()
    {
        if (Instance.m_currentlyShownAvatarEvent)
            CloseMessageScene(Instance.m_currentlyShownAvatarEvent.PhoneEventIndex);
    }
}

internal struct CornMessageEventInstance
{
    public CornMessageEventInstance(CornStoryVnSceneDetails aSceneDetails)
    {
        SceneDetails = aSceneDetails;
        PanelTemplate = null;
        EventHandle = default;
        Avatar = null;
    }

    public CornAvatarInteract Avatar;
    public CornStoryVnSceneDetails SceneDetails;
    public GfgVnSceneHandler PanelTemplate;
    public GfcCoroutineHandle EventHandle;
}