using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfxCharacterPortraits : MonoBehaviour
{
    private static GfxCharacterPortraits Instance;

    [SerializeField] private GfxCharacterPortraitData[] m_characterPortraits;

    public static GfcStoryCharacter GetProtag() { return GfcStoryCharacter.PROTAG; } //we might set a custom protag at some point

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);

        for (int i = 0; i < m_characterPortraits.Length; ++i)
            Debug.Assert(i == (int)m_characterPortraits[i].Character);
    }

    public static GfxCharacterPortraitData GetPortraitData(GfcStoryCharacter aStoryCharacter)
    {
        return Instance.m_characterPortraits[(int)aStoryCharacter];
    }
}

[System.Serializable]
public struct GfxCharacterPortraitData
{
    public GfcStoryCharacter Character;
    public Sprite MainSprite;
    public Sprite PhoneSprite;
}

public enum CharacterEmotion
{
    NEUTRAL,
    SMILE,
    THINKING,
    SURPRISED,
    ANRGY,
    SAD,

    SUSPICIOUS,
    EMBARRASED,
    SMILE_BIG,
    CRYING,
}

public enum CharacterSound
{
    NONE,
    HMMM,
    SIGH,
    SWEAR,
    LAUGH_1,
    LAUGH_2,
    LAUGH_3,
}