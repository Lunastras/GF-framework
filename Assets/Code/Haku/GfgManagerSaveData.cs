using UnityEngine;
using System.IO;

public class GfgManagerSaveData : MonoBehaviour
{
    protected static GfgManagerSaveData Instance;

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