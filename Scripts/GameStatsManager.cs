using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cosmocrush;

public partial class GameStatsManager : Node
{
    private const string SavePath = "user://game_stats.json";

    public int GamesPlayed { get; private set; }
    public int TotalScore { get; private set; }
    public int TopScore { get; private set; }
    public float AverageScore { get; private set; }

    public static GameStatsManager Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadStats();
        }
        else
        {
            QueueFree(); // Destroy duplicate instances
        }
    }

    public void GameStarted()
    {
        GamesPlayed++;
    }

    public void GameFinished(int currentScore)
    {
        TotalScore += currentScore;
        if (currentScore > TopScore)
        {
            TopScore = currentScore;
        }
        UpdateAverageScore();
        SaveStats();
    }

    private void UpdateAverageScore()
    {
        if (GamesPlayed > 0)
        {
            AverageScore = (float)TotalScore / GamesPlayed;
        }
        else
        {
            AverageScore = 0;
        }
    }

    private void LoadStats()
    {
        if (FileAccess.FileExists(SavePath))
        {
            var saveFile = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            if (saveFile != null)
            {
                var jsonString = saveFile.GetAsText();
                saveFile.Close();

                if (!string.IsNullOrEmpty(jsonString))
                {
                    var saveData = Json.ParseString(jsonString) as Dictionary<string, Variant>;
                    if (saveData != null)
                    {
                        GamesPlayed = saveData.TryGetValue("GamesPlayed", out var gamesPlayedVariant) ? gamesPlayedVariant.AsInt32() : 0;
                        TotalScore = saveData.TryGetValue("TotalScore", out var totalScoreVariant) ? totalScoreVariant.AsInt32() : 0;
                        TopScore = saveData.TryGetValue("TopScore", out var topScoreVariant) ? topScoreVariant.AsInt32() : 0;
                        AverageScore = saveData.TryGetValue("AverageScore", out var averageScoreVariant) ? averageScoreVariant.AsSingle() : 0;
                    }
                }
            }
        }
    }

    private void SaveStats()
    {
        var saveData = new Dictionary<string, Variant>
        {
            {"GamesPlayed", GamesPlayed},
            {"TotalScore", TotalScore},
            {"TopScore", TopScore},
            {"AverageScore", AverageScore}
        };

        var saveFile = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (saveFile != null)
        {
            saveFile.StoreString(Json.Stringify(saveData));
            saveFile.Close();
        }
    }
}
