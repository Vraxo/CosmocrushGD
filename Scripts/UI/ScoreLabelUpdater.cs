using Godot;
using System;
using CosmocrushGD; // Add this line

public partial class ScoreLabelUpdater : Label
{
    [Export]
    public ScoreManager ScoreManager { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (ScoreManager == null)
        {
            GD.PushError("ScoreManager not set in ScoreLabelUpdater");
            return;
        }
        ScoreManager.ScoreUpdated += UpdateScoreLabel;
        UpdateScoreLabel(ScoreManager.CurrentScore);
    }

    private void UpdateScoreLabel(int score)
    {
        Text = "Score: " + score.ToString();
    }
}
