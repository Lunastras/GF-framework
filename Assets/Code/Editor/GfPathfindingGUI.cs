using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using GfgPathFindingNamespace;

[CustomEditor(typeof(GfgPathfinding))]
public class GfPathfindingGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GfgPathfinding obj = (GfgPathfinding)target;
        // Add a simple label
        if (GUILayout.Button("Generate node path"))
        {
            obj.GenerateNodePath();
        }


    }
}
