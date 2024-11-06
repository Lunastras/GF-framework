using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

[RequireComponent(typeof(GfcTransitionParent))]
public abstract class GfcTransitionChild : MonoBehaviour
{
    GfcTransitionParent m_parent;

    protected void Awake()
    {
        m_parent = GetComponent<GfcTransitionParent>();
        m_parent.AddChild(this);
    }

    public abstract void SetProgress(float aProgress);
}