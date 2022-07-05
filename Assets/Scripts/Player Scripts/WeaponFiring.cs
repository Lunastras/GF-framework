using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class WeaponFiring : MonoBehaviour
{
    [SerializeField]
    private Transform fireSource;

    [SerializeField]
    private StatsCharacter statsCharacter;

    [SerializeField]
    private float distanceUpdateInterval = 0.05f;

    [SerializeField]
    private float distanceOffset;

    // [SerializeField]
    // private bool canFire = true;

    [SerializeField]
    private float maxFireDistance = 100;

    public int currentLevel { get; set; }

    private WeaponBasic[] weapons = null;

    private RaycastHit lastRayHit;

    private bool hitAnObject;

    private int numWeapons = 0;

    private float timeUntilNextUpdate;

    // Start is called before the first frame update
    void Awake()
    {
        if (null == fireSource)
        {
            fireSource = transform;
        }
        fireSource = Camera.main.transform;

        //currentExp = new List<float>();

        lastRayHit = new RaycastHit();

        if (statsCharacter == null)
        {
            statsCharacter = GetComponent<StatsCharacter>();
        }
    }


    public void SetWeaponArray(WeaponBasic[] weaponArray, int numWeapons = -1)
    {
        weapons = weaponArray;

        if (numWeapons < 0)
            numWeapons = weapons.Length;

        this.numWeapons = numWeapons;
    }

    private void FixedUpdate()
    {
        timeUntilNextUpdate -= Time.deltaTime;
    }

    // Update is called once per frame
    public void Fire()
    {
        Vector3 fireTargetDir = fireSource.forward;

        if (timeUntilNextUpdate < 0)
        {
            timeUntilNextUpdate = distanceUpdateInterval;

            Ray ray = new(fireSource.position, fireTargetDir);
            RaycastHit[] rayHits = GfPhysics.GetRaycastHits();

            hitAnObject = 0 != Physics.RaycastNonAlloc(ray, rayHits, maxFireDistance, ~GfPhysics.IgnoreLayers());

            if (hitAnObject)
            {
                Debug.Log("hAHAHA I HIT SOMETHING OMGGGG");
                lastRayHit = rayHits[0];
                lastRayHit.distance += distanceOffset;
            }
            else 
            { 
                Debug.Log("I T fuck all");
                lastRayHit.distance = maxFireDistance;
            }
        }

        lastRayHit.point = fireSource.position + fireTargetDir * lastRayHit.distance;
      //  Debug.Log("THE POSITION OF THE CAMERA IS " + fireSource.forward);
       // Debug.Log("THE POSITION OF THE CAMERA IS ");

      //  Debug.DrawRay(fireSource.position, fireTargetDir * 100f, Color.green, 0.1f);


        for (int i = 0; i < numWeapons; ++i)
            weapons[i].Fire(lastRayHit, hitAnObject);
        
    }

    public void ReleaseFire()
    {
        for (int i = 0; i < numWeapons; ++i)
        {
            if (weapons[i] != null && weapons[i].gameObject.activeSelf)
                weapons[i].ReleasedFire(lastRayHit, hitAnObject);
        }
    }

    public StatsCharacter GetStatsCharacter()
    {
        return statsCharacter;
    }

}
