using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;
using static Unity.Mathematics.math;

public class ParticleEraser : MonoBehaviour
{
    public static void EraseParticlesFromCenter(NativeArray<ParticleSystem.Particle> particles, ParticleSystem system, Vector3 center, float speedFromCenter)
    {
        ParticleEraserJob jobStruct = new ParticleEraserJob(particles, center, speedFromCenter);
        var handle = jobStruct.Schedule(particles.Length, particles.Length);
        handle.Complete();
        system.SetParticles(particles, particles.Length);
        particles.Dispose();
    }

    public static void EraseParticlesFromCenter(ParticleSystem system, Vector3 center, float speedFromCenter)
    {
        int particlesCount = system.particleCount;

        if (particlesCount > 0)
        {
            NativeArray<ParticleSystem.Particle> particlesArray = new(particlesCount, Allocator.TempJob);
            system.GetParticles(particlesArray, particlesCount);
            EraseParticlesFromCenter(particlesArray, system, center, speedFromCenter);
        }
    }
}


[BurstCompile]
internal struct ParticleEraserJob : IJobParallelFor
{
    NativeArray<ParticleSystem.Particle> m_particleList;
    float3 m_center;
    float m_speed;

    public ParticleEraserJob(NativeArray<ParticleSystem.Particle> particleList, float3 center, float speed)
    {
        m_particleList = particleList;
        m_center = center;
        m_speed = speed;
    }

    public void Execute(int i)
    {
        ParticleSystem.Particle particle = m_particleList[i];
        float3 position = particle.position;
        particle.remainingLifetime = length(m_center - position) / m_speed;
        m_particleList[i] = particle;
    }
}

