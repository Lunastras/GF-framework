using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public struct GfcNetworkVariable<T>
{
    public NetworkVariable<T> NetworkVariable;

    private T m_value;

    private bool m_initialized;

    private const string INITIALIZED_ERROR = "The struct was not initialized properly with a new()";

    public GfcNetworkVariable(NetworkBehaviour aNetworkBehaviourParent, T aValue = default,
            NetworkVariableReadPermission aReadPerm = NetworkVariableBase.DefaultReadPerm,
            NetworkVariableWritePermission aWritePerm = NetworkVariableBase.DefaultWritePerm)
    {
        m_initialized = true;
        m_value = aValue;
        NetworkVariable = NetworkManager.Singleton && aNetworkBehaviourParent ? new(aValue, aReadPerm, aWritePerm) : null;
    }

    private readonly void Validate() { Debug.Assert(m_initialized, INITIALIZED_ERROR); }

    public T Value
    {
        readonly get
        {
            Validate();
            return NetworkVariable != null ? NetworkVariable.Value : m_value;
        }

        set
        {
            Validate();
            if (NetworkVariable != null)
                NetworkVariable.Value = value;
            else
                m_value = value;
        }
    }
}