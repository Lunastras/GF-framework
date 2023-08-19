using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MEC;

public class CharacterSpawner : MonoBehaviour
{
    [SerializeField]
    public CharacterSpawnerPhase[] CharacterSpawnPhases = null;

    [SerializeField]
    protected bool m_playOnStart = false;

    [SerializeField]
    protected bool m_canReplay = false;

    [SerializeField]
    protected bool m_automaticallyLoop = false;

    [SerializeField]
    protected bool m_overrideCharacterType = false;

    [SerializeField]
    protected CharacterTypes m_characterType = CharacterTypes.ENEMY;

    protected float m_timeUntilNextPhase = 0;

    public bool IsPlaying { get; protected set; } = false;

    public bool IsFinished { get; protected set; } = false;

    public bool AllCharactersKilled { get; protected set; } = false;

    protected int m_charactersSpawnedAlive = 0;

    protected int m_currentPhase = -1;

    protected int m_currentSpawnIndex = 0;

    protected int m_currentThreatLevel = 0;

    protected int m_threadLevelCreatedByPhase = 0;

    public Action OnPlay = null;
    public Action OnStop = null;
    public Action OnFinish = null;
    public Action OnCharactersKilled = null;

    protected bool m_coroutineIsRunning;

    CheckpointStateCharacterSpawner m_checkpointState = null;

    void Awake()
    {
        CheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
        IsPlaying = m_playOnStart && GfServerManager.HasAuthority;
    }

    protected virtual void OnHardCheckpoint()
    {
        if (null == m_checkpointState) m_checkpointState = new();
        m_checkpointState.CharactersSpawnedAlive = m_charactersSpawnedAlive;
        m_checkpointState.IsPlaying = IsPlaying;
        m_checkpointState.IsFinished = IsFinished;
        m_checkpointState.TimeUntilPlayPhase = m_timeUntilNextPhase;
        m_checkpointState.CurrentPhase = m_currentPhase;
        m_checkpointState.CurrentSpawnIndex = m_currentSpawnIndex;
        m_checkpointState.OriginalObject = this;
        CheckpointManager.AddCheckpointState(m_checkpointState);
    }

    public void SetCheckpointState(CheckpointStateCharacterSpawner state)
    {
        m_checkpointState = state;
        m_currentPhase = state.CurrentPhase;
        IsPlaying = state.IsPlaying;
        IsFinished = state.IsFinished;
        m_charactersSpawnedAlive = state.CharactersSpawnedAlive;
        m_currentSpawnIndex = state.CurrentSpawnIndex;
        m_timeUntilNextPhase = state.TimeUntilPlayPhase;
    }

    protected void OnDestroy()
    {
        CheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
    }

    void FixedUpdate()
    {
        if (!m_coroutineIsRunning && IsPlaying)
        {
            m_timeUntilNextPhase -= Time.deltaTime;
            if (m_timeUntilNextPhase <= 0 || m_currentThreatLevel < CharacterSpawnPhases[m_currentPhase].MinimumThreatLevelUntilSpawn)
            {
                Timing.RunCoroutine(_SpawnCharacters(CharacterSpawnPhases[m_currentPhase]));
            }
        }
    }

    protected IEnumerator<float> _SpawnCharacters(CharacterSpawnerPhase phase)
    {
        m_coroutineIsRunning = true;

        for (; m_currentSpawnIndex < phase.Spawns.Length && IsPlaying; ++m_currentSpawnIndex)
        {
            yield return Timing.WaitForSeconds(phase.Spawns[m_currentSpawnIndex].Delay);
            if (IsPlaying)
            {
                var spawnDetails = phase.Spawns[m_currentSpawnIndex];
                GameObject obj = GfPooling.PoolInstantiate(spawnDetails.Object);
                StatsCharacter characterSpawned = obj.GetComponent<StatsCharacter>();
                if (characterSpawned)
                {
                    characterSpawned.OnKilled += OnCharacterKilled;
                    m_currentThreatLevel += characterSpawned.GetThreatLevel();
                    ++m_charactersSpawnedAlive;
                    characterSpawned.SetPitch(GfAudioManager.GetPitchFromNote(spawnDetails.Octave, spawnDetails.PianoNote));
                    if (m_overrideCharacterType)
                        characterSpawned.SetCharacterType(m_characterType);

                    characterSpawned.SpawnBehaviour();
                }

                obj.transform.position = phase.Spawns[m_currentSpawnIndex].Position.position;
            }
            else
            {
                m_currentSpawnIndex--; //cancel out the increment
            }
        }

        if (m_currentSpawnIndex == phase.Spawns.Length) //if we spawned all characters in the phase
        {
            m_currentPhase++;
            m_currentSpawnIndex = 0;
            if (m_currentPhase != CharacterSpawnPhases.Length)
            {
                m_timeUntilNextPhase = CharacterSpawnPhases[m_currentPhase].PhaseDelay;
            }
            else //finished 
            {
                IsFinished = true;
                m_currentPhase = -1;
                Stop();
                OnFinish?.Invoke();
            }
        }

        m_coroutineIsRunning = false;
    }

    protected void OnCharacterKilled(StatsCharacter character, ulong killerNetworkId, bool hasKillerNetworkId, int weaponLoadoutIndex, int weaponIndex)
    {
        m_charactersSpawnedAlive--;
        m_currentThreatLevel -= character.GetThreatLevel();

        if (m_charactersSpawnedAlive == 0 && IsFinished)
        {
            OnCharactersKilled?.Invoke();
        }
    }

    public void Play()
    {
        if (GfServerManager.HasAuthority && !IsPlaying && (!IsFinished || m_canReplay) && CharacterSpawnPhases.Length > 0)
        {
            IsFinished = false;

            if (m_currentPhase == -1) //if we start now
            {
                m_currentPhase = 0;
                m_timeUntilNextPhase = CharacterSpawnPhases[m_currentPhase].PhaseDelay;
            }

            OnPlay?.Invoke();
            IsPlaying = true;
        }
    }

    public void Stop()
    {
        if (IsPlaying)
        {
            OnStop?.Invoke();
            IsPlaying = false;
        }
    }
}

[System.Serializable]
public struct CharacterSpawnerPhase
{
    public float PhaseDelay;
    public int MinimumThreatLevelUntilSpawn;
    public CharacterSpawnDetails[] Spawns;
}

[System.Serializable]
public class CharacterSpawnDetails
{
    public Transform Position;
    public GameObject Object;
    public float Delay = 0;
    public int Octave = 4;
    public PianoNotes PianoNote = PianoNotes.C;
}

public enum PianoNotes
{
    C, C_SHARP, D, D_SHARP, E, F, F_SHARP, G, G_SHARP, A, A_SHARP, B
}
