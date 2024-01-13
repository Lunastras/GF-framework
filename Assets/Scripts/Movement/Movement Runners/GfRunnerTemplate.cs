using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GfRunnerTemplate : MonoBehaviour
{
    [SerializeField]
    public bool CanFly;

    [SerializeField]
    protected float m_speed = 10;
    [SerializeField]
    protected float m_mass = 50;

    public Vector3 MovementDirRaw { get; protected set; }

    [HideInInspector]
    public bool FlagJump = false;

    [HideInInspector]
    public bool FlagDash = false;

    protected PriorityValue<float> m_speedMultiplier = new(1);

    protected PriorityValue<float> m_massMultiplier = new(1);

    protected GfMovementGeneric m_mov;

    protected Transform m_transform;

    protected void Awake()
    {
        m_mov = GetComponent<GfMovementGeneric>();
        m_transform = transform;
    }

    public abstract void BeforePhysChecks(float deltaTime);

    public abstract void AfterPhysChecks(float deltaTime);

    public abstract void MgOnCollision(ref MgCollisionStruct collision);

    public bool SetSpeedMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        return m_speedMultiplier.SetValue(multiplier, priority, overridePriority);
    }

    public bool SetMassMultiplier(float multiplier, uint priority = 0, bool overridePriority = false)
    {
        return m_massMultiplier.SetValue(multiplier, priority, overridePriority);
    }

    public virtual void SetMovementDir(Vector3 dir)
    {
        MovementDirRaw = dir;
    }

    public virtual void SetMovementSpeed(float speed) { this.m_speed = speed; }

    public PriorityValue<float> GetSpeedMultiplier()
    {
        return m_speedMultiplier;
    }

    public PriorityValue<float> GetMassMultiplier()
    {
        return m_massMultiplier;
    }

    public GfMovementGeneric GetMovementGeneric() { return m_mov; }
}
