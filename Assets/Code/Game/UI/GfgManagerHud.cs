using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class GfgHudManager : MonoBehaviour
{
    public static GfgHudManager Instance { get; private set; }

    [SerializeField]
    private CanvasGroup m_deathScreenGroup = null;

    [SerializeField]
    private GfgLevelEndScreen m_levelEndScreen = null;

    [SerializeField]
    private float m_deathScreenFadeInTime = 2;

    [SerializeField]
    private float m_deathScreenFadeOutTime = 0.5f;

    [SerializeField]
    private float m_auxFadeToBlackImageFadeInTime = 0.25f;

    [SerializeField]
    private float m_auxFadeToBlackImageFadeOutTime = 0.1f;

    [SerializeField]
    private GameObject m_gameUiElements = null;

    protected List<WeaponGeneric> m_weapons = null;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this) Destroy(Instance);
        Instance = this;

        GfgManagerLevel.OnLevelStart += OnLevelStart;
        GfgManagerLevel.OnLevelEnd += OnLevelEnd;
    }

    protected void OnLevelStart()
    {

    }

    public static List<WeaponGeneric> GetWeapons() { return Instance.m_weapons; }

    protected void OnLevelEnd()
    {
        ToggleGameUiElements(false);
        m_levelEndScreen.EnableEndScreen();
    }

    void OnDestroy()
    {
        GfgManagerLevel.OnLevelStart -= OnLevelStart;
        GfgManagerLevel.OnLevelEnd -= OnLevelEnd;
    }

    public static void ToggleDeathScreen(bool active)
    {
        if (active)
        {
            //enable deathscreen
            Instance.m_deathScreenGroup.CrossFadeAlphaGf(1, Instance.m_deathScreenFadeInTime, true);
        }
        else //disable deathscreen
        {
            Instance.m_deathScreenGroup.CrossFadeAlphaGf(0, Instance.m_deathScreenFadeOutTime);

            GfxUiTools.SetOverlayAlpha(1);
            Instance.m_deathScreenGroup.alpha = 0;
            GfxUiTools.FadeOverlayAlpha(0, 0.4f, false);
        }

        Instance.m_gameUiElements.SetActive(!active);
    }

    public static void ToggleGameUiElements(bool active)
    {
        Instance.m_gameUiElements.SetActive(active);
    }


    public static void TriggerSoftCheckpointVisuals()
    {

    }

    public static void FadeGameBlackScreen(float fadeInTime, float fadeOutTime, float waitTime = 0)
    {
        Timing.RunCoroutine(Instance._FadeBlackScreenCoroutine(fadeInTime, fadeOutTime, waitTime));
    }

    private IEnumerator<float> _FadeBlackScreenCoroutine(float fadeInTime, float fadeOutTime, float waitTime)
    {
        GfxUiTools.SetOverlayAlpha(1);
        yield return Timing.WaitForSeconds(fadeInTime + waitTime);
        GfxUiTools.FadeOverlayAlpha(0, fadeOutTime, false);
    }

    //returns the trigger delay
    public static float ResetSoftCheckpointVisuals()
    {
        FadeGameBlackScreen(Instance.m_auxFadeToBlackImageFadeInTime, Instance.m_auxFadeToBlackImageFadeOutTime, 0.05f);
        return Instance.m_auxFadeToBlackImageFadeInTime;
    }

    public static void TriggerHardCheckpointVisuals()
    {
    }

    public static void ResetHardCheckpointVisuals()
    {
    }


}