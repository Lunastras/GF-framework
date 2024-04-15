using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgTiersThreatsBalancingStats
{
    //the baseline threat level where 100% drop something on kill
    protected const ThreatLevel BASELINE_ITEM_DROP_THREAT_LEVEL = ThreatLevel.INDOMITABLE_SPIRIT;

    //the baseline threat level where we do not drop HP pick ups
    protected const ThreatLevel BASELINE_NON_HP_DROP_THREAT_LEVEL = ThreatLevel.INDOMITABLE_SPIRIT;

    //the baseline threat level where the hp drop is big
    protected const ThreatLevel BASELINE_BIG_HP_DROP_THREAT_LEVEL = ThreatLevel.FORMIDABLE_YOUKAI;

    //the baseline threat level where the drop is blessed
    protected const ThreatLevel BASELINE_BLESSED_DROP_THREAT_LEVEL = ThreatLevel.GODDESS;

    //the baseline threat level where the drop is of the same tier as the enemy
    protected const ThreatLevel BASELINE_SAME_TIER_DROP_THREAT_LEVEL = ThreatLevel.LOWLY_YOUKAI;

    //the baseline threat level where the drop is of higher tier than the enemy
    protected const ThreatLevel BASELINE_HIGHER_TIER_DROP_THREAT_LEVEL = ThreatLevel.GODDESS;


    //exponential coef dropoff for the likelyhood to drop anything
    protected const float ITEM_DROP_PROBABILITY_DROPOFF = 1.3f;

    //exponential coef dropoff for the likelyhood to drop non-hp items
    protected const float ITEM_DROP_NON_HP_PROBABILITY_DROPOFF = 1.3f;

    //exponential coef dropoff for the likelyhood to drop non-hp items
    protected const float ITEM_DROP_BIG_HP_PROBABILITY_DROPOFF = 1.3f;

    //exponential coef dropoff for the likelyhood to drop blessed weapons
    protected const float ITEM_DROP_BLESSED_DROP_PROBABILITY_DROPOFF = 10f;

    //exponential coef dropoff for the likelyhood to drop same tier weapons
    protected const float ITEM_DROP_SAME_TIER_DROP_PROBABILITY_DROPOFF = 5f;

    //exponential coef dropoff for the likelyhood to drop higher tier weapons
    protected const float ITEM_DROP_HIGHER_TIER_DROP_PROBABILITY_DROPOFF = 5f;

    //likelyhood to drop bomb as opposed to hp
    protected const float LIKELYHOOD_TO_DROP_BOMB = 0.1f;

    //likelyhood to drop charm as opposed to a weapon
    protected const float LIKELYHOOD_TO_DROP_CHARM = 0.5f;
    protected const int MAX_WEAPON_TIER = 11;

    //The exponential HP/damage difference between tiers
    protected const float TIER_HP_DAMAGE_MULTIPLIER = 1.3f;

    protected const float WISP_BASE_HP = 50;

    protected const float THREAT_LEVELS_HP_PROGRESSION = 1.5f;

    //Damage/hp multiplier between tiers

    public static float GetTierHpDamageMultiplier(int tier)
    {
        return System.MathF.Pow(TIER_HP_DAMAGE_MULTIPLIER, tier);
    }

    public static float GetTierHp(int tier, ThreatLevel threatLevel)
    {
        return WISP_BASE_HP * GetTierHpDamageMultiplier(tier) * System.MathF.Pow(THREAT_LEVELS_HP_PROGRESSION, (int)threatLevel);
    }

    public static float GetProbabilityToDropAnything(ThreatLevel threatLevel)
    {
        return System.MathF.Min(1f, System.MathF.Pow(ITEM_DROP_PROBABILITY_DROPOFF, (int)threatLevel - (int)BASELINE_ITEM_DROP_THREAT_LEVEL));
    }

    public static float GetProbabilityToDropHpOrBomb(ThreatLevel threatLevel)
    {
        return 1.0f - System.MathF.Min(1f, System.MathF.Pow(ITEM_DROP_NON_HP_PROBABILITY_DROPOFF, (int)threatLevel - (int)BASELINE_NON_HP_DROP_THREAT_LEVEL));
    }

    public static float GetProbabilityToDropBigHp(ThreatLevel threatLevel)
    {
        return System.MathF.Min(1f, System.MathF.Pow(ITEM_DROP_BIG_HP_PROBABILITY_DROPOFF, (int)threatLevel - (int)BASELINE_BIG_HP_DROP_THREAT_LEVEL));
    }

    public static float GetProbabilityToDropBlessed(ThreatLevel threatLevel)
    {
        return System.MathF.Min(1f, System.MathF.Pow(ITEM_DROP_BLESSED_DROP_PROBABILITY_DROPOFF, (int)threatLevel - (int)BASELINE_BLESSED_DROP_THREAT_LEVEL));
    }

    public static float GetProbabilityToDropSameTier(ThreatLevel threatLevel)
    {
        return System.MathF.Min(1f, System.MathF.Pow(ITEM_DROP_SAME_TIER_DROP_PROBABILITY_DROPOFF, (int)threatLevel - (int)BASELINE_SAME_TIER_DROP_THREAT_LEVEL));
    }

    public static float GetProbabilityToDropHigherTierWeapon(ThreatLevel threatLevel)
    {
        return System.MathF.Min(1f, System.MathF.Pow(ITEM_DROP_HIGHER_TIER_DROP_PROBABILITY_DROPOFF, (int)threatLevel - (int)BASELINE_HIGHER_TIER_DROP_THREAT_LEVEL));
    }

    private static bool DropAnything(ThreatLevel threatLevel)
    {
        return Random.Range(0f, 1f) <= GetProbabilityToDropAnything(threatLevel);
    }

    private static bool DropHpOrBomb(ThreatLevel threatLevel)
    {
        return Random.Range(0f, 1f) <= GetProbabilityToDropHpOrBomb(threatLevel);
    }

    private static bool DropBigHp(ThreatLevel threatLevel)
    {
        return Random.Range(0f, 1f) <= GetProbabilityToDropBigHp(threatLevel);
    }

    private static bool DropBlessed(ThreatLevel threatLevel)
    {
        return Random.Range(0f, 1f) <= GetProbabilityToDropBlessed(threatLevel);
    }

    private static bool DropSameTier(ThreatLevel threatLevel)
    {
        return Random.Range(0f, 1f) <= GetProbabilityToDropSameTier(threatLevel);
    }

    private static bool DropHigherTier(ThreatLevel threatLevel)
    {
        return Random.Range(0f, 1f) <= GetProbabilityToDropHigherTierWeapon(threatLevel);
    }

    private static bool DropCharm()
    {
        return Random.Range(0f, 1f) <= LIKELYHOOD_TO_DROP_CHARM;
    }

    private static bool DropBomb()
    {
        return Random.Range(0f, 1f) <= LIKELYHOOD_TO_DROP_BOMB;
    }

    public static ItemDrop GetItemDrop(GfgStatsCharacter GfgStatsCharacter)
    {
        return GetItemDrop(GfgStatsCharacter.GetThreatDetails());
    }

    public static ItemDrop GetItemDrop(ThreatDetails threatDetails)
    {
        return GetItemDrop(threatDetails.Tier, threatDetails.ThreatLevel);
    }

    public static ItemDrop GetItemDrop(int tier, ThreatLevel threatLevel)
    {
        ItemDrop itemDrop = default;
        int currentTier = tier;

        itemDrop.Blessed = DropBlessed(threatLevel);

        if (itemDrop.Blessed || DropAnything(threatLevel))
        {
            //originally thought about dropping a weapon/charm if the drop is blessed, but the thought of dropping a blessed HP pickup sounds hilarious to me, wasted luck lmao
            if (DropHpOrBomb(threatLevel))
            {
                bool bigDrop = DropBigHp(threatLevel);
                if (DropBomb())
                    itemDrop.ItemDropType = bigDrop ? ItemDropType.BIG_BOMBS : ItemDropType.BOMB;
                else
                    itemDrop.ItemDropType = bigDrop ? ItemDropType.BIG_HP : ItemDropType.SMALL_HP;
            }
            else
            {
                //Check if we drop a lower tier weapon
                int auxCurrentThreatLevel = (int)threatLevel;
                while (!DropSameTier((ThreatLevel)auxCurrentThreatLevel) && currentTier > 0)
                {
                    auxCurrentThreatLevel++;
                    currentTier--;
                }

                //if we don't drop a lower tier weapon, check if we can drop higher tier
                if (tier == currentTier)
                {
                    auxCurrentThreatLevel = (int)threatLevel;
                    while (DropHigherTier((ThreatLevel)auxCurrentThreatLevel) && currentTier < MAX_WEAPON_TIER)
                    {
                        auxCurrentThreatLevel--;
                        currentTier++;
                    }
                }

                itemDrop.ItemDropType = DropCharm() ? ItemDropType.CHARM : ItemDropType.WEAPON;
            }
        }

        itemDrop.ThreatLevel = threatLevel;
        itemDrop.Tier = currentTier;
        return itemDrop;
    }
}

public struct ItemDrop
{
    public ItemDropType ItemDropType;
    public ThreatLevel ThreatLevel;
    public int Tier;
    public bool Blessed;
}

public enum ItemDropType
{
    NONE,
    SMALL_HP,
    BIG_HP,
    BOMB,
    BIG_BOMBS,
    WEAPON,
    CHARM
}
