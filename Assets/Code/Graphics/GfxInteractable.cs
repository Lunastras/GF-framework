using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfcInteractable : MonoBehaviour
{
    public GfcInputLockPriority InputKeyPriority = GfcInputLockPriority.UI1;
    [SerializeField] protected bool m_interactable { get; private set; } = true;
    [HideInInspector]
    public string NotInteractableReason
    {
        get { return m_notInteractableReason; }
        set { m_notInteractableReason = m_interactable ? null : value; }
    }

    private string m_notInteractableReason = null;

    public abstract void Interact(GfcCursorRayhit aHit);

    public bool Interactable() { return Interactable(default, out string _); }
    public bool Interactable(out string aNotInteractableReason) { return Interactable(default, out aNotInteractableReason); }
    public bool Interactable(GfcCursorRayhit aHit) { return Interactable(aHit, out string _); }
    public virtual bool Interactable(GfcCursorRayhit aHit, out string aNotInteractableReason) { aNotInteractableReason = NotInteractableReason; return m_interactable && gameObject.ActiveInHierarchyGf(); }

    public virtual void SetInteractable(bool anInteractable, string aNotInteractableReason = null)
    {
        m_interactable = anInteractable;
        NotInteractableReason = aNotInteractableReason;
    }

    public virtual void OnCursorEnter(GfcCursorRayhit aHit) { }
    public virtual void OnCursorExit(GfcCursorRayhit aHit) { }
}