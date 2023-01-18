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

    private static readonly Vector3 DESTROY_POSITION = new Vector3(99999999, 99999999, 99999999);

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

        if (instance != null && objectToSpawn != null)
        {
            List<GameObject> currentPool;
            if(instance.pools.TryGetValue(objectToSpawn.name, out currentPool))
            {
                int count = currentPool.Count; 
                if (--count >= 0)
                {
                    int i = count;
                    if(mustBeInactive) 
                        while (i >= 0 && currentPool[i--].activeSelf); //go through the list until an inactive element is found

                    if(i >= 0) 
                    {
                        spawnedObject = currentPool[count];
                        spawnedObject.SetActive(true);

                        if(i != count) 
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
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null, bool mustBeInactive = true)
    {
        if (PoolSizeAvailable(objectToSpawn) == 0)
            Pool(objectToSpawn, 1);

        return InternalInstantiate(objectToSpawn, position, rotation, parent, true, mustBeInactive);
    }

    private static void InternalDestroy(GameObject objectToDestroy, bool keepActive, bool forceDestroy)
    {
        bool isActive = objectToDestroy.activeSelf;
        bool alreadyDestroyed = !isActive || objectToDestroy.transform.position.Equals(DESTROY_POSITION);
        if (null != objectToDestroy && !alreadyDestroyed)
        {
            forceDestroy &= !keepActive;

            bool destroyObject = true;
            if (keepActive && !instance.pools.ContainsKey(objectToDestroy.name))
                Pool(objectToDestroy, 1, false);

            if (!forceDestroy && instance.pools.TryGetValue(objectToDestroy.name, out List<GameObject> currentPool))
            {
                if (currentPool.Count < currentPool.Capacity)
                {
                    objectToDestroy.SetActive(keepActive);
                    currentPool.Add(objectToDestroy);
                    destroyObject = false;

                    if (keepActive)
                        objectToDestroy.transform.position = DESTROY_POSITION;
                }
            }

            if (destroyObject)
            {
                // Debug.Log("Forced destroyed: " + objectToDestroy.name);
                objectToDestroy.transform.SetParent(null);
                objectToDestroy.SetActive(false);
                GameObject.Destroy(objectToDestroy);
            }
        }
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

    public static void Destroy(GameObject objectToDestroy, bool forceDestroy = false)
    {
        if (null != objectToDestroy)
            InternalDestroy(objectToDestroy, false, forceDestroy);
    }

    public static void DestroyInsert(GameObject objectToDestroy, bool forceDestroy = false, bool keepActive = false)
    {
        if (null != objectToDestroy)
        {
            if (PoolSizeAvailable(objectToDestroy) == 0)
                Pool(objectToDestroy, 1, false);

            InternalDestroy(objectToDestroy, keepActive, forceDestroy);
        }
    }

    public static void FillPool(GameObject pooledObject, int ammount = -1)
    {
        if (ammount < -1 || null == pooledObject) return;

        List<GameObject> currentPool;
        if(instance.pools.TryGetValue(pooledObject.name, out currentPool))
        {

            while (currentPool.Count < currentPool.Capacity && -1 != --ammount)
            {
                GameObject obj = GameObject.Instantiate(pooledObject);
                obj.SetActive(false);
                currentPool.Add(obj);
                obj.name = pooledObject.name;
            }

        }
        else if (ammount > 0)
        {
            Pool(pooledObject, ammount);
        }
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

    public static void ClearAll() {
        foreach (string key in instance.pools.Keys)
        {
            DestroyObjectsFromPool(instance.pools[key], int.MaxValue);
            instance.pools.Remove(key);
        }
    }

    public static void TrimPool(GameObject obj, bool keepPoolIfEmpty = false)
    {
        if (null != obj)
        {
            List<GameObject> currentPool;
            if(instance.pools.TryGetValue(obj.name, out currentPool))
            {
                if(currentPool.Count == 0 && !keepPoolIfEmpty) 
                    instance.pools.Remove(obj.name);
                else 
                    currentPool.TrimExcess();
            }
        }
    }

    public static void ClearPool(GameObject objectToClear, int numInstances = int.MaxValue, bool keepPoolIfEmpty = false)
    {
        if (numInstances <= 0)
            return;

        if (null != objectToClear)
        {
            List<GameObject> currentPool;
            if(instance.pools.TryGetValue(objectToClear.name, out currentPool))
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
    * the given ammount of gameObjects.
*/
    public static void Pool(GameObject objectToPool, int numInstances, bool instantiateObjects = true)
    {
        if (numInstances < 0)
            ClearPool(objectToPool, -numInstances);
        else if (numInstances == 0)
            return;
        //Debug.Log("Pool request for " + numInstances + " " + objectToPool.name);

        List<GameObject> currentPool;
        if(instance.pools.TryGetValue(objectToPool.name, out currentPool))
        {
            currentPool = instance.pools[objectToPool.name];
            int capacity = currentPool.Capacity;
            //if we are over the instantiating limit, then double capacity
            if(currentPool.Capacity == currentPool.Count) currentPool.Capacity += capacity; 
        }
        else
        {
            currentPool = new List<GameObject>(numInstances);
            instance.pools.Add(objectToPool.name, currentPool);
        }

        while (instantiateObjects && 0 <= --numInstances)
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
        if(instance.pools.TryGetValue(objectPooled.name, out pool)) 
           ret = pool.Count;

        return ret;
    }

    public static GameObject GetObjectInPool(GameObject objectPool, int index = 0)
    {
        List<GameObject> pool;
        GameObject ret = null;
        if(instance.pools.TryGetValue(objectPool.name, out pool) && pool.Count > index) 
           ret = pool[index];
        
        return ret;
    }

    public static int PoolSizeMax(GameObject objectPooled)
    {
        List<GameObject> pool;
        int ret = 0;
        if(instance.pools.TryGetValue(objectPooled.name, out pool)) 
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

