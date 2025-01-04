using UnityEngine;

public class GfcRemainActiveWhileHoveringOver : MonoBehaviour
{
    // Update is called once per frame
    void Update() { if (!GfcCursor.IsMouseOverCurrentUI()) gameObject.SetActiveGf(false); }
}