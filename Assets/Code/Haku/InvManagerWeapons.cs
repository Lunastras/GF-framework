using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Unity.Netcode;
using UnityEngine;

public class InvManagerWeapons : MonoBehaviour
{
    [SerializeField]
    public WeaponTier[] m_weaponTiers;

    private static InvManagerWeapons Instance = null;

    const StringTableType WEAPON_DESCRIPTIONS_TIER_1 = StringTableType.WEAPON_DESCRIPTIONS_TIER_1;

    protected static StringTableType GetStringTableTypeForTier(int aTier) { return (StringTableType)((int)WEAPON_DESCRIPTIONS_TIER_1 + aTier); }

    const string NAME_TERMINATION = "_NAME";

    const string DESCRIPTION_TERMINATION = "_DESCRIPTION";

    public const int MAX_TIER = 10;

    public static readonly WeaponData NullWeapon = new() { Index = -1, Tier = -1, Blessed = false };

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance);
        }

        Instance = this;
    }

    public static WeaponData GetRandomWeaponPickupData(ItemDrop aItemDrop)
    {
        return GetRandomWeaponPickupData(aItemDrop.Tier, aItemDrop.Blessed);
    }

    public static WeaponData GetRandomWeaponPickupData(int aTier, bool aBlessed)
    {
        int countWeapons = aBlessed ? Instance.m_weaponTiers[aTier].BlessedWeaponPrefabs.Length : Instance.m_weaponTiers[aTier].WeaponPrefabs.Length;
        return new()
        {
            Index = Random.Range(0, countWeapons - 1),
            Tier = aTier,
            Blessed = aBlessed
        };
    }

    static public GameObject GetWeapon(WeaponData aData)
    {
        return aData.Blessed ? Instance.m_weaponTiers[aData.Tier].BlessedWeaponPrefabs[aData.Index] : Instance.m_weaponTiers[aData.Tier].WeaponPrefabs[aData.Index];
    }

    public static string GetWeaponName(WeaponData aData)
    {
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
        stringBuffer.Concatenate(GetWeapon(aData).name);
        stringBuffer.Concatenate(NAME_TERMINATION);
        string weaponText = GfcLocalization.GetString(GetStringTableTypeForTier(aData.Tier), stringBuffer);
        stringBuffer.Clear();
        return weaponText;
    }

    public static string GetWeaponDescription(WeaponData aData)
    {
        GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
        stringBuffer.Concatenate(GetWeapon(aData).name);
        stringBuffer.Concatenate(DESCRIPTION_TERMINATION);
        string weaponText = GfcLocalization.GetString(GetStringTableTypeForTier(aData.Tier), stringBuffer);
        stringBuffer.Clear();
        return weaponText;
    }

    public static WeaponData GetDefaultWeapon()
    {
        return new(); //not blessed, tier 0 , index 0
    }

    public static bool WeaponExists(WeaponData aData)
    {
        return aData.Index >= 0 && aData.Tier >= 0 && aData.Tier < Instance.m_weaponTiers.Length && (aData.Blessed ? aData.Index < Instance.m_weaponTiers[aData.Tier].BlessedWeaponPrefabs.Length : aData.Index < Instance.m_weaponTiers[aData.Tier].WeaponPrefabs.Length);
    }

    public static Color GetTierColor(int aTier)
    {
        return Instance.m_weaponTiers[aTier].TierColor;
    }

    public static Color GetTierColorComplementary(int aTier)
    {
        return Instance.m_weaponTiers[aTier].TierColorComplementary;
    }
}

[System.Serializable]
public struct WeaponTier
{
    public GameObject[] WeaponPrefabs;
    public GameObject[] BlessedWeaponPrefabs;
    public Color TierColor;

    public Color TierColorComplementary;
}

[System.Serializable]
public struct WeaponData : INetworkSerializable
{
    public int Index;

    public int Tier;

    public bool Blessed;

    public static bool operator ==(WeaponData lhs, WeaponData rhs)
    {
        return Equals(lhs, rhs);
    }

    public static bool operator !=(WeaponData lhs, WeaponData rhs)
    {
        return !Equals(lhs, rhs);
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public void NetworkSerialize<T>(BufferSerializer<T> aSerializer) where T : IReaderWriter
    {
        aSerializer.SerializeValue(ref Index);
        aSerializer.SerializeValue(ref Tier);
        aSerializer.SerializeValue(ref Blessed);
    }
}

[System.Serializable]
public struct WeaponOwnedData : INetworkSerializable
{
    public WeaponData WeaponData;

    public string NameOverride;

    public string DescriptionOverride;

    public int TierOverride;

    public float TierOverrideProgressionPoints;

    public int CountInInventory;

    public int GetEffectiveTier() { return WeaponData.Tier > TierOverride ? WeaponData.Tier : TierOverride; }

    public string GetEffectiveName() { return NameOverride != null && NameOverride.Length > 0 ? NameOverride : InvManagerWeapons.GetWeaponName(WeaponData); }

    public string GetEffectiveDescription() { return DescriptionOverride != null && DescriptionOverride.Length > 0 ? DescriptionOverride : InvManagerWeapons.GetWeaponDescription(WeaponData); }

    public bool Unique { get { return WeaponData.Tier < TierOverride || (NameOverride != null && NameOverride.Length > 0) || (DescriptionOverride != null && DescriptionOverride.Length > 0) || TierOverrideProgressionPoints > 0; } }

    public readonly bool Blessed { get { return WeaponData.Blessed; } }

    public readonly int Tier { get { return WeaponData.Tier; } }

    public readonly int Index { get { return WeaponData.Index; } }

    public readonly Color TierColor { get { return InvManagerWeapons.GetTierColor(Tier); } }

    public readonly Color TierColorComplementary { get { return InvManagerWeapons.GetTierColorComplementary(Tier); } }

    public readonly bool Equals(WeaponOwnedData aOther)
    {
        return WeaponData == aOther.WeaponData
                && GfcTools.Equals(NameOverride, aOther.NameOverride)
                && GfcTools.Equals(DescriptionOverride, aOther.DescriptionOverride)
                && TierOverride == aOther.TierOverride
                && TierOverrideProgressionPoints == aOther.TierOverrideProgressionPoints;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> aSerializer) where T : IReaderWriter
    {
        GfcTools.SerializeValue(aSerializer, ref NameOverride);
        GfcTools.SerializeValue(aSerializer, ref DescriptionOverride);
        aSerializer.SerializeValue(ref WeaponData);
        aSerializer.SerializeValue(ref NameOverride);
        aSerializer.SerializeValue(ref DescriptionOverride);
        aSerializer.SerializeValue(ref TierOverride);
        aSerializer.SerializeValue(ref TierOverrideProgressionPoints);
    }
}
