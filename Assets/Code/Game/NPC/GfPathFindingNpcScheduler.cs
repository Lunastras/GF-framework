using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class GfPathFindingNpcScheduler : GfcJobChild
{
    NpcController m_npcController;

    protected bool m_initialised = false;
    // Start is called before the first frame update
    void Start()
    {
        if (null == m_npcController) m_npcController = GetComponent<NpcController>();
        InitJobChild();
        m_initialised = true;
    }



    void OnEnable()
    {
        if (m_initialised)
            InitJobChild();
    }

    void OnDisable()
    {
        DeinitJobChild();
    }

    public override bool GetJob(out JobHandle handle, float deltaTime, UpdateTypes updateType, int batchSize = 512)
    {
        return m_npcController.GetPathFindingJob(out handle, deltaTime, updateType, batchSize);
    }

    public override void OnJobFinished(float deltaTime, UpdateTypes updateType) { }
}
