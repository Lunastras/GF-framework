using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class GfcLocalization : MonoBehaviour
{
    protected static GfcLocalization Instance = null;

#if UNITY_EDITOR
    [SerializeField] private EnumSingletons<StringTableCollection, GfcLocalizationStringTableType> m_stringTableCollectionsEditor;
#endif //UNITY_EDITOR

    [SerializeField] private EnumSingletons<LocalizedStringTable, GfcLocalizationStringTableType> m_stringTablesPlayer;
    [SerializeField] private bool m_printErrors = true;

    protected Dictionary<string, string>[] m_cachedStringLists = null;
    protected StringTable[] m_stringTables = null;
    protected LocalizedStringDatabase m_localizedStringDatabase = null;
    protected const string UNKNOWN_ENTRY = "_UNKNOWN_ENTRY";
    protected LocaleIdentifier m_defaultLocaleIdentifier;

    private GfcStringBuffer m_stringBuffer = new(15);

    public static void RegisterLocalizedString(string aString, string aKey, GfcLocalizationStringTableType aTable)
    {
#if UNITY_EDITOR
        StringTableCollection stringTableCollection = Instance.m_stringTableCollectionsEditor[aTable];
        StringTable table = stringTableCollection.GetTable(Instance.m_defaultLocaleIdentifier) as StringTable;
        StringTableEntry tableEntry = table.GetEntry(aKey);

        if (tableEntry == null || tableEntry.Value != aString)
        {
            tableEntry = table.AddEntry(aKey, aString);

            Debug.Log("GfcLocalization: Registered string '" + aString + "' for key '" + aKey + "'");

            // We need to mark the table and shared table data entry as we have made changes
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }
#endif //UNITY_EDITOR
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (this != Instance)
        {
            Instance = this;
            LocalizationSettings.Instance.OnSelectedLocaleChanged += OnSelectedLocaleChanged;

            int countStringTables = (int)GfcLocalizationStringTableType.COUNT;
            m_stringTables = new StringTable[countStringTables];
            m_cachedStringLists = new Dictionary<string, string>[countStringTables];

            m_defaultLocaleIdentifier = LocalizationSettings.Instance.GetSelectedLocale().Identifier;
            LoadStringTables(m_defaultLocaleIdentifier, false);
        }

        GfcLocalizedString s = new("Test string english");
    }

    public static bool IsDefaultLocale() { return Instance.m_defaultLocaleIdentifier == LocalizationSettings.Instance.GetSelectedLocale().Identifier; }

    protected void LoadStringTables(LocaleIdentifier aLocaleIdentifier, bool aForceUpdate = false)
    {
        int countStringTables = (int)GfcLocalizationStringTableType.COUNT;

#if UNITY_EDITOR
        m_stringTableCollectionsEditor.Initialize(GfcLocalizationStringTableType.COUNT);
#endif //UNITY_EDITOR
        m_stringTablesPlayer.Initialize(GfcLocalizationStringTableType.COUNT);

        if (null == m_stringTablesPlayer || 0 == m_stringTablesPlayer.Length
#if UNITY_EDITOR
        || null == m_stringTableCollectionsEditor || 0 == m_stringTableCollectionsEditor.Length
#endif //UNITY_EDITOR
        )
            Debug.LogError("The string table links array has not been initialised!");

        for (int i = 0; i < (int)GfcLocalizationStringTableType.COUNT; ++i)
        {
            //m_stringTables[i] = m_stringTableCollectionsEditor[i].GetTable(aLocaleIdentifier) as StringTable;
            m_stringTables[i] = m_stringTablesPlayer[i].GetTable();
        }

        for (int i = 0; i < countStringTables; ++i)
        {
            if (null == m_stringTables[i] && m_printErrors)
                Debug.LogError("The type " + (GfcLocalizationStringTableType)i + " does not have a string table assigned.");
        }
    }

    public void ClearStringDictionary()
    {
        int length = m_cachedStringLists.Length;
        for (int i = 0; i < length; ++i)
            m_cachedStringLists[i]?.Clear();
    }

    protected void OnSelectedLocaleChanged(Locale aNewLocale)
    {
        ClearStringDictionary();
        LoadStringTables(aNewLocale.Identifier, true);
    }

    protected static bool LoadString(StringTable aTable, string aEntryName, out string aLocalizedString)
    {
        StringTableEntry entry = aTable.GetEntry(aEntryName);
        aLocalizedString = null != entry ? entry.GetLocalizedString() : null;
        return aLocalizedString != null;
    }

    //Retrieves the string for the current locale Id found inside the table associated with [aStringTableType] at the entry [aEntryName]
    //Please do not call in the "Awake" function
    public static string GetString(GfcLocalizationStringTableType aStringTableType, string aEntryName) { return Instance.GetStringInternal(aStringTableType, aEntryName); }

    protected string GetStringInternal(GfcLocalizationStringTableType aStringTableType, string aEntryName)
    {
        string valueString;
        int tableIndex = (int)aStringTableType;
        if (null == m_cachedStringLists[tableIndex] || !m_cachedStringLists[tableIndex].TryGetValue(aEntryName, out valueString))
        {
            if (LoadString(m_stringTables[tableIndex], aEntryName, out valueString))
            {
                if (null == m_cachedStringLists[tableIndex])
                    m_cachedStringLists[tableIndex] = new(4);

                m_cachedStringLists[tableIndex].Add(aEntryName, valueString);
            }
            else
            {
                m_stringBuffer.Clear();
                m_stringBuffer.Append(aEntryName);
                m_stringBuffer.Append(UNKNOWN_ENTRY);
                valueString = m_stringBuffer.GetStringCopy();

                if (m_printErrors)
                    Debug.LogError("Could not find the entry '" + aEntryName + "' in the table '" + m_stringTables[tableIndex].name + "' (type: '" + aStringTableType + "' ).");
            }
        }

        return valueString;
    }

    public static string GetDateString(int aDay, int aMonth, int aYear = -1)
    {
        GfcStringBuffer stringBuffer = Instance.m_stringBuffer;
        stringBuffer.Clear();

        stringBuffer.Append(aDay);
        stringBuffer.Append('/');

        switch (aMonth)
        {
            case 0: stringBuffer.Append("JAN"); break;
            case 1: stringBuffer.Append("FEB"); break;
            case 2: stringBuffer.Append("MAR"); break;
            case 3: stringBuffer.Append("APR"); break;
            case 4: stringBuffer.Append("MAY"); break;
            case 5: stringBuffer.Append("JUN"); break;
            case 6: stringBuffer.Append("JUL"); break;
            case 7: stringBuffer.Append("AUG"); break;
            case 8: stringBuffer.Append("SEP"); break;
            case 9: stringBuffer.Append("OCT"); break;
            case 10: stringBuffer.Append("NOV"); break;
            case 11: stringBuffer.Append("DEC"); break;
            default: stringBuffer.Append("UNKNOWN"); break;
        }

        if (aYear >= 0)
        {
            stringBuffer.Append(' ');
            stringBuffer.Append(aYear);
        }

        return stringBuffer.GetStringCopy();
    }
}

