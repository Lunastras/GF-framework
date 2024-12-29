using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Audio;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UI;
using Unity.Netcode;
using System;
using MEC;

using GfgPathFindingNamespace;

public class GfgManagerLevel : MonoBehaviour
{
    public static GfgManagerLevel Instance { get; protected set; } = null;

    protected GfgStatsCharacter m_player = null;

    [SerializeField] protected GfcGameMultiplayerType m_autoStartGameType = GfcGameMultiplayerType.SINGLEPLAYER;

    [SerializeField] protected int m_missionIndex = 1;

    [SerializeField]
    protected GameObject m_pauseScreen = null;

    [SerializeField] protected GfcSound m_deathScreenMusic = null;

    [SerializeField] protected string m_nextMap = null;

    [SerializeField] protected GfcSound m_calmMusic = null;

    [SerializeField] protected GfcSound m_actionMusic = null;

    [SerializeField] protected float m_actionCalmBlendTime = 1;

    [SerializeField] protected string[] m_requiredSceneNames = null;

    [SerializeField]
    protected GfgPathfinding[] m_pathfindingSystems = null;

    protected bool m_isPaused = false;

    public bool CanPause = true;

    protected bool m_pauseButtonReleased = true;


    protected float m_pitchSmoothTime = 2;

    protected bool m_isPlayingCalmMusic = true;


    protected LevelData m_levelData = default;

    [Serializable]
    protected struct LevelData
    {
        public GfgPathfinding.NodePathSaveData[] paths;
    }

    protected string m_levelDataPath = null;

    protected float m_calmSmoothRef = 0;

    protected float m_actionSmoothRef = 0;

    protected bool m_isBlendingCalmAction = false;

    protected float m_desiredActionVolume = 0;

    protected float m_desiredCalmVolume = 1;

    protected float m_currentActionVolume = 0;

    protected float m_currentCalmVolume = 1;

    protected bool m_isShiftingPitch = false;

    protected float m_desiredPitch = 1;

    protected float m_pitchSmoothRef = 0;


    protected GfcAudioSource m_deathMusicSource = null;

    protected float m_deathMusicVolumeSmoothRef = 0;

    protected float m_desiredDeathMusicVolume = 0;

    protected float m_currentDeathMusicVolume = 0;

    protected int m_enemiesKilled = 0;

    protected float m_secondsSinceStart = 0;

    protected int m_resetsCount = 0;

    protected CheckpointGameManager m_checkpointState = null;

    protected LevelState m_currentGameState = LevelState.LEVEL_IDLE;

    public static Action OnLevelStart;

    public static Action OnLevelEnd;

    public static Action OnLevelEndSubmit;

    const float DEFAULT_SMOOTHTIME_ENV = 1;

    protected EnvironmentLightingColors m_envColorsDefault = default;

    protected EnvironmentLightingColors m_envColorsDesired = default;

    protected float m_envSmoothingProgress = 1;

    protected float m_envSmoothingDuration = 0;

    protected float m_envSmoothingRef = 0;

    protected bool m_isShowingDeathScreen = false;

    // Start is called before the first frame update
    protected void Awake()
    {
        if (Instance != this) Destroy(Instance);
        Instance = this;

        GfcBase.InitializeGfBase();

        for (int i = 0; i < m_requiredSceneNames.Length; ++i)
        {
            int sceneBuildIndex = GfgManagerSceneLoader.GetSceneBuildIndexByName(m_requiredSceneNames[i]);
            if (!SceneManager.GetSceneByBuildIndex(sceneBuildIndex).isLoaded)
            {
                SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Additive);
            }
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        m_levelDataPath = Application.persistentDataPath + "/" + gameObject.scene.name + ".dat";

        bool loadedTheNodes = false;
        if (File.Exists(m_levelDataPath))
        {
            try
            {
                m_levelData = JsonUtility.FromJson<LevelData>(File.ReadAllText(m_levelDataPath));

                for (int i = 0; i < m_pathfindingSystems.Length; ++i)
                {
                    m_pathfindingSystems[i].SetNodePathData(m_levelData.paths[i]);
                }

                loadedTheNodes = true;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("There was an error while parsing the level data file '" + m_levelDataPath + "'\nException was: " + exception.ToString());
            }
        }

        //generate nodepaths if the level data file couldn't be read
        if (!loadedTheNodes)
        {
            m_levelData.paths = new GfgPathfinding.NodePathSaveData[m_pathfindingSystems.Length];
            GenerateAllNodePaths();
        }

        m_envColorsDefault.Sky = RenderSettings.ambientSkyColor;
        m_envColorsDefault.Ground = RenderSettings.ambientGroundColor;
        m_envColorsDefault.Equator = RenderSettings.ambientEquatorColor;
        m_envColorsDesired = m_envColorsDefault;
    }

