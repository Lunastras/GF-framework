using UnityEngine;

public class GfgGameStateReturnToPrevious : MonoBehaviour
{
    public bool SmoothTransition;
    public GfxTransitionType Transition;
    public GfcInputType ToggleStateInput = GfcInputType.BACK;
    private string InputPrompt = "Back";

    [HideInInspector] public bool CanToggleGameState = true;
    GfcInputTracker m_toggleInputTracker;

    void Awake()
    {
        m_toggleInputTracker = new(ToggleStateInput);
        m_toggleInputTracker.DisplayPrompt = !InputPrompt.IsEmpty();
        m_toggleInputTracker.ParentGameObject = gameObject;

    }

    void Start()
    {
        if (m_toggleInputTracker.DisplayPrompt) m_toggleInputTracker.DisplayPromptString = new(InputPrompt);
    }

    void Update()
    {
        if (CanToggleGameState && !GfgManagerGame.GameStateTransitioningFadeInPhase() && m_toggleInputTracker.PressedSinceLastCheck())
            GfgManagerGame.SetGameState(GfgManagerGame.GetPreviousGameState(), SmoothTransition, Transition);
    }
}
