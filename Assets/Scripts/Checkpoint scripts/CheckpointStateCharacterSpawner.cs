using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;

public class CheckpointStateCharacterSpawner : CheckpointState
{
    public float TimeUntilPlayPhase;

    public int CurrentPhase;

    public bool IsPlaying;

    public bool IsFinished;

    public int CharactersSpawnedAlive;

    public CharacterSpawner OriginalObject;

    public int CurrentSpawnIndex;

    public override void ExecuteCheckpointState()
    {
        OriginalObject.SetCheckpointState(this);
    }
}
