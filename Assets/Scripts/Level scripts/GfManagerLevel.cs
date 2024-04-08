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

using GfPathFindingNamespace;

public class GfManagerLevel : MonoBehaviour
{
    public static GfManagerLevel Instance { get; protected set; } = null;

    private Transform m_player = null;

    private StatsPlayer m_statsPlayer = null;

    [SerializeField] private GameMultiplayerType m_autoStartGameType = GameMultiplayerType.SINGLEPLAYER;

    [SerializeField] private int m_missionIndex = 1;

    [SerializeField]
    private HudManager m_hudManager = null;

    //[SerializeField]
    //private Image m_loadFadeImage = null;

    [SerializeField]
    private GameObject m_pauseScreen = null;

    [SerializeField] private GfcSound m_deathScreenMusic = null;

    [SerializeField] private string m_nextMap = null;

    [SerializeField] private GfcSound m_calmMusic = null;

    [SerializeField] private GfcSound m_actionMusic = null;

    [SerializeField] private float m_actionCalmBlendTime = 1;

    [SerializeField] private string[] m_requiredSceneNames = null;

    [SerializeField]
    private GfPathfinding[] m_pathfindingSystems = null;


    private bool m_isPaused = false;

    private bool m_isShowingDeathScreen = false;

    public bool CanPause = true;

    private bool m_pauseButtonReleased = true;


    private float m_pitchSmoothTime = 2;

    private bool m_isPlayingCalmMusic = true;


    private LevelData m_levelData = default;

    [System.Serializable]
    private struct LevelData
    {
        public GfPathfinding.NodePathSaveData[] paths;
    }

    private string m_levelDataPath = null;

    private float m_calmSmoothRef = 0;

    private float m_actionSmoothRef = 0;

    private bool m_isBlendingCalmAction = false;

    private float m_desiredActionVolume = 0;

    private float m_desiredCalmVolume = 1;

    private float m_currentActionVolume = 0;

    private float m_currentCalmVolume = 1;

    private bool m_isShiftingPitch = false;

    private float m_desiredPitch = 1;

    private float m_pitchSmoothRef = 0;


    private GfcAudioSource m_deathMusicSource = null;

    private float m_deathMusicVolumeSmoothRef = 0;

    private float m_desiredDeathMusicVolume = 0;

    private float m_currentDeathMusicVolume = 0;

    private int m_enemiesKilled = 0;

    private float m_secondsSinceStart = 0;

    private int m_resetsCount = 0;

    private CheckpointGameManager m_checkpointState = null;

    protected GameState m_currentGameState = GameState.LEVEL_IDLE;

    public static Action OnLevelStart;

    public static Action OnLevelEnd;

    const float DEFAULT_SMOOTHTIME_ENV = 1;

    protected EnvironmentLightingColors m_envColorsDefault = default;

    protected EnvironmentLightingColors m_envColorsDesired = default;

    protected float m_envSmoothingProgress = 1;

    protected float m_envSmoothingDuration = 0;

    protected float m_envSmoothingRef = 0;


    void OnEnable()
    {
        GfcManagerGame.ValidateGameManager();

        for (int i = 0; i < m_requiredSceneNames.Length; ++i)
        {
            int sceneBuildIndex = LoadingScreenManager.GetSceneBuildIndexByName(m_requiredSceneNames[i]);
            if (!SceneManager.GetSceneByBuildIndex(sceneBuildIndex).isLoaded)
            {
                SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Additive);
            }
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this) Destroy(Instance);
        Instance = this;

        Instance = this;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (null == m_hudManager) m_hudManager = GetComponent<HudManager>();

        GfcManagerCharacters.OnCharacterRemoved += OnCharacterKilled;

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
            m_levelData.paths = new GfPathfinding.NodePathSaveData[m_pathfindingSystems.Length];
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

        if (!LoadingScreenManager.CurrentlyLoading)
        {
            GfcManagerGame.StartGame(m_autoStartGameType);
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

        CheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
        m_deathScreenMusic.LoadAudioClip();
        StartLevel();
    }

    private void OnDestroy()
    {
        GfcManagerCharacters.OnCharacterRemoved -= OnCharacterKilled;
        CheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
        m_actionMusic.SetMixerPitch(1);
        m_calmMusic.SetMixerPitch(1);
        m_actionMusic.SetMixerVolume(0);
        m_calmMusic.SetMixerVolume(1);
    }

    private void FixedUpdate()
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

    public void Update()
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

        if (m_isShowingDeathScreen && Input.GetKeyDown(KeyCode.Space))
        {
            m_isShowingDeathScreen = false;
            HudManager.ToggleDeathScreen(false);
            CheckpointManager.Instance.ResetToHardCheckpoint();
            CameraController.SnapToTargetInstance();
            GfManagerLevel.SetLevelMusicPitch(1, 0.2f);
            m_deathMusicSource?.GetAudioSource()?.Stop();
        }

        switch (m_currentGameState)
        {
            case (GameState.LEVEL_IDLE):
                break;

            case (GameState.LEVEL_STARTED):
                m_secondsSinceStart += Time.deltaTime;
                break;

            case (GameState.LEVEL_ENDED):
                if (Input.GetKeyDown(KeyCode.Space) && (NetworkManager.Singleton.IsHost
                || NetworkManager.Singleton.IsServer))
                {
                    GfManagerServer.LoadScene(GfManagerLevel.GetNextMap(), ServerLoadingMode.KEEP_SERVER);
                }
                break;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            m_player.GetComponent<StatsPlayer>().Kill();
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
    }

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

