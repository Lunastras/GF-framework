using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class GfPathFindingNpcScheduler : JobChild
{
    NpcController m_npcController;
    // Start is called before the first frame update
    void Start()
    {
        if (null == m_npcController) m_npcController = GetComponent<NpcController>();
    }

    void OnEnable()
    {
        InitJobChild();
    }

    void OnDisable()
    {
        DeinitJobChild();
    }

    void OnDestroy()
    {
        DeinitJobChild();
    }

    public override bool GetJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512)
    {
        return m_npcController.GetPathFindingJob(out handle, deltaTime, updateType, batchSize);
    }

    public override void OnJobFinished(float deltaTime, UpdateTypes updateType) { }
}
