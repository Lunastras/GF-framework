using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MEC;
using System.Numerics;

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

    protected GfgStatsCharacter m_target;

    protected float m_timeUntilNextPhase = 0;

    public bool IsPlaying { get; protected set; } = false;

    public bool IsFinished { get; protected set; } = false;

    public bool AllCharactersKilled { get; protected set; } = false;

    protected int m_charactersSpawnedAlive = 0;

    protected int m_currentPhase = 0;

    protected int m_currentSpawnIndex = 0;

    protected uint m_currentThreatLevel = 0;

    protected int m_threadLevelCreatedByPhase = 0;

    private Transform m_transform;

    public Action OnPlay = null;
    public Action OnStop = null;
    public Action OnFinish = null;

    public Action OnCheckpointStateSet = null;
    public Action OnCharactersKilled = null;

    public bool m_interruptCoroutine = false;

    protected bool m_coroutineIsRunning;

    CheckpointStateCharacterSpawner m_checkpointState = null;

    void Awake()
    {
        m_transform = transform;
        GfgCheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
        IsPlaying = m_playOnStart && GfcManagerServer.HasAuthority;
        m_timeUntilNextPhase = CharacterSpawnPhases[0].PhaseDelay;

        for (int i = 0; i < CharacterSpawnPhases.Length; i++)
        {
            for (int j = 0; j < CharacterSpawnPhases[i].Spawns.Length; j++)
            {
                if (null == CharacterSpawnPhases[i].Spawns[j])
                    Debug.LogError("The object to spawn set in '" + gameObject.name + "' at phase " + i + " and index " + j + " is null.");
            }
        }
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
        GfgCheckpointManager.AddCheckpointState(m_checkpointState);
    }

    public void SetCheckpointState(CheckpointStateCharacterSpawner aState)
    {
        m_checkpointState = aState;
        m_currentPhase = aState.CurrentPhase;
        IsPlaying = aState.IsPlaying;
        IsFinished = aState.IsFinished;
        m_charactersSpawnedAlive = aState.CharactersSpawnedAlive;
        m_currentSpawnIndex = aState.CurrentSpawnIndex;
        m_timeUntilNextPhase = aState.TimeUntilPlayPhase;
        m_coroutineIsRunning = false;
        m_interruptCoroutine = true;

        OnCheckpointStateSet?.Invoke();
    }

    protected void OnDestroy()
    {
        GfgCheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
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

    protected IEnumerator<float> _SpawnCharacters(CharacterSpawnerPhase aPhase)
    {
        m_interruptCoroutine = false;
        m_coroutineIsRunning = true;

        for (; m_currentSpawnIndex < aPhase.Spawns.Length && !m_interruptCoroutine && IsPlaying; ++m_currentSpawnIndex)
        {
            yield return Timing.WaitForSeconds(aPhase.Spawns[m_currentSpawnIndex].Delay);
            if (!m_interruptCoroutine && IsPlaying)
            {
                var spawnDetails = aPhase.Spawns[m_currentSpawnIndex];
                GameObject obj = GfcPooling.PoolInstantiate(spawnDetails.Object);
                GfgStatsCharacter characterSpawned = obj.GetComponent<GfgStatsCharacter>();

                if (characterSpawned)
                {

                    characterSpawned.OnKilled += OnCharacterKilled;
                    m_currentThreatLevel += (uint)characterSpawned.GetThreatDetails().ThreatLevel;
                    ++m_charactersSpawnedAlive;
                    characterSpawned.SetPitch(GfcManagerAudio.GetPitchFromNote(spawnDetails.Octave, spawnDetails.PianoNote));
                    if (m_overrideCharacterType)
                        characterSpawned.SetCharacterType(m_characterType);

                    // characterSpawned.SetTarget(m_target);
                    characterSpawned.SpawnBehaviour();
                }

                UnityEngine.Vector3 spawnPosition = aPhase.Spawns[m_currentSpawnIndex].Position ? aPhase.Spawns[m_currentSpawnIndex].Position.position : m_transform.position;

                Rigidbody spawnedObjectRb = obj.GetComponent<Rigidbody>();
                if (spawnedObjectRb)
                    spawnedObjectRb.MovePosition(spawnPosition);
                else
                    obj.transform.position = spawnPosition;


            }
            else
            {
                m_currentSpawnIndex--; //cancel out the increment
            }
        }

        if (!m_interruptCoroutine && m_currentSpawnIndex == aPhase.Spawns.Length) //if we spawned all characters in the phase
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

    protected void OnCharacterKilled(GfgStatsCharacter aCharacter, DamageData aDamageData)
    {
        m_charactersSpawnedAlive--;
        m_currentThreatLevel -= (uint)aCharacter.GetThreatDetails().ThreatLevel;

        if (m_charactersSpawnedAlive == 0 && IsFinished)
        {
            OnCharactersKilled?.Invoke();
        }
    }

    public void Play()
    {
        if (GfcManagerServer.HasAuthority && !IsPlaying && (!IsFinished || m_canReplay) && CharacterSpawnPhases.Length > 0)
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