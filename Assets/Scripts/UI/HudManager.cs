using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class HudManager : MonoBehaviour
{
    public static HudManager Instance { get; private set; }

    [SerializeField]
    private GameObject m_deathScreen = null;

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

    private List<WeaponLevelSlider> m_weaponSliders = null;

    // Start is called before the first frame update
    void Awake()
    {
        m_weaponSliders = new(1);
        if (Instance) Destroy(Instance);
        Instance = this;
        //levelSlidersParent = new GameObject("Level Sliders").transform;
    }

    /** Set the max number of sliders and stup the sliders
    *   should only be called once at the start of the game
    */
    public void UpdateWeaponLevelSlidersNumber(List<WeaponBasic> weapons)
    {
        int desiredWeaponCount = 1;
        int currentWeaponCount = weapons.Count;
        if (!m_onlyShowFirstWeapon || 0 == currentWeaponCount) desiredWeaponCount = currentWeaponCount;

        int numSlidersNeeded = desiredWeaponCount - m_weaponSliders.Count;
        int index;
        while (0 < numSlidersNeeded) //add sliders
        {
            index = m_weaponSliders.Count;
            m_weaponSliders.Add(GfPooling.PoolInstantiate(m_levelSliderPrefab).GetComponent<WeaponLevelSlider>());
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

    public void UpdateWeaponLevelSlidersValues(List<WeaponBasic> weapons)
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
        Instance.m_deathScreen.SetActive(active);
    }

    public static void TriggerSoftCheckpointVisuals()
    {
        Debug.Log("Triggered soft checkpoint");
    }

    public static void ResetSoftCheckpointVisuals()
    {
        Debug.Log("Reset soft checkpoint");
    }

    public static void TriggerHardCheckpointVisuals()
    {
        Debug.Log("Triggered HARD checkpoint");
    }

    public static void ResetHardCheckpointVisuals()
    {
        Debug.Log("Reset HARD checkpoint");
    }
}
