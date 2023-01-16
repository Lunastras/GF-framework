using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class ParticleCollision : MonoBehaviour
{
    private int m_frameOfLastCheck = -1;

    private int m_collisionEventsInFrame;

    //would make them static but I don't know in which order the shuriken system calls
    //OnParticleCollision in different particle systems
    private List<ParticleCollisionEvent> m_collisionEvents = new(8);
    private bool[] m_collisionEventChecked = new bool[8];
    private int m_startIndex;

    public ParticleSystem m_particleSystem { get; private set; }

    protected abstract void InternalAwake();

    // Start is called before the first frame update
    void Awake()
    {
        m_particleSystem = GetComponent<ParticleSystem>();
        InternalAwake();
    }

    void OnParticleCollision(GameObject other)
    {
        int i;
        if (m_frameOfLastCheck != Time.frameCount)
        {
            m_startIndex = 0;
            m_frameOfLastCheck = Time.frameCount;

            m_collisionEventsInFrame = m_particleSystem.GetCollisionEvents(other, m_collisionEvents);

            if (m_collisionEventsInFrame > m_collisionEventChecked.Length)
                Array.Resize(ref m_collisionEventChecked, m_collisionEventsInFrame);

            for (i = 0; i < m_collisionEventsInFrame; ++i)
                m_collisionEventChecked[i] = false;
        }

        i = m_startIndex;
        while (m_collisionEvents[i].colliderComponent.gameObject != other && !m_collisionEventChecked[i])
            ++i;

        if (i == (m_startIndex + 1))
            m_startIndex = i;

        m_collisionEventChecked[i] = true;

        // Debug.Log("I HAVE HIT " + other.name + " WITH COLLISION NORMAL: " + collisionEvents[i].normal);
        CollisionBehaviour(other, m_collisionEvents[i]);
    }

    protected abstract void CollisionBehaviour(GameObject other, ParticleCollisionEvent collisionEvent);
}
