using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfxClickInteract : MonoBehaviour
{
    [SerializeField] float m_interactableCheckInterval = 0.02f;
    [SerializeField] LayerMask m_layermask;
    [SerializeField] QueryTriggerInteraction m_queryTriggerInteraction = QueryTriggerInteraction.Ignore;

#if UNITY_EDITOR
    [SerializeField] bool m_printObjectUnderCursor;
    private GameObject m_currentlyHoveredGameObject;
#endif

    private GfcInputTracker m_submitTracker;

    private static GfxClickInteract Instance;

    private GfcInteractable m_currentlyHoveredInteract;

    private GfcLocalizedString m_clickInteractText;

    public static GfcInteractable GetHoveredInteract() { return Instance.m_currentlyHoveredInteract; }

    private float m_timeUntilInteractableCheck = 0;

    void Awake()
    {
        this.SetSingleton(ref Instance);
        m_submitTracker = new(GfcInputType.SUBMIT);
        m_submitTracker.DisplayPrompt = false;
        m_submitTracker.Key = new((int)GfcInputLockPriority.GF_MASTER);
        m_timeUntilInteractableCheck = m_interactableCheckInterval;
    }

    void Start()
    {
        m_clickInteractText = new("Interact");
    }

    void Update()
    {
        m_timeUntilInteractableCheck -= Time.unscaledDeltaTime;
        bool interact = m_submitTracker.PressedSinceLastCheck();

        if (interact || m_timeUntilInteractableCheck <= 0)
        {
            CheckInteract(interact);
            m_timeUntilInteractableCheck = m_interactableCheckInterval;
        }
    }

    private GfcInteractable SetHoveredInteract(GfcCursorRayhit aHit)
    {
        GfcInteractable component = null;

        if (aHit.GameObject)
        {
            component = aHit.GameObject.GetComponent<GfcInteractable>();
#if UNITY_EDITOR
            if (m_printObjectUnderCursor && m_currentlyHoveredGameObject != aHit.GameObject)
                Debug.Log("Hovering over: " + aHit.GameObject.name);
#endif
        }

        if (component != m_currentlyHoveredInteract)
        {
            if (m_currentlyHoveredInteract)
            {
                m_currentlyHoveredInteract.OnCursorExit(aHit);
                GfxUiTools.EraseDisableReason(m_currentlyHoveredInteract);
            }

            if (component)
            {
                GfxUiTools.WriteDisabledReason(component);
                component.OnCursorEnter(aHit);
            }
        }

#if UNITY_EDITOR
        m_currentlyHoveredGameObject = aHit.GameObject;
#endif
        m_currentlyHoveredInteract = component;
        return component;
    }

    void CheckInteract(bool anInteract)
    {
        var hitInfo = GfcCursor.GetGameObjectUnderMouse(false, m_layermask, m_queryTriggerInteraction);
        GfcInteractable component = SetHoveredInteract(hitInfo);

        if (component)
        {
            GfcLockKey inputKey = new((int)component.InputKeyPriority);
            if (component.Interactable(hitInfo))
            {
                if (anInteract && GfcInput.InputLockHandle.AuthorityTest(inputKey))
                {
                    component.Interact(hitInfo);
                }
                else if (!component.HideInteractablePrompt)//somehow let player they can interact
                {
                    GfcInput.UpdateDisplayInput(m_submitTracker.InputType, inputKey, m_clickInteractText);
                }
            }
        }
    }
}