public enum GfcLocalizationStringTableType
{
    MISC,
    DIALOGUE,

    COUNT
}

[Serializable]
public struct StringTableLink
{
    public GfcLocalizationStringTableType Type;
    public StringTableCollection StringTableCollection;
}

//A string that gets translated if the selected locale is different from the default locale, but kept intact if the locale was unchanged
public struct GfcLocalizedString
{
    public GfcLocalizedString(string aRawStringForDefaultLocale, GfcLocalizationStringTableType aTable = GfcLocalizationStringTableType.MISC, string aLocalizedKey = null)
    {
        if (aLocalizedKey.IsEmpty())
            aLocalizedKey = aRawStringForDefaultLocale.GetHashCode().ToString();

        m_rawString = aRawStringForDefaultLocale;
        m_table = aTable;
        m_localeCodeOfCachedString = m_cachedString = null;
        m_key = aLocalizedKey;
        GfcLocalization.RegisterLocalizedString(aRawStringForDefaultLocale, aLocalizedKey, aTable);
    }

    private readonly string m_key;
    private readonly string m_rawString;
    private readonly GfcLocalizationStringTableType m_table;

    private string m_cachedString;
    private string m_localeCodeOfCachedString;

    public static implicit operator string(GfcLocalizedString v) => v.String;

    private readonly bool Valid { get { return m_rawString != null; } }

    public string String
    {
        get
        {
            string localeCode = LocalizationSettings.Instance.GetSelectedLocale().Identifier.Code;
            if (m_localeCodeOfCachedString != localeCode || m_rawString != null)
            {
                m_cachedString = GfcLocalization.GetString(m_table, m_key);
                if (m_cachedString.IsEmpty()) m_cachedString = m_rawString;
                m_localeCodeOfCachedString = localeCode;
            }

            return m_cachedString;
        }
    }
}
