using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MEC;
using Unity.Collections;
using System;
using Unity.Plastic.Antlr3.Runtime;
using UnityEngine.SceneManagement;

public class GfcPooling : MonoBehaviour
{
    private static GfcPooling Instance;

    [SerializeField] private InitializationPool[] poolsToInstantiate;

    //pools with the objects
    private Dictionary<string, PoolStruct> m_pools { get; set; } = new(16);

    private GfcStringBuffer m_sharedGfStringBuffer = new(15);

    private List<Component> m_sharedSomponentsBuffer = new(16);

    private List<string> m_stringKeysBuffer = new(8);

    public static bool ExitingPlaymode = false;

    private struct PoolStruct
    {
        public GameObject Prefab;
        public List<GameObject> List;
        public bool PermanentObjects;

        public PoolStruct(int poolSize, GameObject prefab = null)
        {
            List = null;
            if (poolSize > 0)
                List = new(poolSize);

            this.Prefab = prefab;
            PermanentObjects = false;
        }

        public PoolStruct(GameObject prefab = null)
        {
            List = null;
            this.Prefab = prefab;
            PermanentObjects = false;
        }
    }

    //magic value for the destruction of objects, we really just assume an object cannot be in this position unless we set it here
    //looks like bad design, but it's nicer than using a component specific for keeping track of objects in the pool
    //If these values are too high/low, the physics stop working
    private static readonly Vector3 DESTROY_POSITION = new(99997, 99997, 99997);
    private static readonly Vector3 DEACTIVATE_NEXT_FRAME_POSITION = new(-99997, 99997, 99997);

    private void Awake()
    {
        if (Instance != this)
            Destroy(Instance);
        Instance = this;

        foreach (InitializationPool poolsToCreate in poolsToInstantiate)
        {
            if (null != poolsToCreate.gameObject)
                Pool(poolsToCreate.gameObject, poolsToCreate.instancesToPool);
        }

        poolsToInstantiate = null;
        ExitingPlaymode = false;
        EditorApplication.playModeStateChanged += ModeChanged;
    }

    void ModeChanged(PlayModeStateChange aStateChange)
    {
        ExitingPlaymode = aStateChange == PlayModeStateChange.ExitingPlayMode;
        if (aStateChange == PlayModeStateChange.EnteredEditMode)
            EditorApplication.playModeStateChanged -= ModeChanged;
    }

    public static GfcStringBuffer GfcStringBuffer
    {
        get
        {
            GfcStringBuffer buffer = null;

            if (Instance)
                if (Instance.m_sharedGfStringBuffer.Length == 0)
                    buffer = Instance.m_sharedGfStringBuffer;
                else
                    Debug.LogWarning("The string buffer was not empty when retrieved, is it currently used somewhere else or did someone forget to clear it after using it? The text present is: " + Instance.m_sharedGfStringBuffer.StringBuffer);

            return buffer ?? new();
        }
    }

    public static List<Component> ComponentsBuffer
    {
        get
        {
            Debug.Assert(Instance);
            Debug.Assert(Instance.m_sharedSomponentsBuffer.Count == 0, "The components buffer was not empty when queried, it had a count of " + Instance.m_sharedSomponentsBuffer.Count);
            Instance.m_sharedSomponentsBuffer.Clear();
            return Instance.m_sharedSomponentsBuffer;
        }
    }

