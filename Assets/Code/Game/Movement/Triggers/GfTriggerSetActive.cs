using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerSetActive : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = true;

    [SerializeField]
    protected GameObjectActiveSetter[] m_gameObjects = null;

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        if (!m_onlyForPlayer || GfgManagerLevel.GetPlayer() == movement.transform)
        {
            for (int i = 0; i < m_gameObjects.Length; ++i)
            {
                m_gameObjects[i].GameObject.SetActive(m_gameObjects[i].Active);
            }
        }
    }
}

[System.Serializable]
public struct GameObjectActiveSetter
{
    public GameObject GameObject;
    public bool Active;
}
