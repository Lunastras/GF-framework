using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MEC;

public class GfPooling : MonoBehaviour
{
    private static GfPooling Instance;

    //pools with the objects
    private Dictionary<string, PoolStruct> m_pools { get; set; } = new(16);

    private struct PoolStruct
    {

        public PoolStruct(int poolSize = 4, GameObject prefab = null)
        {
            list = new(4);
            this.prefab = prefab;
        }

        public List<GameObject> list;
        public GameObject prefab;
    }

    [SerializeField]
    private InitializationPool[] poolsToInstantiate;

    //magic value for the destruction of objects, we really just assume an object cannot be in this position unless we set it here
    //looks like bad design, but it's nicer than using a component specific for keeping track of objects in the pool
    //If these values are too high/low, the physics stop working
    private static readonly Vector3 DESTROY_POSITION = new Vector3(9997, 9997, 9997);

    //a dictionary mapping a pooled object to its pool

    private void Awake()
    {
        Instance = this;

        foreach (InitializationPool poolsToCreate in poolsToInstantiate)
        {
            if (null != poolsToCreate.gameObject)
                Pool(poolsToCreate.gameObject, poolsToCreate.instancesToPool);
        }

        poolsToInstantiate = null;
    }

    private static GameObject InternalInstantiate(GameObject objectToSpawn, string objectName, Vector3 position, Quaternion rotation, Transform parent, bool instantiateInWorldSpace = true, bool mustBeInactive = true)
    {
        //Debug.Log("called to instantiate: " + objectToSpawn.name);
        GameObject spawnedObject = null;

        PoolStruct pool;
        if (Instance.m_pools.TryGetValue(objectName, out pool))
        {
            var currentPool = pool.list;
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

            if (spawnedObject == null)
            {
                spawnedObject = GameObject.Instantiate(pool.prefab);
                spawnedObject.name = objectName;
            }
        }

        if (null == spawnedObject && objectToSpawn)
        {
            spawnedObject = GameObject.Instantiate(objectToSpawn);
            spawnedObject.name = objectName;
        }

        if (spawnedObject)
        {
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
        }
        else
        {
            Debug.LogError("GfPooling: Couldn't spawn an object of name '" + objectName + "', no pool exists of the object.");
        }

        return spawnedObject;
    }

    public static GameObject Instantiate(string prefabName, Transform parent = null, bool instantiateInWorldSpace = false, bool mustBeInactive = true)
    {
        return InternalInstantiate(null, prefabName, Vector3.zero, Quaternion.identity, parent, instantiateInWorldSpace, mustBeInactive);
    }

