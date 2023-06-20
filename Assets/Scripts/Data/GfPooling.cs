using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GfPooling : MonoBehaviour
{
    private static GfPooling instance;

    //pools with the objects
    private Dictionary<string, List<GameObject>> pools { get; set; }

    private Transform mainParent = null;

    [SerializeField]
    private InitializationPool[] poolsToInstantiate;

    //magic value for the destruction of objects, we really just assume an object cannot be in this position unless we set it here
    //looks like bad design, but it's nicer than using a component specific for keeping track of objects in the pool
    //If these values are too high/low, the physics stop working
    private static readonly Vector3 DESTROY_POSITION = new Vector3(9997, 9997, 9997);

    //a dictionary mapping a pooled object to its pool

    private void Awake()
    {
        instance = this;

        pools = new Dictionary<string, List<GameObject>>();

        mainParent = new GameObject().transform;
        mainParent.SetParent(transform);
        mainParent.name = "Pools Parent";
        foreach (InitializationPool poolsToCreate in poolsToInstantiate)
        {
            if (null != poolsToCreate.gameObject)
                Pool(poolsToCreate.gameObject, poolsToCreate.instancesToPool);
        }

        poolsToInstantiate = null;
    }

    private static GameObject InternalInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent, bool instantiateInWorldSpace = true, bool mustBeInactive = true)
    {
        //Debug.Log("called to instantiate: " + objectToSpawn.name);
        GameObject spawnedObject = null;

        List<GameObject> currentPool;
        if (instance.pools.TryGetValue(objectToSpawn.name, out currentPool))
        {
            int count = currentPool.Count;
            if (--count >= 0)
            {
                int i = count;
                if (mustBeInactive)
                    while (i >= 0 && currentPool[i].activeSelf) --i; //go through the list until an inactive element is found

                if (i >= 0)
                {
                    spawnedObject = currentPool[count];
                    spawnedObject.SetActive(true);

                    currentPool[i] = currentPool[count];
                    currentPool.RemoveAt(count);
                }
            }
        }

        if (null == spawnedObject)
        {
            spawnedObject = GameObject.Instantiate(objectToSpawn);
            spawnedObject.name = objectToSpawn.name;
        }

        spawnedObject.transform.SetParent(parent);

        if (instantiateInWorldSpace)
        {
            spawnedObject.transform.position = position;
            spawnedObject.transform.rotation = rotation;
        }
        else
        {
            spawnedObject.transform.localPosition = position;
            spawnedObject.transform.localRotation = rotation;
        }

        return spawnedObject;
    }


    public static GameObject Instantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false, bool mustBeInactive = true)
    {
        return InternalInstantiate(objectToSpawn, objectToSpawn.transform.position, objectToSpawn.transform.rotation, parent, instantiateInWorldSpace, mustBeInactive);
    }

    public static GameObject Instantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null, bool mustBeInactive = true)
    {
        return InternalInstantiate(objectToSpawn, position, rotation, parent, true, mustBeInactive);
    }

    /**
    *  Tries to instantiate from pool, if no more objects are in the pool, it will increase the size of the pool
    */
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false, bool mustBeInactive = true)
    {
        if (PoolSizeAvailable(objectToSpawn) == 0)
            Pool(objectToSpawn, 1);

        return InternalInstantiate(objectToSpawn, objectToSpawn.transform.position, objectToSpawn.transform.rotation, parent, instantiateInWorldSpace, mustBeInactive);
    }

    /**
     *  Tries to instantiate from pool, if no more objects are in the pool, it will increase the size of the pool
     */
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null, bool mustBeInactive = true, bool goOverCapacity = true)
    {
        if (PoolSizeAvailable(objectToSpawn) == 0)
            Pool(objectToSpawn, 1);

        return InternalInstantiate(objectToSpawn, position, rotation, parent, true, mustBeInactive);
    }

    //lmao good naming
    public static void DestroyChildren(GameObject obj, bool deleteSelf = false)
    {
        DestroyChildren(obj.transform);
    }

    public static void DestroyChildren(Transform obj, bool deleteSelf = false)
    {
        while (obj.childCount > 0)
            GfPooling.Destroy(obj.GetChild(0).gameObject);
    }

    public static void Destroy(GameObject objectToDestroy, bool goOverCapacity = true)
    {
        InternalDestroy(objectToDestroy, false, goOverCapacity);
    }

    public static void DestroyInsert(GameObject objectToDestroy, bool keepActive = false, bool goOverCapacity = true)
    {
        if (PoolSizeAvailable(objectToDestroy) == 0)
            Pool(objectToDestroy, 1, false);

        InternalDestroy(objectToDestroy, keepActive, goOverCapacity);
    }

    private static void InternalDestroy(GameObject objectToDestroy, bool keepActive, bool goOverCapacity)
    {
        bool destroyObject = true;

        if (instance.pools.TryGetValue(objectToDestroy.name, out List<GameObject> currentPool))
        {
            bool alreadyInPool = objectToDestroy.transform.position.Equals(DESTROY_POSITION);
            destroyObject = !alreadyInPool;
            goOverCapacity |= keepActive;

            if (!alreadyInPool && (goOverCapacity || currentPool.Count < currentPool.Capacity))
            {
                objectToDestroy.transform.position = DESTROY_POSITION;
                currentPool.Add(objectToDestroy);
                destroyObject = false;
                alreadyInPool = true;
            }

            objectToDestroy.SetActive(!alreadyInPool || keepActive);
        }

        if (destroyObject) GameObject.Destroy(objectToDestroy);
    }

    private static void DestroyObjectsFromPool(List<GameObject> objects, int numInstances = -1)
    {
        int count = objects.Count;
        int desiredCapacity = count - numInstances;
        while (0 < count && -1 != --numInstances)
        {
            --count;
            GameObject.Destroy(objects[count]);
            objects.RemoveAt(count);
        }
    }

    public static void ClearAll()
    {
        foreach (string key in instance.pools.Keys)
        {
            DestroyObjectsFromPool(instance.pools[key], int.MaxValue);
            instance.pools.Remove(key);
        }
    }

    public static void TrimPool(GameObject obj)
    {
        if (null != obj)
        {
            List<GameObject> currentPool;
            if (instance.pools.TryGetValue(obj.name, out currentPool))
            {
                currentPool.TrimExcess();
            }
        }
    }

    public static void TrimPools()
    {
        var keysList = instance.pools.Keys;
        foreach (var key in keysList)
        {
            instance.pools[key].TrimExcess();
        }
    }

    public static void ClearPool(GameObject objectToClear, int numInstances = int.MaxValue, bool keepPoolIfEmpty = false)
    {
        if (numInstances <= 0)
            return;

        if (null != objectToClear)
        {
            List<GameObject> currentPool;
            if (instance.pools.TryGetValue(objectToClear.name, out currentPool))
            {
                DestroyObjectsFromPool(currentPool, numInstances);

                if (!keepPoolIfEmpty && numInstances >= currentPool.Capacity)
                    instance.pools.Remove(objectToClear.name);
            }
        }
    }

    /** Pool
    * Add objects to a pool. If the pool already exists, it will increase its size.
    * If it doesn't, it will create a new pool with the given size.
    * If the value is negative, it will reduce the size of the pool and remove  
    * the given ammount of gameObjects.*/
    public static void Pool(GameObject objectToPool, int numInstances, bool instantiateObjects = true, bool goOverCapacity = true)
    {
        if (numInstances < 0)
            ClearPool(objectToPool, -numInstances);
        else if (numInstances == 0)
            return;
        //Debug.Log("Pool request for " + numInstances + " " + objectToPool.name);

        List<GameObject> currentPool;
        if (!instance.pools.TryGetValue(objectToPool.name, out currentPool))
        {
            currentPool = new List<GameObject>(numInstances);
            instance.pools.Add(objectToPool.name, currentPool);
        }

        while (instantiateObjects && 0 <= --numInstances && (goOverCapacity || currentPool.Count < currentPool.Capacity))
        {
            GameObject obj = GameObject.Instantiate(objectToPool);
            obj.SetActive(false);
            obj.name = objectToPool.name;
            currentPool.Add(obj);
        }
    }

    public static bool HasPool(GameObject objectPooled)
    {
        return instance.pools.ContainsKey(objectPooled.name);
    }

    public static int PoolSizeAvailable(GameObject objectPooled)
    {
        int ret = 0;
        List<GameObject> pool;
        if (instance.pools.TryGetValue(objectPooled.name, out pool))
            ret = pool.Count;

        return ret;
    }

    public static GameObject GetObjectInPool(GameObject objectPool, int index = 0)
    {
        List<GameObject> pool;
        GameObject ret = null;
        if (instance.pools.TryGetValue(objectPool.name, out pool) && pool.Count > index)
            ret = pool[index];

        return ret;
    }

    public static int GetPoolCapacity(GameObject objectPooled)
    {
        List<GameObject> pool;
        int ret = 0;
        if (instance.pools.TryGetValue(objectPooled.name, out pool))
            ret = pool.Capacity;

        return ret;
    }

    public static List<GameObject> GetPoolList(GameObject objectPooled)
    {
        List<GameObject> pool;
        instance.pools.TryGetValue(objectPooled.name, out pool);
        return pool;
    }
}

[System.Serializable]
public class InitializationPool
{
    public GameObject gameObject;
    public int instancesToPool;
}

