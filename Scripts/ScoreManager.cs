using Godot;
using System;

public partial class ScoreManager : Node
{
    public int CurrentScore { get; private set; }
    public int GamesPlayed { get; private set; }
    public int TopScore { get; private set; }
    public float AverageScore { get; private set; }

    private const string SavePath = "user://score_data.cfg";

    public override void _Ready()
    {
        LoadScore();
    }

    public void AddScore(int points)
    {
        CurrentScore += points;
        if (CurrentScore > TopScore)
        {
            TopScore = CurrentScore;
        }
        SaveScore();
    }

    public void ResetScore()
    {
        CurrentScore = 0;
    }

    public void GameEnded()
    {
        GamesPlayed++;
        AverageScore = (float)TopScore / GamesPlayed; // Simple average, consider better methods
        SaveScore();
    }

    private void SaveScore()
    {
        var config = new ConfigFile();
        config.SetValue("Score", "current_score", CurrentScore);
        config.SetValue("Score", "games_played", GamesPlayed);
        config.SetValue("Score", "top_score", TopScore);
        config.SetValue("Score", "average_score", AverageScore);
        config.Save(SavePath);
    }

    private void LoadScore()
    {
        var config = new ConfigFile();
        Error err = config.Load(SavePath);
        if (err == Error.Ok)
        {
            CurrentScore = config.GetValue("Score", "current_score", 0).AsInt32();
            GamesPlayed = config.GetValue("Score", "games_played", 0).AsInt32();
            TopScore = config.GetValue("Score", "top_score", 0).AsInt32();
            AverageScore = config.GetValue("Score", "average_score", 0f).AsSingle();
        }
        else
        {
            GD.Print("Failed to load score data, using defaults.");
        }
    }
}