    public static GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, Transform parent = null, bool mustBeInactive = true)
    {
        return InternalInstantiate(null, prefabName, Vector3.zero, Quaternion.identity, parent, true, mustBeInactive);
    }


    public static GameObject Instantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false, bool mustBeInactive = true)
    {
        return InternalInstantiate(objectToSpawn, objectToSpawn.name, objectToSpawn.transform.position, objectToSpawn.transform.rotation, parent, instantiateInWorldSpace, mustBeInactive);
    }

    public static GameObject Instantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null, bool mustBeInactive = true)
    {
        return InternalInstantiate(objectToSpawn, objectToSpawn.name, position, rotation, parent, true, mustBeInactive);
    }

    /**
    *  Tries to instantiate from pool, if no more objects are in the pool, it will increase the size of the pool
    */
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false, bool mustBeInactive = true)
    {
        if (PoolSizeAvailable(objectToSpawn) == 0)
            Pool(objectToSpawn, 1);

        return InternalInstantiate(objectToSpawn, objectToSpawn.name, objectToSpawn.transform.position, objectToSpawn.transform.rotation, parent, instantiateInWorldSpace, mustBeInactive);
    }

    /**
     *  Tries to instantiate from pool, if no more objects are in the pool, it will increase the size of the pool
     */
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null, bool mustBeInactive = true, bool goOverCapacity = true)
    {
        if (PoolSizeAvailable(objectToSpawn) == 0)
            Pool(objectToSpawn, 1);

        return InternalInstantiate(objectToSpawn, objectToSpawn.name, position, rotation, parent, true, mustBeInactive);
    }



    public static void DestroyChildren(Transform obj, bool deleteSelf = false)
    {
        while (obj.childCount > 0)
            GfPooling.Destroy(obj.GetChild(0).gameObject);
    }

    public static void Destroy(GameObject objectToDestroy, float delay = 0, bool goOverCapacity = true)
    {
        InternalDestroy(objectToDestroy, false, goOverCapacity);

        if (delay > 0)
            Timing.RunCoroutine(_WaitUntilDestroy(delay, objectToDestroy, false, goOverCapacity));
        else
            InternalDestroy(objectToDestroy, false, goOverCapacity);
    }

    public static void DestroyInsert(GameObject objectToDestroy, float delay = 0, bool keepActive = false, bool goOverCapacity = true)
    {
        if (PoolSizeAvailable(objectToDestroy) == 0)
            Pool(objectToDestroy, 1, false);

        if (delay > 0)
            Timing.RunCoroutine(_WaitUntilDestroy(delay, objectToDestroy, keepActive, goOverCapacity));
        else
            InternalDestroy(objectToDestroy, keepActive, goOverCapacity);
    }

    public static IEnumerator<float> _WaitUntilDestroy(float time, GameObject objectToDestroy, bool keepActive, bool goOverCapacity)
    {
        yield return Timing.WaitForSeconds(time);
        InternalDestroy(objectToDestroy, keepActive, goOverCapacity);
    }

    private static IEnumerator<float> _AddObjectToPool(GameObject objectToDestroy, List<GameObject> currentPool)
    {
        yield return Timing.WaitForOneFrame;
        currentPool.Add(objectToDestroy); //we wait one frame before putting it in the pool because Unity doesn't like it when an object is enabled and disabled in the same frame
    }

    private static void InternalDestroy(GameObject objectToDestroy, bool keepActive, bool goOverCapacity)
    {
        bool destroyObject = true;

        PoolStruct pool;
        if (Instance.m_pools.TryGetValue(objectToDestroy.name, out pool))
        {
            var currentPool = pool.list;
            bool alreadyInPool = objectToDestroy.transform.position.Equals(DESTROY_POSITION);
            destroyObject = !alreadyInPool;
            goOverCapacity |= keepActive;

            if (!alreadyInPool && (goOverCapacity || currentPool.Count < currentPool.Capacity))
            {
                Timing.RunCoroutine(_AddObjectToPool(objectToDestroy, currentPool));
                objectToDestroy.transform.position = DESTROY_POSITION;
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
        foreach (string key in Instance.m_pools.Keys)
        {
            DestroyObjectsFromPool(Instance.m_pools[key].list, int.MaxValue);
            Instance.m_pools.Remove(key);
        }
    }

    public static void TrimPool(GameObject obj)
    {
        TrimPool(obj.name);
    }

    public static void TrimPool(string prefabName)
    {
        PoolStruct currentPool;
        if (Instance.m_pools.TryGetValue(prefabName, out currentPool))
        {
            currentPool.list.TrimExcess();
        }
    }

    public static void TrimPools()
    {
        var keysList = Instance.m_pools.Keys;
        foreach (var key in keysList)
        {
            Instance.m_pools[key].list.TrimExcess();
        }
    }

    public static void ClearPool(GameObject objectToClear, int numInstances = int.MaxValue, bool keepPoolIfEmpty = false)
    {
        ClearPool(objectToClear.name, numInstances, keepPoolIfEmpty);
    }

    public static void ClearPool(string prefabName, int numInstances = int.MaxValue, bool keepPoolIfEmpty = false)
    {
        PoolStruct pool;
        if (Instance.m_pools.TryGetValue(prefabName, out pool))
        {
            var currentPool = pool.list;
            DestroyObjectsFromPool(currentPool, numInstances);

            if (!keepPoolIfEmpty && numInstances >= currentPool.Capacity)
                Instance.m_pools.Remove(prefabName);
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

        PoolStruct pool;
        if (!Instance.m_pools.TryGetValue(objectToPool.name, out pool))
        {
            pool = new(numInstances, objectToPool);
            Instance.m_pools.Add(objectToPool.name, pool);
        }

        var currentPool = pool.list;

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
        return HasPool(objectPooled.name);
    }

    public static bool HasPool(string prefabName)
    {
        return Instance.m_pools.ContainsKey(prefabName);
    }

    public static int PoolSizeAvailable(GameObject objectPooled)
    {
        return PoolSizeAvailable(objectPooled.name);
    }

    public static int PoolSizeAvailable(string prefabName)
    {
        int ret = 0;
        if (Instance.m_pools.TryGetValue(prefabName, out var pool))
            ret = pool.list.Count;

        return ret;
    }

    public static GameObject GetObjectInPool(GameObject objectPool, int index = 0)
    {
        return GetObjectInPool(objectPool.name, index);
    }

    public static GameObject GetObjectInPool(string prefabName, int index = 0)
    {
        GameObject ret = null;
        if (Instance.m_pools.TryGetValue(prefabName, out var pool) && pool.list.Count > index)
            ret = pool.list[index];

        return ret;
    }

    public static int GetPoolCapacity(GameObject objectPooled)
    {
        return GetPoolCapacity(objectPooled.name);
    }

    public static int GetPoolCapacity(string prefabNam)
    {
        int ret = 0;
        if (Instance.m_pools.TryGetValue(prefabNam, out var pool))
            ret = pool.list.Capacity;

        return ret;
    }

    public static List<GameObject> GetPoolList(GameObject objectPooled)
    {
        return GetPoolList(objectPooled.name);
    }

    public static List<GameObject> GetPoolList(string prefabName)
    {
        Instance.m_pools.TryGetValue(prefabName, out var pool);
        return pool.list;
    }

    public static GameObject GetPrefab(GameObject objectPooled)
    {
        return GetPrefab(GetPrefab(objectPooled.name));
    }

    public static GameObject GetPrefab(string prefabName)
    {
        Instance.m_pools.TryGetValue(prefabName, out var pool);
        return pool.prefab;
    }
}

[System.Serializable]
public class InitializationPool
{
    public GameObject gameObject;
    public int instancesToPool;
}

