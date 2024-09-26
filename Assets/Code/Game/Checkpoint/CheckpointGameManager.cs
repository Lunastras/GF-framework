
public class CheckpointGameManager : CheckpointState
{
    public LevelState CurrentState = 0;

    public float SecondsSinceStart = 0;

    public int EnemiesKilled = 0;

    public EnvironmentLightingColors EnvColors;

    public EnvironmentLightingColors DesiredColors;

    public EnvironmentLightingColors DefaultColors;

    public float EnvSmoothingProgress;

    public float EnvSmoothingDuration;

    public float EnvSmoothingRef;

    public override void ExecuteCheckpointState()
    {
        GfgManagerLevel.SetCheckpointState(this);
    }
}