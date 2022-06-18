using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StatsCharacter : MonoBehaviour
{
    [SerializeField]
    private CharacterTypes characterType;

    // Start is called before the first frame update
    void Awake()
    {
        //HostilityManager.hostilityManager.AddCharacter(this);
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

            HostilityManager.hostilityManager.RemoveCharacter(this);
            characterType = type;
            HostilityManager.hostilityManager.AddCharacter(this);
        }
    }

    private void OnDestroy()
    {
        HostilityManager.hostilityManager.RemoveCharacter(this);
    }

    private void OnDisable()
    {
        HostilityManager.hostilityManager.RemoveCharacter(this);
    }
}
