using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CheckpointStateNpc : CheckpointStatePlayer
{
    public bool WasFollowingPlayer = false;
    public GameObject Prefab = null;

    public Action<StatsCharacter, ulong, bool, int, int> OnKilled = null;

    public override void ExecuteCheckpointState()
    {
        StatsNpc statsNpc = GfPooling.Instantiate(Prefab).GetComponent<StatsNpc>();
        statsNpc.SetCheckpointState(this);
    }

}
