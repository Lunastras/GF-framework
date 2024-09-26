#define ENABLE_UPDATE_FUNCTION_CALLBACK
#define ENABLE_LATEUPDATE_FUNCTION_CALLBACK
#define ENABLE_FIXEDUPDATE_FUNCTION_CALLBACK

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class UnityThread : MonoBehaviour
{
    private static UnityThread Instance = null;

    public static Thread MainThread { get; protected set; } = null;

    private readonly List<Action> m_actionQueuesUpdateFunc = new(16);
    private readonly List<Action> m_actionQueuesLateUpdateFunc = new(16);
    private readonly List<Action> m_actionQueuesFixedUpdateFunc = new(16);

    private readonly ManualResetEvent m_eventUpdate = new(false);
    private readonly ManualResetEvent m_eventLateUpdate = new(false);
    private readonly ManualResetEvent m_eventFixedUpdate = new(false);

    //holds the actions from the static Lists. This is used because we do not want to hold the lock on the buffers throughout the entire execution time.
    private readonly List<Action> m_actionsCopyBuffer = new(16);

    private readonly ReaderWriterLockSlim m_timeConstantsLock = new();

    private int m_frameCountOfLastUpdate = -1;
    private int m_frameCountOfLastLateUpdate = -1;
    private int m_frameCountOfLastFixedUpdate = -1;


    const string NULL_INSTANCE_ERROR = "Our instance is null, are you sure you added UnityThread to a permanent object in the scene?";

    public void Awake()
    {
        this.SetSingleton(ref Instance);
        DontDestroyOnLoad(gameObject);
        MainThread = Thread.CurrentThread;
    }

    public static bool IsOnMainThread() { return MainThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId; }

    public static int FrameCountOfLastExecution(UpdateTypes anUpdateType = UpdateTypes.UPDATE)
    {
        Instance.m_timeConstantsLock.EnterReadLock();

        int frameCount = anUpdateType switch
        {
            UpdateTypes.UPDATE => Instance.m_frameCountOfLastUpdate,
            UpdateTypes.LATE_UPDATE => Instance.m_frameCountOfLastFixedUpdate,
            UpdateTypes.FIXED_UPDATE => Instance.m_frameCountOfLastLateUpdate,
            _ => throw new ArgumentException("Unexpected update type: " + anUpdateType),
        };

        Instance.m_timeConstantsLock.ExitReadLock();

        return frameCount;
    }

    public static void QueueAndWaitUntilDone(Action anAction, UpdateTypes anUpdateType = UpdateTypes.UPDATE, bool anAllowNullAction = false, bool anIgnoreIfMainThread = false)
    {
        WaitUntilDone(Queue(anAction, anUpdateType, anAllowNullAction), anUpdateType, anIgnoreIfMainThread);
    }

    public static void WaitUntilDone(int aQueueFrameCount, UpdateTypes anUpdateType = UpdateTypes.UPDATE, bool anIgnoreIfMainThread = false)
    {
        if (!WasExecuted(aQueueFrameCount, anUpdateType))
            WaitForNextUpdate(anUpdateType, anIgnoreIfMainThread);
    }

    public static bool WasExecuted(int aQueueFrameCount, UpdateTypes anUpdateType = UpdateTypes.UPDATE) { return aQueueFrameCount == FrameCountOfLastExecution(anUpdateType); }

    //Returns the current framecount of the last execution
    public static int Queue(Action anAction, UpdateTypes anUpdateType = UpdateTypes.UPDATE, bool anAllowNullAction = false)
    {
        return QueueInternal(null, anAction, anUpdateType, anAllowNullAction);
    }

    //Returns the current framecount of the last execution
    public static int Queue(IEnumerable<Action> someActions, UpdateTypes anUpdateType = UpdateTypes.UPDATE, bool anAllowNullAction = false)
    {
        return QueueInternal(someActions, null, anUpdateType, anAllowNullAction);
    }

    //Returns the current framecount of the last execution
    public static int QueueInternal(IEnumerable<Action> someActions, Action anAction, UpdateTypes anUpdateType = UpdateTypes.UPDATE, bool anAllowNullAction = false)
    {
        Debug.Assert(Instance, NULL_INSTANCE_ERROR);

        List<Action> actionsQueue = anUpdateType switch
        {
            UpdateTypes.UPDATE => Instance.m_actionQueuesUpdateFunc,
            UpdateTypes.LATE_UPDATE => Instance.m_actionQueuesLateUpdateFunc,
            UpdateTypes.FIXED_UPDATE => Instance.m_actionQueuesFixedUpdateFunc,
            _ => throw new ArgumentException("Unexpected update type: " + anUpdateType),
        };

        //looks ugly, but it is done to keep the lock statements as much as possible, especially since the mainthread will aquire these locks as well
        if (anAction != null)
            lock (actionsQueue)
                actionsQueue.Add(anAction);
        else if (someActions != null)
            lock (actionsQueue)
                actionsQueue.Add(someActions);
        else
            Debug.Assert(anAllowNullAction, "Null actions are not permited.");

        return FrameCountOfLastExecution(anUpdateType);
    }

    public static void WaitForNextUpdate(UpdateTypes anUpdateType = UpdateTypes.UPDATE, bool anIgnoreIfMainThread = false)
    {
        Debug.Assert(Instance, NULL_INSTANCE_ERROR);

        if (!IsOnMainThread())
        {
            ManualResetEvent resetEvent = anUpdateType switch
            {
                UpdateTypes.UPDATE => Instance.m_eventUpdate,
                UpdateTypes.LATE_UPDATE => Instance.m_eventLateUpdate,
                UpdateTypes.FIXED_UPDATE => Instance.m_eventFixedUpdate,
                _ => throw new ArgumentException("Unexpected update type: " + anUpdateType),
            };

            resetEvent.WaitOne();
        }
        else
            Debug.Assert(anIgnoreIfMainThread, "Cannot wait for Unity thread because we are already in the mainthread! If this is expected, please make the anIgnoreErrors argument true.");
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
    private void Update()
    {
        ExecuteActions(m_actionQueuesUpdateFunc);
        m_eventUpdate.Reset();

        m_timeConstantsLock.EnterWriteLock();
        m_frameCountOfLastUpdate = Time.frameCount;
        m_timeConstantsLock.ExitWriteLock();
    }
#endif

#if ENABLE_LATEUPDATE_FUNCTION_CALLBACK
    private void LateUpdate()
    {
        ExecuteActions(m_actionQueuesLateUpdateFunc);
        m_eventLateUpdate.Reset();

        m_timeConstantsLock.EnterWriteLock();
        m_frameCountOfLastLateUpdate = Time.frameCount;
        m_timeConstantsLock.ExitWriteLock();
    }
#endif

#if ENABLE_FIXEDUPDATE_FUNCTION_CALLBACK
    private void FixedUpdate()
    {
        ExecuteActions(m_actionQueuesFixedUpdateFunc);
        m_eventFixedUpdate.Reset();

        m_timeConstantsLock.EnterWriteLock();
        m_frameCountOfLastFixedUpdate = Time.frameCount;
        m_timeConstantsLock.ExitWriteLock();
    }
#endif

    public void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }
}