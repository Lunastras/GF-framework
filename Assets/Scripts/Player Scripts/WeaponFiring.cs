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
    private Transform weaponParent;

    [SerializeField]
    private int[] layersIgnore;

    [SerializeField]
    private bool canFire = true;

    [SerializeField]
    private float maxFireDistance = 100;

    public int currentLevel { get; set; }

    private int layerMaskIgnore;

    private WeaponBasic[] weapons = null;
    private RaycastHit[] rayCastHits;

    private RaycastHit lastRayHit;

    private bool hitAnObject;

    private int numWeapons = 0;

    private bool wasFiring = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (null == fireSource)
        {
            fireSource = transform;
        }

        //currentExp = new List<float>();

        rayCastHits = new RaycastHit[1];

        lastRayHit = new RaycastHit();

        if (statsCharacter == null)
        {
            statsCharacter = GetComponent<StatsCharacter>();
        }

        //weapons = new List<WeaponBasic>();

        for (int i = 0; i < layersIgnore.Length; i++)
        {
            layerMaskIgnore += (int)Mathf.Pow(2, layersIgnore[i]);
        }
    }


    public void SetWeaponArray(WeaponBasic[] weaponArray, int numWeapons = -1)
    {
        weapons = weaponArray;

        if (numWeapons < 0)
            numWeapons = weapons.Length;

        this.numWeapons = numWeapons;
    }

    // Update is called once per frame
    public void Fire()
    {
        bool gunCarFire = false;

        if (weapons == null)
            return;

        for (int i = 0; i < numWeapons; ++i)
        {
            gunCarFire = weapons[i] != null && weapons[i].gameObject.activeSelf && weapons[i].canFire();
            if (gunCarFire) break;
        }

        if (canFire && gunCarFire)
        {
            Vector3 fireTargetDir = fireSource.forward;

            rayCastHits[0].point = Vector3.zero;
            hitAnObject = true;

            Ray ray = new Ray(fireSource.position, fireTargetDir);
            RaycastHit oldHit = rayCastHits[0];

            if (0 == Physics.RaycastNonAlloc(ray, rayCastHits, maxFireDistance, ~layerMaskIgnore))
            {
                hitAnObject = false;
                fireTargetDir *= maxFireDistance;
                fireTargetDir += Camera.main.transform.position;

                lastRayHit.point = fireTargetDir;
                lastRayHit.distance = maxFireDistance;
            }
            else
            {
                rayCastHits[0].point += Camera.main.transform.forward;
                rayCastHits[0].distance++;
                lastRayHit = rayCastHits[0];
            }

            for (int i = 0; i < numWeapons; ++i)
            {
                if (weapons[i] != null && weapons[i].gameObject.activeSelf)
                    weapons[i].Fire(lastRayHit, hitAnObject);
            }
        }
    }

    public void ReleaseFire()
    {

        if (weapons == null)
            return;

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
