using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BulletEmitter : GameObjectSpawner
{
    protected StatsCharacter characterStats;

    [SerializeField]
    protected bool aimsAtTarget = true;

    [SerializeField]
    protected Vector3 rotationOffsetPerFire;

    private Transform targetTransform;

    protected override bool SpawnBehaviour()
    {
        Vector3 spawnPosition = spawnTransform != null ? spawnTransform.position : Vector3.zero;
        Vector3 targetPosition = targetTransform != null ? targetTransform.position : Vector3.zero;

        Vector3 bulletForwardVec = !aimsAtTarget && spawnTransform != null ?
                                    spawnTransform.forward
                                  : (targetPosition - spawnPosition).normalized;

        Vector3 bulletEulerAngles = Quaternion.LookRotation(bulletForwardVec).eulerAngles;
        bulletEulerAngles += rotationOffsetPerFire * objectsSpawned;

        GameObject bulletFired = GfcPooling.Instantiate(spawnerStruct.gameObjectToSpawn, spawnPosition, Quaternion.Euler(bulletEulerAngles));

        bulletFired.GetComponent<HitBoxGeneric>().characterStats = characterStats;

        return true;
    }

    public void SetTarget(Transform targetTransform)
    {
        this.targetTransform = targetTransform;
    }

    public void SetCharacterStats(StatsCharacter stats)
    {
        characterStats = stats;
    }
}
