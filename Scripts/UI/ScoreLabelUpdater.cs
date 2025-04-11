using Godot;
using System;

namespace CosmocrushGD
{
    public partial class ScoreLabelUpdater : Label
    {
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            ScoreManager.Instance.ScoreUpdated += OnScoreUpdated;
            UpdateScoreLabel(ScoreManager.Instance.CurrentScore);
        }

        private void OnScoreUpdated(int newScore)
        {
            UpdateScoreLabel(newScore);
        }

        private void UpdateScoreLabel(int score)
        {
            Text = "Score: " + score.ToString();
        }

        public override void _ExitTree()
        {
            ScoreManager.Instance.ScoreUpdated -= OnScoreUpdated;
        }
    }
}
