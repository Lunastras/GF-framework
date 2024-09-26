using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgGameStateSetter : MonoBehaviour
{
    [SerializeField] private GfcGameState m_gameState;

    [SerializeField] private bool m_smoothTransition;

    [SerializeField] bool m_allowQueueingIfNull = false;

    // Start is called before the first frame update

    void SetGameState(bool anSkipIfInstanceIsNull)
    {
        if (!GfgManagerSceneLoader.CurrentlyLoading && (GfgManagerGame.Instance || !anSkipIfInstanceIsNull))
        {
            Debug.Assert(GfgManagerGame.Instance);
            GfgManagerGame.SetGameState(m_gameState, m_smoothTransition, false, m_allowQueueingIfNull);
            Destroy(this);
        }
    }

    void Awake() { SetGameState(true); }
    void Start() { SetGameState(false); }
}