using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutOfBoundsRespawn : MonoBehaviour
{
    public float respawnYCoord = -10;
    public Transform respawnPoint;
    private Vector3 respawnPosition;
    private MovementGeneric movement;
    // Start is called before the first frame update
    void Start()
    {
        movement = GetComponent<MovementGeneric>();
        if (respawnPoint)
        {
            transform.position = respawnPoint.position;
        }
        else
        {
            respawnPosition = Vector3.zero;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (transform.position.y < respawnYCoord)
        {
            transform.position = respawnPosition;
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
