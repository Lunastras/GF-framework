using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using GfgPathFindingNamespace;

[CustomEditor(typeof(GfgManagerLevel))]
public class GfLevelManagerGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GfgManagerLevel obj = (GfgManagerLevel)target;
        // Add a simple label
        if (GUILayout.Button("Generate all node paths"))
        {
            obj.GenerateAllNodePaths();
        }


    }
}
