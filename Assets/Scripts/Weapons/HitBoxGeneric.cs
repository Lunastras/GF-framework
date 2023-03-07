using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HitBoxGeneric : MonoBehaviour, DamageSource
{
    public StatsCharacter characterStats { get; set; }

    public abstract void CollisionBehaviour(Collider other);
}
