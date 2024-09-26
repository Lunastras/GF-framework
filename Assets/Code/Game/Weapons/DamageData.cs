using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public struct DamageData : INetworkSerializeByMemcpy
{
    public DamageData(float aDamage
    , Vector3 aDamagePosition
    , Vector3 aDamageNormal
    , DamageType aDamageType = DamageType.NORMAL
    , bool aHasEnemyNetworkId = false
    , ulong aEnemyNetworkId = 0
    , int aWeaponLoadoutIndex = -1
    , int aWeaponIndex = -1)
    {
        Damage = aDamage;
        DamageType = aDamageType;
        DamagePosition = aDamagePosition;
        DamageNormal = aDamageNormal;
        HasEnemyNetworkId = aHasEnemyNetworkId;
        EnemyNetworkId = aEnemyNetworkId;
        WeaponLoadoutIndex = aWeaponLoadoutIndex;
        WeaponIndex = aWeaponIndex;
    }

    public float Damage;
    public Vector3 DamagePosition;
    public Vector3 DamageNormal;
    public DamageType DamageType;
    public ulong EnemyNetworkId;
    public bool HasEnemyNetworkId;
    public int WeaponLoadoutIndex;
    public int WeaponIndex;
}