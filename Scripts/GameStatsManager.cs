using Godot;
using System;

namespace CosmocrushGD
{
    public class GameStatsManager
    {
        private const string SavePath = "user://game_stats.dat";
        public static GameStatsManager Instance { get; } = new GameStatsManager();

        public int TotalScore { get; private set; }
        public int GamesPlayed { get; private set; }
        public int TopScore { get; private set; }
        public int AverageScore { get; private set; }
        public int CurrentGameScore { get; set; }

        private GameStatsManager()
        {
            LoadStats();
        }

        public void StartNewGame()
        {
            GamesPlayed++;
            CurrentGameScore = 0;
            SaveStats();
        }

        public void EndGame()
        {
            TotalScore += CurrentGameScore;
            if (CurrentGameScore > TopScore)
            {
                TopScore = CurrentGameScore;
            }
            AverageScore = TotalScore / GamesPlayed;
            SaveStats();
        }

        public void AddScore(int score)
        {
            CurrentGameScore += score;
        }

        private void LoadStats()
        {
            if (FileAccess.FileExists(SavePath))
            {
                using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read))
                {
                    if (file != null)
                    {
                        TotalScore = file.Get32();
                        GamesPlayed = file.Get32();
                        TopScore = file.Get32();
                        AverageScore = file.Get32();
                    }
                }
            }
            else
            {
                ResetStats(); // Initialize stats if save file doesn't exist
            }
        }

        private void SaveStats()
        {
            using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
            {
                if (file != null)
                {
                    file.Store32(TotalScore);
                    file.Store32(GamesPlayed);
                    file.Store32(TopScore);
                    file.Store32(AverageScore);
                }
            }
        }
        
        private void ResetStats()
        {
            TotalScore = 0;
            GamesPlayed = 0;
            TopScore = 0;
            AverageScore = 0;
            SaveStats(); // Save initial stats to file
        }
    }
}
