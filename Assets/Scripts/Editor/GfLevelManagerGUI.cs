using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using GfPathFindingNamespace;

[CustomEditor(typeof(LevelManager))]
public class GfLevelManagerGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        LevelManager obj = (LevelManager)target;
        // Add a simple label
        if (GUILayout.Button("Generate all node paths"))
        {
            obj.GenerateAllNodePaths();
        }


    }
}