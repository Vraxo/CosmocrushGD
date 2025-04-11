using Godot;
using System;

namespace CosmocrushGD;

public partial class ScoreManager : Node
{
    private const string SavePath = "user://score.dat"; // Path to save score
    [Signal] public delegate void ScoreUpdatedEventHandler(int score);
    public int CurrentScore { get; private set; }
    public static ScoreManager Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadScore(); // Load score when game starts
        }
        else
        {
            QueueFree(); // Destroy duplicate instances
        }
    }

    public void IncrementScore(int points = 1)
    {
        CurrentScore += points;
        EmitSignal(SignalName.ScoreUpdated, CurrentScore);
        SaveScore(); // Save score immediately after increment
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        SaveScore(); // Save reset score
    }

    public void GameOver()
    {
        Cosmocrush.GameStatsManager.Instance?.GameFinished(CurrentScore);
        ResetScore();
    }

    private void LoadScore()
    {
        if (FileAccess.FileExists(SavePath))
        {
            var saveFile = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            if (saveFile != null)
            {
                CurrentScore = (int)saveFile.Get32();
                saveFile.Close();
            }
        }
        else
        {
            CurrentScore = 0; // Default score if no save file
        }
    }

    private void SaveScore()
    {
        var saveFile = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (saveFile != null)
        {
            saveFile.Store32((uint)CurrentScore);
            saveFile.Close();
        }
    }
}
