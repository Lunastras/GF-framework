using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfPolymorphism : MonoBehaviour
{
    //the template for 
    [SerializeField]
    protected GameObject templatePrefab;

    //the prefab it is copying right now
    protected GameObject copiedPrefab = null;

    protected void Initialize()
    {
        if (copiedPrefab != null)
            SetCopyPrefab(copiedPrefab);
    }
    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Set the current prefab to copy
    /// </summary>
    public abstract void SetCopyPrefab(GameObject objectToCopy);

    /// <summary>
    /// Get the current copied prefab
    /// </summary>
    public GameObject GetCopyPrefab()
    {
        return copiedPrefab;
    }

    /// <summary>
    /// Get the template prefab of this object
    /// </summary>
    public GameObject GetTemplatePrefab()
    {
        return templatePrefab;
    }

    private static GameObject InternalInstantiate(GameObject objectToSpawn, Transform parent, bool instantiateInWorldSpace, bool poolInstantiate)
    {

        GfPolymorphism polymorphism = objectToSpawn.GetComponent<GfPolymorphism>();
        GameObject instantiatedObj;

        if (null != polymorphism)
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

            instantiatedObj.GetComponent<GfPolymorphism>().SetCopyPrefab(objectToSpawn);
        }
        else
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

        return instantiatedObj;
    }

    private static GameObject InternalInstantiate(GameObject objectToSpawn, Vector3 position, Quaternion rotation, Transform parent, bool poolInstantiate)
    {

        GfPolymorphism polymorphism = objectToSpawn.GetComponent<GfPolymorphism>();
        GameObject instantiatedObj;

        if (null != polymorphism)
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

            instantiatedObj.GetComponent<GfPolymorphism>().SetCopyPrefab(objectToSpawn);
        }
        else
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
