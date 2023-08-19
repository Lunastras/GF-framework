using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfMovementAnimator : MonoBehaviour
{
    [SerializeField]
    protected int m_animationIndexIdle = 0;

    [SerializeField]
    protected int m_animationIndexRunning = 1;

    [SerializeField]
    protected GfMovementGeneric m_movement;

    [SerializeField]
    protected GfSpriteAnimator3D m_spriteAnimator;

    [SerializeField]
    protected float m_defaultSpeed = 8;

    // Start is called before the first frame update
    void Start()
    {
        if (null == m_movement) m_movement = GetComponent<GfMovementGeneric>();
        if (null == m_spriteAnimator) m_spriteAnimator = GetComponent<GfSpriteAnimator3D>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 velocity = m_movement.GetVelocity();
        GfTools.RemoveAxis(ref velocity, m_movement.GetUpvecRotation());
        float speed = System.MathF.Sqrt(velocity.sqrMagnitude);

        if (speed > 0.01f || true) //running
        {
            m_spriteAnimator.PlayState(m_animationIndexRunning);
            m_spriteAnimator.SpeedMultiplier = speed / m_defaultSpeed;
        }
        else //idle
        {
            m_spriteAnimator.PlayState(m_animationIndexIdle);
            m_spriteAnimator.SpeedMultiplier = 1;
        }
    }
}
