using Godot;
using CosmocrushGD; // Added for Player access if needed later

namespace Cosmocrush;

public partial class World : WorldEnvironment
{
	[Export] private PackedScene pauseMenuScene;
	[Export] private Button pauseButton;
	[Export] private CanvasLayer hudLayer; // Reference the existing HUD layer
	[Export] private Label scoreLabel; // Reference to the new score label

	public int Score { get; private set; } = 0; // Public getter, private setter
	private PauseMenu pauseMenu;

	public override void _Ready()
	{
		if (hudLayer == null)
		{
			GD.PrintErr("HUD Layer reference not set in World!");
		}
		if (scoreLabel == null)
		{
			GD.PrintErr("Score Label reference not set in World!");
		}
		if (pauseButton != null) // Check if pauseButton exists before connecting
		{
			pauseButton.Pressed += OnPauseButtonPressed;
		}
		else
		{
			GD.PrintErr("Pause Button reference not set in World!");
		}

		// Increment games played when the world starts
		StatisticsManager.Instance.IncrementGamesPlayed();
		// Note: Saving happens within StatisticsManager methods now, or on quit.

		UpdateScoreLabel(); // Initialize label text
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel") && !GetTree().Paused)
		{
			Pause();
		}
	}

	public void AddScore(int amount)
	{
		Score += amount;
		UpdateScoreLabel();
	}

	private void UpdateScoreLabel()
	{
		if (scoreLabel != null)
		{
			scoreLabel.Text = $"Score: {Score}";
		}
	}

	private void OnPauseButtonPressed()
	{
		if (!GetTree().Paused)
		{
			Pause();
		}
		else if (pauseMenu is not null && IsInstanceValid(pauseMenu))
		{
			pauseMenu.TriggerContinue();
		}
	}

	private void Pause()
	{
		if (GetTree().Paused)
		{
			return;
		}

		if (pauseMenu is null || !IsInstanceValid(pauseMenu))
		{
			if (pauseMenuScene is null)
			{
				GD.PrintErr("PauseMenuScene is not set in World script!");
				return;
			}

			if (hudLayer is null)
			{
				GD.PrintErr("HUD Layer is not set or found in World script!");
				return;
			}

			pauseMenu = pauseMenuScene.Instantiate<PauseMenu>();
			hudLayer.AddChild(pauseMenu);
		}

		GetTree().Paused = true;
		pauseMenu.Show();
	}
}
