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

    protected override void SetCopyPrefabInternal(GameObject objectToCopy)
    {
      //  Debug.Log("I have been called lmao to copy " + objectToCopy.name);

        if(CanCopyObject(objectToCopy))
        {
            //Destroy(objectToCopy);

            if (objectToCopy != copiedPrefab)
            {
                copiedPrefab = objectToCopy;
                PolymorphicOdama odamaOriginal = objectToCopy.GetComponent<PolymorphicOdama>();

                WeaponBasic weaponBasicOriginal = odamaOriginal.GetWeaponBasic();

                odamaBehaviour.SetOdamaValues(odamaOriginal.GetOdamaBehaviour().GetOdamaValues());
                //   weaponBasic.SetParticleSystems(weaponBasicOriginal.GetParticleSystems());
                weaponBasic.SetWeaponValues(weaponBasicOriginal.GetWeaponValues());

                odamaBehaviour.enabled = odamaOriginal.GetOdamaBehaviour().enabled;
                weaponBasic.enabled = weaponBasicOriginal.enabled;

                GameObject copyGraphicsObj = odamaOriginal.GetGraphicsObject();
                graphicsObject.SetActive(copyGraphicsObj.activeSelf);

                SpriteRenderer copySpriteRenderer = copyGraphicsObj.GetComponent<SpriteRenderer>();

                if (null != copySpriteRenderer)
                {
                    quadGraphics.SetquadSpriteValues(odamaOriginal.GetQuadGraphics().GetquadSpriteValues());
                    quadGraphics.enabled = odamaOriginal.GetQuadGraphics().enabled;

                    SpriteRenderer objectSpriteRenderer = graphicsObject.GetComponent<SpriteRenderer>();
                    objectSpriteRenderer.color = copySpriteRenderer.color;
                    objectSpriteRenderer.sprite = copySpriteRenderer.sprite;
                    objectSpriteRenderer.flipX = copySpriteRenderer.flipX;
                    objectSpriteRenderer.flipY = copySpriteRenderer.flipY;
                    objectSpriteRenderer.material = copySpriteRenderer.sharedMaterial;
                    objectSpriteRenderer.drawMode = copySpriteRenderer.drawMode;
                    objectSpriteRenderer.spriteSortPoint = copySpriteRenderer.spriteSortPoint;
                }

                transform.localScale = objectToCopy.transform.localScale;
                graphicsObject.transform.localScale = copyGraphicsObj.transform.localScale;
                graphicsObject.transform.localPosition = copyGraphicsObj.transform.localPosition;
                graphicsObject.transform.localRotation = copyGraphicsObj.transform.localRotation;

            }
        } else
        {
            Debug.LogError(objectToCopy.name + " is an invalid object to copy for " + gameObject.name);
        }   
    }

    public override bool CanCopyObject(GameObject objectToCheck)
    {
        PolymorphicOdama polyOdama = objectToCheck.GetComponent<PolymorphicOdama>();
        return isTemplate && null != polyOdama && (!polyOdama.isTemplate || null != polyOdama.GetCopyPrefab());
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