    public void Start()
    {
        m_calmMusic.LoadAudioClip();
        m_actionMusic.LoadAudioClip();

        if (!GfgManagerSceneLoader.CurrentlyLoading)
        {
            GfgManagerGame.StartGame(m_autoStartGameType);
        }

        if (null != m_calmMusic && null != m_calmMusic.Clip)
        {
            m_calmMusic.Play();
            m_actionMusic.SetMixerVolume(m_currentCalmVolume);
        }

        if (null != m_actionMusic && null != m_actionMusic.Clip)
        {
            m_actionMusic.Play();
            m_actionMusic.SetMixerVolume(m_currentActionVolume);
        }

        GfgManagerCharacters.Instance.OnCharacterRemoved += OnCharacterKilled;
        GfgCheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
        m_deathScreenMusic.LoadAudioClip();
        StartLevel();
    }

    protected void OnDisable()
    {
        m_actionMusic.SetMixerPitch(1);
        m_calmMusic.SetMixerPitch(1);
        m_actionMusic.SetMixerVolume(0);
        m_calmMusic.SetMixerVolume(1);
    }

    protected void OnDestroy()
    {
        if (GfgManagerCharacters.Instance)
            GfgManagerCharacters.Instance.OnCharacterRemoved -= OnCharacterKilled;

        GfgCheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
    }

    protected void FixedUpdate()
    {
        if (m_isBlendingCalmAction)
        {
            m_currentActionVolume = Mathf.SmoothDamp(m_currentActionVolume, m_desiredActionVolume, ref m_actionSmoothRef, m_actionCalmBlendTime);
            m_currentCalmVolume = Mathf.SmoothDamp(m_currentCalmVolume, m_desiredCalmVolume, ref m_calmSmoothRef, m_actionCalmBlendTime);

            m_actionMusic.SetMixerVolume(m_currentActionVolume);
            m_calmMusic.SetMixerVolume(m_currentCalmVolume);

            m_isBlendingCalmAction = m_currentActionVolume != m_desiredActionVolume
                                    || m_currentCalmVolume != m_desiredCalmVolume;
        }

        if (m_isShiftingPitch)
        {
            float currentPitch = m_actionMusic.GetMixerPitch();
            currentPitch = Mathf.SmoothDamp(currentPitch, m_desiredPitch, ref m_pitchSmoothRef, m_pitchSmoothTime);

            m_actionMusic.SetMixerPitch(currentPitch);
            m_calmMusic.SetMixerPitch(currentPitch);

            m_isShiftingPitch = currentPitch != m_desiredPitch;
        }

        if (m_desiredDeathMusicVolume != m_currentDeathMusicVolume)
        {
            m_currentDeathMusicVolume = Mathf.SmoothDamp(m_currentDeathMusicVolume, m_desiredDeathMusicVolume, ref m_deathMusicVolumeSmoothRef, 3);
            m_deathScreenMusic.SetMixerVolume(m_currentDeathMusicVolume);
        }
    }

