using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System.IO;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using Unity.Mathematics;
using System;
using Unity.Netcode;
using Unity.Collections;
using System.Reflection;
public class GfcManagerSaveData : MonoBehaviour
{
    protected static GfcManagerSaveData Instance;

    static readonly System.DateTime UNIX_TIME_START = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

    private System.DateTime m_dateTimeOfProgramStart;

    private const string SAVE_DATA_PATH = "SaveData/";

    private const string SAVE_DATA_FILENAME = "SAVE_PROFILE_";

    public const int MAX_NUM_SAVE_FILES = 4;

    public const int MAX_EQUIPPED_WEAPONS = 2;

    private double m_timeOfLastSave;

    private int m_usedSaveDataSlot = -1;

    private PlayerSaveData m_playerSaveData = null;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        if (!Directory.Exists(SAVE_DATA_PATH)) Directory.CreateDirectory(SAVE_DATA_PATH);

        m_timeOfLastSave = Time.unscaledTimeAsDouble;
        m_dateTimeOfProgramStart = System.DateTime.UtcNow;
        m_playerSaveData = null;
        m_usedSaveDataSlot = -1;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public static void MakeSureIHaveASaveData()
    {
        if (Instance.m_playerSaveData == null)
        {
            Debug.Log("No save file loaded, automatically loading data...");
            int currentSaveIndex;
            bool foundSave = false;

            for (currentSaveIndex = 0; currentSaveIndex < MAX_NUM_SAVE_FILES && !foundSave; ++currentSaveIndex)
                foundSave = SaveExists(currentSaveIndex);

            if (foundSave)
                LoadAndSetActivePlayerSaveData(--currentSaveIndex);

            if (Instance.m_playerSaveData == null)
            {
                SetActivePlayerSaveData(CreatePlayerSaveData("DEBUG MAESTRO"), 0);
                Debug.LogWarning("No save found, using debug save.");
            }
        }
    }

    public static PlayerSaveData CreatePlayerSaveData(string aName)
    {
        return new(aName, GetCurrentUnixTime());
    }

    public static PlayerRuntimeGameData GetPlayerRuntimeGameData()
    {
        PlayerSaveData currentSaveData = GetActivePlayerSaveData();
        return currentSaveData.GetRuntimeGameData();
    }

    public static PlayerSaveData GetActivePlayerSaveData()
    {
        MakeSureIHaveASaveData();
        return Instance.m_playerSaveData;
    }

    public static void LoadAndSetActivePlayerSaveData(int aSaveIndex = 0)
    {
        var playerSaveData = GetPlayerSaveData(aSaveIndex);
        if (playerSaveData != null)
            SetActivePlayerSaveData(playerSaveData, aSaveIndex);
        else if (SaveExists(aSaveIndex))
            Debug.LogError("Could not load save " + aSaveIndex + ". Might be corrupted or invalid.");
        else
            Debug.LogWarning("There is no save at index " + aSaveIndex + ".");

    }

    public static void SetActivePlayerSaveData(PlayerSaveData aPlayerSaveData, int aSaveIndex = 0)
    {
        if (aPlayerSaveData != null)
        {
            if (!aPlayerSaveData.ValidateSaveFile())
                Debug.LogError("Save file validation failed, the save file might be corrupted... :(");
            else
                Debug.Log("Save file " + aPlayerSaveData.GetName() + " loaded and validated successfuly");

            Instance.m_timeOfLastSave = Time.unscaledTimeAsDouble;
            Instance.m_playerSaveData = aPlayerSaveData;
            Instance.m_usedSaveDataSlot = aSaveIndex;

            Debug.Log("Save '" + aPlayerSaveData.GetName() + "' was loaded. Using save slot " + aSaveIndex + ".");
        }
        else
            Debug.LogError("The given PlayerSaveData is null!");
    }

    public static PlayerSaveData GetPlayerSaveData(int aSaveIndex = 0)
    {
        GfcStringBuffer savePath = GetSaveDataFilePath(aSaveIndex);
        PlayerSaveData saveData = null;

        if (File.Exists(savePath))
            saveData = JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(savePath));

