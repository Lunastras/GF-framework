using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickItemBehaviour : MonoBehaviour
{

    [SerializeField]
    private CollectibleType itemType;
    [SerializeField]
    private float itemValue;

    [SerializeField]
    private float bopSpeed = 3f;

    [SerializeField]
    private float bopRange = 0.4f;

    [SerializeField]
    private float rotationSpeed = 20;

    [SerializeField]
    private Transform itemTransform;

    [SerializeField]
    private Sound collisionSound;

    [SerializeField]
    private float playerMagnetismRange = 10f;

    [SerializeField]
    private float pickedUpRange = 0.1f;

    [SerializeField]
    private float magnetSpeed = 10f;

    [SerializeField]
    private float timeUntilDestroy = 10;

    [SerializeField]
    private Sound destroySound;

    [SerializeField]
    private GameObject destroyParticle;

    // private const timeBetween 

    private float initializationTime;
    private float timeOfLastBlink;
    //time relative to timeUntilDestroy to start first blink phase
    private float firstBlinkStartPercent = 0.5f;
    private float firstBlinkInterval = 0.05f;

    //time relative to timeUntilDestroy to start second blink phase
    private float secondBlinkStartPercent = 0.75f;
    private float secondBlinkInterval = 0.01f;

    private Transform playerTransform;

    private float currentBopValue = 0;
    private Rigidbody rigidBody;

    private float refBopSmoothSpeed;

    private Renderer itemRenderer;

    private bool followingPlayer = false;

    // Start is called before the first frame update
    void Start()
    {
        firstBlinkStartPercent *= timeUntilDestroy;
        secondBlinkStartPercent *= timeUntilDestroy;

        currentBopValue = Random.Range(0, 10);

        if (itemTransform == null)
        {
            itemTransform = transform.GetChild(0);
        }

        itemRenderer = itemTransform.GetComponent<SpriteRenderer>();
        if (itemRenderer == null)
        {
            itemRenderer = itemTransform.GetComponent<MeshRenderer>();
        }

        itemTransform.Rotate(Vector3.up * Random.Range(0, 360f));
        rigidBody = GetComponent<Rigidbody>();

        playerTransform = GameManager.gameManager.GetPlayer();

        Initialize();
    }

    public void Initialize() {
        initializationTime = Time.time;
        followingPlayer = false;
        if(itemRenderer != null)
            itemRenderer.enabled = true;

        if (rigidBody != null) {
            rigidBody.isKinematic = false;
            rigidBody.velocity = Random.insideUnitSphere * 6;
        }
    }

    private void OnEnable() {
        Initialize();
    }

    private void OnDisable() {
        if (rigidBody != null) {
            rigidBody.isKinematic = true;
            rigidBody.velocity = Vector3.zero;
            
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 dirToPlayer = playerTransform.position - itemTransform.position;
        float distFromPlayer = dirToPlayer.magnitude;

        if (followingPlayer)
        {
            if (distFromPlayer <= pickedUpRange)
            {
                playerTransform.GetComponent<StatsPlayer>().AddPoints(itemType, itemValue);
                GfPooling.Destroy(gameObject);
            }
            else
            {
                itemTransform.position += dirToPlayer.normalized * magnetSpeed * Time.deltaTime;
            }
        }
        else
        {

            if (distFromPlayer <= playerMagnetismRange)
            {
                // Debug.Log("I AM BEING MAGNETED AA");
                followingPlayer = true;
                rigidBody.isKinematic = true;
                return;
            }

            currentBopValue += Time.deltaTime * bopSpeed;
            float verticalBop = Mathf.Sin(currentBopValue) * bopRange;
            itemTransform.localPosition = new Vector3(itemTransform.localPosition.x, verticalBop, itemTransform.localPosition.z);
            itemTransform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }

        float currentTime = Time.time;
        float timeSinceStart = currentTime - initializationTime;
        if (timeSinceStart >= firstBlinkStartPercent)
        {
            if (timeSinceStart >= timeUntilDestroy)
            {
                if (destroyParticle != null)
                {
                    GameObject particle = GfPooling.Instantiate(destroyParticle);
                    particle.transform.position = itemTransform.position;
                    GfPooling.Destroy(destroyParticle, 2.0f);
                }
                GfPooling.Destroy(gameObject);
            }

            float blinkDelay = (timeSinceStart >= secondBlinkStartPercent) ? secondBlinkInterval : firstBlinkInterval;
            float timeSinceLastBlink = currentTime - timeOfLastBlink;

            if (timeSinceLastBlink > blinkDelay)
            {
                itemRenderer.enabled = !itemRenderer.enabled;
                timeOfLastBlink = currentTime;
            }
        }
    }

    public CollectibleType GetItemType() { return itemType; }

    public float GetValue() { return itemValue; }
}
