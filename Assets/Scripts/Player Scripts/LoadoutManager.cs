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

    private int[] weaponsInInventory;

    private int odamaCapacity = 2;

    private WeaponLoadOut[] loadOuts;

    private int currentLoadOutIndex = 0;


    //if -1, all loadouts are empty
    private int indexOfLastLoadout = -1;

    public static readonly int MAX_ODAMA = 4;
    public static readonly int MAX_LOADOUTS = 6;

    private WeaponBasic[] weapons = null;

    private int numWeapons = 0;

    private Transform inactiveOdamasParent;

    private Dictionary<String, Transform> inactiveOdamas;

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

        inactiveOdamasParent = new GameObject("Inactive Odamas").transform;
        inactiveOdamasParent.parent = odamasParent;

        if (weaponFiring == null)
        {
            weaponFiring = GetComponent<WeaponFiring>();
        }

        weaponsInInventory = new int[WeaponMaster.NumWeapons()];

        inactiveOdamas = new(10);

        loadOuts = new WeaponLoadOut[MAX_LOADOUTS];

        for (int i = 0; i < MAX_LOADOUTS; ++i)
        {
            loadOuts[i] = new WeaponLoadOut(MAX_ODAMA);
        }

        if (WeaponMaster.NumWeapons() > 0 && weaponsInInventory[0] == 0)
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
        AddOdamaToInventory(1);
        AddOdamaToInventory(1);
        AddOdamaToInventory(0);
        AddOdamaToInventory(0);
        AddOdamaToInventory(0);

        ChangeLoadOutWeapon(1, 1, 1);
        ChangeLoadOutWeapon(1, 1, 2);
        ChangeLoadOutWeapon(1, 1, 3);
        ChangeLoadOutWeapon(0, 1, 3);
        ChangeLoadOutWeapon(0, 1, 1);

        ChangeLoadOutWeapon(0, 0, 2);
        ChangeLoadOutWeapon(0, 0, 3);

        ChangeLoadOutWeapon(1, 2, 3);


        //SetCurrentLoadout(1);
      //  Debug.Log(" ");  

        // ChangeLoadOutWeapon(null, 0, 2);
    }



    private void InternalChangeLoadOutWeapon(int newWeapon, int loadOutIndex, int weaponIndex)
    {
        //Debug.Log("I got here! weapon chaneg loadout");

        bool loadoutWasEmpty = loadOuts[loadOutIndex].NoWeapons();
        bool loadoutIsEmpty;

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
        bool canAddOdama = !(-1 == newWeapon && weaponsInInventory[newWeapon] > 0);

        loadOutIndex %= MAX_LOADOUTS;
        weaponIndex %= MAX_ODAMA;

        int currentWeapon = loadOuts[loadOutIndex].weapons[weaponIndex];

        if (canAddOdama && newWeapon != currentWeapon)
        {
            InternalChangeLoadOutWeapon(newWeapon, loadOutIndex, weaponIndex);
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

    private GameObject GetOdama(GameObject reference)
    {
        GameObject objectToReturn = null;
        if(inactiveOdamas.TryGetValue(reference.name, out Transform parent) && parent.childCount > 0) {
            objectToReturn = parent.GetChild(0).gameObject;
        }

        if(null == objectToReturn)
        {
            objectToReturn = GfPooling.PoolInstantiate(reference);
        }

        return objectToReturn;
    }

    private void DestroyOdama(WeaponBasic weaponToDestroy)
    {
        if(weaponToDestroy.IsAlive(true))
        {
            weaponToDestroy.destroyWhenDone = true;
            inactiveOdamas.TryGetValue(weaponToDestroy.name, out Transform parent);
            if(null == parent)
            {
                parent = new GameObject(weaponToDestroy + " Pool").transform;
                parent.parent = inactiveOdamasParent;
                inactiveOdamas.Add(weaponToDestroy.name, parent);               
            }

            weaponToDestroy.transform.parent = parent;
            weaponToDestroy.transform.position = new Vector3(999999, 999999, 999999);
        } 
        else
        {
            GfPooling.DestroyInsert(weaponToDestroy.gameObject);
        }
    }

    public void InternalSetCurrentLoadout(int index)
    {
        currentLoadOutIndex = index % Math.Max(1, indexOfLastLoadout + 1);

        int i = 0;
        for (; i < numWeapons; ++i) {
            weapons[i].StopFiring();
            OdamaBehaviour ob = weapons[i].GetComponent<OdamaBehaviour>();

            if(ob) 
                ob.enabled = false;
            
            DestroyOdama(weapons[i]);
        }

        numWeapons = loadOuts[currentLoadOutIndex].NumWeapons();
        float angleBetweenOdamas = 360.0f / numWeapons;

        for (i = 0; i < numWeapons; ++i)
        {               
            GameObject desiredWeapon = WeaponMaster.GetWeapon(loadOuts[currentLoadOutIndex].weapons[i]);
            weapons[i] = GetOdama(desiredWeapon).GetComponent<WeaponBasic>();
            weapons[i].destroyWhenDone = false;

            OdamaBehaviour ob = weapons[i].GetComponent<OdamaBehaviour>();
            ob.SetAngle(i * angleBetweenOdamas);
            ob.SetParent(weaponFiring.transform);
            ob.enabled = true;
                      
            weapons[i].SetStatsCharacter(weaponFiring.GetStatsCharacter());  
            weapons[i].transform.parent = odamasParent;
            weapons[i].transform.position = weaponFiring.transform.position;
        }

        //refresh the levelweapons
        RefreshExpForWeapons();
        weaponFiring.SetWeaponArray(weapons, numWeapons);
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
            weaponsInInventory[weapon] = Mathf.Min(count + weaponsInInventory[weapon], MAX_ODAMA);
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
