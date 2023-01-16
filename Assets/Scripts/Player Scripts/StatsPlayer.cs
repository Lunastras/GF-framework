using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsPlayer : StatsCharacter
{
    [SerializeField]
    protected float m_maxHealth = 200;

    [SerializeField]
    protected Sound m_damageSound;
    [SerializeField]
    protected Sound m_deathSound;

    [SerializeField]
    private WeaponFiring m_playerGun;

    [SerializeField]
    private Sound m_itemPickUpSound;

    [SerializeField]
    private LoadoutManager m_loadoutManager;

    private float m_currentHealth;

    [SerializeField]
    private AudioSource m_audioSource;

    public bool IsDead { get; protected set; }

    private void Start()
    {
        m_currentHealth = m_maxHealth;

        if (null == m_playerGun)
            m_playerGun = FindObjectOfType<WeaponFiring>();

        if (null == m_loadoutManager)
            m_loadoutManager = GetComponent<LoadoutManager>();

        if (null == m_audioSource)
        {
            m_audioSource = GetComponent<AudioSource>();
            if (null == m_audioSource)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        m_initialised = true;
        HostilityManager.AddCharacter(this);
    }

    public override void Damage(float damage, StatsCharacter enemy = null)
    {
        if (IsDead && false)
            return;

        float damagePercent = damage / m_maxHealth;
        m_loadoutManager.AddExpPercent(-damagePercent);

        m_currentHealth -= damage;
        m_currentHealth = Mathf.Max(0, m_currentHealth);

        // Debug.Log("I HAVE BEEN DAMAGED, i have " + currentHealth + "hp");

        m_damageSound.Play(m_audioSource);

        if (m_currentHealth == 0)
        {
            IsDead = true;
            Kill();
        }
    }

    public override void Kill()
    {
        IsDead = true;
        // Debug.Log("I DIED");
    }

    public void AddPoints(PickItemBehaviour.PickUpTypes itemType, float value)
    {
        m_itemPickUpSound.Play(m_audioSource);

        switch (itemType)
        {
            case (PickItemBehaviour.PickUpTypes.POINTS):

                break;

            case (PickItemBehaviour.PickUpTypes.POWER):
                m_loadoutManager.AddExpPoints(value);
                break;
        }

    }
}
