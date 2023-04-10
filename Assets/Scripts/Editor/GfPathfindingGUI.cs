using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomEditor(typeof(GfPathfinding))]
public class GfPathfindingGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GfPathfinding obj = (GfPathfinding)target;
        // Add a simple label
        if (GUILayout.Button("Generate node path"))
        {
            obj.GenerateNodePath();
        }


    }
}
