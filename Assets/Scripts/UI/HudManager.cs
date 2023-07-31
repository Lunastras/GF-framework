using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class HudManager : MonoBehaviour
{
    public static HudManager Instance { get; private set; }

    [SerializeField]
    private CanvasGroup m_deathScreenGroup = null;

    [SerializeField]
    private float m_deathScreenFadeInTime = 2;

    [SerializeField]
    private Image m_auxFadeToBlackImage = null;

    [SerializeField]
    private float m_auxFadeToBlackImageFadeInTime = 0.25f;

    [SerializeField]
    private float m_auxFadeToBlackImageFadeOutTime = 0.1f;

    [SerializeField]
    private bool m_onlyShowFirstWeapon = false;

    [SerializeField]
    private GameObject m_levelSliderPrefab = null;


    [SerializeField]
    private float m_levelSlidersYOffset = 10;

    //  private Dictionary<GameObject, int> weaponFrequency;

    [SerializeField]
    private RectTransform m_levelSlidersParent = null;

    [SerializeField]
    private HealthUIBehaviour m_healthUI = null;

    [SerializeField]
    private GameObject m_gameUiElements = null;

    private List<ExperienceSliderWeapon> m_weaponSliders = null;

    // Start is called before the first frame update
    void Awake()
    {
        m_weaponSliders = new(1);
        if (Instance) Destroy(Instance);
        Instance = this;
        //levelSlidersParent = new GameObject("Level Sliders").transform;
    }

    void Start()
    {
        Color col = m_auxFadeToBlackImage.color;
        col.a = 1;
        m_auxFadeToBlackImage.color = col;
        m_auxFadeToBlackImage.CrossFadeAlpha(0f, 0f, true);
    }

    /** Set the max number of sliders and stup the sliders
    *   should only be called once at the start of the game
    */
    public void UpdateWeaponLevelSlidersNumber(List<WeaponGeneric> weapons)
    {
        int desiredWeaponCount = 1;
        int currentWeaponCount = weapons.Count;
        if (!m_onlyShowFirstWeapon || 0 == currentWeaponCount) desiredWeaponCount = currentWeaponCount;

        int numSlidersNeeded = desiredWeaponCount - m_weaponSliders.Count;
        int index;
        while (0 < numSlidersNeeded) //add sliders
        {
            index = m_weaponSliders.Count;
            m_weaponSliders.Add(GfPooling.PoolInstantiate(m_levelSliderPrefab).GetComponent<ExperienceSliderWeapon>());
            RectTransform weaponTransform = m_weaponSliders[index].GetComponent<RectTransform>();
            weaponTransform.SetParent(m_levelSlidersParent);

            Vector3 position = new Vector3(0, -index * (weaponTransform.rect.height + m_levelSlidersYOffset), 0);
            weaponTransform.localPosition = position;

            --numSlidersNeeded;
        }

        while (0 > numSlidersNeeded) //remove sliders
        {
            index = m_weaponSliders.Count - 1;
            GfPooling.DestroyInsert(m_weaponSliders[index].gameObject);
            m_weaponSliders.RemoveAt(index);
            ++numSlidersNeeded;
        }

        UpdateWeaponLevelSlidersValues(weapons);
    }

    public HealthUIBehaviour GetHealthUI()
    {
        return m_healthUI;
    }

    public void UpdateWeaponLevelSlidersValues(List<WeaponGeneric> weapons)
    {
        int effectiveWeaponCount = 1;
        int realWeaponCount = m_weaponSliders.Count;
        if (!m_onlyShowFirstWeapon || 0 == realWeaponCount) effectiveWeaponCount = realWeaponCount;
        // Debug.Log("I am here updating values");
        for (int i = 0; i < realWeaponCount; ++i)
        {
            // Debug.Log("UPDATEEEEE " + i);

            var weaponSlider = m_weaponSliders[i];
            weaponSlider.SetWeaponCount(weapons.Count);

            WeaponLevels weapon = weapons[i] as WeaponLevels;
            if (weapon)
            {
                //Debug.Log("yeee we are level" + i);
                weaponSlider.SetLevel(weapon.CurrentLevel);
                weaponSlider.SetProgress(weapon.NextLevelProgress);
            }
            else
            {
                //Debug.Log("whacky " + i);

                weaponSlider.SetLevel(0);
                weaponSlider.SetProgress(0);
            }
        }
    }
    public static void ToggleDeathScreen(bool active)
    {
        if (active) //enable deathscreen
            GfUITools.CrossFadeAlphaGroup(Instance.m_deathScreenGroup, 1, Instance.m_deathScreenFadeInTime);
        else //disable deathscreen
        {
            //GfUITools.CrossFadeAlphaGroup(Instance.m_deathScreenGroup, 0, Instance.m_deathScreenFadeOutTime);

            Instance.m_auxFadeToBlackImage.CrossFadeAlpha(1, 0, true);
            Instance.m_deathScreenGroup.alpha = 0;
            Instance.m_auxFadeToBlackImage.CrossFadeAlpha(0, 0.4f, false);
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
        m_auxFadeToBlackImage.CrossFadeAlpha(1, fadeInTime, false);
        yield return Timing.WaitForSeconds(fadeInTime + waitTime);
        m_auxFadeToBlackImage.CrossFadeAlpha(0, fadeOutTime, false);
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
