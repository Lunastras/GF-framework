using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvManagerLevel : GfgManagerLevel
{
    protected static InvManagerLevel m_invInstance = null;
    protected StatsPlayer m_statsPlayer = null;

    [SerializeField] protected HudManager m_hudManager = null;

    protected bool m_isShowingDeathScreen = false;

    // Start is called before the first frame update
    new protected void Awake()
    {
        if (m_invInstance)
            Destroy(m_invInstance);
        m_invInstance = this;

        base.Awake();
        if (null == m_hudManager) m_hudManager = GetComponent<HudManager>();
    }

    // Update is called once per frame
    new void Update()
    {
        base.Update();
        if (m_isShowingDeathScreen && Input.GetKeyDown(KeyCode.Space))
        {
            m_isShowingDeathScreen = false;
            HudManager.ToggleDeathScreen(false);
            GfgCheckpointManager.Instance.ResetToHardCheckpoint();
            GfgCameraController.SnapToTargetInstance();
            GfgManagerLevel.SetLevelMusicPitch(1, 0.2f);
            m_deathMusicSource?.GetAudioSource()?.Stop();
        }
    }

    public static StatsPlayer GetPlayerStats()
    {
        return m_invInstance.m_statsPlayer;
    }

    public static void PlayerDied()
    {
        HudManager.ToggleDeathScreen(true);
        m_invInstance.m_isShowingDeathScreen = true;
        GfgManagerLevel.SetLevelMusicPitch(0, 2);
        m_invInstance.m_deathMusicSource = m_invInstance.m_deathScreenMusic.Play();
        m_invInstance.m_deathScreenMusic.SetMixerVolume(0);
        m_invInstance.m_deathMusicVolumeSmoothRef = 0;
        m_invInstance.m_desiredDeathMusicVolume = 1;
        m_invInstance.m_currentDeathMusicVolume = 0;
    }

    protected override void EndLevelInternal()
    {
        if (m_invInstance.m_currentGameState != GameState.LEVEL_ENDED)
        {
            m_invInstance.m_currentGameState = GameState.LEVEL_ENDED;

            Instance.CanPause = false;
            if (m_invInstance.m_isPaused)
                PauseToggle();

            bool firstFinish = GfgManagerSaveData.GetActivePlayerSaveData().CompletedMission(m_invInstance.m_missionIndex);
            OnLevelEnd?.Invoke();
        }
    }

    protected override float OnCheckpointResetInternal(GfgCheckpointManager GfgCheckpointManager, bool hardCheckpoint)
    {
        float delay = 0;
        if (GfgCheckpointManager.transform == GetPlayer())
        {
            if (hardCheckpoint)
            {
                HudManager.ResetHardCheckpointVisuals();
                GameParticles.ClearParticles();
            }
            else
                delay = HudManager.ResetSoftCheckpointVisuals();
        }

        return delay;
    }

    protected override void OnCheckpointSetInternal(GfgCheckpointManager GfgCheckpointManager, bool hardCheckpoint)
    {
        if (GfgCheckpointManager.transform == GetPlayer())
        {
            if (hardCheckpoint)
                HudManager.TriggerHardCheckpointVisuals();
            else
                HudManager.TriggerSoftCheckpointVisuals();
        }
    }

    public static void SetPlayer(StatsPlayer player)
    {
        m_invInstance.m_player = player.transform;
        m_invInstance.m_statsPlayer = player;
    }

    public static HudManager GetHudManager() { return m_invInstance.m_hudManager; }
}
