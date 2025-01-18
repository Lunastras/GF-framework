using UnityEngine;

public class GfgPauseToggle : MonoBehaviour
{
    private static GfgPauseToggle Instance;

    [SerializeField] private GameObject m_pauseScreen;
    [SerializeField] private GfcGameState[] m_statesWherePauseValid;

    private GfcInputTracker m_pauseInputTracker;
    private bool m_paused = false;
    private float m_cachedTimeScale = 0;
    private bool m_canPause = true;

    private bool m_cachedCursorVisible;
    private CursorLockMode m_cachedCursorLockMode;
    private GfcLockKey m_lockKey;

    public static bool Paused { get { return Instance.m_paused; } }
    public static bool CanPause
    {
        get { return Instance.m_canPause && !GfgManagerGame.GameStateTransitioningFadeInPhase(); }
        set
        {
            Instance.m_canPause = value;
            if (value == false)
                SetPause(false);
        }
    }

    public static float GetPauseTimeScale() { return GfgManagerGame.IsMultiplayer ? GfgManagerGame.GetTimeScale() : 0; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Debug.Assert(m_pauseScreen);
        this.SetSingleton(ref Instance);
        m_pauseScreen.SetActive(m_paused);
        m_pauseInputTracker = new(GfcInputType.PAUSE) { DisplayPrompt = false, Key = new((int)GfcInputLockPriority.PAUSE) };
    }

    void Update()
    {
        if ((CanPause || Paused) && (!Paused || !GfgMenuPause.Instance || GfgMenuPause.Instance.ShowingMainScreen))
        {
            GfcGameState gameState = GfgManagerGame.GetGameState();
            int i = 0, length = m_statesWherePauseValid.Length;
            while (i < length && gameState != m_statesWherePauseValid[i++]) ;
            if (gameState == m_statesWherePauseValid[--i] && m_pauseInputTracker.PressedSinceLastCheck())
                TogglePause();
        }
    }

    public static void TogglePause() { SetPause(!Instance.m_paused); }
    public static void SetPause(bool aPaused)
    {
        bool paused = Paused;
        if (paused != aPaused)
        {
            if (aPaused)
            {
                Instance.m_cachedTimeScale = Time.timeScale;
                Instance.m_cachedCursorVisible = Cursor.visible;
                Instance.m_cachedCursorLockMode = Cursor.lockState;

                Time.timeScale = GetPauseTimeScale();
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                Instance.m_lockKey = GfcInput.InputLockHandle.Lock(Instance, (int)GfcInputLockPriority.PAUSE);
            }
            else
            {
                Debug.Assert(Time.timeScale == GetPauseTimeScale(), "TimeScale was changed while paused.");
                Time.timeScale = Instance.m_cachedTimeScale;

                Cursor.visible = Instance.m_cachedCursorVisible;
                Cursor.lockState = Instance.m_cachedCursorLockMode;

                GfcInput.InputLockHandle.Unlock(ref Instance.m_lockKey);
            }

            Instance.m_pauseScreen.SetActiveGf(aPaused);
            Instance.m_paused = aPaused;
        }
    }
}