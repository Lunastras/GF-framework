using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsNpc : StatsCharacter
{
    [SerializeField]
    protected float maxHealth = 200;

    public bool isDead { get; protected set; }

    [SerializeField]
    protected GameObject graphics;

    [SerializeField]
    protected Sound damageSound;

    [SerializeField]
    protected Sound deathSound;

    [SerializeField]
    protected GameObject[] itemDropsAfterDeath;

    [SerializeField]
    protected int[] dropsToSpawn;

    protected float currentHealth;
    protected const float lowHealthThreshold = 0.3f;

    //The damage values in total from the enemies
    private Dictionary<GameObject, float> damageFrom;
    private float biggestDamageReceived = 0;

    [SerializeField]
    private NpcController npcController;

    [SerializeField]
    private Collider objectCollider;

    [SerializeField]
    private TurretBehaviour turret;

    [SerializeField]
    private AudioSource audioSource;

    private readonly int maxSizeDamageDictionary;

    // Start is called before the first frame update

    void Awake()
    {
        if (null == objectCollider)
            objectCollider = GetComponent<Collider>();

        if (null == npcController)
            npcController = GetComponent<NpcController>();

        if (null == turret)
            turret = GetComponent<TurretBehaviour>();

        if (null == audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (null == audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }


        damageFrom = new Dictionary<GameObject, float>(maxSizeDamageDictionary);
    }

    void Start()
    {
        HostilityManager.hostilityManager.AddCharacter(this);
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        ClearDamageList();

        if (HostilityManager.hostilityManager != null)
        {
            HostilityManager.hostilityManager.AddCharacter(this);
        }

        if (null != graphics)
            graphics.SetActive(true);

        if (null != objectCollider)
            objectCollider.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void Kill()
    {
        isDead = true;

        deathSound.Play(audioSource);

        for (int i = 0; i < itemDropsAfterDeath.Length; i++)
        {
            for (int j = 0; j < dropsToSpawn[Mathf.Min(dropsToSpawn[i], dropsToSpawn.Length - 1)]; j++)
            {
                GfPooling.Instantiate(itemDropsAfterDeath[i]).transform.position = transform.position;
            }
        }

        if (null != graphics)
            graphics.SetActive(false);

        if (null != turret)
            turret.Pause();

        if (null != objectCollider)
            objectCollider.enabled = false;

        if (null != npcController)
            npcController.PauseMovement();


        HostilityManager.hostilityManager.RemoveCharacter(this);

        GfPooling.Destroy(gameObject, deathSound.Length() * 2.0f);
    }

    public void ClearDamageList()
    {
        damageFrom.Clear();
        biggestDamageReceived = 0;
    }

    public override void Damage(float damage, StatsCharacter enemy = null)
    {
        if (isDead)
            return;

        bool isOfSameType = enemy.GetCharacterType() != GetCharacterType();
        if (isOfSameType)
            damage *= 0.1f;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        //Debug.Log("I HAVE BEEN DAMAGED, i have " + currentHealth + "hp");

        if (currentHealth <= maxHealth * lowHealthThreshold)
        {
            damageSound.Play(audioSource, 1.5f, 2);
        }
        else
        {
            damageSound.Play(audioSource);
        }

        if (currentHealth == 0)
        {
            isDead = true;
            Kill();
        }
        else if (npcController != null && enemy != null)
        {
            bool canDoDamageCheck = damageFrom.ContainsKey(enemy.gameObject);

            if (!canDoDamageCheck && damageFrom.Count == maxSizeDamageDictionary)
            {
                damageFrom.Add(enemy.gameObject, 0);
                canDoDamageCheck = true;
            }

            if (canDoDamageCheck)
            {
                damageFrom[enemy.gameObject] += damage;
                if (damageFrom[enemy.gameObject] > biggestDamageReceived)
                {
                    //if they are the same type, only attack if the damage dealt is greater than %10 of their total hp
                    if (!isOfSameType || 0.1f * maxHealth >= damageFrom[enemy.gameObject])
                    {
                        biggestDamageReceived = damageFrom[enemy.gameObject];
                        npcController.SetDestination(enemy.transform, true, true);
                    }
                }
            }
        }
    }
}
