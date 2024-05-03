using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReusableParticleWeapon : WeaponGeneric
{
    [SerializeField]
    private GameObject m_particleSystemObj = null;

    [SerializeField]
    private float m_fireCoolDownSeconds = 0.1f;

    [SerializeField]
    private int m_particlesToEmit = 1;

    [SerializeField]
    private bool m_applyRotation = false;

    private float m_timeUntilCanFire = 0;

    private ParticleSystem m_effectiveParticleSystem = null;

    private Transform m_effectivePsTransform = null;

    private bool m_firing = false;

    private Transform m_transform = null;

    private int m_reusableParticleSystemIndex = -1;

    private Quaternion m_desiredRotation = Quaternion.identity;

    private WeaponParticleTrigger m_particleTriggerWeaponComponent = null;

    private uint m_particlesFired;

    private ParticleSystem GetTemplateParticleSystem()
    {
        if (!GfcPooling.HasPool(m_particleSystemObj))
            GfcPooling.Pool(m_particleSystemObj, 1);

        var objList = GfcPooling.GetPoolList(m_particleSystemObj);
        objList[0].SetActive(true);
        return objList[0].GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        m_effectiveParticleSystem = GetTemplateParticleSystem();
        m_particleTriggerWeaponComponent = m_effectiveParticleSystem.GetComponent<WeaponParticleTrigger>();
        if (m_particleTriggerWeaponComponent)
        {
            m_particleTriggerWeaponComponent.m_getDamageSourceFromColor = true;
        }
        else
        {
            Debug.Log("The templated weapon '" + m_effectiveParticleSystem.name + "' does not have a ParticleTriggerDamage component, the functionality of this class won't work.");
        }
        m_effectivePsTransform = m_effectiveParticleSystem.transform;

        m_transform = transform;
        ReusableParticleSystemManager.AddWeapon(this);
    }

    private void Update()
    {
        m_timeUntilCanFire -= Time.deltaTime;

        if (m_firing && m_timeUntilCanFire <= 0)
        {

            m_timeUntilCanFire = m_fireCoolDownSeconds;

            ParticleSystem.EmitParams emitParams = new();

            Color32 startColor = new Color32(
              (byte)(m_reusableParticleSystemIndex >> 24)
            , (byte)(m_reusableParticleSystemIndex >> 16)
            , (byte)(m_reusableParticleSystemIndex >> 8)
            , (byte)m_reusableParticleSystemIndex);

            emitParams.startColor = startColor;
            emitParams.position = m_transform.position;
            emitParams.applyShapeToPosition = true;
            emitParams.randomSeed = m_particlesFired;

            if (m_target)
            {
                Vector3 dirToTarget = m_target.position;
                GfcTools.Minus(ref dirToTarget, emitParams.position);
                GfcTools.Normalize(ref dirToTarget);
                Vector3 upVec = Vector3.up;
                if (m_movementParent) upVec = m_movementParent.GetUpvecRotation();
                m_effectivePsTransform.rotation = Quaternion.LookRotation(dirToTarget, upVec);
            }
            else if (m_applyRotation)
            {
                m_effectivePsTransform.rotation = m_transform.rotation;
            }
            else
            {
                m_effectivePsTransform.rotation = m_desiredRotation;
            }

            m_effectiveParticleSystem.Emit(emitParams, m_particlesToEmit);
            ++m_particlesFired;
        }
    }

    public override bool IsFiring() { return false; }

    public override void StopFiring(bool killBullets)
    {
        m_firing = false;
        m_particlesFired = 0;
    }

    public override void Fire(FireHit hit = default, FireType fireType = FireType.MAIN, bool forceFire = false)
    {
        Vector3 dirBullet = hit.point - m_transform.position;
        GfcTools.Normalize(ref dirBullet);

        Vector3 upVec = Vector3.up;
        if (m_movementParent) upVec = m_movementParent.GetUpvecRotation();
        m_desiredRotation = Quaternion.LookRotation(dirBullet, upVec);
        m_firing = true;
    }

    public override void ReleasedFire(FireHit hit = default, FireType fireType = FireType.MAIN) { }

    public override bool IsAlive()
    {
        return false;
    }

    public ParticleSystem GetParticleSystem() { return m_effectiveParticleSystem; }

    private void OnDestroy()
    {
        ReusableParticleSystemManager.RemoveWeapon(this);
    }

    public void SetReusableParticleSystemIndex(int index)
    {
        m_reusableParticleSystemIndex = index;
    }

    public int GetReusableParticleSystemIndex()
    {
        return m_reusableParticleSystemIndex;
    }
}
