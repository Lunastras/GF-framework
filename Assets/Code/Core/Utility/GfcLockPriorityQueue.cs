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
    public GfcLockKey Lock(object anObjectHandle, int aPriority = 0, bool anInclusivePriority = true, bool aPrintErrors = true)
    {
        GfcLockKey key = default;
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

    public bool Unlock(ref GfcLockKey aKey, bool aPrintErrors = true)
    {
        int anIndex = FindKey(aKey);
        if (anIndex >= 0)
        {
            GfcLockHandle handle = m_handles[anIndex];
            handle.Unlock(ref aKey);
            m_handles[anIndex] = handle;
        }
        else if (aPrintErrors)
            Debug.LogError("Could not find a lock with the given key.");

        FlushInvalidHandles();
        return m_handles.Count == 0;
    }

    public bool UnlockAll(ref GfcLockKey aKey, bool aPrintErrors = true)
    {
        int count = m_handles.Count;
        for (int i = 0; i < count; i++)
            m_handles[i].Unlock(ref aKey, aPrintErrors);

        FlushInvalidHandles();
        return m_handles.Count == 0;
    }

    public bool AuthorityTest(GfcLockKey aKey) { return GetHeadCopy().AuthorityTest(aKey); }

    public bool KeyTest(GfcLockKey aKey) { return FindKey(aKey) >= 0; }

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

    private int FindKey(GfcLockKey aKey)
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
    public GfcLockKey m_key;
    public object ObjectHandle { get; private set; }
    public bool InclusivePriority { get; private set; }
    public readonly int Priority { get { return m_key.Priority; } }

    public GfcLockKey Lock(object anObjectHandle, int aPriority = 0, bool anInclusivePriority = false, bool aPrintErrors = true)
    {
        Debug.Assert(anObjectHandle != null, "The object passed is null.");

        GfcLockKey key = default;
        if (!Locked() || m_key.Priority < aPriority)
        {
            key = m_key = new(aPriority);
            ObjectHandle = anObjectHandle;
            Debug.Assert(m_key.Valid());
            InclusivePriority = anInclusivePriority;
        }
        else if (aPrintErrors)
            Debug.LogError("Failed to aquire lock, lock is owned by object (" + ObjectHandle + ").");

        return key;
    }

    public bool Unlock(ref GfcLockKey aKey, bool aPrintErrors = true)
    {
        if (AuthorityTest(aKey))
        {
            ObjectHandle = null;
            m_key = default;
            InclusivePriority = false;
            aKey = default;
        }
        else if (aPrintErrors)
            Debug.LogError("Failed to unlock lock, lock is owned by object (" + ObjectHandle + ")."); //cool sentence

        return ObjectHandle == null;
    }

    public readonly bool AuthorityTest(GfcLockKey aKey) { return KeyTest(aKey) || ObjectHandle == null || aKey.Priority > Priority || (InclusivePriority && aKey.Priority == Priority); }
    public readonly bool KeyTest(GfcLockKey aKey) { return m_key.Key.Equals(aKey.Key); }
    public readonly bool Locked() { return ObjectHandle != null && m_key.Valid(); }

    public readonly bool Equals(GfcLockHandle anOther) { return Priority == anOther.Priority && KeyTest(anOther.m_key) && ObjectHandle == anOther.ObjectHandle; }
    public readonly int CompareTo(GfcLockHandle aData)
    {
        int priorityDiff = Priority - aData.Priority;
        if (priorityDiff == 0)
            return m_key.Key.CompareTo(aData.m_key.Key);
        return priorityDiff.Sign();
    }
}

public class GfcLockHandleShared : IGfcLockHandle
{
    private GfcLockHandle m_handle = default;

    public GfcLockKey Lock(object anObjectHandle, int aPriority = 0, bool anInclusivePriority = false, bool aPrintErrors = true) { return m_handle.Lock(anObjectHandle, aPriority, anInclusivePriority, aPrintErrors); }
    public bool Unlock(ref GfcLockKey aKey, bool aPrintErrors = true) { return m_handle.Unlock(ref aKey, aPrintErrors); }
    public bool AuthorityTest(GfcLockKey aKey) { return m_handle.AuthorityTest(aKey); }
    public bool KeyTest(GfcLockKey aKey) { return m_handle.KeyTest(aKey); }
    public bool Locked() { return m_handle.Locked(); }

    public GfcLockHandle GetHandleCopy() { return m_handle; }
}

public interface IGfcLockHandle
{
    public GfcLockKey Lock(object anObjectHandle, int aPriority = 0, bool anInclusivePriority = false, bool aPrintErrors = true);
    public bool Unlock(ref GfcLockKey aKey, bool aPrintErrors = true);
    public bool AuthorityTest(GfcLockKey aKey);
    public bool KeyTest(GfcLockKey aKey);
    public bool Locked();
}

public struct GfcLockKey : IEquatable<GfcLockKey>
{
    public GfcTimeStamp Key { get; private set; }
    public int Priority { get; private set; }

    public GfcLockKey(int aPriority)
    {
        Key = new(0);
        Priority = aPriority;
    }

    public GfcLockKey(GfcInputLockPriority aPriority)
    {
        Key = new(0);
        Priority = (int)aPriority;
    }

    public GfcLockKey(GfcTimeStamp aKey, int aPriority = 0)
    {
        Key = aKey;
        Priority = aPriority;
    }

    public GfcLockKey(GfcTimeStamp aKey, GfcInputLockPriority aPriority = GfcInputLockPriority.BASE)
    {
        Key = aKey;
        Priority = (int)aPriority;
    }

    public readonly bool Valid() { return Key.Valid(); }
    public readonly bool Equals(GfcLockKey anOther) { return Priority == anOther.Priority && Key.Equals(anOther.Key); }
}