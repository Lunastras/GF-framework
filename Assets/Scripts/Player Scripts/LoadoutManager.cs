using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using Unity.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class LoadoutManager : NetworkBehaviour
{
    [SerializeField]
    protected GfMovementGeneric m_parentMovement;

    [SerializeField]
    protected StatsCharacter m_statsCharacter;

    [SerializeField]
    protected FiringWeapons m_weaponFiring;

    [SerializeField]
    protected HudManager m_hudManager = null;

    protected int m_weaponCapacity = 2;

    protected List<List<WeaponLoadoutData>> m_loadouts = new(6);

    protected int m_currentLoadOutIndex = 0; // = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    protected PriorityValue<float> m_weaponCapacityMultiplier = new(1);
    protected float[] m_weaponMultipliers = Enumerable.Repeat(1f, (int)WeaponMultiplierTypes.COUNT_TYPES).ToArray();


    protected List<WeaponGeneric> m_weapons = new(4);

    protected const int MAX_WEAPONS = 128;
    protected const int MAX_LOADOUTS = 16;

    protected static readonly Vector3 DESTROY_POSITION = new Vector3(99999999, 99999999, 99999999);

    protected bool HasAuthority
    {
        get
        {
            bool ret = false;
            if (NetworkManager.Singleton) ret = NetworkManager.Singleton.IsServer;
            return ret;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (null == m_weaponFiring)
        {
            m_weaponFiring = GetComponent<FiringWeapons>();
        }

        if (null == m_parentMovement)
        {
            m_parentMovement = GetComponent<GfMovementGeneric>();
        }

        if (null == m_statsCharacter)
        {
            m_statsCharacter = GetComponent<StatsCharacter>();
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < m_weapons.Count; ++i)
        {
            DestroyWeapon(m_weapons[i]);
        }
    }


    private void OnEnable()
    {
        SetCurrentLoadout(m_currentLoadOutIndex);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            m_hudManager = GfManagerLevel.GetHudManager();
        }

        SetCurrentLoadout(m_currentLoadOutIndex);

        /*
        if (!IsOwner && IsClient && !HasAuthority)
        {
            RequestLoadoutDataServerRpc();
        }*/
    }

    /*

    [ServerRpc(RequireOwnership = false)]
    public void RequestLoadoutDataServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;

        int effectiveCapacity = (int)System.Math.Round(m_weaponCapacity * m_weaponCapacityMultiplier);
        WeaponLoadoutData[] weaponsData = new WeaponLoadoutData[m_loadouts.Count * effectiveCapacity];
        for (int i = 0; i < m_loadouts.Count; ++i)
        {
            List<WeaponLoadoutData> loadout = m_loadouts[i];
            for (int j = 0; j < effectiveCapacity; ++j)
            {
                bool hasWeapon = null != loadout && j < loadout.Count;
                int index = i * effectiveCapacity + j;
                if (hasWeapon)
                {
                    weaponsData[index] = loadout[j];
                }
                else
                {
                    weaponsData[index] = new(-1);
                }
            }
        }

        ClientRpcParams clientParams = default;
        NativeArray<ulong> clientIdsNativeArray = new(1, Allocator.Temp);
        clientIdsNativeArray[0] = clientId;
        clientParams.Send.TargetClientIdsNativeArray = clientIdsNativeArray;
        ReceiveLoadoutDataClientRpc(weaponsData, m_loadouts.Count, effectiveCapacity, m_currentLoadOutIndex, clientParams);
        clientIdsNativeArray.Dispose();
    }

    [ClientRpc]
    public void ReceiveLoadoutDataClientRpc(WeaponLoadoutData[] weaponsData, int rows, int columns, int currentLoadoutIndex, ClientRpcParams clientParams)
    {
        Debug.Log("I received some data huehuehuehuehuehuhehuehuehueheduehuehueh");
        if (null == m_loadouts) m_loadouts = new(rows);
        while (m_loadouts.Count > rows) m_loadouts.RemoveAt(m_loadouts.Count);
        while (m_loadouts.Count < rows) m_loadouts.Add(new(columns));

        for (int i = 0; i < rows; ++i)
        {
            if (m_loadouts[i] == null) m_loadouts[i] = new(columns);
            List<WeaponLoadoutData> loadout = m_loadouts[i];
            loadout.Clear();

            int j = 0;
            WeaponLoadoutData weaponData = weaponsData[i * columns];
            while (-1 != weaponData.Weapon) //while it has a weapon
            {
                j++;
                loadout.Add(weaponData);
                weaponData = weaponsData[i * columns + j];
            }
        }

        InternalSetCurrentLoadout(currentLoadoutIndex);
    }*/

    /*
    [ClientRpc]
    public void SetLoadoutAllWeaponsClientRpc(int aIndexLoadout, WeaponData aNewWeapon, bool aFillToCapacity, bool aSwitchToLoadout)
    {
        InternalSetLoadoutAllWeapons(aIndexLoadout, aNewWeapon, false, aSwitchToLoadout);
    }

    [ServerRpc]
    public void SetLoadoutAllWeaponsServerRpc(int aIndexLoadout, WeaponData aNewWeapon, bool aFillToCapacity, bool aSwitchToLoadout)
    {
        SetLoadoutAllWeaponsClientRpc(aIndexLoadout, aNewWeapon, aFillToCapacity, aSwitchToLoadout);
    }

    */

    public void SetLoadoutAllWeapons(int aIndexLoadout, WeaponData aNewWeapon, bool aFillToCapacity, bool aSwitchToLoadout)
    {
        /*
        if (HasAuthority)
        {
            SetLoadoutAllWeaponsClientRpc(aIndexLoadout, aNewWeapon, aFillToCapacity, aSwitchToLoadout);
        }
        else if (IsOwner)
        {
            SetLoadoutAllWeaponsServerRpc(aIndexLoadout, aNewWeapon, aFillToCapacity, aSwitchToLoadout);
        }*/

        InternalSetLoadoutAllWeapons(aIndexLoadout, aNewWeapon, false, aSwitchToLoadout);
    }

    private bool InternalSetLoadoutAllWeapons(int aIndexLoadout, WeaponData aNewWeapon, bool aFillToCapacity, bool aSwitchToLoadout)
    {
        bool ret = false;
        if (aIndexLoadout < m_loadouts.Count)
        {
            int numWeapons = aFillToCapacity ? m_weaponCapacity : m_loadouts[aIndexLoadout].Count;

            while (--numWeapons >= 0)
            {
                ret |= InternalSetLoadoutWeapon(aIndexLoadout, numWeapons, aNewWeapon, aSwitchToLoadout);
            }
        }

        if (ret && aIndexLoadout == m_currentLoadOutIndex)
            SetCurrentLoadout(m_currentLoadOutIndex);

        return ret;
    }

    /*
    [ClientRpc]
    public void SetLoadoutWeaponClientRpc(int aIndexLoadout, int aIndexWeapon, WeaponData aNewWeapon, bool aSwitchToLoadout)
    {
        InternalSetLoadoutWeapon(aIndexLoadout, aIndexWeapon, aNewWeapon, aSwitchToLoadout);
    }

    [ServerRpc]
    public void SetLoadoutWeaponServerRpc(int aIndexLoadout, int aIndexWeapon, WeaponData aNewWeapon, bool aSwitchToLoadout)
    {
        SetLoadoutWeaponClientRpc(aIndexLoadout, aIndexWeapon, aNewWeapon, aSwitchToLoadout);
    }
    */

    public bool SetLoadoutWeapon(int aIndexLoadout, int aIndexWeapon, WeaponData aNewWeapon, bool aSwitchToLoadout)
    {
        bool changedWeapon = false;
        /*
        if (HasAuthority)
        {
            SetLoadoutWeaponClientRpc(aIndexLoadout, aIndexWeapon, aNewWeapon, aSwitchToLoadout);
        }
        else if (IsOwner)
        {
            SetLoadoutWeaponServerRpc(aIndexLoadout, aIndexWeapon, aNewWeapon, aSwitchToLoadout);
        }*/
        changedWeapon = InternalSetLoadoutWeapon(aIndexLoadout, aIndexWeapon, aNewWeapon, aSwitchToLoadout);

        return changedWeapon;
    }

    private unsafe bool InternalSetLoadoutWeapon(int aIndexLoadout, int aIndexWeapon, WeaponData aNewWeapon, bool aSwitchToLoadout)
    {
        bool changedWeapon = false;
        if (aIndexLoadout <= m_loadouts.Count)
        {
            m_loadouts.Add(new(1));
            aIndexLoadout = m_loadouts.Count - 1;
        }

        bool removeWeapon = !GfgManagerWeapons.WeaponExists(aNewWeapon);
        WeaponData currentWeapon = GfgManagerWeapons.NullWeapon;
        if (aIndexWeapon < m_loadouts[aIndexLoadout].Count)
            currentWeapon = m_loadouts[aIndexLoadout][aIndexWeapon].Weapon;

        bool currentWeaponNull = !GfgManagerWeapons.WeaponExists(currentWeapon);
        if (removeWeapon || aNewWeapon != currentWeapon)
        {
            if (removeWeapon)
            {
                if (!currentWeaponNull)
                {
                    m_loadouts[aIndexLoadout].RemoveAt(aIndexWeapon);
                    if (m_loadouts.Count == 0)
                        m_loadouts.RemoveAt(aIndexLoadout);

                    changedWeapon = true;
                }
            }
            else //add weapon to loadout
            {
                if (currentWeaponNull)
                {
                    if (m_loadouts[aIndexLoadout].Count < m_weaponCapacity)
                    {
                        m_loadouts[aIndexLoadout].Add(new(aNewWeapon));
                        changedWeapon = true;
                    }
                }
                else
                {
                    m_loadouts[aIndexLoadout][aIndexWeapon] = new WeaponLoadoutData(m_loadouts[aIndexLoadout][aIndexWeapon], aNewWeapon);
                    changedWeapon = true;
                }
            }

            if (changedWeapon && aSwitchToLoadout)
                m_currentLoadOutIndex = aIndexLoadout;

            if (changedWeapon && aIndexLoadout == m_currentLoadOutIndex)
                InternalSetCurrentLoadout(m_currentLoadOutIndex);
        }

        return changedWeapon;
    }

    private WeaponGeneric GetWeapon(GameObject aDesiredWeapon, int aDesiredSeed)
    {
        WeaponGeneric objectToReturn = null;

        List<GameObject> objList = GfcPooling.GetPoolList(aDesiredWeapon);
        int listCount = null != objList ? listCount = objList.Count : 0;
        int index = listCount;

        while (0 <= --index)
        {
            GameObject obj = objList[index];
            WeaponGeneric wb = obj.GetComponent<WeaponGeneric>();
            if (!obj.activeSelf || (wb.GetStatsCharacter() == m_statsCharacter && wb.GetSeed() == aDesiredSeed)) //check if it is inactive or if they have the same character stats
            {
                obj.SetActive(true);
                objectToReturn = wb;

                objList[index] = objList[--listCount];
                objList.RemoveAt(listCount);

                index = 0;
            }
        }

        if (null == objectToReturn)
        {
            objectToReturn = GfcPooling.PoolInstantiate(aDesiredWeapon).GetComponent<WeaponGeneric>();
        }

        return objectToReturn;
    }

    private void DestroyWeapon(WeaponGeneric aWeaponToDestroy)
    {
        aWeaponToDestroy.StopFiring(false);
        aWeaponToDestroy.SetMovementParent(null);
        aWeaponToDestroy.WasSwitchedOff();

        GameObject obj = aWeaponToDestroy.gameObject;
        aWeaponToDestroy.DisableWhenDone = true;
        bool isAlive = aWeaponToDestroy.IsAlive();
        GfcPooling.DestroyInsert(obj, 0, isAlive);
    }

    protected virtual void OnWeaponSet(WeaponGeneric aWeapon) { }

    protected virtual void OnWeaponsCleared() { }

    #region SET_LOADOUT_REGION

    [ServerRpc]
    protected void SetCurrentLoadoutServerRpc(int aIndex)
    {
        SetCurrentLoadoutClientRpc(aIndex);
    }

    [ClientRpc]
    protected void SetCurrentLoadoutClientRpc(int aIndex)
    {
        InternalSetCurrentLoadout(aIndex);
    }

    public void SetCurrentLoadout(int aIndex)
    {
        if (HasAuthority)
        {
            SetCurrentLoadoutClientRpc(aIndex);
        }
        else if (IsOwner)
        {
            SetCurrentLoadoutServerRpc(aIndex);
        }
    }

    private void InternalSetCurrentLoadout(int aIndexLoadout)
    {
        if (null != m_loadouts && 0 < m_loadouts.Count)
        {
            int i = 0;
            int weaponsCount = m_weapons.Count;
            bool canChangeWeapon = true;

            for (i = 0; i < weaponsCount && canChangeWeapon; ++i)
            {
                canChangeWeapon &= m_weapons[i].CanBeSwitchedOff();
            }

            if (canChangeWeapon)
            {

                GetPointsFromWeapons();
                m_currentLoadOutIndex = GfcTools.Mod(aIndexLoadout, m_loadouts.Count);

                i = m_weapons.Count;
                while (--i >= 0) // destroy all current weapons
                {
                    DestroyWeapon(m_weapons[i]);
                    m_weapons.RemoveAt(i);
                }

                OnWeaponsCleared();
                m_weaponFiring.ClearWeapons();

                weaponsCount = m_loadouts[m_currentLoadOutIndex].Count;
                for (i = 0; i < weaponsCount; ++i)
                {
                    GameObject desiredWeapon = GfgManagerWeapons.GetWeapon(m_loadouts[m_currentLoadOutIndex][i].Weapon);
                    m_weapons.Add(GetWeapon(desiredWeapon, i));

                    WeaponGeneric weapon = m_weapons[i];
                    weapon.Initialize();
                    weapon.StopFiring(false);
                    weapon.SetMovementParent(m_parentMovement);
                    weapon.SetLoadoutCount(weaponsCount);
                    weapon.SetLoadoutWeaponIndex(i);
                    weapon.SetLoadoutIndex(m_currentLoadOutIndex);
                    weapon.SetSeed((uint)i);
                    weapon.SetStatsCharacter(m_weaponFiring.GetStatsCharacter());
                    weapon.WasSwitchedOn();
                    weapon.DisableWhenDone = false;
                    weapon.DestroyWhenDone = false;
                    weapon.transform.position = m_parentMovement.transform.position;

                    for (int j = 0; j < (int)WeaponMultiplierTypes.COUNT_TYPES; ++j)
                    {
                        weapon.SetMultiplier((WeaponMultiplierTypes)j, m_weaponMultipliers[j]);
                    }

                    m_weaponFiring.SetWeapon(weapon, i);
                    OnWeaponSet(weapon);
                }

                //refresh the levelweapons
                RefreshPointsForWeapons();
            }
        }
    }

    #endregion //SET_LOADOUT_REGION

    #region SET_WEAPON_CAPACITY

    [ClientRpc]
    protected void SetWeaponCapacityClientRpc(int aNewCapacity, bool aRepeatWeapons = true, bool aKeepSamePoints = true)
    {
        m_weaponCapacity = aNewCapacity;
        int effectiveCapacity = (int)System.Math.Round(aNewCapacity * m_weaponCapacityMultiplier);
        InternalSetWeaponCapacity(effectiveCapacity, aRepeatWeapons, aKeepSamePoints);
    }

    public void SetWeaponCapacity(int aNewCapacity, bool aRepeatWeapons = true, bool aKeepSamePoints = true)
    {
        if (HasAuthority)
        {
            if (aNewCapacity != m_weaponCapacity)
            {
                m_weaponCapacity = aNewCapacity;
                int effectiveCapacity = (int)System.Math.Round(aNewCapacity * m_weaponCapacityMultiplier);
                InternalSetWeaponCapacity(effectiveCapacity, aRepeatWeapons, aKeepSamePoints);
                SetWeaponCapacityClientRpc(aNewCapacity, aRepeatWeapons, aKeepSamePoints);
            }
        }
    }

    protected void InternalSetWeaponCapacity(int aNewCapacity, bool aRepeatWeapons, bool aKeepSamePoints)
    {
        aNewCapacity = System.Math.Min(MAX_WEAPONS, System.Math.Max(0, aNewCapacity));

        int numLoadouts = m_loadouts.Count;
        bool updateCurrentWeapon = false;
        while (--numLoadouts >= 0)
        {
            var loadout = m_loadouts[numLoadouts];
            int numWeapon = loadout.Count;
            int weaponDiff = numWeapon - aNewCapacity;
            updateCurrentWeapon |= m_currentLoadOutIndex == numLoadouts && numWeapon != aNewCapacity;

            if (weaponDiff > 0) //remove extra weapons
            {
                while (--weaponDiff >= 0)
                {
                    int lastIndex = numWeapon - 1;
                    --numWeapon;
                    loadout.RemoveAt(lastIndex);
                }
            }
            else if (aRepeatWeapons && weaponDiff < 0) //add extra weapons 
            {
                bool hasWeapon = numWeapon > 0;
                while (++weaponDiff <= 0 && hasWeapon)
                {
                    int indexOfCopy = hasWeapon ? loadout.Count % numWeapon : 0; //repeat the initial weapons for the refill
                    loadout.Add(new WeaponLoadoutData(loadout[indexOfCopy], aKeepSamePoints));
                }
            }
        }

        if (updateCurrentWeapon)
        {
            InternalSetCurrentLoadout(m_currentLoadOutIndex);
        }
    }
    #endregion //SET_WEAPON_CAPACITY

    public int GetWeaponCapacity()
    {
        return m_weaponCapacity;
    }

    /* TODO has to work with network behaviour
    public void AddWeaponToInventory(int weapon, int count = 1)
    {
        if (weapon >= 0 && m_weaponsInInventory != null && null != WeaponMaster.GetWeapon(weapon))
        {
            m_weaponsInInventory[weapon] = count + m_weaponsInInventory[weapon];
        }
    }*/

    private void RefreshPointsForWeapons()
    {
        List<WeaponLoadoutData> loadOut = m_loadouts[m_currentLoadOutIndex];
        int countWeapons = loadOut.Count;
        int countTypes = (int)WeaponPointsTypes.NUMBER_OF_TYPES;

        for (int currentWeapon = 0; currentWeapon < countWeapons; ++currentWeapon)
        {
            for (int currentType = 0; currentType < countTypes; ++currentType)
            {
                float currentExp = m_weapons.Count > currentWeapon ? m_weapons[currentWeapon].SetPoints((WeaponPointsTypes)currentType, loadOut[currentWeapon].GetPoints(currentType)) : 0;
                loadOut[currentWeapon].SetPoints(currentType, currentExp);
            }
        }

        if (m_hudManager) m_hudManager.UpdateWeaponLevelSlidersNumber(m_weapons);
    }

    private void GetPointsFromWeapons()
    {
        List<WeaponLoadoutData> loadOut = m_loadouts[m_currentLoadOutIndex];
        int countWeapons = loadOut.Count;
        int countTypes = (int)WeaponPointsTypes.NUMBER_OF_TYPES;

        for (int currentWeapon = 0; currentWeapon < countWeapons; ++currentWeapon)
        {
            for (int currentType = 0; currentType < countTypes; ++currentType)
            {
                float currentExp = m_weapons.Count > currentWeapon ? m_weapons[currentWeapon].GetPoints((WeaponPointsTypes)currentType) : 0;
                loadOut[currentWeapon].SetPoints(currentType, currentExp);
            }
        }

        if (m_hudManager) m_hudManager.UpdateWeaponLevelSlidersNumber(m_weapons);
    }

    public void AddPointsAll(WeaponPointsTypes aTypePoints, float aPoints)
    {
        if (HasAuthority)
            AddPointAllClientRpc(aTypePoints, aPoints);
        else if (IsOwner)
            AddPointAllServerRpc(aTypePoints, aPoints);
    }

    [ServerRpc]
    private void AddPointAllServerRpc(WeaponPointsTypes aTypePoints, float aPoints)
    {
        AddPointAllClientRpc(aTypePoints, aPoints);
    }

    [ClientRpc]
    private void AddPointAllClientRpc(WeaponPointsTypes aTypePoints, float aPoints)
    {
        InternalAddPointsAll(aTypePoints, aPoints);
    }

    protected void InternalAddPointsAll(WeaponPointsTypes aTypePoints, float aPoints)
    {
        int numLoadouts = m_loadouts.Count;
        while (--numLoadouts >= 0)
        {
            InternalAddPoints(aTypePoints, aPoints, numLoadouts);
        }
    }

    public void AddPoints(WeaponPointsTypes aTypePoints, float aPoints, int aLoadoutIndex = -1)
    {
        if (HasAuthority)
            AddPointsClientRpc(aTypePoints, aPoints, aLoadoutIndex);
        else if (IsOwner)
            AddPointsServerRpc(aTypePoints, aPoints, aLoadoutIndex);
    }

    [ServerRpc]
    private void AddPointsServerRpc(WeaponPointsTypes aTypePoints, float aPoints, int aLoadoutIndex)
    {
        AddPointsClientRpc(aTypePoints, aPoints, aLoadoutIndex);
    }

    [ClientRpc]
    private void AddPointsClientRpc(WeaponPointsTypes aTypePoints, float aPoints, int aLoadoutIndex)
    {
        InternalAddPoints(aTypePoints, aPoints, aLoadoutIndex);
    }

    /**
    *   Adds exp points to the weapons equipped in this moment
    *   @param points The ammount of points to be added
    */
    private void InternalAddPoints(WeaponPointsTypes aTypePoints, float aPoints, int aLoadoutIndex = -1)
    {
        if (aLoadoutIndex < 0) aLoadoutIndex = m_currentLoadOutIndex;

        if (null != m_loadouts && 0 < m_loadouts.Count && m_loadouts.Count >= aLoadoutIndex)
        {
            List<WeaponLoadoutData> loadOut = m_loadouts[m_currentLoadOutIndex];
            int numWeapon = loadOut.Count;
            //Debug.Log("Added exp to weapons, weapon count is " + numWeapon);

            while (--numWeapon >= 0)
            {
                float currentExp = m_weapons[numWeapon].AddPoints(aTypePoints, aPoints);
                loadOut[numWeapon].SetPoints((int)aTypePoints, currentExp);
            }

            if (aLoadoutIndex == m_currentLoadOutIndex && m_hudManager) m_hudManager.UpdateWeaponLevelSlidersValues(m_weapons);
        }
    }

    [ClientRpc]
    protected void AddLoadoutClientRpc(int aCount)
    {
        InternalAddLoadout(aCount);
    }

    [ServerRpc]
    protected void AddLoadoutServerRpc(int aCount)
    {
        AddLoadoutClientRpc(aCount);
    }

    public void AddLoadout(int count = 1)
    {
        if (HasAuthority)
        {
            AddLoadoutClientRpc(count);
        }
        else if (IsOwner)
        {
            AddLoadoutServerRpc(count);
        }
    }

    protected void InternalAddLoadout(int aCount = 1)
    {
        while (--aCount >= 0 && m_loadouts.Count < MAX_LOADOUTS)
            m_loadouts.Add(new(m_weaponCapacity)); //todo
    }

    public virtual void Respawned()
    {
        for (int i = 0; i < (int)WeaponMultiplierTypes.COUNT_TYPES; ++i)
            SetMultiplier((WeaponMultiplierTypes)i, 1);
    }

    public void RemoveLoadout(int index)
    {
        m_loadouts.RemoveAt(index);
    }

    public void ClearLoadouts()
    {

        m_loadouts.Clear();
    }

    public List<WeaponLoadoutData> GetCurrentLoadout()
    {
        return m_loadouts[m_currentLoadOutIndex];
    }

    public List<WeaponLoadoutData> GetLoadout(int aIndexLoadout)
    {
        return m_loadouts[aIndexLoadout];
    }

    public List<List<WeaponLoadoutData>> GetLoadoutsList()
    {
        return m_loadouts;
    }

    public int GetCurrentLoadoutIndex()
    {
        return m_currentLoadOutIndex;
    }

    public int GetCountLoadouts()
    {
        return m_loadouts.Count;
    }

    public int GetCountWeapons(int aIndexLoadout)
    {
        return m_loadouts[aIndexLoadout].Count;
    }

    public WeaponData GetWeaponData(int indexLoadout, int indexWeapon)
    {
        return m_loadouts[indexLoadout][indexWeapon].Weapon;
    }

    public void RemoveWeapon(int aIndexLoadout, int aIndexWeapon, bool aSwitchToLoadout)
    {
        SetLoadoutWeapon(aIndexLoadout, aIndexWeapon, GfgManagerWeapons.NullWeapon, aSwitchToLoadout);
    }

    public void NextLoadout(bool aMustHaveWeapon = true)
    {
        int originalIndex = m_currentLoadOutIndex;
        do
        {
            ++m_currentLoadOutIndex;
            m_currentLoadOutIndex %= m_loadouts.Count;
        } while (originalIndex != m_currentLoadOutIndex && aMustHaveWeapon && 0 >= m_loadouts[m_currentLoadOutIndex].Count);

        if (originalIndex != m_currentLoadOutIndex)
            SetCurrentLoadout(m_currentLoadOutIndex);
    }

    public void PreviousLoadout(bool mustHaveWeapon = true)
    {
        int originalIndex = m_currentLoadOutIndex;
        do
        {
            --m_currentLoadOutIndex;
            m_currentLoadOutIndex = GfcTools.Mod(m_currentLoadOutIndex, m_loadouts.Count);
        } while (originalIndex != m_currentLoadOutIndex && mustHaveWeapon && 0 >= m_loadouts[m_currentLoadOutIndex].Count);

        if (originalIndex != m_currentLoadOutIndex)
            SetCurrentLoadout(m_currentLoadOutIndex);
    }

    public List<WeaponGeneric> GetWeapons()
    {
        return m_weapons;
    }


    public virtual float GetMultiplier(WeaponMultiplierTypes type) { return m_weaponMultipliers[(int)type]; }

    public virtual bool SetMultiplier(WeaponMultiplierTypes type, float multiplier)
    {
        bool changedValue = false;
        if (HasAuthority)
        {
            changedValue = InternalSetMultiplier(type, multiplier);
            if (changedValue) SetMultiplierClientRpc(type, multiplier);
        }

        return changedValue;
    }

    [ClientRpc]
    protected virtual void SetMultiplierClientRpc(WeaponMultiplierTypes type, float multiplier)
    {
        InternalSetMultiplier(type, multiplier); //override regardless of priority
    }

    protected virtual bool InternalSetMultiplier(WeaponMultiplierTypes type, float multiplier)
    {
        bool changedValue = multiplier != m_weaponMultipliers[(int)type];
        if (changedValue)
        {
            int weaponCount = m_weapons.Count;
            for (int i = 0; i < weaponCount; ++i)
            {
                m_weapons[i].SetMultiplier(type, multiplier);
            }
        }

        return changedValue;
    }

    #region WEAPON_CAPACITY_MULTIPLIER
    public virtual PriorityValue<float> GetWeaponCapacityMultiplier() { return m_weaponCapacityMultiplier; }

    protected virtual bool InternalSetWeaponCapacityMultiplier(float multiplier, uint priority = 0, bool overridePriority = false, bool repeatWeapons = true, bool keepSameExp = true)
    {
        float oldMultiplier = m_weaponCapacityMultiplier;
        bool changedValue = m_weaponCapacityMultiplier.SetValue(multiplier, priority, overridePriority) && multiplier != oldMultiplier;
        if (changedValue)
        {
            int effectiveCapacity = (int)System.Math.Round(m_weaponCapacity * m_weaponCapacityMultiplier);
            InternalSetWeaponCapacity(effectiveCapacity, repeatWeapons, keepSameExp);
        }
        return changedValue;
    }

    public virtual bool SetWeaponCapacityMultiplier(float multiplier, uint priority = 0, bool overridePriority = false, bool repeatWeapons = true, bool keepSameExp = true)
    {
        bool changedValue = false;
        if (HasAuthority)
        {
            changedValue = InternalSetWeaponCapacityMultiplier(multiplier, priority, overridePriority);
            if (changedValue) SetWeaponCapacityMultiplierClientRpc(multiplier, priority);
        }

        return changedValue;
    }

    [ClientRpc]
    protected virtual void SetWeaponCapacityMultiplierClientRpc(float multiplier, uint priority = 0, bool repeatWeapons = true, bool keepSameExp = true)
    {
        InternalSetWeaponCapacityMultiplier(multiplier, priority, true); //override regardless of priority
    }

    #endregion //WEAPON_CAPACITY_MULTIPLIER

    [System.Serializable]
    public struct WeaponLoadoutData : INetworkSerializable
    {
        public WeaponData Weapon;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)WeaponPointsTypes.NUMBER_OF_TYPES)]
        private float[] WeaponPoints; //we use marshalling to avoid creating garbage when removing a struct

        public WeaponLoadoutData(bool aEmptyWeapon = true)
        {
            WeaponPoints = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];
            Weapon = aEmptyWeapon ? new() { Index = -1, Tier = -1 } : default;
        }

        public WeaponLoadoutData(WeaponData aWeapon)
        {
            WeaponPoints = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];
            Weapon = aWeapon;
        }

        public WeaponLoadoutData(WeaponLoadoutData aData, bool aCopyPoints = true)
        {
            WeaponPoints = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];
            Weapon = aData.Weapon;
            int count = (int)WeaponPointsTypes.NUMBER_OF_TYPES;
            for (int i = 0; i < count && aCopyPoints; ++i)
                WeaponPoints[i] = aData.WeaponPoints[i];
        }

        public WeaponLoadoutData(WeaponLoadoutData aData, WeaponData aNewWeaponData)
        {
            WeaponPoints = new float[(int)WeaponPointsTypes.NUMBER_OF_TYPES];
            Weapon = aNewWeaponData;
            int count = (int)WeaponPointsTypes.NUMBER_OF_TYPES;
            for (int i = 0; i < count; ++i)
                WeaponPoints[i] = aData.WeaponPoints[i];
        }

        public float GetPoints(WeaponPointsTypes aType)
        {
            return GetPoints((int)aType);
        }

        public float GetPoints(int aType)
        {
            return WeaponPoints[aType];
        }

        public void SetPoints(WeaponPointsTypes aType, float aValue)
        {
            SetPoints((int)aType, aValue);
        }

        public void SetPoints(int type, float value)
        {
            WeaponPoints[type] = value;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Length
            int length = 0;
            if (!serializer.IsReader)
            {
                length = WeaponPoints.Length;
            }

            serializer.SerializeValue(ref length);
            serializer.SerializeValue(ref Weapon);

            // Array
            if (serializer.IsReader)
            {
                WeaponPoints = new float[length];
            }

            for (int n = 0; n < length; ++n)
            {
                serializer.SerializeValue(ref WeaponPoints[n]);
            }
        }
    }
}


