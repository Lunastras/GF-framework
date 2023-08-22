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

//[ExecuteInEditMode]
public class GfLevelManager : MonoBehaviour
{
    public static GfLevelManager Instance { get; protected set; } = null;

    private Transform m_player = null;

    [SerializeField]
    private HudManager m_hudManager = null;

    //[SerializeField]
    //private Image m_loadFadeImage = null;

    [SerializeField]
    private GameObject m_pauseScreen = null;

    [SerializeField] private GfSound m_deathScreenMusic = null;

    [SerializeField] private string m_nextMap = null;

    [SerializeField] private GfSound m_calmMusic = null;

    [SerializeField] private GfSound m_actionMusic = null;

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


    private GfAudioSource m_deathMusicSource = null;

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


    void OnEnable()
    {
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

        HostilityManager.OnCharacterRemoved += OnCharacterKilled;

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
    }

    public void Start()
    {
        m_calmMusic.LoadAudioClip();
        m_actionMusic.LoadAudioClip();

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
        HostilityManager.OnCharacterRemoved -= OnCharacterKilled;
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
            CameraController.SnapInstanceToTarget();
            GfLevelManager.SetLevelMusicPitch(1, 0.2f);
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
                    GfServerManager.LoadScene(GfLevelManager.GetNextMap(), ServerLoadingMode.KEEP_SERVER);
                }
                break;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            m_player.GetComponent<StatsPlayer>().Kill();
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

            OnLevelEnd?.Invoke();
        }
    }

    public static void PlayerDied()
    {
        HudManager.ToggleDeathScreen(true);
        Instance.m_isShowingDeathScreen = true;
        GfLevelManager.SetLevelMusicPitch(0, 2);
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
        Time.timeScale = Instance.m_isPaused && !GfGameManager.IsMultiplayer ? 0 : 1;
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
        GfGameManager.QuitToMenu();
    }

    public void QuitGame()
    {
        GfGameManager.QuitToMenu();
    }

    public static void CheckpointStatesExecuted(CheckpointManager checkpointManager)
    {
        GameParticles.DisableNonEmittingGraves();
    }

    //called when an enemy is approaching this character
    public static void NotifyEnemyEngaging(int enemyCount, ulong enemyNetworkId)
    {
        if (enemyCount != 0) GfLevelManager.StartActionMusic();
    }

    //called when an enemy stop engaging
    public static void NotifyEnemyDisengaging(int enemyCount, ulong enemyNetworkId)
    {
        if (enemyCount == 0) GfLevelManager.StartCalmMusic();
    }

    public static Transform GetPlayer()
    {
        return Instance.m_player;
    }

    public static Vector3 GetPlayerPositionOnScreen()
    {
        return GfGameManager.Camera.WorldToScreenPoint(Instance.m_player.position);
    }

    public static void SetPlayer(Transform player)
    {
        Instance.m_player = player;
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

        m_checkpointState.CurrentState = m_currentGameState;
        m_checkpointState.SecondsSinceStart = m_secondsSinceStart;
        m_checkpointState.EnemiesKilled = m_enemiesKilled;

        CheckpointManager.AddCheckpointState(m_checkpointState);
    }

    public static void SetCheckpointState(CheckpointGameManager state)
    {
        CheckpointGameManager managerState = state as CheckpointGameManager;

        Instance.m_currentGameState = managerState.CurrentState;
        Instance.m_enemiesKilled = managerState.EnemiesKilled;
        Instance.m_secondsSinceStart = managerState.SecondsSinceStart;

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
