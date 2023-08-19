using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DamageSource : MonoBehaviour
{
    [SerializeField]
    private StatsCharacter m_statsCharacter;

    public DamageSource ParentDamageSource = null;

    public void SetStatsCharacter(StatsCharacter value)
    {
        m_statsCharacter = value;
        if (ParentDamageSource) ParentDamageSource.SetStatsCharacter(value);
    }

    public StatsCharacter GetStatsCharacter()
    {
        if (ParentDamageSource)
            return ParentDamageSource.GetStatsCharacter();
        else
            return m_statsCharacter;
    }

    public virtual void OnDamageDealt(float damage, StatsCharacter damagedCharacter, bool isServerCall) { }
    public virtual void OnCharacterKilled(StatsCharacter damagedCharacter, bool isServerCall) { }
}
