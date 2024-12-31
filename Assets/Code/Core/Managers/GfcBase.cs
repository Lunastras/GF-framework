
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GfcBase
{
    public static string GFC_BASE_SCENE_PATH { get { return Application.dataPath + "/Data/Scenes/GfBaseScene.unity"; } } //hardcoded because Scene.path returns the wrong value if the scene isn't loaded

    private static bool WaitingForSceneToLoad = false;

    public static void InitializeGfBase()
    {
        int sceneBuildIndex = (int)GfcSceneId.GF_BASE;
        Scene gfBaseScene = SceneManager.GetSceneByBuildIndex(sceneBuildIndex);
        if (!gfBaseScene.isLoaded && !WaitingForSceneToLoad)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                gfBaseScene = EditorSceneManager.OpenScene(GFC_BASE_SCENE_PATH, OpenSceneMode.Additive);
                //if (gfBaseScene.IsValid()) Debug.LogError("Could not find the scene '" + GFC_BASE_SCENE_PATH + "'.");
            }
            else
#endif
            {
                SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Additive);
                SceneManager.sceneLoaded += OnSceneLoaded;
                WaitingForSceneToLoad = true;
            }
        }
    }

    public static void OnSceneLoaded(Scene aScene, LoadSceneMode aMode)
    {
        WaitingForSceneToLoad = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}