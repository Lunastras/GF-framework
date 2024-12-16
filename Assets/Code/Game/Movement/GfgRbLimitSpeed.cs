using UnityEngine;

[RequireComponent(typeof(GfcRigidbody))]
public class GfgRbLimitSpeed : MonoBehaviour
{
    public float MaxSpeed = 20;
    public Vector3 Direction;
    public bool DirectionIsAxis = false; //speed will be affected on the -Direction as well as Direction

    private GfcRigidbody m_rb;

    void Start()
    {
        Debug.Assert(Direction.magnitude == 1 || Direction.sqrMagnitude == 0);
        m_rb = GetComponent<GfcRigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 velocity = m_rb.linearVelocity;
        float speed = GfcTools.Magnitude(velocity);
        Vector3 velDir = GfcTools.DivSafe(velocity, speed);
        float dotDirAndVelocity = velDir.Dot(Direction);
        int dotDirAndVelocitySign = dotDirAndVelocity.Sign();
        bool axisDependent = Direction.sqrMagnitude != 0;

        if (axisDependent)
        {
            if (DirectionIsAxis)
                dotDirAndVelocity.AbsSelf();
            else
                dotDirAndVelocity.MaxSelf(0);

            speed *= dotDirAndVelocity;
        }

        if (speed > MaxSpeed)
        {
            if (axisDependent)
            {
                velDir = Direction;
                GfcTools.Mult(ref velDir, dotDirAndVelocitySign * (speed - MaxSpeed));
                GfcTools.Minus(ref velocity, velDir);
            }
            else
            {
                velocity = velDir * MaxSpeed;
            }

            m_rb.linearVelocity = velocity;
        }
    }
}
