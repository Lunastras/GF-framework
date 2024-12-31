using UnityEngine;

public class GfcBaseLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GfcBase.InitializeGfBase();
        Destroy(this);
    }
}
