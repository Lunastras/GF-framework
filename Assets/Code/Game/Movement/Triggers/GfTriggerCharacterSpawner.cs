using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfTriggerCharacterSpawner : GfMovementTriggerable
{
    [SerializeField]
    protected bool m_onlyForPlayer = true;

    [SerializeField]
    protected CharacterSpawner[] m_characterSpawners = null;

    void Start()
    {
        if (m_characterSpawners.Length == 0)
        {
            CharacterSpawner spawner = GetComponent<CharacterSpawner>();
            if (spawner)
            {
                m_characterSpawners = new CharacterSpawner[1];
                m_characterSpawners[0] = spawner;
            }
        }
    }

    public override void MgOnTrigger(GfMovementGeneric movement)
    {
        if (!m_onlyForPlayer || GfgManagerLevel.Player.transform == movement.transform)
        {
            for (int i = 0; i < m_characterSpawners.Length; ++i)
            {
                m_characterSpawners[i].Play();
            }
        }
    }
}
