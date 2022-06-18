using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCollisionStressTest : MonoBehaviour
{
    int collisionAmount = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(collisionAmount);
    }

    private void OnParticleCollision(GameObject other)
    {
        collisionAmount++;
    }
}
