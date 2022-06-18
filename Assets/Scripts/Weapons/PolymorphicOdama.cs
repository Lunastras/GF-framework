using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolymorphicOdama : GfPolymorphism
{
    [SerializeField]
    private WeaponBasic weaponBasic;

    [SerializeField]
    private OdamaBehaviour odamaBehaviour;

    [SerializeField]
    private QuadSpriteGraphics quadGraphics;

    [SerializeField]
    private GameObject graphicsObject;


    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    public override void SetCopyPrefab(GameObject objectToCopy)
    {
        PolymorphicOdama odamaCopy = objectToCopy.GetComponent<PolymorphicOdama>();

       odamaBehaviour.SetOdamaValues(odamaCopy.GetOdamaBehaviour().GetOdamaValues());
       weaponBasic.SetWeaponValues(odamaCopy.GetWeaponBasic().GetWeaponValues());

       odamaBehaviour.enabled = odamaCopy.GetOdamaBehaviour().enabled;
       weaponBasic.enabled = odamaCopy.GetWeaponBasic().enabled;

        copiedPrefab = objectToCopy;

        GameObject copyGraphicsObj = odamaCopy.GetGraphicsObject();
        graphicsObject.SetActive(copyGraphicsObj.activeSelf);

        SpriteRenderer copySpriteRenderer = copyGraphicsObj.GetComponent<SpriteRenderer>();

        if (null != copySpriteRenderer)
        {
            quadGraphics.SetquadSpriteValues(odamaCopy.GetQuadGraphics().GetquadSpriteValues());
            quadGraphics.enabled = odamaCopy.GetQuadGraphics().enabled;

            SpriteRenderer objectSpriteRenderer = graphicsObject.GetComponent<SpriteRenderer>();
            objectSpriteRenderer.color = copySpriteRenderer.color;
            objectSpriteRenderer.sprite = copySpriteRenderer.sprite;
            objectSpriteRenderer.flipX = copySpriteRenderer.flipX;
            objectSpriteRenderer.flipY = copySpriteRenderer.flipY;
            objectSpriteRenderer.material = copySpriteRenderer.sharedMaterial;
            objectSpriteRenderer.drawMode = copySpriteRenderer.drawMode;
            objectSpriteRenderer.spriteSortPoint = copySpriteRenderer.spriteSortPoint;
        }
        /* else
         {
             MeshFilter copyMeshFilter = copyGraphicsObj.GetComponent<MeshFilter>();

             if (null != copyMeshFilter)
             {
                 MeshFilter objectMeshFilter = graphicsObject.GetComponent<MeshFilter>();

                 objectMeshFilter.mesh = copyMeshFilter.mesh;
             }
         }*/

        // GetComponent<Renderer>().sharedMaterials = 

        transform.localScale = objectToCopy.transform.localScale;
        graphicsObject.transform.localScale = copyGraphicsObj.transform.localScale;
        graphicsObject.transform.localPosition = copyGraphicsObj.transform.localPosition;
        graphicsObject.transform.localRotation = copyGraphicsObj.transform.localRotation;
    }


    public GameObject GetGraphicsObject()
    {
        return graphicsObject;
    }

    public OdamaBehaviour GetOdamaBehaviour()
    {
        return odamaBehaviour;
    }

    public QuadSpriteGraphics GetQuadGraphics()
    {
        return quadGraphics;
    }

    public WeaponBasic GetWeaponBasic()
    {
        return weaponBasic;
    }
}
