using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class LoadoutManager : MonoBehaviour
{
    [SerializeField]
    private Transform odamasParent;

    [SerializeField]
    private WeaponFiring weaponFiring;

    [SerializeField]
    public HudManager hudManager = null;

    private OdamaInventoryValue[] weaponsInInventory;

    private int odamaCapacity = 2;

    private WeaponLoadOut[] loadOuts;

    private int currentLoadOutIndex = 0;

    //if -1, all loadouts are empty
    private int indexOfLastLoadout = -1;

    public static readonly int MAX_ODAMA = 4;
    public static readonly int MAX_LOADOUTS = 6;

    private Transform inactiveOdamasParent;

    private Transform activeOdamasParent;

    private WeaponBasic[] weapons = null;

    private int activeLevelWeapons;
    // Start is called before the first frame update
    void Start()
    {
        hudManager.SetMaxNumSliders(MAX_ODAMA);

        weapons = new WeaponBasic[MAX_ODAMA];
        for (int i = 0; i < MAX_ODAMA; ++i)
        {
            weapons[i] = null;
        }

        if (odamasParent == null)
        {
            odamasParent = transform;
        }

        if (weaponFiring == null)
        {
            weaponFiring = GetComponent<WeaponFiring>();
        }

        weaponsInInventory = new OdamaInventoryValue[WeaponMaster.NumWeapons()];

        for (int i = 0; i < weaponsInInventory.Length; ++i)
        {
            weaponsInInventory[i] = new OdamaInventoryValue(MAX_ODAMA);
        }

        loadOuts = new WeaponLoadOut[MAX_LOADOUTS];

        for (int i = 0; i < MAX_LOADOUTS; ++i)
        {
            loadOuts[i] = new WeaponLoadOut(MAX_ODAMA);
        }

        activeOdamasParent = new GameObject("Active Odamas").transform;
        inactiveOdamasParent = new GameObject("Inactive Odamas").transform;

        activeOdamasParent.parent = odamasParent;
        inactiveOdamasParent.parent = odamasParent;

        if (WeaponMaster.NumWeapons() > 0 && weaponsInInventory[0].odamasInInventory == 0)
            AddOdamaToInventory(0);

        if (-1 == indexOfLastLoadout)
        {
            currentLoadOutIndex = 0;

            SetCurrentLoadout(currentLoadOutIndex);
            ChangeLoadOutWeapon(0, 0, 0);
        }

        SetCurrentLoadout(currentLoadOutIndex);

        Test();
    }

    private void Test()
    {
        IncreaseOdamaCapacity(3);

        AddOdamaToInventory(1);
        AddOdamaToInventory(0);
        AddOdamaToInventory(0);
        AddOdamaToInventory(0);

        ChangeLoadOutWeapon(1, 1, 1);
        ChangeLoadOutWeapon(0, 0, 2);
        ChangeLoadOutWeapon(0, 0, 3);

        //SetCurrentLoadout(1);
        Debug.Log(" ");  

        // ChangeLoadOutWeapon(null, 0, 2);
    }



    private void InternalChangeLoadOutWeapon(int newWeapon, int loadOutIndex, int weaponIndex)
    {
        Debug.Log("I got here! weapon chaneg loadout");
        //make sure that it doesn't let all the loadouts be empty
        if (-1 == newWeapon && 0 == indexOfLastLoadout && (MAX_ODAMA - 1) == loadOuts[loadOutIndex].NumNullWeapons())
            return;

        int currentWeapon = loadOuts[loadOutIndex].weapons[weaponIndex];

        bool loadoutWasEmpty = loadOuts[loadOutIndex].NoWeapons();
        bool loadoutIsEmpty = loadoutWasEmpty;

        if (-1 != currentWeapon)
        {
            //countOfWeapon - 1 because the array starts at 0
            //the code cannot run here if GetCountOfWeapon yields 0, it will be at least 1
            //so when we check the array, we will never check the index -1
            //however, if the index -1 is checked, then someone messed up the loadouts and that someone will be fired
            int countOfWeapon = loadOuts[loadOutIndex].GetCountOfWeapon(currentWeapon) - 1;
            weaponsInInventory[currentWeapon].weaponCountFrequency[countOfWeapon]--;
            if (0 == weaponsInInventory[currentWeapon].weaponCountFrequency[countOfWeapon])
            {
                // Debug.Log("I will resize (down) the pool for " + currentWeapon + " with " + countOfWeapon);
                weaponsInInventory[currentWeapon].setCountBiggestIndex = countOfWeapon - 1;
                GfPooling.ResizePool(WeaponMaster.GetWeapon(currentWeapon), countOfWeapon, true);
            }
        }

        if (-1 != newWeapon)
        {
            int countOfWeapon = loadOuts[loadOutIndex].GetCountOfWeapon(newWeapon);
            weaponsInInventory[newWeapon].weaponCountFrequency[countOfWeapon]++;

            if (1 == weaponsInInventory[newWeapon].weaponCountFrequency[countOfWeapon])
            {
                //Debug.Log("I will resize (up) the pool for " + newWeapon + " with " + (countOfWeapon + 1));
                weaponsInInventory[newWeapon].setCountBiggestIndex = countOfWeapon;
                GfPooling.ResizePool(WeaponMaster.GetWeapon(newWeapon), countOfWeapon + 1, true);
            }
        }

        loadOuts[loadOutIndex].SetWeapon(newWeapon, weaponIndex);

        loadoutIsEmpty = loadOuts[loadOutIndex].NoWeapons();

        if (loadoutIsEmpty != loadoutWasEmpty)
        {
            if (loadoutWasEmpty)
            {
                WeaponLoadOut aux = loadOuts[++indexOfLastLoadout];
                loadOuts[indexOfLastLoadout] = loadOuts[loadOutIndex];
                loadOuts[loadOutIndex] = aux;
            }
            else
            {
                WeaponLoadOut aux = loadOuts[loadOutIndex];
                loadOuts[loadOutIndex] = loadOuts[indexOfLastLoadout];
                loadOuts[--indexOfLastLoadout] = aux;

            }
        }

        if (loadOutIndex == currentLoadOutIndex)
            SetCurrentLoadout(currentLoadOutIndex);
    }

    public void ChangeLoadOutWeapon(int newWeapon, int loadOutIndex, int weaponIndex)
    {
        if (-1 == newWeapon || null != WeaponMaster.GetWeapon(newWeapon))
        {
            bool canAddOdama = true;

            if (-1 == newWeapon)
            {
                OdamaInventoryValue odamaData = weaponsInInventory[newWeapon];
                //check if there are odamas left in the inventory
                canAddOdama = odamaData.odamasInInventory > 0;
            }

            loadOutIndex %= MAX_LOADOUTS;
            weaponIndex %= MAX_ODAMA;

            int currentWeapon = loadOuts[loadOutIndex].weapons[weaponIndex];

            if (canAddOdama && newWeapon != currentWeapon)
            {
                InternalChangeLoadOutWeapon(newWeapon, loadOutIndex, weaponIndex);
            }
        }
    }

    public void NextLoadout()
    {
        SetCurrentLoadout(currentLoadOutIndex + 1);
    }

    public void PreviousLoadout()
    {
        SetCurrentLoadout(currentLoadOutIndex + 1);
    }

    public void InternalSetCurrentLoadout(int index)
    {
        index = index % Math.Max(1, indexOfLastLoadout + 1);

        GfPooling.DestroyChildren(activeOdamasParent);

        currentLoadOutIndex = index;
        WeaponLoadOut loadOut = loadOuts[currentLoadOutIndex];

        float numActiveOdamas = Mathf.Min(loadOut.NumWeapons(), odamaCapacity);
        // Debug.Log("The loadout is:");
        for (int i = 0; i < weapons.Length; ++i)
        {
            GameObject spawnedOdama = GfPooling.Instantiate(WeaponMaster.GetWeapon(loadOut.weapons[i]));
            weapons[i] = null;

            if (spawnedOdama != null)
            {
                // Debug.Log("weapon " + loadOut.weapons[i].name);
                weapons[i] = spawnedOdama.GetComponent<WeaponBasic>();
                OdamaBehaviour ob = weapons[i].GetComponent<OdamaBehaviour>();
                ob.SetAngle(i * (360.0f / numActiveOdamas));
                ob.SetParent(weaponFiring.transform);
                weapons[i].statsCharacter = weaponFiring.GetStatsCharacter();

                weapons[i].gameObject.SetActive(true);
                weapons[i].transform.parent = activeOdamasParent;
            }
        }

        //refresh the levelweapons
        RefreshExpForWeapons();

        weaponFiring.SetWeaponArray(weapons, loadOut.NumWeapons());
    }

    public void SetCurrentLoadout(int index)
    {
        InternalSetCurrentLoadout(index);
    }

    public void IncreaseOdamaCapacity(int count = 1)
    {
        odamaCapacity = Mathf.Clamp(count + odamaCapacity, 1, MAX_ODAMA);
    }

    public void AddOdamaToInventory(int weapon, int count = 1)
    {
        if (weaponsInInventory != null && null != WeaponMaster.GetWeapon(weapon))
        {
            weaponsInInventory[weapon].odamasInInventory = Mathf.Min(count + weaponsInInventory[weapon].odamasInInventory, MAX_ODAMA);
        }
    }

    private void RefreshExpForWeapons()
    {
        WeaponLoadOut loadOut = loadOuts[currentLoadOutIndex];

        int numWeapon = loadOut.NumWeapons();
        //Debug.Log("Switched weapon, the weapon count is " + numWeapon);

        while (--numWeapon >= 0)
        {
            weapons[numWeapon].AddExpPoint(float.MinValue); //set exp to be 0
            loadOut.expWeapons[numWeapon] = weapons[numWeapon].AddExpPoint(loadOut.expWeapons[numWeapon]);
        }

        hudManager.UpdateWeaponSliders(weapons, loadOuts[currentLoadOutIndex].NumWeapons());
    }
    /**
    *   Adds exp points to the weapons equipped in this moment
    *   @param points The ammount of points to be added
    */
    public void AddExpPoints(float points)
    {
        WeaponLoadOut loadOut = loadOuts[currentLoadOutIndex];
        points /= Mathf.Max(loadOut.numOfExpWeapons, 1.0f);

        int numWeapon = loadOut.NumWeapons();
        //Debug.Log("Added exp to weapons, weapon count is " + numWeapon);

        while (--numWeapon >= 0)
        {
            loadOut.expWeapons[numWeapon] = weapons[numWeapon].AddExpPoint(points);
        }

        hudManager.UpdateLevelWeaponSliders(weapons, loadOuts[currentLoadOutIndex].NumWeapons());
    }

    /** Adds a fixed percentage of progress relative to the 
   * exp required for the current and next level to all of the equipped weapons
   * @param percent The percentage of progress to add
   */
    public void AddExpPercent(float percent)
    {
        WeaponLoadOut loadOut = loadOuts[currentLoadOutIndex];
        int numWeapon = loadOut.NumWeapons();

        while (--numWeapon >= 0)
        {
            loadOut.expWeapons[numWeapon] = weapons[numWeapon].AddExpPercent(percent);
        }

        hudManager.UpdateLevelWeaponSliders(weapons, loadOuts[currentLoadOutIndex].NumWeapons());
    }

    public void UpdateWeaponHud()
    {
    }
}

