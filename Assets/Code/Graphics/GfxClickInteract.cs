using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfxClickInteract : MonoBehaviour
{
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

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);
        m_submitTracker = new(GfcInputType.SUBMIT);
        m_submitTracker.DisplayPrompt = false;
        m_submitTracker.Key = new((int)GfcInputLockPriority.GF_MASTER);
        m_clickInteractText = new("Interact");
    }

    // Update is called once per frame
    void Update()
    {
        if (m_submitTracker.PressedSinceLastCheck()) //on demand interact
            CheckInteract(true);
    }

    void FixedUpdate()
    {
        CheckInteract(false); //check every so often so highlight to the player they can interact with something
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
            string notInteractableReason = null;
            GfcLockKey inputKey = new((int)component.InputKeyPriority);
            if (component.Interactable(hitInfo, out notInteractableReason))
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
            else
            {
                //somehow let player they cannot interact
            }
        }
    }
}
