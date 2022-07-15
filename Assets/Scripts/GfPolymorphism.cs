using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfPolymorphism : MonoBehaviour
{
    //the template for 
    [SerializeField]
    protected bool isTemplate = false;

    [SerializeField]
    protected GameObject templatePrefab;

    //the prefab it is copying right now
    protected GameObject copiedPrefab = null;

    private static Dictionary<string, GameObject> instantiatedTemplates = new(16);

    protected void Initialize()
    {
        if (copiedPrefab != null)
            SetCopyObject(copiedPrefab);
    }
    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// Set the current prefab to copy
    /// </summary>
    protected virtual void SetCopyPrefabInternal(GameObject objectToCopy) { }



    /// <summary>
    /// Set the current prefab to copy
    /// </summary>
    public void SetCopyObject(GameObject objectToCopy)
    {
        objectToCopy = GetValidInstance(objectToCopy).gameObject;

        if (isTemplate)
            SetCopyPrefabInternal(objectToCopy);
        else Debug.LogError("Non template objects are not polymorphic, please check if they are templates using the IsTemplate() function");
    }

    /// <summary>
    /// Get the current copied prefab
    /// </summary>
    public GameObject GetCopyPrefab()
    {
        return copiedPrefab;
    }

    public abstract bool CanCopyObject(GameObject objectToCheck);

    /// <summary>
    /// Get the template prefab of this object
    /// </summary>
    public GameObject GetTemplatePrefab()
    {
        return templatePrefab;
    }

    public bool IsTemplate()
    {
        return isTemplate;
    }

    private static InstanceForceSpawn GetValidInstance(GameObject objectToCheck)
    {
        GfPolymorphism polymorphism = objectToCheck.GetComponent<GfPolymorphism>();
        bool isPolymorphic = null != polymorphism;
        bool forceInstantiate = false;

        if (isPolymorphic)
        {
            if (polymorphism.isTemplate)
            {
                objectToCheck = polymorphism.GetCopyPrefab();
                if (null == objectToCheck)
                    Debug.LogError("Cannot copy a null template object, set its copied prefab to a valid value");
            }


            if (!instantiatedTemplates.ContainsKey(objectToCheck.name))
            {
                forceInstantiate = GfPooling.PoolSizeAvailable(objectToCheck) == 0;
                if (forceInstantiate)
                {
                    GfPooling.Pool(objectToCheck, 1);
                }

                instantiatedTemplates.Add(objectToCheck.name, GfPooling.GetObjectInPool(objectToCheck));
            }

            objectToCheck = instantiatedTemplates[objectToCheck.name];
        } else
        {
            forceInstantiate = GfPooling.PoolSizeAvailable(objectToCheck) > 0;
        }
        
        return new InstanceForceSpawn(instantiatedTemplates[objectToCheck.name], forceInstantiate);
    }

    private static GameObject InternalInstantiate(GameObject objectToSpawn, Transform parent, bool instantiateInWorldSpace, bool poolInstantiate)
    {
        GfPolymorphism polymorphism = objectToSpawn.GetComponent<GfPolymorphism>();
        InstanceForceSpawn instanceSpawn = GetValidInstance(objectToSpawn);
        objectToSpawn = instanceSpawn.gameObject;
        GameObject instantiatedObj = null;

        //
        // bool hasPoolAvailable = GfPooling.PoolSizeAvailable(objectToSpawn) > 0;
        // bool isPolymorphic = null != polymorphism;
        //
        // //the reference object must be in a pool to be used as a reference for a 
        // //polymorphic object
        // bool createTemplateReference = !GfPooling.HasPool(objectToSpawn) && isPolymorphic;
        // bool forceInstantiate = hasPoolAvailable || !isPolymorphic || createTemplateReference;
        //
        // Debug.Log("given object " + objectToSpawn.name +" is polymorphic: " + isPolymorphic);
        // Debug.Log("given object has pool size: " + GfPooling.PoolSizeAvailable(objectToSpawn));


        if (instanceSpawn.forceInstantiate)
        {
            if (poolInstantiate)
            {
                instantiatedObj = GfPooling.PoolInstantiate(objectToSpawn, parent, instantiateInWorldSpace);
            }
            else
            {
                instantiatedObj = GfPooling.Instantiate(objectToSpawn, parent, instantiateInWorldSpace);
            }
        } 
        else
        {
            GameObject templateObj = polymorphism.GetTemplatePrefab();

            if (poolInstantiate)
            {
                instantiatedObj = GfPooling.PoolInstantiate(templateObj, parent, instantiateInWorldSpace);
            }
            else
            {
                instantiatedObj = GfPooling.Instantiate(templateObj, parent, instantiateInWorldSpace);
            }

            instantiatedObj.GetComponent<GfPolymorphism>().SetCopyObject(instantiatedTemplates[objectToSpawn.name]);
        }

        return instantiatedObj;
    }

    private static GameObject InternalInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent, bool poolInstantiate)
    {
        GfPolymorphism polymorphism = objectToSpawn.GetComponent<GfPolymorphism>();
        InstanceForceSpawn instanceSpawn = GetValidInstance(objectToSpawn);
        objectToSpawn = instanceSpawn.gameObject;
        GameObject instantiatedObj = null;

        //GfPolymorphism polymorphism = objectToSpawn.GetComponent<GfPolymorphism>();
        //GameObject instantiatedObj;
        //
        //bool hasPoolAvailable = GfPooling.PoolSizeAvailable(objectToSpawn) > 0;
        //bool objectHasPool = GfPooling.HasPool(objectToSpawn);
        //bool isPolymorphic = null != polymorphism;
        //
        ////the reference object must be in a pool to be used as a reference for a 
        ////polymorphic object
        //bool createTemplateReference = !objectHasPool && isPolymorphic;
        //bool forceInstantiate = hasPoolAvailable || !isPolymorphic || createTemplateReference;

        if (instanceSpawn.forceInstantiate)
        {
            if (poolInstantiate)
            {
                instantiatedObj = GfPooling.PoolInstantiate(objectToSpawn, position, rotation, parent);
            }
            else
            {
                instantiatedObj = GfPooling.Instantiate(objectToSpawn, position, rotation, parent);
            }
        }
        else
        {
            GameObject templateObj = polymorphism.GetTemplatePrefab();

            if (poolInstantiate)
            {
                instantiatedObj = GfPooling.PoolInstantiate(templateObj, position, rotation, parent);
            }
            else
            {
                instantiatedObj = GfPooling.Instantiate(templateObj, position, rotation, parent);
            }

            instantiatedObj.GetComponent<GfPolymorphism>().SetCopyObject(instantiatedTemplates[objectToSpawn.name]);
        }

        return instantiatedObj;
    }

    /// <summary>
    /// Get the template prefab of this object
    /// </summary>
    public static GameObject Instantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false)
    {
        return InternalInstantiate(objectToSpawn, parent, instantiateInWorldSpace, false);
    }

    /// <summary>
    /// Get the template prefab of this object
    /// </summary>
    public static GameObject Instantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return InternalInstantiate(objectToSpawn, position, rotation, parent, false);
    }

    /// <summary>
    /// Get the template prefab of this object
    /// </summary>
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Transform parent = null, bool instantiateInWorldSpace = false)
    {
        return InternalInstantiate(objectToSpawn, parent, instantiateInWorldSpace, true);
    }

    /// <summary>
    /// Get the template prefab of this object
    /// </summary>
    public static GameObject PoolInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return InternalInstantiate(objectToSpawn, position, rotation, parent, true);
    }
}

internal struct InstanceForceSpawn
{
    public InstanceForceSpawn(GameObject obj, bool forceInstantiate)
    {
        gameObject = obj;
        this.forceInstantiate = forceInstantiate;
    }
    public GameObject gameObject;
    public bool forceInstantiate;
}
