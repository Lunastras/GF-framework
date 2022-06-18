using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GfPooling : MonoBehaviour
{
    private static GfPooling instance;

    //pools with the objects
    private Dictionary<string, PoolClass> pools { get; set; }

    private Transform mainParent = null;

    [SerializeField]
    private InitializationPool[] poolsToInstantiate;

    //a dictionary mapping a pooled object to its pool

    private void Awake()
    {
        instance = this;

        pools = new Dictionary<string, PoolClass>();

        mainParent = new GameObject().transform;
        mainParent.SetParent(transform);
        mainParent.name = "Pools Parent";
        foreach (InitializationPool poolsToCreate in poolsToInstantiate)
        {
            if (null != poolsToCreate.gameObject)
                ResizePool(poolsToCreate.gameObject, poolsToCreate.instancesToPool);
        }

        poolsToInstantiate = null;
    }

    private static GameObject InternalInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent, bool instantiateInWorldSpace = true)
    {
        //Debug.Log("called to instantiate: " + objectToSpawn.name);
        GameObject spawnedObject = null;

        if (instance != null && objectToSpawn != null)
        {
            bool forceInstantiate = true;
            if (instance.pools.ContainsKey(objectToSpawn.name))
            {
                if (instance.pools[objectToSpawn.name].parent.childCount > 0)
                {
                    spawnedObject = instance.pools[objectToSpawn.name].parent.GetChild(0).gameObject;
                    spawnedObject.SetActive(true);
                    //  Debug.Log("Instantiated from pool: " + spawnedObject);

                    forceInstantiate = false;
                }
            }
            if (forceInstantiate)
            {
                //Debug.Log("Forced instantiated: " + objectToSpawn);
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


    public static GameObject Instantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false)
    {
        if (objectToSpawn != null)
            return InternalInstantiate(objectToSpawn, objectToSpawn.transform.position, objectToSpawn.transform.rotation, parent, instantiateInWorldSpace);

        return null;
    }

    public static GameObject Instantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return InternalInstantiate(objectToSpawn, position, rotation, parent, true);
    }

    /**
    *  Tries to instantiate from pool, if no more objects are in the pool, it will increase the size of the pool
    */
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false)
    {
        if (PoolSizeAvailable(objectToSpawn) == 0)
            Pool(objectToSpawn, 1);

        return InternalInstantiate(objectToSpawn, objectToSpawn.transform.position, objectToSpawn.transform.rotation, parent, instantiateInWorldSpace);
    }

    /**
     *  Tries to instantiate from pool, if no more objects are in the pool, it will increase the size of the pool
     */
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (PoolSizeAvailable(objectToSpawn) == 0)
            Pool(objectToSpawn, 1);

        return InternalInstantiate(objectToSpawn, position, rotation, parent, true);
    }

    private static void InternalDestroy(GameObject objectToDestroy, bool forceDestroy = false)
    {
        if (null != objectToDestroy)
        {
            bool destroyObject = true;
            forceDestroy = false;

            if (!forceDestroy && instance.pools.ContainsKey(objectToDestroy.name))
            {
                PoolClass currentPool = instance.pools[objectToDestroy.name];
                if (currentPool.parent.childCount < currentPool.capacity)
                {
                    //  Debug.Log("Put into pool: " + objectToDestroy.name);
                    objectToDestroy.SetActive(false);
                    objectToDestroy.transform.SetParent(currentPool.parent);
                    destroyObject = false;
                }
                else
                {
                    destroyObject = objectToDestroy.transform.parent != currentPool.parent;
                    //Debug.Log("Pool " + currentPool.parent.name + " is full, it has numofchildren: " + currentPool.parent.childCount + " the current object is " + objectToDestroy.name);
                }
            }
            else
            {
                //Debug.Log("Object not found in dictionary");
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
    public static void DestroyChildren(GameObject obj)
    {
        DestroyChildren(obj.transform);
    }

    public static void DestroyChildren(Transform obj)
    {
        while (obj.childCount > 0)
            GfPooling.Destroy(obj.GetChild(0).gameObject);
    }

    private static IEnumerator DestroyCoroutine(GameObject obj, float delay, bool forceDestroy = false)
    {
        yield return new WaitForSeconds(delay);

        InternalDestroy(obj, forceDestroy);
    }

    private static void Destroy(GameObject objectToDestroy, float delay, bool forceDestroy = false)
    {
        if (null != objectToDestroy)
            instance.StartCoroutine(DestroyCoroutine(objectToDestroy, delay, forceDestroy));
    }

    public static void Destroy(GameObject objectToDestroy, bool forceDestroy = false)
    {
        if (null != objectToDestroy)
            InternalDestroy(objectToDestroy, forceDestroy);
    }

    public static void FillPool(GameObject pooledObject, int ammount = -1)
    {
        if (ammount < -1 || null == pooledObject) return;

        if (instance.pools.ContainsKey(pooledObject.name))
        {
            PoolClass currentPool = instance.pools[pooledObject.name];

            while (currentPool.parent.childCount < currentPool.capacity && -1 != --ammount)
            {
                GameObject obj = GameObject.Instantiate(pooledObject);
                obj.SetActive(false);
                obj.transform.SetParent(currentPool.parent);
                obj.name = pooledObject.name;
            }

        }
        else if (ammount > 0)
        {
            Pool(pooledObject, ammount);
        }
    }

    private static void DestroyObjectsFromPool(Transform pool, int numInstances = -1)
    {
        while (0 < pool.childCount && -1 != --numInstances)
        {
            GameObject.Destroy(pool.GetChild(0).gameObject);
        }
    }

    public static void ClearPool(GameObject objectToClear = null, int numInstances = int.MaxValue, bool keepPool = false)
    {
        if (numInstances <= 0)
            return;

        if (null != objectToClear)
        {
            if (instance.pools.ContainsKey(objectToClear.name))
            {
                PoolClass currentPool = instance.pools[objectToClear.name];
                DestroyObjectsFromPool(currentPool.parent, numInstances);
                currentPool.capacity -= numInstances;

                if (currentPool.capacity <= 0 && !keepPool)
                {
                    GameObject.Destroy(currentPool.parent.gameObject);
                    instance.pools.Remove(objectToClear.name);
                }
            }
        }
        else
        {
            //remove everything;
            foreach (string key in instance.pools.Keys)
            {
                DestroyObjectsFromPool(instance.pools[key].parent, numInstances);
                instance.pools.Remove(key);
            }
        }
    }

    /** Pool
    * Add objects to a pool. If the pool already exists, it will increase its size.
    * If it doesn't, it will create a new pool with the given size.
    * If the value is negative, it will reduce the size of the pool and remove  
    * the given ammount of gameObjects.
*/
    public static void Pool(GameObject objectToPool, int numInstances)
    {
        if (numInstances < 0)
        {
            ClearPool(objectToPool, -numInstances);
        }
        else if (numInstances == 0)
        {
            return;
        }

        //Debug.Log("Pool request for " + numInstances + " " + objectToPool.name);

        PoolClass currentPool = null;

        if (!instance.pools.ContainsKey(objectToPool.name))
        {
            currentPool = new PoolClass();

            instance.pools.Add(objectToPool.name, currentPool);
            currentPool.capacity = numInstances;

            Transform poolParent = new GameObject().transform;
            poolParent.SetParent(instance.mainParent);
            poolParent.name = objectToPool.name + " Pool";

            currentPool.parent = poolParent;
        }
        else
        {
            currentPool = instance.pools[objectToPool.name];
            currentPool.capacity += numInstances;
        }

        while (0 <= --numInstances)
        {
            GameObject obj = GameObject.Instantiate(objectToPool);
            obj.SetActive(false);
            obj.transform.SetParent(currentPool.parent);
            obj.name = objectToPool.name;
            // Debug.Log("creating an object for pool: " + objectToPool.name + " objects in pool: " + currentPool.parent.childCount);
        }
    }

    public static void ResizePool(GameObject obj, int newSize, bool keepPool = false)
    {
        newSize = Mathf.Max(0, newSize);

        // Debug.Log("Resize pool request for " + obj + " to " + (newSize));

        if (instance.pools.ContainsKey(obj.name))
        {
            PoolClass currentPool = instance.pools[obj.name];
            if (newSize > currentPool.capacity)
            {
                Pool(obj, newSize - currentPool.capacity);
            }
            else if (newSize < currentPool.capacity)
            {
                ClearPool(obj, currentPool.capacity - newSize, keepPool);
            }
        }
        else
        {
            // Debug.Log("No pool for " + obj + ", creating it with size: " + (newSize));
            Pool(obj, newSize);
        }
    }

    public static bool HasPool(GameObject objectPooled)
    {
        return instance.pools.ContainsKey(objectPooled.name);
    }

    public static int PoolSizeAvailable(GameObject objectPooled)
    {
        return instance.pools.ContainsKey(objectPooled.name) ? instance.pools[objectPooled.name].parent.childCount : 0;
    }

    public static int PoolSizeMax(GameObject objectPooled)
    {
        return instance.pools.ContainsKey(objectPooled.name) ? instance.pools[objectPooled.name].capacity : 0;
    }

    public static Transform GetPoolParent(GameObject objectPooled)
    {
        if (instance.pools.ContainsKey(objectPooled.name))
        {
            return instance.pools[objectPooled.name].parent;
        }

        return null;
    }
}

[System.Serializable]
public class InitializationPool
{
    public GameObject gameObject;
    public int instancesToPool;
}

internal class PoolClass
{
    public int capacity;
    public Transform parent;
}

