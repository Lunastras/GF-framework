using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static System.MathF;

public class GfRunnerFast : GfRunnerTemplate
{
    [SerializeField]
    protected float m_smoothingTime = 0.5f;

    [SerializeField]
    protected float m_midAirSmoothTime = 20;

    [SerializeField]
    protected float m_fallSpeed = 50;
    [SerializeField]
    protected float m_jumpForce = 20;

    [SerializeField]
    protected bool m_requireJumpRelease = true;

    [SerializeField]
    protected int m_maxJumps = 1;

    protected int m_currentJumpsCount = 0;
    protected float m_effectiveSmoothingTime;
    protected bool m_jumpedThisFrame = false;

    //whether we touched the current parent this frame or not
    protected bool m_touchedParent;

    protected bool m_jumpFlagReleased = true;

    protected float m_effectiveSpeed;

    protected PriorityValue<float> m_accelerationMultiplier = new(1);
    protected PriorityValue<float> m_deaccelerationMultiplier = new(1);


    [SerializeField]
    protected bool m_breakWhenUnparent = false;

    public override void BeforePhysChecks(float deltaTime)
    {
        m_touchedParent = m_jumpedThisFrame = false;
        Vector3 movDir = m_mov.MovementDirComputed(MovementDirRaw, CanFly);

        CalculateEffectiveValues();
        CalculateVelocity(deltaTime, movDir);
        CalculateJump();
        CalculateRotation(deltaTime, movDir);
    }

    public override void AfterPhysChecks(float deltaTime)
    {
        if (!m_touchedParent && null != m_mov.GetParentTransform())
        {
            m_mov.DetachFromParentTransform();
            if (m_breakWhenUnparent) Debug.Break();
        }
    }

    protected void CalculateEffectiveValues()
    {
        if (m_mov.GetIsGrounded() || CanFly)
        {
            m_effectiveSmoothingTime = m_smoothingTime;
        }
        else
        {
            m_effectiveSmoothingTime = m_midAirSmoothTime;
        }

        m_effectiveSpeed = m_speed * m_speedMultiplier;
    }

    protected virtual void CalculateRotation(float deltaTime, Vector3 movDir)
    {
        //ROTATION SECTION
        if (!movDir.Equals(Vector3.zero))
        {
            Vector3 m_rotationUpVec = m_mov.GetUpvecRotation();
            Vector3 desiredForwardVec = GfTools.RemoveAxis(movDir, m_rotationUpVec);
            m_mov.SetRotation(Quaternion.LookRotation(desiredForwardVec, m_rotationUpVec));
        }
    }

    Vector3 m_velocitySmoothref;
    protected virtual void CalculateVelocity(float deltaTime, Vector3 movDir)
    {
        Vector3 slope = m_mov.GetSlope();
        Vector3 velocity = m_mov.GetVelocity();

        GfTools.Mult3(ref movDir, m_effectiveSpeed);

        if (!CanFly)
        {
            GfTools.Mult3(ref slope, m_fallSpeed);
            GfTools.Minus3(ref movDir, slope);
        }

        velocity = Vector3.SmoothDamp(velocity
                            , movDir
                            , ref m_velocitySmoothref
                            , m_effectiveSmoothingTime
                            , float.MaxValue
                            , Time.deltaTime * m_mov.GetDeltaTimeCoef());

        m_mov.SetVelocity(velocity);
    }

    protected virtual void CalculateJump()
    {
        if (FlagJump)
        {
            FlagJump = false;
            if (m_jumpFlagReleased || !m_requireJumpRelease)
            {
                m_jumpFlagReleased = false;
                PerformJump();
            }
        }
        else
        {
            m_jumpFlagReleased = true;
        }
    }

    protected virtual void PerformJump()
    {
        DefaultJump();
    }

    protected virtual void DefaultJump()
    {
        if (m_maxJumps > m_currentJumpsCount)
        {
            m_currentJumpsCount++;
            //we use the rotation upVec because it feels more natural when the player's rotation is still changing
            Vector3 m_velocity = m_mov.GetVelocity();
            Vector3 upVecRotation = m_mov.GetUpvecRotation();
            GfTools.RemoveAxis(ref m_velocity, upVecRotation);
            GfTools.Add3(ref m_velocity, upVecRotation * m_jumpForce);
            m_mov.SetVelocity(m_velocity);
            m_mov.SetIsGrounded(false);

            m_mov.DetachFromParentTransform();
        }
    }

    public override void MgOnCollision(ref MgCollisionStruct collision)
    {
        Transform collisionTrans = collision.collider.transform;
        if (collision.isGrounded) m_currentJumpsCount = 0;

        if (collision.isGrounded && collisionTrans != m_mov.GetParentTransform())
            m_mov.SetParentTransform(collisionTrans);

        m_touchedParent |= collision.isGrounded && m_mov.GetParentTransform() == collisionTrans;
    }
}