using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReusableParticleWeapon : WeaponParticle
{
    [SerializeField]
    private GameObject m_particleSystemObj;

    [SerializeField]
    private float m_fireCoolDownSeconds = 0.1f;

    [SerializeField]
    private int m_particlesToEmit = 1;

    [SerializeField]
    private bool m_playOnStart = false;

    [SerializeField]
    private bool m_applyRotation = false;

    

    private float m_timeUntilCanFire;

    private ParticleSystem m_effectiveParticleSystem;

    private Transform m_effectivePsTransform;

    private bool m_firing = false;

    private Transform m_transform;
    
    private ParticleSystem GetTemplateParticleSystem() {
        
        return ReusableTemplateObjects.GetInstanceFromTemplate(m_particleSystemObj).GetComponent<ParticleSystem>();
    }

    private void Start() {
        m_effectiveParticleSystem = GetTemplateParticleSystem();
        m_effectivePsTransform = m_effectiveParticleSystem.transform;

        m_transform = transform;
    }

    private void Update() {
        m_timeUntilCanFire -= Time.deltaTime;

        if(m_firing && m_timeUntilCanFire <= 0) {
            m_timeUntilCanFire = m_fireCoolDownSeconds;

            ParticleSystem.EmitParams emitParams = new();
            //emitParams.ResetStartLifetime();
            //emitParams.ResetAngularVelocity();
            // emitParams.ResetAxisOfRotation();
            // emitParams.ResetMeshIndex();
            //emitParams.ResetPosition();
            // emitParams.ResetRandomSeed();
            //emitParams.ResetRotation();
            //emitParams.ResetStartColor();
            //emitParams.ResetStartSize();
            //emitParams.ResetVelocity();

            emitParams.position = m_transform.position;

            if(m_applyRotation)
                m_effectivePsTransform.rotation = m_transform.rotation;
            else 
                m_effectivePsTransform.rotation = Quaternion.identity;
             
            m_effectiveParticleSystem.Emit(emitParams, m_particlesToEmit);
        }
    }

    public override void StopFiring()
    {
        m_firing = false;
    }

    public override void Fire(RaycastHit hit = default, bool hitAnObject = true, bool forceFire = false)
    {
        m_firing = true;

    }

    public override void ReleasedFire(RaycastHit hit = default, bool hitAnObject = false) { }

    public override bool IsAlive(bool withChildren = true) {
        return false;
    }

    public override ParticleSystem GetParticleSystem() { return m_effectiveParticleSystem; }
}
