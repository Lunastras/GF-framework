using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfxCharacterPortraits : MonoBehaviour
{
    private static GfxCharacterPortraits Instance;

    [SerializeField] private CharacterPortrait[] m_characterPortraits;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        for (int i = 0; i < m_characterPortraits.Length; ++i)
            Debug.Assert(i == (int)m_characterPortraits[i].Character);
    }

    public static Sprite GetPortrait(StoryCharacter aStoryCharacter)
    {
        if ((int)aStoryCharacter < Instance.m_characterPortraits.Length)
            return Instance.m_characterPortraits[(int)aStoryCharacter].Sprite;
        else
            return null;
    }
}

[System.Serializable]
public struct CharacterPortrait
{
    public StoryCharacter Character;
    public Sprite Sprite;
}