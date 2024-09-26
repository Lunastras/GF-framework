using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgTriggerHitBox : GfMovementTriggerable
{
    public GfgStatsCharacter Owner;

    public CharacterTypes DefaultCharacterType;

    public DamageType DamageType;

    public float Damage = 1;

    public bool SoftCheckpoint = false;

    void Awake()
    {
        this.GetComponentIfNull(ref Owner);
    }

    public override void MgOnTrigger(GfMovementGeneric aCharacter)
    {
        GfgStatsCharacter stats = aCharacter.GetComponent<GfgStatsCharacter>();

        if (stats)
        {
            CharacterTypes effectiveType = Owner ? Owner.GetCharacterType() : DefaultCharacterType;
            float effectiveDamage = Damage * GfgManagerCharacters.DamageMultiplier(stats.GetCharacterType(), effectiveType);
            stats.Damage(effectiveDamage, DamageType, Owner);
        }
    }
}