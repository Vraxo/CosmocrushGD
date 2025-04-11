using Godot;
using System;

public partial class ScoreLabelUpdater : Label
{
	private ScoreManager _scoreManager;

	public override void _Ready()
	{
		// Get the ScoreManager singleton
		_scoreManager = GetNode<ScoreManager>("/root/ScoreManager");
		if (_scoreManager == null)
		{
			GD.PrintErr("ScoreManager not found in ScoreLabelUpdater.");
			return; // Cannot proceed without ScoreManager
		}

		// Connect to the signal
		_scoreManager.ScoreUpdated += OnScoreUpdated;

		// Set initial score text
		OnScoreUpdated(_scoreManager.CurrentScore);
	}

	public override void _ExitTree()
	{
		// Disconnect the signal when the node is removed from the scene tree
		// to prevent potential memory leaks or errors.
		if (_scoreManager != null)
		{
			_scoreManager.ScoreUpdated -= OnScoreUpdated;
		}
	}

	private void OnScoreUpdated(int newScore)
	{
		// Update the label's text
		Text = $"Score: {newScore}";
	}
}
