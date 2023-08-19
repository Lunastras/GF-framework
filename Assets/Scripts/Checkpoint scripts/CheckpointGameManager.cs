
public class CheckpointGameManager : CheckpointState
{
    public float SecondsSinceStart = 0;
    public int EnemiesKilled = 0;

    public GameState CurrentState = 0;

    public override void ExecuteCheckpointState()
    {
        GfLevelManager.SetCheckpointState(this);
    }
}