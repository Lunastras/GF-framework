using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

using GfPathFindingNamespace;

//[ExecuteInEditMode]
public class LevelManager : MonoBehaviour
{
    private static LevelManager Manager = null;

    private LevelData m_levelData = default;

    [SerializeField]
    private GfPathfinding[] m_pathfindingSystems = null;

    [System.Serializable]
    private struct LevelData
    {
        public GfPathfinding.NodePathSaveData[] paths;
    }

    private string m_levelDataPath;

    // Start is called before the first frame update
    void Awake()
    {
        if (Manager != this) Destroy(Manager);
        Manager = this;

        m_levelDataPath = Application.persistentDataPath + "/" + gameObject.scene.name + ".dat";

        if (File.Exists(m_levelDataPath))
        {
            m_levelData = JsonUtility.FromJson<LevelData>(File.ReadAllText(m_levelDataPath));

            for (int i = 0; i < m_pathfindingSystems.Length; ++i)
            {
                m_pathfindingSystems[i].SetNodePathData(m_levelData.paths[i]);
            }
        }
        else
        {
            Debug.Log("Node path data not found, generating it...");
            m_levelData.paths = new GfPathfinding.NodePathSaveData[m_pathfindingSystems.Length];
            GenerateAllNodePaths();
        }
    }

    public void GenerateAllNodePaths()
    {
        Debug.Log("Generating every path, might take a few seconds.");
        m_levelData.paths = new GfPathfinding.NodePathSaveData[m_pathfindingSystems.Length];
        for (int i = 0; i < m_pathfindingSystems.Length; ++i)
        {
            var pathData = m_pathfindingSystems[i].GenerateNodePathData();
            m_pathfindingSystems[i].SetNodePathData(pathData);
            m_levelData.paths[i] = pathData;
        }

        SaveLevelData();

        Debug.Log("Generated every node path!");
    }

    public static void SetNodePathData(GfPathfinding system, GfPathfinding.NodePathSaveData data)
    {
        if (Manager)
        {
            int index = -1;
            for (int i = 0; i < Manager.m_pathfindingSystems.Length; ++i)
            {
                if (Manager.m_pathfindingSystems[i] == system)
                {
                    index = i;
                    break;
                }
            }

            if (-1 != index)
            {
                Manager.m_levelData.paths[index] = data;
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
        File.WriteAllText(Manager.m_levelDataPath, JsonUtility.ToJson(Manager.m_levelData));
    }

    // Update is called once per frame
    void Update()
    {

    }
}
