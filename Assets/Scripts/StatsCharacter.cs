using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StatsCharacter : MonoBehaviour
{
    [SerializeField]
    private CharacterTypes characterType;

    // Start is called before the first frame update
    void Start()
    {
        HostilityManager.AddCharacter(this);
    }

    public abstract void Damage(float damage, StatsCharacter enemy = null);

    public abstract void Kill();

    public CharacterTypes GetCharacterType()
    {
        return characterType;
    }

    public void SetCharacterType(CharacterTypes type)
    {
        if (characterType != type)
        {
            HostilityManager.RemoveCharacter(this);
            characterType = type;
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
        HostilityManager.AddCharacter(this);
    }
}
