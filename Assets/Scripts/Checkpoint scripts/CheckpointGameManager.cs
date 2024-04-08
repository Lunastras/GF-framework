
public class CheckpointGameManager : CheckpointState
{
    public GameState CurrentState = 0;

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
        GfManagerLevel.SetCheckpointState(this);
    }
}