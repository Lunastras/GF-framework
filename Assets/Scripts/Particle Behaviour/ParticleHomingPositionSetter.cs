using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleHomingPositionSetter : MonoBehaviour
{
    [SerializeField]
    private bool m_HomeOnAllCharacters = false;

    [SerializeField]
    private CharacterTypes m_characterTypeToHomeOn = CharacterTypes.ENEMY;

    [SerializeField]
    protected ParticleHoming m_particleHomingSystem = null;

    void Awake()
    {
        if (null == m_particleHomingSystem) m_particleHomingSystem = GetComponent<ParticleHoming>();
        m_particleHomingSystem.OnJobSchedule += OnJobSchedule;
    }
    // Update is called once per frame
    void OnJobSchedule()
    {
        if (m_HomeOnAllCharacters)
        {
            m_particleHomingSystem.SetTargetPositions(GfcManagerCharacters.GetAllCharacterPositions());
        }
        else
        {
            m_particleHomingSystem.SetTargetPositions(GfcManagerCharacters.GetCharacterPositions(m_characterTypeToHomeOn));
        }
    }
}
