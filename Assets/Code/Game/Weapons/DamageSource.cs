using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DamageSource : MonoBehaviour
{
    [SerializeField]
    private GfgStatsCharacter m_statsCharacter;

    public DamageSource ParentDamageSource = null;

    public void SetStatsCharacter(GfgStatsCharacter value)
    {
        m_statsCharacter = value;
        //if (ParentDamageSource) ParentDamageSource.SetStatsCharacter(value);
    }

    public GfgStatsCharacter GetGfgStatsCharacter()
    {
        if (ParentDamageSource)
            return ParentDamageSource.GetGfgStatsCharacter();
        else
            return m_statsCharacter;
    }

    public virtual void OnDamageDealt(float damage, GfgStatsCharacter damagedCharacter, bool isServerCall) { }
    public virtual void OnCharacterKilled(GfgStatsCharacter damagedCharacter, bool isServerCall) { }
}