    protected void Update()
    {
        if (Input.GetAxisRaw("Pause") > 0.1f)
        {
            if (m_pauseButtonReleased)
            {
                m_pauseButtonReleased = false;
                PauseToggle();
            }
        }
        else
        {
            m_pauseButtonReleased = true;
        }

        switch (m_currentGameState)
        {
            case LevelState.LEVEL_IDLE:
                break;

            case LevelState.LEVEL_STARTED:
                m_secondsSinceStart += Time.deltaTime;
                break;

            case LevelState.LEVEL_ENDED:

                if (GfcInput.GetInput(GfcInputType.SUBMIT)
                && (!NetworkManager.Singleton || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
                {
                    OnLevelEndSubmit?.Invoke();

                    string nextMap = GetNextMap();
                    if (nextMap.IsEmpty())
                        GfcManagerServer.LoadScene(nextMap, ServerLoadingMode.KEEP_SERVER);
                }
                break;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            m_player.GetComponent<GfgStatsCharacter>().Kill();
        }

        if (m_envSmoothingProgress < 0.99999f)
        {
            m_envSmoothingProgress = Mathf.SmoothDamp(m_envSmoothingProgress, 1, ref m_envSmoothingRef, m_envSmoothingDuration);

            Color sky = RenderSettings.ambientSkyColor;
            Color ground = RenderSettings.ambientGroundColor;
            Color equator = RenderSettings.ambientEquatorColor;

            GfcTools.Blend(ref sky, m_envColorsDesired.Sky, m_envSmoothingProgress);
            GfcTools.Blend(ref ground, m_envColorsDesired.Ground, m_envSmoothingProgress);
            GfcTools.Blend(ref equator, m_envColorsDesired.Equator, m_envSmoothingProgress);

            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.ambientEquatorColor = equator;
        }

        if (m_isShowingDeathScreen && Input.GetKeyDown(KeyCode.Space))
        {
            m_isShowingDeathScreen = false;
            GfgHudManager.ToggleDeathScreen(false);
            GfgCheckpointManager.Instance.ResetToHardCheckpoint();
            GfgCameraController.Instance.SnapToTarget();
            SetLevelMusicPitch(1, 0.2f);
            m_deathMusicSource?.GetAudioSource()?.Stop();
        }
    }

    protected virtual void PlayerDiedInternal()
    {
        GfgHudManager.ToggleDeathScreen(true);
        m_isShowingDeathScreen = true;
        SetLevelMusicPitch(0, 2);
        m_deathMusicSource = m_deathScreenMusic.Play();
        m_deathScreenMusic.SetMixerVolume(0);
        m_deathMusicVolumeSmoothRef = 0;
        m_desiredDeathMusicVolume = 1;
        m_currentDeathMusicVolume = 0;
    }

    public static void PlayerDied() { Instance.PlayerDiedInternal(); }

    public static float GetSecondsSinceStart()
    {
        return Instance.m_secondsSinceStart;
    }

    public static int GetCurrentKillCount()
    {
        return Instance.m_enemiesKilled;
    }

    public static int GetResetsCount()
    {
        return Instance.m_resetsCount;
    }

    public static LevelState GetCurrentGameState()
    {
        return Instance.m_currentGameState;
    }

    public static void StartLevel()
    {
        if (Instance.m_currentGameState == LevelState.LEVEL_IDLE)
        {
            Instance.m_currentGameState = LevelState.LEVEL_STARTED;
            OnLevelStart?.Invoke();
        }
    }

    public static void EndLevel() { Instance.EndLevelInternal(); }

    protected virtual void EndLevelInternal()
    {
        if (Instance.m_currentGameState != LevelState.LEVEL_ENDED)
        {
            Instance.m_currentGameState = LevelState.LEVEL_ENDED;

            Instance.CanPause = false;
            if (Instance.m_isPaused)
                PauseToggle();

            OnLevelEnd?.Invoke();
        }
    }

    public static void PauseToggle()
    {
        if (!Instance.m_isPaused && (!Instance.CanPause || GfgManagerSceneLoader.CurrentlyLoading))
            return;

        Instance.m_isPaused = !Instance.m_isPaused;
        Time.timeScale = Instance.m_isPaused && !GfgManagerGame.IsMultiplayer ? 0 : 1;
        Instance.m_pauseScreen.SetActive(Instance.m_isPaused);
        Cursor.visible = Instance.m_isPaused;
        Cursor.lockState = Instance.m_isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public static bool IsPaused()
    {
        return Instance ? Instance.m_isPaused : false;
    }

    public static float OnCheckpointReset(GfgCheckpointManager aCheckpointManager, bool anIsHardCheckpoint)
    {
        return Instance.OnCheckpointResetInternal(aCheckpointManager, anIsHardCheckpoint);
    }

    protected virtual float OnCheckpointResetInternal(GfgCheckpointManager aCheckpointManager, bool anIsHardCheckpoint)
    {
        float delay = 0;

        if (anIsHardCheckpoint)
        {
            GfgHudManager.ResetHardCheckpointVisuals();
            //GameParticles.ClearParticles();
        }
        else
            delay = GfgHudManager.ResetSoftCheckpointVisuals();

        return delay;
    }

    public static void OnCheckpointSet(GfgCheckpointManager GfgCheckpointManager, bool hardCheckpoint)
    {
        Instance.OnCheckpointSetInternal(GfgCheckpointManager, hardCheckpoint);
    }

    protected virtual void OnCheckpointSetInternal(GfgCheckpointManager GfgCheckpointManager, bool hardCheckpoint) { }

    public void QuitToMenu()
    {
        GfgManagerGame.QuitToMenu();
    }

    public void QuitGame()
    {
        GfgManagerGame.QuitToMenu();
    }

    public static void CheckpointStatesExecuted(GfgCheckpointManager GfgCheckpointManager)
    {
    }

    //called when an enemy is approaching this character
    public static void NotifyEnemyEngaging(int enemyCount, ulong enemyNetworkId)
    {
        if (enemyCount != 0) GfgManagerLevel.StartActionMusic();
    }

    //called when an enemy stop engaging
    public static void NotifyEnemyDisengaging(int enemyCount, ulong enemyNetworkId)
    {
        if (enemyCount == 0) GfgManagerLevel.StartCalmMusic();
    }

    public static GfgStatsCharacter Player { get { return Instance.m_player; } set { Instance.m_player = value; } }

    public static Vector3 GetPlayerPositionOnScreen()
    {
        try
        {
            return GfgCameraController.Instance.Camera.WorldToScreenPoint(Instance.m_player.transform.position);
        }
        catch (Exception) //bad practice, but it's better than 4 if checks
        {
            return Vector3.zero;
        }
    }

    public void OnCharacterKilled(GfgStatsCharacter character)
    {
        if (character.GetCharacterType() == CharacterTypes.ENEMY && character.IsDead() && m_currentGameState != LevelState.LEVEL_ENDED)
        {
            ++m_enemiesKilled;
        }
    }


    protected void OnHardCheckpoint()
    {
        if (null == m_checkpointState) m_checkpointState = new();

        EnvironmentLightingColors envColors = default;
        envColors.Sky = RenderSettings.ambientSkyColor;
        envColors.Ground = RenderSettings.ambientGroundColor;
        envColors.Equator = RenderSettings.ambientEquatorColor;

        m_checkpointState.CurrentState = m_currentGameState;
        m_checkpointState.SecondsSinceStart = m_secondsSinceStart;
        m_checkpointState.EnemiesKilled = m_enemiesKilled;

        m_checkpointState.EnvColors = envColors;
        m_checkpointState.DesiredColors = m_envColorsDesired;
        m_checkpointState.DefaultColors = m_envColorsDefault;
        m_checkpointState.EnvSmoothingProgress = m_envSmoothingProgress;
        m_checkpointState.EnvSmoothingDuration = m_envSmoothingDuration;
        m_checkpointState.EnvSmoothingRef = m_envSmoothingRef;

        GfgCheckpointManager.AddCheckpointState(m_checkpointState);
    }

    public static void SetCheckpointState(CheckpointGameManager state)
    {
        CheckpointGameManager managerState = state as CheckpointGameManager;

        Instance.m_currentGameState = managerState.CurrentState;
        Instance.m_secondsSinceStart = managerState.SecondsSinceStart;
        Instance.m_enemiesKilled = managerState.EnemiesKilled;

        RenderSettings.ambientSkyColor = state.EnvColors.Sky;
        RenderSettings.ambientGroundColor = state.EnvColors.Ground;
        RenderSettings.ambientEquatorColor = state.EnvColors.Equator;

        Instance.m_envColorsDesired = state.DesiredColors;
        Instance.m_envColorsDefault = state.DefaultColors;
        Instance.m_envSmoothingProgress = state.EnvSmoothingProgress;
        Instance.m_envSmoothingDuration = state.EnvSmoothingDuration;
        Instance.m_envSmoothingRef = state.EnvSmoothingRef;

        Instance.m_resetsCount++;
    }

    public static string GetNextMap() { return Instance.m_nextMap; }

    public static void PauseMusic()
    {

    }

    public static void PlayMusic()
    {

    }

    public static void StartActionMusic()
    {
        if (Instance.m_isPlayingCalmMusic)
        {
            Instance.m_isPlayingCalmMusic = false;
            Instance.m_actionSmoothRef = 0;
            Instance.m_calmSmoothRef = 0;
            Instance.m_isBlendingCalmAction = true;
            Instance.m_desiredActionVolume = 1;
            Instance.m_desiredCalmVolume = 0;
        }
    }

    public static void StartCalmMusic()
    {
        if (!Instance.m_isPlayingCalmMusic)
        {
            Instance.m_isPlayingCalmMusic = true;
            Instance.m_actionSmoothRef = 0;
            Instance.m_calmSmoothRef = 0;
            Instance.m_isBlendingCalmAction = true;
            Instance.m_desiredActionVolume = 0;
            Instance.m_desiredCalmVolume = 1;
        }
    }

    public static void ReturnToDefaultEnvironmentColors(float smoothTime = DEFAULT_SMOOTHTIME_ENV)
    {
        SetEnvironmentColors(Instance.m_envColorsDefault, smoothTime);
    }

    public static void SetDefaultEnvironmentColors(EnvironmentLightingColors colors, float smoothTime = DEFAULT_SMOOTHTIME_ENV)
    {
        Instance.m_envColorsDefault = colors;
        SetEnvironmentColors(colors, smoothTime);
    }

    public static void SetEnvironmentColors(EnvironmentLightingColors colors, float smoothTime = DEFAULT_SMOOTHTIME_ENV)
    {
        Instance.m_envSmoothingRef = 0;
        Instance.m_envSmoothingProgress = 0;
        Instance.m_envColorsDesired = colors;
        Instance.m_envSmoothingDuration = smoothTime;
    }

    public static void SetLevelMusicPitch(float desiredPitch, float smoothTime)
    {
        Instance.m_isShiftingPitch = true;
        Instance.m_pitchSmoothRef = 0;
        Instance.m_desiredPitch = desiredPitch;
        Instance.m_pitchSmoothTime = smoothTime;
    }

    public void GenerateAllNodePaths()
    {
        Debug.Log("Generating nodepaths, might take a few seconds...");
        m_levelData.paths = new GfgPathfinding.NodePathSaveData[m_pathfindingSystems.Length];
        for (int i = 0; i < m_pathfindingSystems.Length; ++i)
        {
            var pathData = m_pathfindingSystems[i].GenerateNodePathData();
            m_pathfindingSystems[i].SetNodePathData(pathData);
            m_levelData.paths[i] = pathData;
        }

        try
        {
            SaveLevelData();
            Debug.Log("Generated every node path!");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("An error has occured while trying to save the level data, the exception message is: " + exception.ToString());
        }
    }

    public static void SetNodePathData(GfgPathfinding system, GfgPathfinding.NodePathSaveData data)
    {
        if (Instance)
        {
            int index = -1;
            for (int i = 0; i < Instance.m_pathfindingSystems.Length; ++i)
            {
                if (Instance.m_pathfindingSystems[i] == system)
                {
                    index = i;
                    break;
                }
            }

            if (-1 != index)
            {
                Instance.m_levelData.paths[index] = data;
                SaveLevelData();
            }
            else
            {
                Debug.LogError("The pathfinding system " + system.name + " is not in the pathfinding systems list in the LevelManager component. Please add it.");
            }
        }
        else
        {
            Debug.LogError("LevelManager: The manager is not initialised. Either this was called before Awake() or the LevelManager component doesn't exist in this scene");
        }
    }

    protected static void SaveLevelData()
    {
        File.WriteAllText(Instance.m_levelDataPath, JsonUtility.ToJson(Instance.m_levelData));
    }
}


[Serializable]
public struct EnvironmentLightingColors
{
    public Color Sky;
    public Color Equator;
    public Color Ground;
}

[Serializable]
public enum LevelState
{
    LEVEL_IDLE,
    LEVEL_STARTED,
    LEVEL_ENDED
}