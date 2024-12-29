using UnityEngine;

public class GfgBaseLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GfcBase.InitializeGfBase();
        Destroy(this);
    }
}
