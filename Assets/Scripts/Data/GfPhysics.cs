using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfPhysics : MonoBehaviour
{
    public const int MAX_RAYCASTHITS = 12;
    public const int MAX_COLLIDERS = 12;

    private static Collider[] m_colliders = null;
    private static RaycastHit[] m_raycastHits = null;

    private static GfPhysics Instance;

    private int[] m_layerMasks;

    //layer mask of objects that can be considered ground
    [SerializeField]
    public LayerMask m_collisionsNonGroundLayerMask;

    //layer mask of objects that can be considered ground
    [SerializeField]
    public LayerMask m_targetableCollisions;

    [SerializeField]
    public LayerMask m_groundLayerMask;

    //layer mask of wallrunnable objects
    [SerializeField]
    public LayerMask m_wallrunLayerMask;

    [SerializeField]
    public LayerMask m_nonCharacterCollisions;

    [SerializeField]
    public LayerMask m_characterCollisions;

    /** Layer mask of objects that have collisions
    * that should not affect physics.
    */
    [SerializeField]
    public LayerMask m_physicsIgnoreLayerMask;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance);
        }

        m_layerMasks = new int[32];

        for (int layer = 0; layer < 32; layer++)
        {
            int mask = 0;

            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, i)) mask |= 1 << i;
            }

            m_layerMasks[layer] = mask;
        }

        Instance = this;
    }

    public static int GetLayerMask(int layer) => Instance.m_layerMasks[layer];

    public static Collider[] GetCollidersArray()
    {
        if (null == m_colliders)
        {
            m_colliders = new Collider[MAX_COLLIDERS];
        }

        return m_colliders;
    }

    public static RaycastHit[] GetRaycastHits()
    {
        if (null == m_raycastHits)
        {
            m_raycastHits = new RaycastHit[MAX_RAYCASTHITS];
        }

        return m_raycastHits;
    }

    /** Get the layer mask of objects that can be considered ground
    */
    public static int CollisionsNoGroundLayers()
    {
        return Instance.m_collisionsNonGroundLayerMask;
    }

    public static int TargetableCollisions()
    {
        return Instance.m_targetableCollisions;
    }

    public static int NonCharacterCollisions()
    {
        return Instance.m_nonCharacterCollisions;
    }

    public static int CharacterCollisions()
    {
        return Instance.m_characterCollisions;
    }

    /** Get the layer mask of objects that can be considered ground
    */
    public static int GroundLayers()
    {
        return Instance.m_groundLayerMask;
    }

    /** Get the layer mask of wallrunnable objects
    */
    public static int WallrunLayers()
    {
        return Instance.m_wallrunLayerMask;
    }

    /** Get the layer mask of objects that have collisions
    * that should not affect physics.
    */
    public static int IgnoreLayers()
    {
        return Instance.m_physicsIgnoreLayerMask;
    }

    public static bool LayerIsInMask(int layer, int mask)
    {
        return mask == (mask | (1 << layer));
    }
}
