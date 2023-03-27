using System;
using System.Collections.Generic;

[System.Serializable]
public struct PriorityValueSetter<T>
{
    public T m_value;
    public uint m_priority;

    public bool m_overridePriority;

    public bool m_ignore; //if the value should be discarded

    public PriorityValueSetter(T value = default, uint priority = 0, bool overridePriority = false, bool ignore = false)
    {
        m_value = value;
        m_priority = priority;
        m_overridePriority = overridePriority;
        m_ignore = ignore;
    }

    public static implicit operator T(PriorityValueSetter<T> d) => d.m_value;


}

[System.Serializable]
public struct PriorityValue<T>
{
    private T m_value;
    private uint m_priority;

    public static implicit operator T(PriorityValue<T> d) => d.m_value;

    public PriorityValue(T value = default, uint priority = 0)
    {
        m_value = value;
        m_priority = priority;
    }

    public bool SetValue(PriorityValueSetter<T> values)
    {
        bool changedValue = false;
        if (!values.m_ignore)
            changedValue = SetValue(values.m_value, values.m_priority, values.m_overridePriority);

        return false;
    }

    public bool SetValue(PriorityValue<T> value, bool overridePriority = false)
    {
        return SetValue(value.m_value, value.m_priority, overridePriority);
    }

    public bool SetValue(T value, uint priority = 0, bool overridePriority = false)
    {
        bool changeValue = overridePriority || priority >= m_priority;
        if (changeValue)
        {
            m_priority = priority;
            m_value = value;
        }

        return changeValue;
    }

    public bool CanSet(uint priority, bool overridePriority = false)
    {
        return overridePriority || priority >= m_priority;
    }

    public T GetValue() { return m_value; }

    public uint GetPriority() { return m_priority; }

    public static bool operator ==(T lhs, PriorityValue<T> rhs)
    {
        return EqualityComparer<T>.Default.Equals(lhs, rhs.m_value);
    }

    public static bool operator ==(PriorityValue<T> lhs, T rhs)
    {
        return EqualityComparer<T>.Default.Equals(lhs.m_value, rhs);
    }

    public static bool operator !=(T lhs, PriorityValue<T> rhs)
    {
        return !EqualityComparer<T>.Default.Equals(lhs, rhs.m_value); ;
    }

    public static bool operator !=(PriorityValue<T> lhs, T rhs)
    {
        return !EqualityComparer<T>.Default.Equals(lhs.m_value, rhs);
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        return base.Equals(obj);
    }

    // override object.GetHashCode
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}