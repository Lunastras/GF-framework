using UnityEngine;
using System.IO;

public class GfgManagerSaveData : MonoBehaviour
{
    protected static GfgManagerSaveData Instance;
    private System.DateTime m_dateTimeOfProgramStart;

    [SerializeField] private bool m_printLogs = true;
    [SerializeField] private bool m_forceNewSave = false;

    private const string SAVE_DATA_PATH = "SaveData/";
    private const string SAVE_DATA_FILENAME = "SAVE_PROFILE_";
    public const int MAX_NUM_SAVE_FILES = 4;
    public const int MAX_EQUIPPED_WEAPONS = 2;
    private long m_timeOfLastSaveUnix;
    private int m_usedSaveDataSlot = -1;

    private GfgPlayerSaveData m_playerSaveData = null;

    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;

        if (!Directory.Exists(SAVE_DATA_PATH)) Directory.CreateDirectory(SAVE_DATA_PATH);

        m_timeOfLastSaveUnix = GfcTools.GetCurrentUnixUtcTime();
        m_dateTimeOfProgramStart = System.DateTime.UtcNow;
        m_playerSaveData = null;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public static void MakeSureIHaveASaveData()
    {
        if (Instance.m_playerSaveData == null)
        {
            if (Instance.m_printLogs) Debug.Log("No save file loaded, automatically loading data...");

            if (!Instance.m_forceNewSave)
            {
                int currentSaveIndex;
                bool foundSave = false;

                for (currentSaveIndex = 0; currentSaveIndex < MAX_NUM_SAVE_FILES && !foundSave; ++currentSaveIndex)
                    foundSave = SaveExists(currentSaveIndex);

                if (foundSave)
                    LoadAndSetActivePlayerSaveData(currentSaveIndex);
            }

            if (Instance.m_playerSaveData == null)
            {
                SetActivePlayerSaveData(CreatePlayerSaveData("DEBUG MAESTRO"), 0);
                Debug.LogWarning("No save found, using debug save.");
            }
        }
    }

    public static GfgPlayerSaveData CreatePlayerSaveData(string aName) { return new(aName); }

    public static GfgPlayerSaveData[] GetPlayerSaveDatasCopy(int aSize = MAX_NUM_SAVE_FILES)
    {
        GfgPlayerSaveData[] saves = new GfgPlayerSaveData[aSize];
        for (int i = 0; i < aSize; i++)
            saves[i] = GetPlayerSaveData(i);

        return saves;
    }

    public static GfgPlayerSaveData GetActivePlayerSaveData()
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

    public static void SetActivePlayerSaveData(int aSaveIndex) { SetActivePlayerSaveData(GetPlayerSaveData(aSaveIndex), aSaveIndex); }

    public static void SetActivePlayerSaveData(GfgPlayerSaveData aPlayerSaveData, int aSaveIndex = 0)
    {
        if (ValidSaveDataIndex(aSaveIndex))
        {
            if (aPlayerSaveData != null)
            {
                if (!aPlayerSaveData.ValidateSaveFile())
                    Debug.LogError("Save file validation failed, the save file might be corrupted... :(");
                else if (Instance.m_printLogs)
                    Debug.Log("Save file " + aPlayerSaveData.GetName() + " loaded and validated successfuly");

                Instance.m_timeOfLastSaveUnix = GfcTools.GetCurrentUnixUtcTime();
                Instance.m_playerSaveData = aPlayerSaveData;
                Instance.m_usedSaveDataSlot = aSaveIndex;

                if (Instance.m_printLogs) Debug.Log("Save '" + aPlayerSaveData.GetName() + "' was loaded. Using save slot " + aSaveIndex + ".");
            }
            else
                Debug.LogError("The given PlayerSaveData is null!");
        }
        else
            Debug.LogError("The index " + aSaveIndex + " is not valid.");
    }

    public static GfgPlayerSaveData GetPlayerSaveData(int aSaveIndex = 0)
    {
        GfcStringBuffer savePath = GetSaveDataFilePath(aSaveIndex);
        GfgPlayerSaveData saveData = null;

        if (File.Exists(savePath))
        {
            saveData = JsonUtility.FromJson<GfgPlayerSaveData>(File.ReadAllText(savePath));
            if (saveData != null && Instance.m_printLogs)
                Debug.Log("Loaded save file " + savePath);
        }

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

    public static bool ValidSaveDataIndex(int aSaveIndex) { return 0 <= aSaveIndex && aSaveIndex < MAX_NUM_SAVE_FILES; }

    public static bool SaveExists(int aSaveIndex = 0)
    {
        GfcStringBuffer savePath = GetSaveDataFilePath(aSaveIndex);
        bool saveExists = ValidSaveDataIndex(aSaveIndex) && File.Exists(savePath);
        savePath.Clear();
        return saveExists;
    }

    public static void SaveGame(int aSaveIndex = -1)
    {
        if (aSaveIndex <= -1)
        {
            aSaveIndex = Instance.m_usedSaveDataSlot;
            if (aSaveIndex <= -1) //most likely using a debug save file, we still want to save somewhere
                for (aSaveIndex = 0; aSaveIndex < MAX_NUM_SAVE_FILES && SaveExists(aSaveIndex); aSaveIndex++) ; //find an unused slot

            if (aSaveIndex >= MAX_NUM_SAVE_FILES)
                Debug.LogError("Could not find an unused save slot, please select the save slot used.");
        }

        if (aSaveIndex >= 0 && aSaveIndex < MAX_NUM_SAVE_FILES)
        {
            GfgPlayerSaveData saveData = GetActivePlayerSaveData();
            long currentTime = GfcTools.GetCurrentUnixUtcTime();
            saveData.SecondsPlayed += currentTime - Instance.m_timeOfLastSaveUnix;

            Instance.m_timeOfLastSaveUnix = currentTime;
            GfcStringBuffer savePath = GetSaveDataFilePath(aSaveIndex);
            File.WriteAllText(savePath, JsonUtility.ToJson(saveData));
            savePath.Clear();

            if (Instance.m_printLogs) Debug.Log("Saved player data at index " + aSaveIndex);
        }
    }

    public static GfcStringBuffer GetSaveDataFilePath(int aSaveIndex = 0)
    {
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
        stringBuffer.Append(SAVE_DATA_PATH);
        stringBuffer.Append(SAVE_DATA_FILENAME);
        return stringBuffer.Append(aSaveIndex);
    }
}