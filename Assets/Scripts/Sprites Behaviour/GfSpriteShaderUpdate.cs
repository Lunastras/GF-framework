using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfSpriteShaderUpdate : MonoBehaviour
{
    public GfMovementGeneric m_movement;
    public Material m_material;

    private Transform m_targetTransform;

    private const string OBJECT_POSITION = "_objectPosition";
    private const string UP_DIR = "_upDir";


    // Start is called before the first frame update
    void Start()
    {
        if (null == m_material)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer)
            {
                List<Material> m_materials = new(1);
                renderer.GetMaterials(m_materials);
                if (m_materials.Count > 0)
                {
                    m_material = m_materials[0];
                }
            }
        }

        if (null == m_movement) m_movement = GetComponent<GfMovementGeneric>();
        m_targetTransform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        m_material.SetVector(UP_DIR, m_movement.GetUpvecRotation());
        m_material.SetVector(OBJECT_POSITION, m_targetTransform.position);
    }
}
