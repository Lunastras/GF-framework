using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleSingleHit : ParticleCollision
{
    [SerializeField]
    private StatsCharacter m_statsCharacter;
    [SerializeField]
    private float m_damage = 10;
    [SerializeField]
    private bool m_canDamageSelf = false;
    [SerializeField]
    private Sound m_damageSound;

    [SerializeField]
    private Sound m_collisionSound;

    private Transform m_target = null;

    private void OnEnable()
    {
        m_statsCharacter = null;
        m_target = null;
    }

    private void OnDisable()
    {
        m_statsCharacter = null;
    }

    protected override void InternalAwake()
    {
        m_statsCharacter = null == m_statsCharacter ? GetComponent<StatsCharacter>() : m_statsCharacter;
    }

    private void FixedUpdate()
    {
        if (m_target)
        {
            transform.LookAt(m_target);
            //var mainModule = m_particleSystem.main;
        }
    }

    protected virtual bool HitTarget(StatsCharacter target, float damageMultiplier, ParticleCollisionEvent collisionEvent)
    {
        //  Debug.Log("GONNA DAMAJE IT " + target.name);
        // Debug.Log("I AM HIT, DESTROY BULLET NOW");
        AudioManager.PlayAudio(m_damageSound, collisionEvent.intersection);
        target.Damage(m_damage, damageMultiplier, m_statsCharacter, this);

        return true;
    }

    protected virtual bool HitNonDamageTarget(StatsCharacter target, ParticleCollisionEvent collisionEvent)
    {
        // target.Damage(damage, characterStats);
        return true;
    }

    protected virtual void HitCollision(GameObject other, ParticleCollisionEvent collisionEvent)
    {
        AudioManager.PlayAudio(m_collisionSound, collisionEvent.intersection);
        GameParticles.PlayParticleDust(collisionEvent.intersection, collisionEvent.normal);
    }

    protected override void CollisionBehaviour(GameObject other, ParticleCollisionEvent collisionEvent)
    {
        StatsCharacter collisionStats = other.GetComponent<StatsCharacter>();
        if (collisionStats != null)
        {
            bool hitSelf = m_statsCharacter == collisionStats;
            float damageMultiplier = HostilityManager.DamageMultiplier(m_statsCharacter, collisionStats);

            //check if it can damage target
            if (!hitSelf || (hitSelf && m_canDamageSelf))
                HitTarget(collisionStats, damageMultiplier, collisionEvent);
            else
                HitNonDamageTarget(collisionStats, collisionEvent);
        }
        else
        {
            HitCollision(other, collisionEvent);
        }
    }

    public bool IsAlive(bool withChildren = true) {
        return m_particleSystem.IsAlive(withChildren);
    }

    public void SetStatsCharacter(StatsCharacter stats) {
        m_statsCharacter = stats;
    }

    public ParticleSystem GetParticleSystem() { return m_particleSystem; }

    public StatsCharacter GetStatsCharacter() { return m_statsCharacter; }

    public GameObject GetGameObject() { return gameObject; }

    public void OnDamageDealt(float damage, StatsCharacter damagedCharacter) { }

    public void OnCharacterKilled(StatsCharacter damagedCharacter) { }

    public void StopFiring() {
        m_particleSystem.Stop(true);
    }

    public void Fire(RaycastHit hit = default, bool hitAnObject = true, bool forceFire = false) {
        m_particleSystem.Play();
    }

    public void SetTarget(Transform target) { m_target = target; }
    public Transform GetTarget() { return m_target; }

    public void ReleasedFire(RaycastHit hit = default, bool hitAnObject = false) {}
}
