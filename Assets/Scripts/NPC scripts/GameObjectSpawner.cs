using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameObjectSpawner
{
    [SerializeField]
    protected ObjectSpawnerStruct spawnerStruct;

    protected Transform spawnTransform = null;

    protected float timeOfNextSpawn;

    protected int objectsSpawned = 0;
    protected int timesRepeated = 0;

    protected float lengthInSeconds = -1;

    protected bool canSpawn;

    // Start is called before the first frame update

    protected virtual bool SpawnBehaviour()
    {
        GameObject spawnedObject = GfPooling.Instantiate(spawnerStruct.gameObjectToSpawn);
        spawnedObject.transform.position = spawnTransform.position;

        return true;
    }

    /**Spawns gameobjects 
    * If spawnCoolDown is 0, it will spawn all objects
    * @param forceSpawn Ignore cooldown and Spawn object
    * @return true if the spawner can still spawn, false otherwise
    */
    public bool Spawn(bool forceSpawn = false)
    {

        float currentTime = Time.time;

        if (forceSpawn || (currentTime >= timeOfNextSpawn && objectsSpawned < spawnerStruct.objectsToSpawn))
        {
            if (SpawnBehaviour())
            {
                timeOfNextSpawn = currentTime + spawnerStruct.spawnCooldown;
                objectsSpawned++;

                if (objectsSpawned >= spawnerStruct.objectsToSpawn)
                {
                    if (spawnerStruct.timesToRepeat < 0 || timesRepeated < spawnerStruct.timesToRepeat)
                    {
                        objectsSpawned = 0;
                        timeOfNextSpawn = currentTime + spawnerStruct.repeatCooldown;
                        timesRepeated++;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (spawnerStruct.spawnCooldown == 0)
                {
                    Spawn();
                }
            }
        }

        return objectsSpawned < spawnerStruct.objectsToSpawn || (spawnerStruct.timesToRepeat < 0 || timesRepeated < spawnerStruct.timesToRepeat);
    }

    public void Restart()
    {
        objectsSpawned = timesRepeated = 0;
    }

    public void Pause(float durationInSeconds = -1)
    {
        if (durationInSeconds <= 0)
        {
            /*
             * if a value is not given or is something like 0
             * then pause for 68.096 years (almost nice?)
             * surely no one will wait that long, right?
             */
            timeOfNextSpawn = Mathf.Pow(2, sizeof(float) - 1);
        }
        else
        {
            timeOfNextSpawn = Time.time + durationInSeconds;
        }
    }

    public void Resume(float delayInSeconds = -1)
    {
        timeOfNextSpawn = delayInSeconds <= 0 ? 0 : (Time.time + delayInSeconds);
    }

    public void SetSpawnTransform(Transform transform)
    {
        this.spawnTransform = transform;
    }

    public virtual float GetLength()
    {
        if (lengthInSeconds == -1)
        {
            lengthInSeconds = (spawnerStruct.timesToRepeat + 1) * (spawnerStruct.objectsToSpawn * spawnerStruct.spawnCooldown + spawnerStruct.repeatCooldown);
        }

        return lengthInSeconds;
    }
}

[System.Serializable]
public struct ObjectSpawnerStruct
{
    public ObjectSpawnerStruct(GameObject gameObjectToSpawn)
    {
        this.gameObjectToSpawn = gameObjectToSpawn;
        objectsToSpawn = 1;
        spawnCooldown = 0.1f;
        timesToRepeat = 0;
        repeatCooldown = 1.0f;
    }

    public ObjectSpawnerStruct(GameObject gameObjectToSpawn, int objectsToSpawn, float spawnCooldown, int timesToRepeat, float repeatCooldown)
    {
        this.gameObjectToSpawn = gameObjectToSpawn;
        this.objectsToSpawn = objectsToSpawn;
        this.spawnCooldown = spawnCooldown;
        this.timesToRepeat = timesToRepeat;
        this.repeatCooldown = repeatCooldown;
    }

    [SerializeField]
    public GameObject gameObjectToSpawn;

    [SerializeField]
    public int objectsToSpawn;

    [SerializeField]
    public float spawnCooldown;

    //if negative, it won't stop
    [SerializeField]
    public int timesToRepeat;

    [SerializeField]
    public float repeatCooldown;
}
