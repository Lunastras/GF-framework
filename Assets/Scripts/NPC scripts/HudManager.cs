using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class HudManager : MonoBehaviour
{
    [SerializeField]
    private GameObject levelSliderPrefab;

    [SerializeField]
    private float levelSlidersYOffset = 0;

    //  private Dictionary<GameObject, int> weaponFrequency;

    [SerializeField]
    private RectTransform levelSlidersParent;

    private WeaponLevelSlider[] weaponSliders = null;

    // Start is called before the first frame update
    void Awake()
    {
        //levelSlidersParent = new GameObject("Level Sliders").transform;
    }

    /** Set the max number of sliders and stup the sliders
    *   should only be called once at the start of the game
    */
    public void SetMaxNumSliders(int num)
    {
        weaponSliders = new WeaponLevelSlider[num];
        GfPooling.DestroyChildren(levelSlidersParent);

        if (num > 0)
        {
            List<GameObject> poolParent = GfPooling.GetPoolList(levelSliderPrefab);

            for (int i = 0; i < num; ++i)
            {
                //Debug.Log("AAAA CREATING SHID FOR CHILD " + i);
                weaponSliders[i] = GfPooling.PoolInstantiate(levelSliderPrefab).GetComponent<WeaponLevelSlider>();
                RectTransform weaponTransform = weaponSliders[i].GetComponent<RectTransform>();
                weaponTransform.SetParent(levelSlidersParent);

                Vector3 position = new Vector3(0, -i * (weaponTransform.rect.height + levelSlidersYOffset), 0);
                weaponTransform.localPosition = position;
            }
            // Debug.Log("Num of children in sliders is: " + levelSlidersParent.childCount);
        }
    }

    public void UpdateWeaponSliders(List<WeaponBasic> weapons)
    {
        int count =  Mathf.Min(1, weapons.Count);

        //Debug.Log("called to update the level bars");
        if (weapons != null)
        {
            for (int i = 0; i < count; ++i)
                weaponSliders[i].gameObject.SetActive(i < count);
        }
        else
        {
            GfPooling.DestroyChildren(levelSlidersParent);
        }

        UpdateLevelWeaponSliders(weapons);
    }

    public void UpdateLevelWeaponSliders(List<WeaponBasic> weapons)
    {
        int count =  Mathf.Min(1, weapons.Count);
        //Debug.Log("called to update the levels");
        for (int i = 0; i < count; ++i)
        {
            // Debug.Log("UPDATING FOR INDEX " + i + " the currentxpis " + weapons[i].currentExp);
            weaponSliders[i].SetLevelText(weapons[i].currentLevel);
            weaponSliders[i].SetProgress(weapons[i].nextLevelProgress);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