public class WeaponLoadOut
{
    public int[] weapons { get; private set; }

    //the numbers of weapons that use exp in the loadout
    public int numOfExpWeapons { get; private set; } = 0;

    private int indexOfLastWeapon = -1;

    public float[] expWeapons;

    public WeaponLoadOut(int maxWeapons)
    {
        expWeapons = new float[maxWeapons];
        weapons = new int[maxWeapons];

        for (int i = 0; i < maxWeapons; ++i)
        {
            weapons[i] = -1;
            expWeapons[i] = 0;
        }
    }

    public int GetCountOfWeapon(int weaponToCheck)
    {
        int count = 0;
        int i = indexOfLastWeapon + 1;

        while (--i > 0)
            count += Convert.ToInt32(weapons[i] == weaponToCheck);

        return count;
    }

    public bool ContainsWeapon(int weaponToCheck)
    {
        int i = indexOfLastWeapon;
        while (weaponToCheck != weapons[i] && (--i > -1)) ;
        return weaponToCheck != weapons[Mathf.Max(i, 0)];
    }

    public void SetWeapon(int weapon, int index)
    {
        //index %= weapons.Length;

        if (weapon != -1)
        {
            WeanponType weanponType = WeaponMaster.GetWeapon(weapon).GetComponent<WeaponBasic>().GetWeaponType();

            if (weapons[index] != -1)
            {
                WeanponType currentType = WeaponMaster.GetWeapon(weapons[index]).GetComponent<WeaponBasic>().GetWeaponType();
                weapons[index] = weapon;

                if (weanponType == WeanponType.EXPERIENCE && currentType != WeanponType.EXPERIENCE)
                    ++numOfExpWeapons;
                else if (weanponType != WeanponType.EXPERIENCE && currentType == WeanponType.EXPERIENCE)
                    --numOfExpWeapons;
            }
            else
            {
                weapons[++indexOfLastWeapon] = weapon;

                if (weanponType == WeanponType.EXPERIENCE)
                    ++numOfExpWeapons;
            }
        }
        else //if weapon is null
        {
            if (weapons[index] != -1)
            {
                WeanponType currentType = WeaponMaster.GetWeapon(weapons[index]).GetComponent<WeaponBasic>().GetWeaponType();

                if (currentType == WeanponType.EXPERIENCE)
                    --numOfExpWeapons;

                expWeapons[index] = expWeapons[indexOfLastWeapon];
                expWeapons[indexOfLastWeapon] = 0;

                weapons[index] = weapons[indexOfLastWeapon];
                weapons[indexOfLastWeapon] = -1;

                --indexOfLastWeapon;
            }
        }
    }

    public bool NoWeapons()
    {
        return indexOfLastWeapon == -1;
    }

    public int NumWeapons()
    {
        return (indexOfLastWeapon + 1);
    }

    public int NumNullWeapons()
    {
        return (weapons.Length - indexOfLastWeapon + 1);
    }
}

internal class OdamaInventoryValue
{
    //number of odamas available;
    public int odamasInInventory = 0;

    //how many sets exist with the respective count of the given GameObject
    //e.g. there are 2 sets with 3 weapons : [ 2, 2, 2, 0 ]
    // +1 set with 2 weapons : [ 3, 3, 2, 0 ]
    // +1 set with 4 weapons: [ 4, 4, 3, 1 ]
    public int[] weaponCountFrequency;

    public int setCountBiggestIndex = -1;

    public OdamaInventoryValue(int maxInstances)
    {
        weaponCountFrequency = new int[maxInstances];

        for (int i = 0; i < maxInstances; i++)
        {
            weaponCountFrequency[i] = 0;
        }
    }
}