        savePath.Clear();
        return saveData;
    }

    public static void DeleteSaveData(int aSaveIndex = 0)
    {
        aSaveIndex = Mathf.Clamp(aSaveIndex, 0, MAX_NUM_SAVE_FILES - 1);
        GfcStringBuffer savePath = GetSaveDataFilePath(aSaveIndex);
        if (File.Exists(savePath)) File.Delete(savePath);
        savePath.Clear();
    }

    public static bool SaveExists(int aSaveIndex = 0)
    {
        GfcStringBuffer savePath = GetSaveDataFilePath(aSaveIndex);
        bool saveExists = aSaveIndex >= 0 && aSaveIndex < MAX_NUM_SAVE_FILES && File.Exists(savePath);
        savePath.Clear();
        return saveExists;
    }

    public static void SaveGame(int aSaveIndex = -1)
    {
        if (aSaveIndex == -1) aSaveIndex = Instance.m_usedSaveDataSlot;
        aSaveIndex = Mathf.Min(aSaveIndex, 0, MAX_NUM_SAVE_FILES - 1);

        if (aSaveIndex <= -1) //most likely using a debug save file, we still want to save somewhere
        {
            for (aSaveIndex = 0; aSaveIndex < MAX_NUM_SAVE_FILES && SaveExists(aSaveIndex); aSaveIndex++) ; //find an unused slot
        }

        PlayerSaveData saveData = Instance.m_playerSaveData;
        double currentTime = Time.unscaledTimeAsDouble;
        saveData.SecondsPlayed += currentTime - Instance.m_timeOfLastSave;

        Instance.m_timeOfLastSave = currentTime;
        GfcStringBuffer savePath = GetSaveDataFilePath(aSaveIndex);
        File.WriteAllText(savePath, JsonUtility.ToJson(saveData));
        savePath.Clear();
    }

    public static GfcStringBuffer GetSaveDataFilePath(int aSaveIndex = 0)
    {
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
        stringBuffer.Concatenate(SAVE_DATA_PATH);
        stringBuffer.Concatenate(SAVE_DATA_FILENAME);
        return stringBuffer.Concatenate(aSaveIndex);
    }

    public static double GetCurrentUnixTime() { return (System.DateTime.UtcNow - UNIX_TIME_START).TotalSeconds; }
}

[Serializable]
public class PlayerSaveData
{
    [SerializeField] private CharmInventoryData[] m_ownedCharms = new CharmInventoryData[(int)CharmTypes.COUNT_CHARMS];

    [SerializeField] private List<WeaponOwnedData> m_ownedWeapons = new(4);

    [SerializeField] private List<EquipCharmData> m_equippedCharms = new(4);

    [SerializeField] private List<WeaponData> m_ownedWeaponsAllTime = new(4);

    [SerializeField] private List<int> m_equippedWeaponsIndeces = new(4);

    [SerializeField] private string m_name;

    private List<WeaponOwnedData> m_equippedWeapons = new(4);

    private double m_unixTimeOfCreation;

    public double m_charmLevelUpPoint;

    public int HighestMission;

    public double SecondsPlayed;

    public ulong TotalKills;

    public ulong MissionsCompleted;

    public ulong TimesDied;

    public ulong DamageDealt;

    private const int MISSIONS_PER_TIER = 10;

    public PlayerSaveData(string aName, double aCurrentUnixTime)
    {
        for (int i = 0; i < m_ownedCharms.Length; ++i)
            m_ownedCharms[i] = new() { Level = -1, LevelProgress = 0 }; //level -1 means we do not have it

        m_name = aName;
        m_unixTimeOfCreation = aCurrentUnixTime;

        ValidateSaveFile();
    }

    public string GetName() { return m_name; }

    public int GetTier()
    {
        return HighestMission / MISSIONS_PER_TIER;
    }

    public bool CompletedMission(int aMissionIndex)
    {
        bool firstFinish = aMissionIndex > HighestMission;
        if (firstFinish)
            HighestMission = aMissionIndex;

        MissionsCompleted++;
        return firstFinish;
    }

    public double GetUnixTimeOfCreation() { return m_unixTimeOfCreation; }

