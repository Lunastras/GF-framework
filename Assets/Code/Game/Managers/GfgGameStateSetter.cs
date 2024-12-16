using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfgGameStateSetter : MonoBehaviour
{
    [SerializeField] private GfcGameState m_gameState;

    [SerializeField] private bool m_smoothTransition;

    [SerializeField] private GfxTransitionType Transition;

    void SetGameState(bool anSkipIfInstanceIsNull)
    {
        if (GfgManagerGame.Instance || !anSkipIfInstanceIsNull)
        {
            Debug.Assert(GfgManagerGame.Instance);
            GfgManagerSceneLoader.RequestGameStateAfterLoad(m_gameState, m_smoothTransition, Transition);
            Destroy(this);
        }
    }

    void Awake() { SetGameState(true); }
    void Start() { SetGameState(false); }
}