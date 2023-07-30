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

        m_particleList = new(m_numActiveParticles, Allocator.TempJob);
        m_particleSystem.GetParticles(m_particleList, m_numActiveParticles);
        m_particlePositions.Clear();

        for (int i = 0; i < m_numActiveParticles; ++i)
        {
            m_particlePositions.Add(m_particleList[i].position);
        }

        m_checkpointState.GravesPositions = m_particlePositions;
        m_checkpointState.GravityReference = GetGravityReference();

        CheckpointManager.AddCheckpointState(m_checkpointState);

        m_particleList.Dispose();
    }

    new protected void OnEnable()
    {
        base.OnEnable();
        CheckpointManager.OnHardCheckpoint += OnHardCheckpoint;
    }

    new protected void OnDisable()
    {
        base.OnDisable();
        CheckpointManager.OnHardCheckpoint -= OnHardCheckpoint;
    }

    new protected void OnDestroy()
    {
        base.OnDestroy();
    }
}