    public float AddCharm(EquipCharmData aNewCharm, out CharmAquireInfo aCharmAquireInfo)
    {
        int charmIndex = (int)aNewCharm.CharmType;
        aCharmAquireInfo = CharmAquireInfo.ALREADY_OWNED;

        CharmData charmData = GfgManagerCharms.GetCharmData(aNewCharm.CharmType);
        int tier = charmData.TiersRequiredForLevels != null && charmData.TiersRequiredForLevels.Length > 0 ? charmData.TiersRequiredForLevels[aNewCharm.Level] : 0;
        float charmValue = GfgManagerCharms.GetCharmPointsSellWorth(charmData.Rank, tier);

        if (m_ownedCharms[charmIndex].Level < aNewCharm.Level)
        {
            aCharmAquireInfo = m_ownedCharms[charmIndex].Level < 0 ? CharmAquireInfo.NEW_CHARM : CharmAquireInfo.LEVEL_UP;
            m_ownedCharms[charmIndex] = new() { Level = aNewCharm.Level, LevelProgress = 0 };
        }
        else //already owned the charm
        {
            m_charmLevelUpPoint += charmValue;
        }

        return charmValue;
    }
    public List<WeaponOwnedData> GetOwnedWeapons() { return m_ownedWeapons; }

    public int CountAvailableOwnedWeaponAt(int aOwnedWeaponIndex)
    {
        int currentCountWeapon = m_ownedWeapons[aOwnedWeaponIndex].CountInInventory;
        for (int i = 0; i < m_equippedWeaponsIndeces.Count && currentCountWeapon > 0; ++i)
            if (m_equippedWeaponsIndeces[i] == aOwnedWeaponIndex) currentCountWeapon--;

        return currentCountWeapon;
    }

    public List<int> GetEquippedWeapons()
    {
        MakeSureIHaveAWeapon();
        return m_equippedWeaponsIndeces;
    }

    public void RemoveOwnedWeapon(int aOwnedWeaponIndex)
    {
        WeaponOwnedData currentWeapon = m_ownedWeapons[aOwnedWeaponIndex];
        currentWeapon.CountInInventory--;
        m_ownedWeapons[aOwnedWeaponIndex] = currentWeapon;

        if (currentWeapon.CountInInventory <= 0)
        {
            if (currentWeapon.CountInInventory < 0)
                Debug.LogError("The count for the weapon at index " + aOwnedWeaponIndex + " is " + currentWeapon.CountInInventory + ", find the place that is messing the count up.");

            m_ownedWeapons.RemoveAt(aOwnedWeaponIndex);
        }
    }

    //Returns true if the weapon is new
    public void AddWeapon(WeaponData aWeaponPickupData, out WeaponAquireInfo aWeaponAquireInfo)
    {
        WeaponOwnedData weaponInventoryData = new()
        {
            WeaponData = aWeaponPickupData,
            CountInInventory = 1
        };

        aWeaponAquireInfo = WeaponAquireInfo.ALREADY_OWNED;

        bool weaponInInventory = false;

        //todo will eventually sort these lists and do a binary search on them but ahh I am lazy and there are other things that most be done
        if (!m_ownedWeaponsAllTime.Contains(aWeaponPickupData))
        {
            m_ownedWeaponsAllTime.Add(aWeaponPickupData);
            aWeaponAquireInfo = WeaponAquireInfo.NEW_WEAPON;
        }
        else //had the weapon at some point already
        {
            for (int i = 0; i < m_ownedWeapons.Count; ++i)
            {
                if (m_ownedWeapons[i].Equals(weaponInventoryData))
                {
                    WeaponOwnedData currentWeapon = m_ownedWeapons[i];
                    currentWeapon.CountInInventory++;
                    m_ownedWeapons[i] = currentWeapon;
                    aWeaponAquireInfo = WeaponAquireInfo.IN_INVENTORY;
                    weaponInInventory = true;
                    break;
                }
            }
        }

        if (!weaponInInventory)
            m_ownedWeapons.Add(weaponInventoryData);
    }

    public void MakeSureIHaveAWeapon()
    {


        if (m_ownedWeapons.Count == 0)
            AddWeapon(GfgManagerWeapons.GetDefaultWeapon(), out WeaponAquireInfo info);

        if (m_equippedWeaponsIndeces.Count == 0)
            EquipWeapon(0, 0);
    }
    public void UnequipWeapon(int aWeaponEquipIndex) { EquipWeapon(aWeaponEquipIndex, -1); }

