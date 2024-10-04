using UnityEngine;

[System.Serializable]
public struct FloatAndInv
{
    public float Value
    {
        get { return m_value; }
        set
        {
            m_value = value;
            m_inverse = value.SafeInverse();
        }
    }

    public FloatAndInv(float aValue = 0)
    {
        m_value = aValue;
        m_inverse = aValue.SafeInverse();
    }

    public void Refresh() { Value = m_value; }

    public readonly float Inverse { get { return m_inverse; } }

    public static implicit operator float(FloatAndInv d) => d.Value;

    [SerializeField] private float m_value;
    private float m_inverse;
}