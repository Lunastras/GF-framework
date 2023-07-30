using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;

public class CheckpointStateGraves : CheckpointState
{
    public List<Vector3> GravesPositions;

    public GravityReference GravityReference;

    public override void ExecuteCheckpointState()
    {
        GameParticles.SpawnGraves(GravesPositions, GravityReference);
    }
}
