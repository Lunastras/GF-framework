using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetAsSpawn : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        FindObjectOfType<OutOfBoundsRespawn>().SetRespawn(transform.position);
        GameObject.Destroy(gameObject);
    }
}
