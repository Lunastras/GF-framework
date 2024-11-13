using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[ExecuteAlways]
public class GfxContentSizeFitter : ContentSizeFitter
{
    [SerializeField] public Vector2 LengthsMin = new(0, 0);

    [SerializeField] public Vector2 LengthsMax = new(1920, 1080);

    protected new void Awake()
    {
        base.Awake();
    }

    public override void SetLayoutHorizontal()
    {
        base.SetLayoutHorizontal();
        ClampSize();
    }

    public override void SetLayoutVertical()
    {
        base.SetLayoutVertical();
        ClampSize();
    }

    protected void ClampSize()
    {
        RectTransform rectTransform = transform as RectTransform;
        Vector2 sizeDelta = rectTransform.sizeDelta;
        sizeDelta.x = sizeDelta.x.Clamp(LengthsMin.x, LengthsMax.x);
        sizeDelta.y = sizeDelta.y.Clamp(LengthsMin.y, LengthsMax.y);
        rectTransform.SetSizeWithCurrentAnchors(sizeDelta);
    }
}

[CustomEditor(typeof(GfxContentSizeFitter))]
public class GfxContentSizeFitterEditor : Editor
{
    // override the editor to be able to show the public variables on the inspector.
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}