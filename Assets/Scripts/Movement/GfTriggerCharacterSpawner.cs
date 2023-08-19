using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerCharacterSpawner : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = true;

    [SerializeField]
    protected CharacterSpawner[] m_characterSpawners = null;

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        if (!m_onlyForPlayer || GfLevelManager.GetPlayer() == movement.transform)
        {
            for (int i = 0; i < m_characterSpawners.Length; ++i)
            {
                m_characterSpawners[i].Play();
            }
        }
    }
}