    public void EquipWeapon(int aWeaponEquipIndex, int aWeaponOwnedIndex)
    {
        if (aWeaponEquipIndex < GfcManagerSaveData.MAX_EQUIPPED_WEAPONS)
        {
            if (aWeaponOwnedIndex >= 0) //equip weapon
            {
                if (aWeaponEquipIndex >= m_equippedWeaponsIndeces.Count)
                    m_equippedWeaponsIndeces.Add(aWeaponOwnedIndex);
                else
                    m_equippedWeaponsIndeces[aWeaponEquipIndex] = aWeaponOwnedIndex;
            }
            else if (aWeaponEquipIndex < m_equippedWeaponsIndeces.Count) //remove weapon
                m_equippedWeaponsIndeces.RemoveAt(aWeaponEquipIndex);
        }
        else
        {
            Debug.LogError("Could not equip the weapon at index " + aWeaponEquipIndex + ", the maximum number equipable weapons is " + GfcManagerSaveData.MAX_EQUIPPED_WEAPONS + " with the last index at " + (GfcManagerSaveData.MAX_EQUIPPED_WEAPONS - 1) + ".");
        }
    }

    public bool ValidateSaveFile()
    {
        if (m_ownedCharms == null || m_ownedCharms.Length == 0) m_ownedCharms = new CharmInventoryData[(int)CharmTypes.COUNT_CHARMS];
        if (m_ownedWeapons == null) m_ownedWeapons = new(4);
        if (m_equippedCharms == null) m_equippedCharms = new(4);
        if (m_equippedWeaponsIndeces == null) m_equippedWeaponsIndeces = new(4);
        if (m_ownedWeaponsAllTime == null) m_ownedWeaponsAllTime = new(4);

        return ValidateWeapons();
    }

    public bool ValidateWeapons()
    {
        bool allWeaponsValid = true;
        if (m_ownedWeapons != null)
        {
            for (int i = 0; i < m_ownedWeapons.Count; i++)
            {
                if (!GfgManagerWeapons.WeaponExists(m_ownedWeapons[i].WeaponData))
                {
                    allWeaponsValid = false;
                    Debug.LogError("The weapon " + m_ownedWeapons[i].WeaponData + " does not exist. The save file might be corrupted.");
                    m_ownedWeapons.RemoveAt(i);
                }
            }
        }

        MakeSureIHaveAWeapon();
        return allWeaponsValid;
    }

    public PlayerRuntimeGameData GetRuntimeGameData()
    {
        MakeSureIHaveAWeapon();

        if (m_equippedWeapons == null)
            m_equippedWeapons = new(m_equippedWeaponsIndeces.Count);

        m_equippedWeapons.Clear();
        for (int i = 0; i < m_equippedWeaponsIndeces.Count; ++i)
            m_equippedWeapons.Add(m_ownedWeapons[m_equippedWeaponsIndeces[i]]);

        return new()
        {
            EquippedWeapons = m_equippedWeapons,
            EquippeCharms = m_equippedCharms,
            Name = m_name,
            EffectivePlayerPrefab = EffectivePlayerPrefab.REIMU,
        };
    }
}

public enum WeaponAquireInfo
{
    IN_INVENTORY,
    ALREADY_OWNED,
    NEW_WEAPON,
}

public enum CharmAquireInfo
{
    ALREADY_OWNED,
    LEVEL_UP,
    NEW_CHARM,
}

public enum EffectivePlayerPrefab
{
    REIMU,
    MARISA
}
public struct PlayerRuntimeGameData : INetworkSerializable
{
    public List<WeaponOwnedData> EquippedWeapons;

    public List<EquipCharmData> EquippeCharms;

    public string Name;

    public EffectivePlayerPrefab EffectivePlayerPrefab;

    public void NetworkSerialize<T>(BufferSerializer<T> aSerializer) where T : IReaderWriter
    {
        GfcTools.SerializeValue(aSerializer, ref EquippedWeapons);
        GfcTools.SerializeValue(aSerializer, ref EquippeCharms);
        GfcTools.SerializeValue(aSerializer, ref Name);
        aSerializer.SerializeValue(ref EffectivePlayerPrefab);
    }
}
