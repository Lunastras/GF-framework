using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class GfSpriteRenderer : MonoBehaviour
{
    //The anchor orientation of the sprite
    //Note: if the constant side is X, then the anchor modes RIGHT/LEFT won't change anything and will keep the object centered. 
    //Same for constant side Y with UP / DOWN
    public enum AnchorMode { CENTER, RIGHT, LEFT, UP, DOWN }
    //The axis on which to preserve the 'm_targetLenght' measurement
    public enum ConstantSideMode { X, Y }

    [SerializeField]
    private Sprite m_sprite;

    [SerializeField]
    private Color32 m_color = new Color32(1, 1, 1, 1);

    [SerializeField]
    private float m_targetLenght = 1;

    //[SerializeField]
    // private AnchorMode m_anchor;

    //[SerializeField]
    //private ConstantSideMode m_constantAxis;

    private bool m_xFlipped = false;

    private bool m_yFlipped = false;

    private Material m_material;

    private Transform m_transform;

    private static List<Material> Materials;

    private Texture2D m_currentTexture = null;

    private bool m_initialised = false;

    [ExecuteInEditMode]
    public void Awake()
    {
        if (!m_initialised)
        {
            m_initialised = true;
            m_transform = transform;
            if (null == Materials) Materials = new(1);
            GetComponent<Renderer>().GetMaterials(Materials);
            if (Materials.Count > 0)
            {
                m_material = Materials[0];
                if (null == m_material.mainTexture) Debug.LogError("No texture was found on object: " + gameObject.name);
            }
            else Debug.LogError("No material was found on object: " + gameObject.name);

            if (m_sprite) SetSprite(m_sprite, true);
        }
    }

    public Sprite GetSprite() { return m_sprite; }

    public void SetSprite(Sprite sprite, bool forceSet = false)
    {
        forceSet |= m_material.mainTexture != sprite.texture;
        //Debug.Log("YYEEEE");
        if (m_sprite != sprite || forceSet)
        {
            //  Debug.Log("I am setting the thing ");
            m_sprite = sprite;
            m_material.color = m_color;

            Texture2D texture = sprite.texture;
            if (forceSet)
            {
                m_currentTexture = texture;
                m_material.mainTexture = texture;
            }



            Rect spriteRect = sprite.rect;
            float heightInv = 1.0f / (float)m_currentTexture.height;
            float widthInv = 1.0f / (float)m_currentTexture.width;

            float yOffset = spriteRect.yMin * heightInv;
            float xOffset = spriteRect.xMin * widthInv;
            float yTile = spriteRect.yMax * heightInv - yOffset;
            float xTile = spriteRect.xMax * widthInv - xOffset;

            m_material.mainTextureOffset = new Vector2(xOffset, yOffset);
            m_material.mainTextureScale = new Vector2(xTile, yTile);

            // Debug.Log("The tiling is: " + yTile + "/" + xTile);
            // Debug.Log("The offset is " + yOffset + "/" + xOffset);

            float height = spriteRect.height;
            float width = spriteRect.width;
            float pixelsPerUnit = sprite.pixelsPerUnit;

            float xScale = m_targetLenght * (spriteRect.width / pixelsPerUnit);
            float yScale = m_targetLenght * (spriteRect.height / pixelsPerUnit);
            if (m_xFlipped) xScale *= -1.0f;
            if (m_yFlipped) yScale *= -1.0f;

            m_transform.localScale = new Vector3(xScale, yScale, 1.0f);

            /*
            if (m_constantAxis == ConstantSideMode.X)
            {

            }
            else //m_constantAxis == ConstantSideMode.Y
            {
                m_transform.localScale = new Vector3(m_targetLenght * (spriteRect.width / spriteRect.height), m_targetLenght, 1.0f);
            }*/
        }

        // texture.
        // m_material.SetTextureOffset()
    }

    public bool GetFlippedX() { return m_xFlipped; }

    public bool GetFlippedY() { return m_yFlipped; }

    public void SetFlippedX(bool flipped) { m_xFlipped = flipped; }

    public void SetFlippedY(bool flipped) { m_yFlipped = flipped; }
}

