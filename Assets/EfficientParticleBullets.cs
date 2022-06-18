using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EfficientParticleBullets : MonoBehaviour
{
    [SerializeField] private int _rotationSpeed=1000;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            GetComponent<ParticleSystem>().Emit(1);
        }
        transform.Rotate(Vector3.up, Mathf.Sin(Time.time));
    }
}
