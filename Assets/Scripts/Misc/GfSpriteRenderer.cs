using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class GfSpriteRenderer : MonoBehaviour
{
    [SerializeField]
    private Sprite m_sprite;

    [SerializeField]
    private string m_mainTexture2DName = "_MainTex";

    private Material m_material;

    private static List<Material> Materials;

    private Texture2D m_currentTexture;

    void Start()
    {
        if (null == Materials) Materials = new(1);
        GetComponent<Renderer>().GetMaterials(Materials);
        if (Materials.Count > 0)
        {
            m_material = Materials[0];
            m_currentTexture = m_material.GetTexture("_MainTex") as Texture2D;
            if (null == m_currentTexture)
            {
                Debug.LogError("The gameobject: '" + gameObject.name + "' material has no Texture2D of name " + m_mainTexture2DName);
            }
        }
        else Debug.LogError("No material was found on object: " + gameObject.name);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public Sprite GetSprite() { return m_sprite; }

    public void SetSprite(Sprite sprite)
    {
        m_sprite = sprite;
        Texture2D texture = sprite.texture;
        if (m_currentTexture != texture)
        {
            m_currentTexture = texture;
            m_material.SetTexture(m_mainTexture2DName, texture);
        }

        // texture.
        // m_material.SetTextureOffset()
    }


}
