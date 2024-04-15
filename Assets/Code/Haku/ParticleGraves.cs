using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;

public class ParticleGraves : ParticleHoming
{
    protected List<Vector3> m_particlePositions = new(0);

    protected CheckpointStateGraves m_checkpointState;
    protected void OnHardCheckpoint()
    {
        if (null == m_checkpointState)
            m_checkpointState = new();

        int numActiveParticles = m_particleSystem.particleCount;
        m_particleList = new(numActiveParticles, Allocator.TempJob);
        m_particleSystem.GetParticles(m_particleList, numActiveParticles);
        m_particlePositions.Clear();

        for (int i = 0; i < numActiveParticles; ++i)
        {
            m_particlePositions.Add(m_particleList[i].position);
        }

        m_checkpointState.GravesPositions = m_particlePositions;
        m_checkpointState.GravityReference = GetGravityReference();

        GfgCheckpointManager.AddCheckpointState(m_checkpointState);

        m_particleList.Dispose();
    }

    new protected void OnEnable()
    {
        base.OnEnable();
        GfgCheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
    }

    new protected void OnDisable()
    {
        base.OnDisable();
        GfgCheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
    }
}
