using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReusableParticleWeapon : WeaponParticle
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

    private ParticleTriggerDamage m_particleTriggerWeaponComponent = null;

    private uint m_particlesFired;

    private ParticleSystem GetTemplateParticleSystem()
    {
        return ReusableTemplateObjects.GetInstanceFromTemplate(m_particleSystemObj).GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        m_effectiveParticleSystem = GetTemplateParticleSystem();
        m_particleTriggerWeaponComponent = m_effectiveParticleSystem.GetComponent<ParticleTriggerDamage>();
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
                GfTools.Minus3(ref dirToTarget, emitParams.position);
                GfTools.Normalize(ref dirToTarget);
                m_effectivePsTransform.rotation = Quaternion.LookRotation(dirToTarget);
            }
            else if (m_applyRotation)
            {
                m_effectivePsTransform.rotation = m_transform.rotation;
            }
            else
            {
                m_effectivePsTransform.rotation = Quaternion.identity;
            }

            m_effectiveParticleSystem.Emit(emitParams, m_particlesToEmit);
            ++m_particlesFired;
        }
    }

    public override void StopFiring()
    {
        m_firing = false;
        m_particlesFired = 0;
    }

    public override void Fire(RaycastHit hit = default, bool hitAnObject = true, bool forceFire = false)
    {
        m_firing = true;

    }

    public override void ReleasedFire(RaycastHit hit = default, bool hitAnObject = false) { }

    public override bool IsAlive(bool withChildren = true)
    {
        return false;
    }

    public override ParticleSystem GetParticleSystem() { return m_effectiveParticleSystem; }

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