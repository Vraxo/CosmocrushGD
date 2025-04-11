using Godot;
using System;

namespace CosmocrushGD
{
    public class ScoreManager : Node
    {
        private int _currentScore = 0;

        public static ScoreManager Instance { get; private set; }
        private GameStatsManager _gameStatsManager;

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                _gameStatsManager = GameStatsManager.Instance;
                _currentScore = _gameStatsManager.CurrentGameScore; // Load current game score
            }
            else
            {
                QueueFree(); // Ensure only one instance exists
            }
        }

        public int CurrentScore
        {
            get => _currentScore;
            private set
            {
                _currentScore = value;
                _gameStatsManager.CurrentGameScore = _currentScore; // Update GameStatsManager
                EmitSignal(nameof(ScoreUpdated), _currentScore);
            }
        }

        public int TopScore => _gameStatsManager.TopScore;
        public float AverageScore => _gameStatsManager.AverageScore;
        public int GamesPlayed => _gameStatsManager.GamesPlayed;


        [Signal]
        public delegate void ScoreUpdated(int score);

        public void ResetScore()
        {
            CurrentScore = 0;
        }

        public void IncrementScore()
        {
            CurrentScore += 1; // Increment current score
        }

        public void StartNewGame()
        {
            _gameStatsManager.StartNewGame();
            ResetScore();    // Reset current score for new game
        }

        public void GameEnded()
        {
            _gameStatsManager.EndGame();
        }
    }
}
