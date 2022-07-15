using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class ParticleCollision : MonoBehaviour
{
    private int frameOfLastCheck = -1;

    private int collisionEventsInFrame;

    //would make them static but I don't know in which order the shuriken system calls
    //OnParticleCollision in different particle systems
    private List<ParticleCollisionEvent> collisionEvents = new(8);
    private bool[] collisionEventChecked = new bool[8];
    private int startIndex;

    public new ParticleSystem particleSystem { get; private set; }

    protected abstract void InternalAwake();

    // Start is called before the first frame update
    void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        InternalAwake();
    }

    void OnParticleCollision(GameObject other)
    {
        int i;
        if (frameOfLastCheck != Time.frameCount)
        {
            startIndex = 0;
            frameOfLastCheck = Time.frameCount;

            collisionEventsInFrame = particleSystem.GetCollisionEvents(other, collisionEvents);

            if (collisionEventsInFrame > collisionEventChecked.Length)
                Array.Resize(ref collisionEventChecked, collisionEventsInFrame);

            for (i = 0; i < collisionEventsInFrame; ++i)
                collisionEventChecked[i] = false;
        }

        i = startIndex;
        while (collisionEvents[i].colliderComponent.gameObject != other && !collisionEventChecked[i])
            ++i;

        if (i == (startIndex + 1))
            startIndex = i;

        collisionEventChecked[i] = true;

        // Debug.Log("I HAVE HIT " + other.name + " WITH COLLISION NORMAL: " + collisionEvents[i].normal);
        CollisionBehaviour(other, collisionEvents[i]);
    }

    protected abstract void CollisionBehaviour(GameObject other, ParticleCollisionEvent collisionEvent);
}
