using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomEditor(typeof(OldGfPathfinding))]
public class OldGfPathfindingGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        OldGfPathfinding obj = (OldGfPathfinding)target;
        // Add a simple label
        if (GUILayout.Button("Generate node path"))
        {
            obj.GenerateNodePath();
        }
    }
}
