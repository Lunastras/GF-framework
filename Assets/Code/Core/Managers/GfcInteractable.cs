using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfcInteractable : MonoBehaviour
{
    public bool Interactable = true;

    public bool InteractVisible = true;

    public abstract void Interact(GfcCursorRayhit aHit);

    public virtual bool InteractIsVisible(GfcCursorRayhit aHit) { return InteractVisible; }

    public virtual bool IsInteractable(GfcCursorRayhit aHit, ref string aNonInteractableReason) { return Interactable && gameObject.ActiveInHierarchyGf(); }
    public virtual void OnCursorEnter(GfcCursorRayhit aHit) { }
    public virtual void OnCursorExit(GfcCursorRayhit aHit) { }
}