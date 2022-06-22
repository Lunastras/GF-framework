using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterSeconds : MonoBehaviour
{
    [SerializeField] float SecondsToDestroy = 2;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, SecondsToDestroy);
    }

}