    public static GameState GetCurrentGameState()
    {
        return Instance.m_currentGameState;
    }

    public static void StartLevel()
    {
        if (Instance.m_currentGameState == GameState.LEVEL_IDLE)
        {
            Instance.m_currentGameState = GameState.LEVEL_STARTED;
            OnLevelStart?.Invoke();
        }
    }

    public static void EndLevel()
    {
        if (Instance.m_currentGameState != GameState.LEVEL_ENDED)
        {
            Instance.m_currentGameState = GameState.LEVEL_ENDED;

            Instance.CanPause = false;
            if (Instance.m_isPaused)
                PauseToggle();

            bool firstFinish = GfcManagerSaveData.GetActivePlayerSaveData().CompletedMission(Instance.m_missionIndex);
            OnLevelEnd?.Invoke();
        }
    }

    public static void PlayerDied()
    {
        HudManager.ToggleDeathScreen(true);
        Instance.m_isShowingDeathScreen = true;
        GfManagerLevel.SetLevelMusicPitch(0, 2);
        Instance.m_deathMusicSource = Instance.m_deathScreenMusic.Play();
        Instance.m_deathScreenMusic.SetMixerVolume(0);
        Instance.m_deathMusicVolumeSmoothRef = 0;
        Instance.m_desiredDeathMusicVolume = 1;
        Instance.m_currentDeathMusicVolume = 0;
    }

    public static void PauseToggle()
    {
        if (!Instance.m_isPaused && (!Instance.CanPause || LoadingScreenManager.CurrentlyLoading))
            return;

        Instance.m_isPaused = !Instance.m_isPaused;
        Time.timeScale = Instance.m_isPaused && !GfcManagerGame.IsMultiplayer ? 0 : 1;
        Instance.m_pauseScreen.SetActive(Instance.m_isPaused);
        Cursor.visible = Instance.m_isPaused;
        Cursor.lockState = Instance.m_isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public static bool IsPaused()
    {
        return Instance.m_isPaused;
    }

    public static float OnCheckpointReset(CheckpointManager checkpointManager, bool hardCheckpoint)
    {
        float delay = 0;
        if (checkpointManager.transform == GetPlayer())
        {
            if (hardCheckpoint)
            {
                HudManager.ResetHardCheckpointVisuals();
                GameParticles.ClearParticles();
            }
            else
                delay = HudManager.ResetSoftCheckpointVisuals();
        }

        return delay;
    }

    public static void OnCheckpointSet(CheckpointManager checkpointManager, bool hardCheckpoint)
    {
        if (checkpointManager.transform == GetPlayer())
        {
            if (hardCheckpoint)
                HudManager.TriggerHardCheckpointVisuals();
            else
                HudManager.TriggerSoftCheckpointVisuals();
        }
    }

    public void QuitToMenu()
    {
        GfcManagerGame.QuitToMenu();
    }

    public void QuitGame()
    {
        GfcManagerGame.QuitToMenu();
    }

    public static void CheckpointStatesExecuted(CheckpointManager checkpointManager)
    {
    }

    //called when an enemy is approaching this character
    public static void NotifyEnemyEngaging(int enemyCount, ulong enemyNetworkId)
    {
        if (enemyCount != 0) GfManagerLevel.StartActionMusic();
    }

    //called when an enemy stop engaging
    public static void NotifyEnemyDisengaging(int enemyCount, ulong enemyNetworkId)
    {
        if (enemyCount == 0) GfManagerLevel.StartCalmMusic();
    }

    public static Transform GetPlayer()
    {
        return Instance.m_player;
    }

    public static StatsPlayer GetPlayerStats()
    {
        return Instance.m_statsPlayer;
    }

    public static Vector3 GetPlayerPositionOnScreen()
    {
        try
        {
            return GfcManagerGame.Camera.WorldToScreenPoint(Instance.m_player.position);
        }
        catch (Exception) //bad practice, but it's better than 4 if checks
        {
            return Vector3.zero;
        }
    }

    public static void SetPlayer(StatsPlayer player)
    {
        Instance.m_player = player.transform;
        Instance.m_statsPlayer = player;
    }

    public void OnCharacterKilled(StatsCharacter character)
    {
        if (character.GetCharacterType() == CharacterTypes.ENEMY && character.IsDead() && m_currentGameState != GameState.LEVEL_ENDED)
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

        CheckpointManager.AddCheckpointState(m_checkpointState);
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
    public static HudManager GetHudManager() { return Instance.m_hudManager; }


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
        m_levelData.paths = new GfPathfinding.NodePathSaveData[m_pathfindingSystems.Length];
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

    public static void SetNodePathData(GfPathfinding system, GfPathfinding.NodePathSaveData data)
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

    private static void SaveLevelData()
    {
        File.WriteAllText(Instance.m_levelDataPath, JsonUtility.ToJson(Instance.m_levelData));
    }
}


[System.Serializable]
public struct EnvironmentLightingColors
{
    public Color Sky;
    public Color Equator;
    public Color Ground;
}