using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralWeaponGenerator : MonoBehaviour
{
    private enum WeaponFormatEnum { Projectile,Beam};
    private enum WeaponTriggerTypeEnum { FullAuto,SemiAuto};

    [SerializeField] private Camera _mainCamera;
    [SerializeField] private GameObject _offsetObject;

    [SerializeField] private WeaponFormatEnum _weaponFormat;
    [SerializeField] private WeaponTriggerTypeEnum _weaponTriggerType;

    [SerializeField] private GameObject _beamObject;
    [SerializeField] private GameObject _projectileObject;

    // Start is called before the first frame update
    void Start()
    {
        _beamObject = Instantiate(_beamObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Fire();
        }
        if (Input.GetMouseButtonUp(0))
        {
            StopFire();
        }
    }



    private void Fire()
    {
        switch (_weaponTriggerType)
        {
            case WeaponTriggerTypeEnum.FullAuto:
                InvokeRepeating("FullAutoFire", 0, 0.1f);
                //StartCoroutine(FullAutoFire());
                break;
            case WeaponTriggerTypeEnum.SemiAuto:
                SpawnEntity();
                break;
            default:
                break;
        }
    }
    private void FullAutoFire()
    {
        SpawnEntity();
        //yield return new WaitForSeconds(0.1f);
    }
    private void StopFire()
    {
        CancelInvoke("FullAutoFire");
        //StopCoroutine(FullAutoFire());
        _beamObject.SetActive(false);
    }

    private void SpawnEntity()
    {
        switch (_weaponFormat)
        {
            case WeaponFormatEnum.Projectile:
                Instantiate(_projectileObject, _offsetObject.transform.position, _mainCamera.transform.rotation);
                break;
            case WeaponFormatEnum.Beam:
                _beamObject.SetActive(true);
                _beamObject.transform.position = _offsetObject.transform.position;
                _beamObject.transform.rotation = _mainCamera.transform.rotation;
                break;
            default:
                break;
        }
    }
}
