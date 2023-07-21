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

using GfPathFindingNamespace;

//[ExecuteInEditMode]
public class LevelManager : MonoBehaviour
{
    [SerializeField] private Sound m_calmMusic = null;

    [SerializeField] private Sound m_actionMusic = null;

    [SerializeField] private float m_actionCalmBlendTime = 1;

    [SerializeField] private int[] m_requiredScenesIndeces = null;

    [SerializeField]
    private GfPathfinding[] m_pathfindingSystems = null;

    private float m_pitchSmoothTime = 2;

    private bool m_isPlayingCalmMusic = true;

    private static LevelManager Instance = null;

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


    // Start is called before the first frame update
    void Awake()
    {



        if (Instance != this) Destroy(Instance);
        Instance = this;

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
        for (int i = 0; i < m_requiredScenesIndeces.Length; ++i)
        {
            if (!SceneManager.GetSceneByBuildIndex(m_requiredScenesIndeces[i]).isLoaded)
            {
                SceneManager.LoadScene(m_requiredScenesIndeces[i], LoadSceneMode.Additive);
            }
        }

        m_calmMusic.LoadAudioClip();
        m_actionMusic.LoadAudioClip();

        if (null != m_calmMusic && null != m_calmMusic.m_clip)
        {
            m_calmMusic.Play();
            m_actionMusic.SetMixerVolume(m_currentCalmVolume);
        }

        if (null != m_actionMusic && null != m_actionMusic.m_clip)
        {
            m_actionMusic.Play();
            m_actionMusic.SetMixerVolume(m_currentActionVolume);
        }
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
    }

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