    private static GameObject InternalInstantiate(GameObject objectToSpawn, string objectName, Vector3 position, Quaternion rotation, Transform parent, bool instantiateInWorldSpace = true, bool mustBeInactive = true, Transform transformReference = null)
    {
        GameObject spawnedObject = null;

        //Debug.Log("called to instantiate: " + objectToSpawn.name);
        GetNameWithoutNumberParenthesis(ref objectName);
        if (Instance && Instance.m_pools.TryGetValue(objectName, out PoolStruct pool))
        {
            var currentPool = pool.List;
            if (null != currentPool && currentPool.Count > 0)
            {
                int lastIndex = currentPool.Count - 1;

                int i = lastIndex;
                if (mustBeInactive)
                    while (currentPool[i] && (!currentPool[i].activeSelf ^ !ObjectQueuedForPool(currentPool[i])) && --i >= 0) ; //go through the list until an inactive element is found //changes don't work

                if (i >= 0)
                {
                    spawnedObject = currentPool[i];
                    currentPool.RemoveAtSwapBack(i);
                }
            }

            if (spawnedObject) spawnedObject.SetActiveGf(true);
        }

        if (null == spawnedObject && objectToSpawn)
        {
            spawnedObject = GameObject.Instantiate(objectToSpawn);
            spawnedObject.name = objectName;
        }

        if (spawnedObject)
        {
            spawnedObject.transform.SetParent(parent);
            locSetObjectPositionAndRotation(spawnedObject.transform, position, rotation, !instantiateInWorldSpace, transformReference);
        }

        Debug.Assert(spawnedObject);
        return spawnedObject;
    }

    public static bool ObjectIsInPool(GameObject aGameObject) { return aGameObject.transform.position.Equals(DESTROY_POSITION) || aGameObject.transform.position.Equals(DEACTIVATE_NEXT_FRAME_POSITION); }
    public static bool ObjectQueuedForPool(GameObject aGameObject) { return aGameObject.transform.position.Equals(DEACTIVATE_NEXT_FRAME_POSITION); }

    public static GameObject Instantiate(string prefabName, Transform parent = null, bool instantiateInWorldSpace = false, bool mustBeInactive = true)
    {
        return InternalInstantiate(null, prefabName, Vector3.zero, Quaternion.identity, parent, instantiateInWorldSpace, mustBeInactive);
    }

