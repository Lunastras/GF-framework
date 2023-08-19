using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using GfPathFindingNamespace;

[CustomEditor(typeof(GfLevelManager))]
public class GfLevelManagerGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GfLevelManager obj = (GfLevelManager)target;
        // Add a simple label
        if (GUILayout.Button("Generate all node paths"))
        {
            obj.GenerateAllNodePaths();
        }


    }
}
