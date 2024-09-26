using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class GfgGameStateObject : MonoBehaviour
{
    public GfcGameState[] GameStates;

    [SerializeField] public bool m_instantDisable;

    private void Start()
    {
        GfgManagerGame.Instance.OnGameStateChanged += OnGameStateChanged;
        GfcGameState currentGameState = GfgManagerGame.GetGameState();

        if (currentGameState != GfcGameState.INVALID)
            OnGameStateChanged(currentGameState, GfcGameState.INVALID);

        m_instantDisable = true;
    }

    private void OnDestroy() { if (GfgManagerGame.Instance) GfgManagerGame.Instance.OnGameStateChanged -= OnGameStateChanged; }

    private CoroutineHandle m_disableCoroutine;

    private void OnGameStateChanged(GfcGameState aNewGameState, GfcGameState aOldGameState)
    {
        if (GameStates.Length == 0)
            return;

        bool active = false;

        for (int i = 0; i < GameStates.Length && !active; i++)
            active = GameStates[i] == aNewGameState;

        if (active)
        {
            gameObject.SetActive(true);

            if (m_disableCoroutine.IsValid) Timing.KillCoroutines(m_disableCoroutine);
            m_disableCoroutine = default;
        }
        else if (m_instantDisable)
        {
            gameObject.SetActive(false);
        }
        else if (!m_disableCoroutine.IsValid)
            m_disableCoroutine = gameObject.SetActiveInFrames(false, 1);
    }
}