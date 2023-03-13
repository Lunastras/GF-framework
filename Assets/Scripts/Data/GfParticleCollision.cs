using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct GfParticleCollision
{
    public GfParticleCollision(ParticleSystem particleSystem, int defaultArraySize = 8)
    {
        this.particleSystem = particleSystem;
        frameOfLastCheck = -1;
        collisionEvents = new(defaultArraySize);
        collisionEventsInFrame = 0;
        collisionEventChecked = new bool[defaultArraySize];
        startIndex = 0;
    }

    ParticleCollisionEvent GetCollisionEvent(GameObject other)
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

        int startIndexPlusOne = startIndex + 1;
        if (i == startIndexPlusOne)
            startIndex = i;

        collisionEventChecked[i] = true;

        // Debug.Log("I HAVE HIT " + other.name + " WITH COLLISION NORMAL: " + collisionEvents[i].normal);
        return collisionEvents[i];
    }

    public ParticleSystem particleSystem;

    public int frameOfLastCheck;

    public int collisionEventsInFrame;

    //would make them static but I don't know in which order the shuriken system calls
    //OnParticleCollision in different particle systems
    public List<ParticleCollisionEvent> collisionEvents;
    public bool[] collisionEventChecked;
    public int startIndex;
}