using UnityEngine;

public class GfgRbLimitSpeed : MonoBehaviour
{
    public float MaxSpeed = 20;
    public Vector3 Direction;
    public bool DirectionIsAxis = false; //speed will be affected on the -Direction as well as Direction

    private Rigidbody m_rb;
    private Rigidbody2D m_rb2D;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Assert(Direction.magnitude == 1 || Direction.sqrMagnitude == 0);

        m_rb = GetComponent<Rigidbody>();
        m_rb2D = GetComponent<Rigidbody2D>();

        Debug.Assert(m_rb ^ m_rb2D, "The must have a single Rigidbody or Rigidbody2D");
    }

    void FixedUpdate()
    {
        Vector3 velocity = m_rb ? m_rb.linearVelocity : m_rb2D.linearVelocity;
        float speed = GfcTools.Magnitude(velocity);
        Vector3 velDir = GfcTools.DivSafe(velocity, speed);
        float dotDirAndVelocity = velDir.Dot(Direction);
        int dotDirAndVelocitySign = dotDirAndVelocity.Sign();

        if (DirectionIsAxis)
            dotDirAndVelocity.AbsSelf();
        else
            dotDirAndVelocity.Max(0);

        speed *= dotDirAndVelocity;

        if (speed > MaxSpeed)
        {
            /*
            GfcTools.RemoveAxis(ref velocity, Direction);

            velDir = Direction;
            GfcTools.Mult(ref velDir, dotDirAndVelocitySign * MaxSpeed);
            GfcTools.Add(ref velocity, velDir);*/

            velDir = Direction;
            GfcTools.Mult(ref velDir, dotDirAndVelocitySign * (speed - MaxSpeed));
            GfcTools.Minus(ref velocity, velDir);

            if (m_rb)
                m_rb.linearVelocity = velocity;
            else
                m_rb2D.linearVelocity = velocity;
        }
    }
}
