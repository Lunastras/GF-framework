using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DamageSource : MonoBehaviour
{
    public virtual void OnDamageDealt(float damage, StatsCharacter damagedCharacter) {}
    public virtual void OnCharacterKilled(StatsCharacter damagedCharacter) {}
}
