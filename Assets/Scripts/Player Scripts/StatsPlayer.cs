using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsPlayer : StatsCharacter
{
    [SerializeField]
    protected float maxHealth = 200;

    public bool isDead { get; protected set; }

    [SerializeField]
    protected Sound damageSound;
    [SerializeField]
    protected Sound deathSound;

    [SerializeField]
    private WeaponFiring playerGun;

    [SerializeField]
    private Sound itemPickUpSound;

    [SerializeField]
    private LoadoutManager loadoutManager;

    private float currentHealth;

    [SerializeField]
    private AudioSource audioSource;

    private void Start()
    {
        HostilityManager.hostilityManager.AddCharacter(this);

        currentHealth = maxHealth;

        if (null == playerGun)
            playerGun = FindObjectOfType<WeaponFiring>();      

        if (null == loadoutManager)     
            loadoutManager = GetComponent<LoadoutManager>();

        if (null == audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (null == audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    public override void Damage(float damage, StatsCharacter enemy = null)
    {
        if (isDead && false)
            return;

        float damagePercent = damage / maxHealth;
        loadoutManager.AddExpPercent(-damagePercent);

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // Debug.Log("I HAVE BEEN DAMAGED, i have " + currentHealth + "hp");

        damageSound.Play(audioSource);

        if (currentHealth == 0)
        {
            isDead = true;
            Kill();
        }
    }

    public override void Kill()
    {
        isDead = true;
       // Debug.Log("I DIED");
    }

    public void AddPoints(PickItemBehaviour.PickUpTypes itemType, float value)
    {
        itemPickUpSound.Play(audioSource);

        switch (itemType)
        {
            case (PickItemBehaviour.PickUpTypes.POINTS):

                break;

            case (PickItemBehaviour.PickUpTypes.POWER):
                loadoutManager.AddExpPoints(value);
                break;
        }

    }
}
