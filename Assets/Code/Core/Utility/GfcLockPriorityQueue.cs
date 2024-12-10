using System;
using System.Collections.Generic;
using UnityEngine;

public class GfcLockPriorityQueue : IGfcLockHandle
{
    private List<GfcLockHandle> m_handles;

    public GfcLockPriorityQueue(int anInitialCount = 4)
    {
        m_handles = new(anInitialCount);
    }

    //returns the key
    public GfcTimeStamp Lock(object anObjectHandle, int aPriority = 0, bool anInclusivePriority = true, bool aPrintErrors = true)
    {
        GfcTimeStamp key = default;
        if (!ObjectHasLock(anObjectHandle))
        {
            GfcLockHandle lockHandle = new();
            key = lockHandle.Lock(anObjectHandle, aPriority, anInclusivePriority, aPrintErrors);
            m_handles.AddSorted(lockHandle, Order.DESCENDING);
        }
        else if (aPrintErrors)
            Debug.LogError("The object " + anObjectHandle + " already has a lock.");

        return key;
    }

    public bool Unlock(ref GfcTimeStamp aKey, int aPriority = 0, bool aPrintErrors = true)
    {
        int anIndex = FindKey(aKey);
        if (anIndex >= 0)
        {
            GfcLockHandle handle = m_handles[anIndex];
            handle.Unlock(ref aKey, aPriority);
            m_handles[anIndex] = handle;
        }
        else if (aPrintErrors)
            Debug.LogError("Could not find a lock with the given key.");

        FlushInvalidHandles();
        return m_handles.Count == 0;
    }

    public bool UnlockAll(ref GfcTimeStamp aKey, int aPriority = 0, bool aPrintErrors = true)
    {
        int count = m_handles.Count;
        for (int i = 0; i < count; i++)
            m_handles[i].Unlock(ref aKey, aPriority, false);

        FlushInvalidHandles();
        return m_handles.Count == 0;
    }

    public bool AuthorityTest(GfcTimeStamp aKey, int aPriority = 0) { return GetHeadCopy().AuthorityTest(aKey, aPriority); }

    public bool KeyTest(GfcTimeStamp aKey) { return FindKey(aKey) >= 0; }

    public bool Locked()
    {
        FlushInvalidHandles();
        return m_handles.Count > 0;
    }

    private int GetLockIndexWithObjectHandle(object anObjectHandle)
    {
        Debug.Assert(anObjectHandle != null);
        bool foundLock = false;
        int i = -1, count = m_handles.Count;
        for (; anObjectHandle != null && !foundLock && ++i < count;)
            foundLock = m_handles[i].ObjectHandle == anObjectHandle;
        return i == count ? -1 : i;
    }

    public bool ObjectHasLock(object anObjectHandle) { return GetLockIndexWithObjectHandle(anObjectHandle) >= 0; }

    private int FindKey(GfcTimeStamp aKey)
    {
        int count = m_handles.Count;
        for (int i = 0; i < count; i++)
            if (m_handles[i].KeyTest(aKey))
                return i;
        return -1;
    }

    private void FlushInvalidHandles()
    {
        int headIndex = GetHeadIndex();
        int headPopIncrement = GetHeadPopIndexIncrement();
        int count = m_handles.Count;
        while (count-- != 0 && headIndex >= 0 && !m_handles[headIndex].Locked())
        {
            m_handles.RemoveAt(headIndex);
            headIndex += headPopIncrement;
        }
    }

    public GfcLockHandle GetHeadCopy()
    {
        FlushInvalidHandles();
        int lastIndex = GetHeadIndex();
        return lastIndex >= 0 ? m_handles[lastIndex] : default;
    }

    private int GetHeadIndex() { return m_handles.Count - 1; }
    private int GetHeadPopIndexIncrement() { return GetHeadIndex() == 0 ? 0 : -1; }
}

public struct GfcLockHandle : IGfcLockHandle, IEquatable<GfcLockHandle>, IComparable<GfcLockHandle>
{
    public GfcTimeStamp m_key;
    public object ObjectHandle { get; private set; }
    public int Priority { get; private set; }
    public bool InclusivePriority { get; private set; }

    public GfcTimeStamp Lock(object anObjectHandle, int aPriority = 0, bool anInclusivePriority = false, bool aPrintErrors = true)
    {
        Debug.Assert(anObjectHandle != null, "The object passed is null.");

        GfcTimeStamp key = default;
        if (!Locked() || Priority < aPriority)
        {
            Priority = aPriority;
            ObjectHandle = anObjectHandle;
            key = m_key = new(0);
            Debug.Assert(m_key.Valid());
            InclusivePriority = anInclusivePriority;
        }
        else if (aPrintErrors)
            Debug.LogError("Failed to aquire lock, lock is owned by object (" + ObjectHandle + ").");

        return key;
    }

    public bool Unlock(ref GfcTimeStamp aKey, int aPriority = 0, bool aPrintErrors = true)
    {
        if (AuthorityTest(aKey, aPriority))
        {
            ObjectHandle = null;
            m_key = default;
            Priority = 0;
            InclusivePriority = false;
            aKey = default;
        }
        else if (aPrintErrors)
            Debug.LogError("Failed to unlock lock, lock is owned by object (" + ObjectHandle + ")."); //cool sentence

        return ObjectHandle == null;
    }

    public readonly bool AuthorityTest(GfcTimeStamp aKey, int aPriority = 0) { return KeyTest(aKey) || ObjectHandle == null || aPriority > Priority || (InclusivePriority && aPriority == Priority); }
    public readonly bool KeyTest(GfcTimeStamp aKey) { return m_key.Equals(aKey); }
    public readonly bool Locked() { return ObjectHandle != null && m_key.Valid(); }

    public readonly bool Equals(GfcLockHandle anOther) { return Priority == anOther.Priority && m_key.Equals(anOther.m_key) && ObjectHandle == anOther.ObjectHandle; }
    public readonly int CompareTo(GfcLockHandle aData)
    {
        int priorityDiff = Priority - aData.Priority;
        if (priorityDiff == 0)
            return m_key.CompareTo(aData.m_key);
        return priorityDiff.Sign();
    }
}

public class GfcLockHandleShared : IGfcLockHandle
{
    private GfcLockHandle m_handle = default;

    public GfcTimeStamp Lock(object anObjectHandle, int aPriority = 0, bool anInclusivePriority = false, bool aPrintErrors = true) { return m_handle.Lock(anObjectHandle, aPriority, anInclusivePriority, aPrintErrors); }
    public bool Unlock(ref GfcTimeStamp aKey, int aPriority = 0, bool aPrintErrors = true) { return m_handle.Unlock(ref aKey, aPriority, aPrintErrors); }
    public bool AuthorityTest(GfcTimeStamp aKey, int aPriority = 0) { return m_handle.AuthorityTest(aKey); }
    public bool KeyTest(GfcTimeStamp aKey) { return m_handle.KeyTest(aKey); }
    public bool Locked() { return m_handle.Locked(); }
}

public interface IGfcLockHandle
{
    public GfcTimeStamp Lock(object anObjectHandle, int aPriority = 0, bool anInclusivePriority = false, bool aPrintErrors = true);
    public bool Unlock(ref GfcTimeStamp aKey, int aPriority = 0, bool aPrintErrors = true);
    public bool AuthorityTest(GfcTimeStamp aKey, int aPriority = 0);
    public bool KeyTest(GfcTimeStamp aKey);
    public bool Locked();
}