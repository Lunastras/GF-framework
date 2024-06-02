using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Utilities;
using UnityEngine.Localization.Tables;

public class GfcLocalization : MonoBehaviour
{
    [SerializeField]
    private StringTableLink[] m_stringTableLinks = null;

    [SerializeField]
    private bool m_printErrorsWhenKeyInvalid = true;

    protected static GfcLocalization Instance = null;
    protected Dictionary<string, string>[] m_cachedStringLists = null;

    protected StringTable[] m_stringTables = null;

    protected LocalizedStringDatabase m_localizedStringDatabase = null;

    private GfcStringBuffer m_stringBuffer = new(15);

    protected const string UNKNOWN_ENTRY = "_UNKNOWN_ENTRY";

    // Start is called before the first frame update
    void Awake()
    {
        if (this != Instance)
        {
            Destroy(Instance);

            Instance = this;
            LocalizationSettings.Instance.OnSelectedLocaleChanged += OnSelectedLocaleChanged;


            int countStringTables = (int)StringTableType.COUNT;
            m_stringTables = new StringTable[countStringTables];
            m_cachedStringLists = new Dictionary<string, string>[countStringTables];

            LocaleIdentifier localeIdentifier = LocalizationSettings.Instance.GetSelectedLocale().Identifier;
            LoadStringTables(localeIdentifier, false);
        }
    }

    protected void LoadStringTables(LocaleIdentifier aLocaleIdentifier, bool aForceUpdate = false)
    {
        int countStringTables = (int)StringTableType.COUNT;

        if (null == m_stringTableLinks || 0 == m_stringTableLinks.Length)
            Debug.LogError("The string table links array has not been initialised!");

        if (null != m_stringTableLinks)
        {
            int linksLength = m_stringTableLinks.Length;
            for (int i = 0; i < linksLength; ++i)
            {
                int typeIndex = (int)m_stringTableLinks[i].Type;

                //too lazy to do this in the UI, I already wasted enough time over-engineering stuff
                if (i != typeIndex) Debug.LogWarning("The StringTableLink for the type " + m_stringTableLinks[i].Type + " is at index " + i + ", please reorder it at index " + typeIndex);

                if (!aForceUpdate && null != m_stringTables[typeIndex])
                    Debug.LogError("The type " + m_stringTableLinks[i].Type + " was already asssigned to string table '" + m_stringTables[typeIndex].name + "'.");
                else if (null == m_stringTableLinks[i].StringTableCollection)
                    Debug.LogError("The StringTableCollection for the type " + m_stringTableLinks[i].Type + " is null, please assign it from the editor.");
                else
                    m_stringTables[typeIndex] = m_stringTableLinks[i].StringTableCollection.GetTable(aLocaleIdentifier) as StringTable;
            }

            for (int i = 0; i < countStringTables; ++i)
            {
                if (null == m_stringTables[i])
                    Debug.LogError("The type " + (StringTableType)i + " does not have a string table assigned.");
            }
        }
    }

    public void ClearStringDictionary()
    {
        int length = m_cachedStringLists.Length;
        for (int i = 0; i < length; ++i)
        {
            if (null != m_cachedStringLists[i])
                m_cachedStringLists[i].Clear();
        }
    }

    protected void OnSelectedLocaleChanged(Locale aNewLocale)
    {
        ClearStringDictionary();
        LoadStringTables(aNewLocale.Identifier, true);
    }

    protected static bool LoadString(StringTable aTable, string aEntryName, out string aLocalizedString)
    {
        var entry = aTable.GetEntry(aEntryName);
        aLocalizedString = null != entry ? entry.GetLocalizedString() : null;
        return aLocalizedString != null;
    }

    public static string GetDateString(int aDay, int aMonth, int aYear = -1)
    {
        GfcStringBuffer stringBuffer = Instance.m_stringBuffer;
        stringBuffer.Clear();

        stringBuffer.Concatenate(aDay);
        stringBuffer.Concatenate('/');

        switch (aMonth)
        {
            case 0: stringBuffer.Concatenate("JAN"); break;
            case 1: stringBuffer.Concatenate("FEB"); break;
            case 2: stringBuffer.Concatenate("MAR"); break;
            case 3: stringBuffer.Concatenate("APR"); break;
            case 4: stringBuffer.Concatenate("MAY"); break;
            case 5: stringBuffer.Concatenate("JUN"); break;
            case 6: stringBuffer.Concatenate("JUL"); break;
            case 7: stringBuffer.Concatenate("AUG"); break;
            case 8: stringBuffer.Concatenate("SEP"); break;
            case 9: stringBuffer.Concatenate("OCT"); break;
            case 10: stringBuffer.Concatenate("NOV"); break;
            case 11: stringBuffer.Concatenate("DEC"); break;
            default: stringBuffer.Concatenate("UNKNOWN"); break;
        }

        if (aYear >= 0)
        {
            stringBuffer.Concatenate(' ');
            stringBuffer.Concatenate(aYear);
        }

        return stringBuffer.GetStringCopy();
    }

    //Retrieves the string for the current locale Id found inside the table associated with [aStringTableType] at the entry [aEntryName]
    //Please do not call in the "Awake" function
    public static string GetString(StringTableType aStringTableType, string aEntryName) { return Instance.GetStringInternal(aStringTableType, aEntryName); }

    protected string GetStringInternal(StringTableType aStringTableType, string aEntryName)
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
                m_stringBuffer.Concatenate(aEntryName);
                m_stringBuffer.Concatenate(UNKNOWN_ENTRY);
                valueString = m_stringBuffer.GetStringCopy();

                if (m_printErrorsWhenKeyInvalid)
                    Debug.LogError("Could not find the entry '" + aEntryName + "' in the table '" + m_stringTables[tableIndex].name + "' (type: '" + aStringTableType + "' ).");
            }
        }

        return valueString;
    }
}

public enum StringTableType
{
    CHARM_DESCRIPTIONS,

    WEAPON_DESCRIPTIONS_TIER_1,
    WEAPON_DESCRIPTIONS_TIER_2,
    WEAPON_DESCRIPTIONS_TIER_3,
    WEAPON_DESCRIPTIONS_TIER_4,
    WEAPON_DESCRIPTIONS_TIER_5,
    WEAPON_DESCRIPTIONS_TIER_6,
    WEAPON_DESCRIPTIONS_TIER_7,
    WEAPON_DESCRIPTIONS_TIER_8,
    WEAPON_DESCRIPTIONS_TIER_9,
    WEAPON_DESCRIPTIONS_TIER_10,

    COUNT
}

[System.Serializable]
public struct StringTableLink
{
    public StringTableType Type;
    public StringTableCollection StringTableCollection;
}
