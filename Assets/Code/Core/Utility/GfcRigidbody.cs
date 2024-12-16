using UnityEngine;

public class GfcRigidbody : MonoBehaviour
{
    public Rigidbody Rigidbody;
    public Rigidbody2D Rigidbody2D;

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody2D = GetComponent<Rigidbody2D>();
        Debug.Assert(Rigidbody ^ Rigidbody2D, "The must have a single Rigidbody or Rigidbody2D");
    }

    public Vector3 linearVelocity
    {
        get { if (Rigidbody) return Rigidbody.linearVelocity; else return Rigidbody2D.linearVelocity; }
        set { { if (Rigidbody) Rigidbody.linearVelocity = value; else Rigidbody2D.linearVelocity = value; } }
    }

    public float mass
    {
        get { if (Rigidbody) return Rigidbody.mass; else return Rigidbody2D.mass; }
        set { { if (Rigidbody) Rigidbody.mass = value; else Rigidbody2D.mass = value; } }
    }

    public float linearDamping
    {
        get { if (Rigidbody) return Rigidbody.linearDamping; else return Rigidbody2D.linearDamping; }
        set { { if (Rigidbody) Rigidbody.linearDamping = value; else Rigidbody2D.linearDamping = value; } }
    }

    public float angularDamping
    {
        get { if (Rigidbody) return Rigidbody.angularDamping; else return Rigidbody2D.angularDamping; }
        set { { if (Rigidbody) Rigidbody.angularDamping = value; else Rigidbody2D.angularDamping = value; } }
    }

    public Vector3 centerOfMass
    {
        get { if (Rigidbody) return Rigidbody.centerOfMass; else return Rigidbody2D.centerOfMass; }
        set { { if (Rigidbody) Rigidbody.centerOfMass = value; else Rigidbody2D.centerOfMass = value; } }
    }

    public Vector3 position
    {
        get { if (Rigidbody) return Rigidbody.position; else return Rigidbody2D.position; }
        set { { if (Rigidbody) Rigidbody.position = value; else Rigidbody2D.position = value; } }
    }

    public bool freezeRotation
    {
        get { if (Rigidbody) return Rigidbody.freezeRotation; else return Rigidbody2D.freezeRotation; }
        set { { if (Rigidbody) Rigidbody.freezeRotation = value; else Rigidbody2D.freezeRotation = value; } }
    }

    public Vector3 angularVelocity
    {
        get { if (Rigidbody) return Rigidbody.angularVelocity; else return new(0, 0, Rigidbody2D.angularVelocity); }
        set { { if (Rigidbody) Rigidbody.angularVelocity = value; else Rigidbody2D.angularVelocity = value.z; } }
    }

    public void AddForce(Vector3 aForce) { if (Rigidbody) Rigidbody.AddForce(aForce); else Rigidbody2D.AddForce(aForce); }
    public void MovePosition(Vector3 aPosition) { if (Rigidbody) Rigidbody.MovePosition(aPosition); else Rigidbody2D.MovePosition(aPosition); }
    public void MoveRotation(Quaternion aRotation) { if (Rigidbody) Rigidbody.MoveRotation(aRotation); else Rigidbody2D.MoveRotation(aRotation); }
}
