using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfcClickInteract : MonoBehaviour
{
    [SerializeField] LayerMask m_layermask;
    [SerializeField] QueryTriggerInteraction m_queryTriggerInteraction = QueryTriggerInteraction.Ignore;

    private GfcInputTracker m_submitTracker;

    private static GfcClickInteract Instance;

    private GfcInteractable m_currentlyHoveredInteract;

    private GfcLocalizedString m_clickInteractText;

    public static GfcInteractable GetHoveredInteract() { return Instance.m_currentlyHoveredInteract; }

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);
        m_submitTracker = new(GfcInputType.SUBMIT);
        m_submitTracker.DisplayPrompt = false;
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
            component = aHit.GameObject.GetComponent<GfcInteractable>();

        if (component != m_currentlyHoveredInteract)
        {
            m_currentlyHoveredInteract?.OnCursorExit(aHit);
            component?.OnCursorEnter(aHit);
        }

        m_currentlyHoveredInteract = component;
        return component;
    }

    void CheckInteract(bool anInteract)
    {
        var hitInfo = GfcCursor.GetGameObjectUnderMouse();
        GfcInteractable component = SetHoveredInteract(hitInfo);

        if (component)
        {
            if (component)
            {
                string nonInteractableReason = null;
                if (component.IsInteractable(hitInfo, ref nonInteractableReason))
                {
                    if (anInteract)
                    {
                        component.Interact(hitInfo);
                    }
                    else //somehow let player they can interact
                    {
                        GfcInput.UpdateDisplayInput(m_submitTracker.InputType, m_clickInteractText);
                    }
                }
                else
                {
                    //somehow let player they cannot interact
                }
            }
        }
    }
}
