using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolymorphicBullet : GfPolymorphism
{
    [SerializeField]
    private BulletMovement bulletMovement;

    [SerializeField]
    private HitBoxSingleBehaviour hitBoxBehaviour;

    [SerializeField]
    private QuadSpriteGraphics quadGraphics;

    [SerializeField]
    private GameObject graphicsObject;

    [SerializeField]
    private Rigidbody rigidbody;

    [SerializeField]
    private SphereCollider sphereCollider;




    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    public override void SetCopyPrefab(GameObject objectToCopy)
    {
        PolymorphicBullet bulletCopy = objectToCopy.GetComponent<PolymorphicBullet>();

        bulletMovement.SetBulletMovementValues(bulletCopy.GetBulletMovement().GetBulletMovementValues());
        hitBoxBehaviour.SetHitBoxValues(bulletCopy.GetHitBoxBehaviour().GetHitBoxValues());
        sphereCollider.radius = bulletCopy.GetSphereCollider().radius;

        bulletMovement.enabled = bulletCopy.GetBulletMovement().enabled;
        hitBoxBehaviour.enabled = bulletCopy.GetHitBoxBehaviour().enabled;
        sphereCollider.enabled = bulletCopy.GetSphereCollider().enabled;

        Rigidbody copyRb = bulletCopy.GetRigidBody();

        rigidbody.isKinematic = copyRb.isKinematic;
        rigidbody.mass = copyRb.mass;

        copiedPrefab = objectToCopy;

        GameObject copyGraphicsObj = bulletCopy.GetGraphicsObject();
        graphicsObject.SetActive(copyGraphicsObj.activeSelf);

        SpriteRenderer copySpriteRenderer = copyGraphicsObj.GetComponent<SpriteRenderer>();

        if (null != copySpriteRenderer)
        {
            quadGraphics.SetquadSpriteValues(bulletCopy.GetQuadGraphics().GetquadSpriteValues());
            quadGraphics.enabled = bulletCopy.GetQuadGraphics().enabled;

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

    public BulletMovement GetBulletMovement()
    {
        return bulletMovement;
    }

    public HitBoxSingleBehaviour GetHitBoxBehaviour()
    {
        return hitBoxBehaviour;
    }

    public GameObject GetGraphicsObject()
    {
        return graphicsObject;
    }

    public Rigidbody GetRigidBody()
    {
        return rigidbody;
    }

    public QuadSpriteGraphics GetQuadGraphics()
    {
        return quadGraphics;
    }

    public SphereCollider GetSphereCollider()
    {
        return sphereCollider;
    }
}
