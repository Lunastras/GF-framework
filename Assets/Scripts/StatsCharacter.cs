using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StatsCharacter : MonoBehaviour
{
    [SerializeField]
    private CharacterTypes m_characterType;

    private int m_characterIndex = -1;

    protected bool m_initialised = false;

    // Start is called before the first frame update
    void Start()
    {
        HostilityManager.AddCharacter(this);
        m_initialised = true;
    }

    public abstract void Damage(float damage, StatsCharacter enemy = null);

    public abstract void Kill();

    public CharacterTypes GetCharacterType()
    {
        return m_characterType;
    }

    public void SetCharacterType(CharacterTypes type)
    {
        if (m_characterType != type)
        {
            HostilityManager.RemoveCharacter(this);
            m_characterType = type;
            HostilityManager.AddCharacter(this);
        }
    }

    private void OnDestroy()
    {
        HostilityManager.RemoveCharacter(this);
    }

    private void OnDisable()
    {
        HostilityManager.RemoveCharacter(this);
    }

    private void OnEnable()
    {
        if (m_initialised)
            HostilityManager.AddCharacter(this);
    }

    public int GetCharacterIndex()
    {
        return m_characterIndex;
    }

    public void SetCharacterIndex(int index)
    {
        m_characterIndex = index;
    }
}
