using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class OutOfBoundsRespawn : MonoBehaviour
{
    public float respawnYCoord = -10;
    public Transform respawnPoint;
    private Vector3 respawnPosition;
    private GfMovementGeneric movement;
    // Start is called before the first frame update
    void Start()
    {
        movement = GetComponent<GfMovementGeneric>();

        if (null == respawnPoint) respawnPoint = GameObject.Find("Spawnpoint").transform;
        if (respawnPoint)
        {
            transform.position = respawnPoint.position;
        }
        else
        {
            respawnPosition = Vector3.zero;
        }

        if (!NetworkManager.Singleton.IsServer) Destroy(this);
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (transform.position.y < respawnYCoord)
        {
            transform.position = respawnPoint.position;
            if (movement != null)
            {
                movement.SetVelocity(Vector3.zero);
            }
        }
    }

    public void SetRespawn(Vector3 pos)
    {
        respawnPosition = pos;
    }
}
