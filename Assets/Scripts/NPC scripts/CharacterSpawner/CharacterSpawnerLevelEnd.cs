using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawnerLevelEnd : MonoBehaviour
{
    [SerializeField]
    protected CharacterSpawner[] Spawners;

    [SerializeField]
    protected EnvironmentLightingColors m_EnvironmentColors;
    // Start is called before the first frame update
    void Awake()
    {

        for (int i = 0; i < Spawners.Length; ++i)
        {
            Spawners[i].OnCharactersKilled += OnCharactersKilled;
        }

        if (Spawners.Length == 0)
        {
            CharacterSpawner spawner = GetComponent<CharacterSpawner>();
            spawner.OnCharactersKilled += OnCharactersKilled;
        }
    }

    protected void OnCharactersKilled()
    {
        GfManagerLevel.EndLevel();
    }
}
