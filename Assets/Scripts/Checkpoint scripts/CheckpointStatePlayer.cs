using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CheckpointStatePlayer : CheckpointState
{
    public List<EquipCharmData> AquiredCharms;

    public List<WeaponData> AquiredWeapons;

    public Vector3 Position = Vector3.zero;

    public Quaternion Rotation = Quaternion.identity;

    public Vector3 Scale = new Vector3(1, 1, 1);

    public float CurrentHp = 100;

    public Transform MovementParent = null;

    public uint MovementParentPriority = 0;

    public Transform MovementParentSpherical = null;

    public uint MovementGravityPriority = 0;

    public Vector3 Velocity;

    public Vector3 UpVec;

    public bool IsDead = false;

    public Action<StatsCharacter, DamageData> OnKilled = null;

    public override void ExecuteCheckpointState()
    {
        StatsPlayer.OwnPlayer.SetCheckpointState(this);
    }
}
