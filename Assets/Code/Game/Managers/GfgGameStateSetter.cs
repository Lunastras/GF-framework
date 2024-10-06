using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgGameStateSetter : MonoBehaviour
{
    [SerializeField] private GfcGameState m_gameState;

    [SerializeField] private bool m_smoothTransition;

    void SetGameState(bool anSkipIfInstanceIsNull)
    {
        if (!GfgManagerSceneLoader.CurrentlyLoading && (GfgManagerGame.Instance || !anSkipIfInstanceIsNull))
        {
            Debug.Assert(GfgManagerGame.Instance);
            GfgManagerGame.SetGameState(m_gameState, m_smoothTransition);
            Destroy(this);
        }
    }

    void Awake() { SetGameState(true); }
    void Start() { SetGameState(false); }
}