using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfPathfindingScheduler : MonoBehaviour
{
    [SerializeField]
    private float m_pathfindingCooldown = 0.3f;

    private float m_timeUntillPathfind = 0;

    private static GfPathfindingScheduler Instance = null;

    public static bool CanSchedule()
    {
        return Instance.m_timeUntillPathfind <= 0;
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (this != Instance)
            Destroy(Instance);

        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (m_timeUntillPathfind <= 0)
            m_timeUntillPathfind = m_pathfindingCooldown;

        m_timeUntillPathfind -= Time.deltaTime;
    }
}
