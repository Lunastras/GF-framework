using UnityEngine;

public class GfgGameStateSetterInput : MonoBehaviour
{
    public GfgGameStateSetDescriptor GameStateToToggle = new() { State = GfcGameState.INVALID, SmoothTransition = true, Transition = GfxTransitionType.BLACK_FADE };
    [SerializeField] private GfcInputType m_toggleStateInput = GfcInputType.BACK;
    [SerializeField] private GfcGameState[] m_statesWhereToggleEnabled;

    [HideInInspector] public bool CanToggleGameState = true;
    [HideInInspector] private string InputPrompt = "Back";

    GfcInputTracker m_toggleInputTracker;

    void Awake()
    {
        m_toggleInputTracker = new(m_toggleStateInput);
        m_toggleInputTracker.DisplayPrompt = !InputPrompt.IsEmpty();
        m_toggleInputTracker.ParentGameObject = gameObject;
        if (m_toggleInputTracker.DisplayPrompt)
            m_toggleInputTracker.DisplayPromptString = new(InputPrompt);
    }

    void Update()
    {
        if (CanToggleGameState)
        {
            bool validState = m_statesWhereToggleEnabled.Length == 0;
            GfcGameState gameState = GfgManagerGame.GetGameState();

            for (int i = 0; !validState && i < m_statesWhereToggleEnabled.Length; i++)
                validState = gameState == m_statesWhereToggleEnabled[i];

            if (validState && !GfgManagerGame.GameStateTransitioningFadeInPhase() && m_toggleInputTracker.PressedSinceLastCheck())
                GfgManagerGame.SetGameState(GameStateToToggle);
        }
    }
}