    public static GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, Transform parent = null, bool mustBeInactive = true)
    {
        return InternalInstantiate(null, prefabName, position, rotation, parent, true, mustBeInactive);
    }

    public static GameObject Instantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false, bool mustBeInactive = true)
    {
        return InternalInstantiate(objectToSpawn, objectToSpawn.name, objectToSpawn.transform.position, objectToSpawn.transform.rotation, parent, instantiateInWorldSpace, mustBeInactive, objectToSpawn.transform);
    }

    public static GameObject Instantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null, bool mustBeInactive = true)
    {
        return InternalInstantiate(objectToSpawn, objectToSpawn.name, position, rotation, parent, true, mustBeInactive, objectToSpawn.transform);
    }

    /**
    *  Tries to instantiate from pool, if no more objects are in the pool, it will increase the size of the pool
    */
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false, bool mustBeInactive = true)
    {
        if (PoolSizeAvailable(objectToSpawn) == 0)
            Pool(objectToSpawn, 1);

        return Instantiate(objectToSpawn, parent, instantiateInWorldSpace, mustBeInactive);
    }

    /**
     *  Tries to instantiate from pool, if no more objects are in the pool, it will increase the size of the pool
     */
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null, bool mustBeInactive = true)
    {
        if (PoolSizeAvailable(objectToSpawn) == 0)
            Pool(objectToSpawn, 1);

        return Instantiate(objectToSpawn, position, rotation, parent, mustBeInactive);
    }

    public static void DestroyChildren(Transform obj, bool deleteSelf = false, bool anInsertInPool = false, bool aKeepActive = false, bool anUseTransition = true)
    {
        int currentChildIndex = 0;
        int childCount = obj.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform currentChild = obj.GetChild(currentChildIndex);
            if (anInsertInPool)
                GfcPooling.DestroyInsert(currentChild.gameObject, 0, aKeepActive, true, anUseTransition);
            else
                GfcPooling.Destroy(currentChild.gameObject, 0, true, anUseTransition);

            //in case the object has an destroy animation and is still parented, go to the next index
            if (currentChild && currentChild.parent == obj)
                currentChildIndex++;
        }

        if (deleteSelf)
            GfcPooling.Destroy(obj.gameObject);
    }

    private IEnumerator<float> _DeactivateOneFrameLate(GameObject aGameObject)
    {
        yield return Timing.WaitForOneFrame; //wait one frame to see if we have to spawn this object, frames where we destroy objects are also frames where its likely we will spawn objects
        if (ObjectQueuedForPool(aGameObject)) //if it was moved out of the pool, don't deactivate
        {
            aGameObject.SetActive(false);
            yield return Timing.WaitForOneFrame; //wait one frame before moving into destroyed position, we do this because objects cannot be activated the turn they were inactivated
            locSetObjectPosition(aGameObject.transform, DESTROY_POSITION, false);
        }
    }

    private static CoroutineHandle InternalDestroy(GameObject objectToDestroy, float delay, bool keepActive, bool goOverCapacity, bool anUseTransition, bool anInsertInPool)
    {
        CoroutineHandle handle = default;

        if (!ExitingPlaymode)
        {
            if (anInsertInPool && PoolSizeAvailable(objectToDestroy) == 0)
                Pool(objectToDestroy, 1, false);

            GfcTransitionActive transitionActive = null;
            anUseTransition = anUseTransition && objectToDestroy.TryGetComponent(out transitionActive);

            if (delay > 0 || anUseTransition)
                handle = Timing.RunCoroutine(_WaitUntilDestroy(delay, objectToDestroy, false, goOverCapacity, transitionActive));
            else
                InternalDestroy(objectToDestroy, keepActive, goOverCapacity);
        }

        return handle;
    }

    public static CoroutineHandle Destroy(GameObject objectToDestroy, float delay = 0, bool goOverCapacity = true, bool anUseTransition = true)
    {
        return InternalDestroy(objectToDestroy, delay, false, goOverCapacity, anUseTransition, false);
    }

    public static CoroutineHandle DestroyInsert(GameObject objectToDestroy, float delay = 0, bool keepActive = false, bool goOverCapacity = true, bool anUseTransition = true)
    {
        return InternalDestroy(objectToDestroy, delay, keepActive, goOverCapacity, anUseTransition, true);
    }

    public static IEnumerator<float> _WaitUntilDestroy(float time, GameObject objectToDestroy, bool keepActive, bool goOverCapacity, GfcTransitionActive aTransitionActive)
    {
        yield return Timing.WaitForSeconds(time);
        if (aTransitionActive)
            yield return Timing.WaitUntilDone(aTransitionActive.SetActive(false, keepActive));

        InternalDestroy(objectToDestroy, keepActive, goOverCapacity);
    }

    private static IEnumerator<float> _AddObjectToPool(GameObject objectToDestroy, List<GameObject> currentPool)
    {
        yield return Timing.WaitForOneFrame;
        //we wait one frame before putting it in the pool because Unity doesn't like it when an object is enabled and disabled in the same frame
    }

    protected void OnDestroy()
    {
        Instance = null;
    }

    protected static void locSetObjectPosition(Transform obj, Vector3 position, bool local)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb)
        {
            if (local)
            {
                Transform parent = obj.parent;
                if (parent)
                    GfcTools.Add(ref position, parent.position);
            }

            rb.position = position;
        }

        if (local)
            obj.localPosition = position;
        else
            obj.position = position;
    }

    protected static void locSetObjectPositionAndRotation(Transform aTransform, Vector3 aPosition, Quaternion aRotation, bool anIsLocal, Transform aTransformReference)
    {
        Rigidbody rb = aTransform.GetComponent<Rigidbody>();
        if (aTransformReference)
        {
            aTransform.CopyTransformData(aTransformReference, anIsLocal);
        }
        else
        {
            if (rb)
            {
                if (anIsLocal)
                {
                    Transform parent = aTransform.parent;
                    if (parent)
                    {
                        GfcTools.Add(ref aPosition, parent.position);
                        aRotation = parent.rotation * aRotation;
                    }
                }

                rb.position = aPosition;
                rb.rotation = aRotation;
            }
            else if (anIsLocal)
            {
                aTransform.localPosition = aPosition;
                aTransform.localRotation = aRotation;
            }
            else
            {
                aTransform.position = aPosition;
                aTransform.rotation = aRotation;
            }
        }
    }

    private static void InternalDestroy(GameObject anObjectToDestroy, bool aKeepActive, bool aGoOverCapacity)
    {
        bool destroyObject = true;

        if (anObjectToDestroy)
        {
            if (Instance && Instance.isActiveAndEnabled && Instance.m_pools.TryGetValue(anObjectToDestroy.name, out PoolStruct pool) && null != pool.List)
            {
                var poolList = pool.List;
                bool alreadyInPool = ObjectIsInPool(anObjectToDestroy);
                destroyObject = !alreadyInPool;
                aGoOverCapacity |= aKeepActive;

                if (!alreadyInPool && poolList != null && (aGoOverCapacity || poolList.Count < poolList.Capacity))
                {
                    poolList.Add(anObjectToDestroy);
                    destroyObject = false;
                    anObjectToDestroy.transform.SetParent(null);

                    Vector3 newPos = DESTROY_POSITION;
                    if (!aKeepActive)
                    {
                        newPos = DEACTIVATE_NEXT_FRAME_POSITION;
                        Timing.RunCoroutine(Instance._DeactivateOneFrameLate(anObjectToDestroy));
                    }

                    locSetObjectPosition(anObjectToDestroy.transform, newPos, false);
                    Scene scene = pool.PermanentObjects ? SceneManager.GetSceneByBuildIndex((int)GfcSceneId.GF_BASE) : SceneManager.GetActiveScene();
                    SceneManager.MoveGameObjectToScene(anObjectToDestroy, scene);
                }
            }

            if (destroyObject)
                GameObject.Destroy(anObjectToDestroy);
        }
    }

    private static void DestroyObjectsFromPool(List<GameObject> objects, int numInstances = -1)
    {
        int count = objects.Count;
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
            DestroyObjectsFromPool(Instance.m_pools[key].List, int.MaxValue);
            Instance.m_pools.Remove(key);
        }
    }

    public static void TrimPool(GameObject obj)
    {
        TrimPool(obj.name);
    }

    public static void TrimPool(string prefabName)
    {
        if (Instance.m_pools.TryGetValue(prefabName, out PoolStruct pool) && null != pool.List)
        {
            pool.List.TrimExcess();
        }
    }

    public static void TrimPools()
    {
        var keysList = Instance.m_pools.Keys;
        foreach (var key in keysList)
        {
            Instance.m_pools[key].List.TrimExcess();
        }
    }

    public static void ClearNonPersistentPools()
    {
        Instance.m_stringKeysBuffer.Clear();
        foreach (var pool in Instance.m_pools)
            if (!pool.Value.PermanentObjects)
                Instance.m_stringKeysBuffer.Add(pool.Key);

        foreach (var pool in Instance.m_stringKeysBuffer)
            ClearPool(pool);
    }

    public static void ClearPool(GameObject objectToClear, int numInstances = int.MaxValue, bool keepPoolIfEmpty = false)
    {
        ClearPool(objectToClear.name, numInstances, keepPoolIfEmpty);
    }

    public static void ClearPool(string prefabName, int numInstances = int.MaxValue, bool keepPoolIfEmpty = false)
    {
        if (Instance.m_pools.TryGetValue(prefabName, out PoolStruct pool) && null != pool.List)
        {
            var currentPool = pool.List;
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

        if (!Instance.m_pools.TryGetValue(objectToPool.name, out PoolStruct pool) || null == pool.List)
        {
            pool = new(numInstances, objectToPool);

            if (Instance.m_pools.ContainsKey(objectToPool.name))
                Instance.m_pools[objectToPool.name] = pool;
            else
                Instance.m_pools.Add(objectToPool.name, pool);
        }

        var currentPool = pool.List;

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
        if (Instance.m_pools.TryGetValue(prefabName, out var pool) && null != pool.List)
            ret = pool.List.Count;

        return ret;
    }

    public static GameObject GetObjectInPool(GameObject objectPool, int index = 0)
    {
        return GetObjectInPool(objectPool.name, index);
    }

    public static GameObject GetObjectInPool(string prefabName, int index = 0)
    {
        GameObject ret = null;
        if (Instance.m_pools.TryGetValue(prefabName, out var pool) && null != pool.List && pool.List.Count > index)
            ret = pool.List[index];

        return ret;
    }

    public static int GetPoolCapacity(GameObject objectPooled)
    {
        return GetPoolCapacity(objectPooled.name);
    }

    public static int GetPoolCapacity(string prefabNam)
    {
        int ret = 0;
        if (Instance.m_pools.TryGetValue(prefabNam, out var pool) && null != pool.List)
            ret = pool.List.Capacity;

        return ret;
    }

    public static List<GameObject> GetPoolList(GameObject aObjectPooled, bool aCleanUpNullObjects = true)
    {
        if (aCleanUpNullObjects)
            return CleanUpNullObjectsFromList(GetPoolList(aObjectPooled.name));
        else
            return GetPoolList(aObjectPooled.name);
    }

    protected static List<GameObject> CleanUpNullObjectsFromList(List<GameObject> aPoolList)
    {
        for (int i = 0; null != aPoolList && i < aPoolList.Count; ++i)
            while (i < aPoolList.Count && aPoolList[i] == null)
                aPoolList.RemoveAtSwapBack(i);

        return aPoolList;
    }

    public static List<GameObject> GetPoolList(string prefabName, bool aCleanUpNullObjects = true)
    {
        Instance.m_pools.TryGetValue(prefabName, out var pool);

        if (aCleanUpNullObjects && pool.List != null)
            return CleanUpNullObjectsFromList(pool.List);
        else
            return pool.List;
    }

    public static GameObject GetPrefab(GameObject objectPooled)
    {
        return GetPrefab(GetPrefab(objectPooled.name));
    }

    public static void GetNameWithoutNumberParenthesis(ref string name)
    {
        if (name[^1] == ')') // ^1 is the last index in an array
        {
            int currentIndex = name.Length - 1;
            for (; currentIndex >= 0 && name[currentIndex] != '('; --currentIndex) ;
            --currentIndex; //we need to remove the space before the parenthesis start as well
            if (currentIndex > 0)
                name = name[..currentIndex]; //substring between index 0 to currentIndex
        }
    }

    public static void SetPoolPermanent(GameObject aGameObjectPool)
    {
        PoolStruct pool;
        if (!Instance.m_pools.TryGetValue(aGameObjectPool.name, out pool))
        {
            pool = new(aGameObjectPool);
            Instance.m_pools.Add(aGameObjectPool.name, pool);
        }

        if (!pool.PermanentObjects)
        {
            pool.PermanentObjects = true;
            List<GameObject> poolList = pool.List;
            int count = poolList != null ? poolList.Count : 0;
            for (int i = 0; i < count; ++i)
                if (poolList[i])
                    SceneManager.MoveGameObjectToScene(poolList[i], SceneManager.GetSceneByBuildIndex((int)GfcSceneId.GF_BASE));

            Instance.m_pools[aGameObjectPool.name] = pool;
        }
    }

    public static GameObject GetPrefab(string prefabName)
    {
        GetNameWithoutNumberParenthesis(ref prefabName);
        Instance.m_pools.TryGetValue(prefabName, out var pool);
        GameObject prefab = pool.Prefab;
        if (null == prefab)
        {
            Debug.LogWarning("GfPool warning, the requested prefab of name " + prefabName
                             + " could not be found, make sure you instantiated it at least once with GfPooling or that you add it to do prefab list in the editor.");
        }

        return prefab;
    }
}

[Serializable]
public class InitializationPool
{
    public GameObject gameObject;
    public int instancesToPool;
}