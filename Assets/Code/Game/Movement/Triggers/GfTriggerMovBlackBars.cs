using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerMovBlackBars : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_autoTrigger = true;

    [SerializeField]
    protected bool m_destroyAfterTrigger = false;

    [SerializeField]
    protected bool m_onlyForPlayer = true;
    [SerializeField]
    protected bool m_turnOn = false;

    [SerializeField]
    protected float m_delay = 0;

    [SerializeField]
    protected bool m_constantOpacity = false;

    [SerializeField]
    protected bool m_constantAnchors = false;

    [SerializeField]
    protected bool m_ignoreTimeScale = false;

    private void Start()
    {
        if (m_autoTrigger)
            GfxUiTools.SetBlackBars(m_turnOn, m_delay, m_constantOpacity, m_constantAnchors, m_ignoreTimeScale);

        if (m_destroyAfterTrigger)
            GfcPooling.Destroy(gameObject);
    }

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        if (!m_onlyForPlayer || GfgManagerLevel.Player.transform == movement.transform)
        {
            GfxUiTools.SetBlackBars(m_turnOn, m_delay, m_constantOpacity, m_constantAnchors, m_ignoreTimeScale);
            if (m_destroyAfterTrigger)
                GfcPooling.Destroy(gameObject);
        }
    }
}
