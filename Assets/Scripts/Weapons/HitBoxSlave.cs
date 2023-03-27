using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class HitBoxSlave : MonoBehaviour
{
    public HitBoxGeneric hitbox { get; set; } = null;
    // Start is called before the first frame update
    private void OnTriggerStay(Collider other)
    {

        if (hitbox != null)
            hitbox.CollisionBehaviour(other);
    }
}
