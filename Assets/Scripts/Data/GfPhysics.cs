using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfPhysics : MonoBehaviour
{
    private static Collider[] colliders = null;
    private static RaycastHit[] raycastHits = null;

    private static GfPhysics instance;

    //layer mask of objects that can be considered ground
    [SerializeField]
    public int collisionsNonGroundLayerMask = 0;
    
    //layer mask of objects that can be considered ground
    [SerializeField]
    public int groundLayerMask = 0;

    //layer mask of wallrunnable objects
    [SerializeField]
    public int wallrunLayerMask = 0;

    /** Layer mask of objects that have collisions
    * that should not affect physics.
    */
    [SerializeField]
    public int physicsIgnoreLayerMask = 0;

    private void Awake() {
        if(instance != null) {
            Destroy(instance);
        }

        instance = this;
    }   

    public static Collider[] GetCollidersArray()
    {
        if (null == colliders)
        {
            colliders = new Collider[1];
            colliders[0] = null;
        }

        return colliders;
    }

    public static RaycastHit[] GetRaycastHits()
    {
        if (null == raycastHits)
        {
            raycastHits = new RaycastHit[1];
        }

        return raycastHits;
    }

    /** Get the layer mask of objects that can be considered ground
    */
    public static int CollisionsNoGroundLayers() {
        return instance.collisionsNonGroundLayerMask;
    }

    /** Get the layer mask of objects that can be considered ground
    */
    public static int GroundLayers() {
        return instance.groundLayerMask;
    }

    /** Get the layer mask of wallrunnable objects
    */
    public static int WallrunLayers() {
        return instance.wallrunLayerMask;
    }
    
    /** Get the layer mask of objects that have collisions
    * that should not affect physics.
    */
    public static int IgnoreLayers() {
        return instance.physicsIgnoreLayerMask;
    }

    public static bool LayerIsInMask(int layer, int mask)
    {
        return mask == (mask | (1 << layer));
    }
}
