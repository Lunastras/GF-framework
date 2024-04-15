using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HitBoxGeneric : DamageSource
{
    public GfgStatsCharacter characterStats { get; set; }

    public abstract void CollisionBehaviour(Collider other);
}
