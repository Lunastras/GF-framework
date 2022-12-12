using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfPhysics : MonoBehaviour
{
    public const int MAX_RAYCASTHITS = 12;
    public const int MAX_COLLIDERS = 12;

    private static Collider[] colliders = null;
    private static RaycastHit[] raycastHits = null;

    private static GfPhysics instance;

    private int[] layerMasks;

    //layer mask of objects that can be considered ground
    [SerializeField]
    public LayerMask collisionsNonGroundLayerMask;

    //layer mask of objects that can be considered ground
    [SerializeField]
    public LayerMask groundLayerMask;

    //layer mask of wallrunnable objects
    [SerializeField]
    public LayerMask wallrunLayerMask;

    [SerializeField]
    public LayerMask nonCharacterCollisions;

    /** Layer mask of objects that have collisions
    * that should not affect physics.
    */
    [SerializeField]
    public LayerMask physicsIgnoreLayerMask;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        layerMasks = new int[32];

        for (int layer = 0; layer < 32; layer++)
        {
            int mask = 0;

            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, i)) mask |= 1 << i;
            }

            layerMasks[layer] = mask;
        }

        instance = this;
    }

    public static int GetLayerMask(int layer) => instance.layerMasks[layer];

    public static Collider[] GetCollidersArray()
    {
        if (null == colliders)
        {
            colliders = new Collider[MAX_COLLIDERS];
        }

        return colliders;
    }

    public static RaycastHit[] GetRaycastHits()
    {
        if (null == raycastHits)
        {
            raycastHits = new RaycastHit[MAX_RAYCASTHITS];
        }

        return raycastHits;
    }

    /** Get the layer mask of objects that can be considered ground
    */
    public static int CollisionsNoGroundLayers()
    {
        return instance.collisionsNonGroundLayerMask;
    }

    public static int NonCharacterCollisions()
    {
        return instance.nonCharacterCollisions;
    }

    /** Get the layer mask of objects that can be considered ground
    */
    public static int GroundLayers()
    {
        return instance.groundLayerMask;
    }

    /** Get the layer mask of wallrunnable objects
    */
    public static int WallrunLayers()
    {
        return instance.wallrunLayerMask;
    }

    /** Get the layer mask of objects that have collisions
    * that should not affect physics.
    */
    public static int IgnoreLayers()
    {
        return instance.physicsIgnoreLayerMask;
    }

    public static bool LayerIsInMask(int layer, int mask)
    {
        return mask == (mask | (1 << layer));
    }
}
