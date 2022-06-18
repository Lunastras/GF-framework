using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosestPointSphere : MonoBehaviour
{
    [SerializeField]
    private int collidersSize = 2;

    private float radius = 1;

    private Collider[] collisions;
    // Start is called before the first frame update
    void Start()
    {
        collisions = new Collider[collidersSize];
        radius = transform.localScale.x / 2.0f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        int numCollisions = Physics.OverlapSphereNonAlloc(transform.position, radius, collisions);
        Debug.Log("Collisions length :" + collisions.Length + " numCollisions: " + numCollisions);

        foreach (Collider col in collisions)
        {
            if (col != null)
            {
                Vector3 dirToCollision = col.ClosestPoint(transform.position) - transform.position;
                // dirToCollision = dirToCollision.normalized;
                Debug.Log("Distance To Point " + dirToCollision.magnitude);
            }
            else
            {
                Debug.Log("This value is empty");
            }
        }
    }
}
