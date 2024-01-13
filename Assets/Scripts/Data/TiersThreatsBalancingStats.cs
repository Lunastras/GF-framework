using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TiersThreatsBalancingStats
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
    protected const float TIER_MULTIPLIER_HP_DAMAGE_MULTIPLIER = 1.3f;

    protected static readonly uint[] THREAT_LEVELS_HP = {
          50     //0 WISP

        , 100   //1 LESSER_FAIRY
        , 200   //2 FAIRY
        , 400   //3 GREATER_FAIRY

        , 800   //4 LESSER_YOUKAI
        , 1600  //5 YOUKAI
        , 3200  //6 GREATER_YOUKAI
        
        , 6400  //7 REVERED_SPIRIT
        , 12800  //8 DEMIGODDESS
        , 25600  //9 GODDESS

        , 51200  //10 YUKKURI
    };

    //Damage/hp multiplier between tiers

    public static float GetTierHpDamageMultiplier(uint tier)
    {
        return System.MathF.Pow(TIER_MULTIPLIER_HP_DAMAGE_MULTIPLIER, tier);
    }

    public static float GetTierHp(uint tier, ThreatLevel threatLevel)
    {
        return GetTierHpDamageMultiplier(tier) * THREAT_LEVELS_HP[(uint)threatLevel];
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

    public static ItemDrop GetItemDrop(StatsCharacter statsCharacter)
    {
        return GetItemDrop(statsCharacter.GetThreatDetails());
    }

    public static ItemDrop GetItemDrop(ThreatDetails threatDetails)
    {
        return GetItemDrop(threatDetails.Tier, threatDetails.ThreatLevel);
    }

    public static ItemDrop GetItemDrop(uint tier, ThreatLevel threatLevel)
    {
        ItemDrop itemDrop = default;
        uint currentTier = tier;

        itemDrop.Blessed = DropBlessed(threatLevel);

        if (DropAnything(threatLevel))
        {
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

        itemDrop.Tier = currentTier;
        return itemDrop;
    }
}

public struct ItemDrop
{
    public ItemDropType ItemDropType;
    public bool Blessed;
    public uint Tier;
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
