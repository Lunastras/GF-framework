#define ENABLE_UPDATE_FUNCTION_CALLBACK
#define ENABLE_LATEUPDATE_FUNCTION_CALLBACK
#define ENABLE_FIXEDUPDATE_FUNCTION_CALLBACK

using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityThread : MonoBehaviour
{
    private static UnityThread Instance = null;

    private readonly List<Action> m_actionQueuesUpdateFunc = new(16);
    private readonly List<Action> m_actionQueuesLateUpdateFunc = new(16);
    private readonly List<Action> m_actionQueuesFixedUpdateFunc = new(16);

    //holds the actions from the static Lists. This is used because we do not want to hold the lock on the buffers throughout the entire execution time.
    private readonly List<Action> m_actionsCopyBuffer = new(16);

    private void Awake()
    {
        this.SetSingleton(ref Instance);
        DontDestroyOnLoad(gameObject);
    }

    public static void Execute(Action anAction, UpdateTypes anUpdateType = UpdateTypes.UPDATE, bool anAllowNullAction = false)
    {
        Debug.Assert(Instance, "Our instance is null, are you sure you added UnityThread to a permanent object in the scene?");

        if (anAction != null)
        {
            List<Action> actionsQueue = anUpdateType switch
            {
                UpdateTypes.UPDATE => Instance.m_actionQueuesUpdateFunc,
                UpdateTypes.FIXED_UPDATE => Instance.m_actionQueuesFixedUpdateFunc,
                UpdateTypes.LATE_UPDATE => Instance.m_actionQueuesLateUpdateFunc,
                _ => throw new ArgumentException("Unexpected update type: " + anUpdateType),
            };

            lock (actionsQueue)
                actionsQueue.Add(anAction);
        }
        else if (!anAllowNullAction)
            Debug.LogError("Null actions are not permited");
    }

    private void ExecuteActions(List<Action> someActions)
    {
        m_actionsCopyBuffer.Clear();

        lock (someActions)
        {
            m_actionsCopyBuffer.AddRange(someActions);
            someActions.Clear();
        }

        for (int i = 0; i < m_actionsCopyBuffer.Count; i++)
            m_actionsCopyBuffer[i].Invoke();
    }

#if ENABLE_UPDATE_FUNCTION_CALLBACK
    public void Update() { ExecuteActions(m_actionQueuesUpdateFunc); }
#endif

#if ENABLE_LATEUPDATE_FUNCTION_CALLBACK
    public void LateUpdate() { ExecuteActions(m_actionQueuesLateUpdateFunc); }
#endif

#if ENABLE_FIXEDUPDATE_FUNCTION_CALLBACK
    public void FixedUpdate() { ExecuteActions(m_actionQueuesFixedUpdateFunc); }
#endif

    public void OnDisable()
    {
        if (Instance == this)
            Instance = null;

        m_actionQueuesUpdateFunc.Clear();
        m_actionQueuesLateUpdateFunc.Clear();
        m_actionQueuesFixedUpdateFunc.Clear();
    }
}