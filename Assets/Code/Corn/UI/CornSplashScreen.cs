using MEC;
using System.Collections.Generic;
using UnityEngine;

public class CornSplashScreen : MonoBehaviour
{
    void Awake()
    {
        GfcBase.InitializeGfBase();
    }

    void Start()
    {
        Timing.RunCoroutine(_SplashRoutine().CancelWith(gameObject));
    }

    private IEnumerator<float> _SplashRoutine()
    {
        yield return Timing.WaitForSeconds(1);
        yield return Timing.WaitUntilDone(GfgManagerSceneLoader.LoadScene(GfcSceneId.MAIN_MENU));
    }
}