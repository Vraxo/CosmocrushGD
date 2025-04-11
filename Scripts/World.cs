using Godot;

namespace CosmocrushGD;

using Godot;

public partial class World : WorldEnvironment
{
	[Export] private PackedScene pauseMenuScene;
	[Export] private Button pauseButton;
	[Export] private CanvasLayer hudLayer; // Reference the existing HUD layer
	[Export] private EnemySpawner enemySpawner; // Add reference to EnemySpawner
	[Export] private Player Player; // Add reference to Player

	private PauseMenu pauseMenu;
	private ScoreManager scoreManager; // Add ScoreManager reference
	private GameStatsManager gameStatsManager; // Add GameStatsManager reference

	public override void _Ready()
	{
		if (hudLayer == null)
		{
			GD.PrintErr("HUD Layer reference not set in World!");
		}

		gameStatsManager = GameStatsManager.Instance;
		gameStatsManager.StartNewGame();

		// Create score label
		Label scoreLabel = new Label();
		scoreLabel.Name = "ScoreLabel";
		scoreLabel.Text = "Score: 0";
		scoreLabel.HorizontalAlignment = HorizontalAlignment.Left;
		scoreLabel.VerticalAlignment = VerticalAlignment.Top;
		scoreLabel.Position = new Vector2(10, 10); // Add some margin
		hudLayer.AddChild(scoreLabel);

		// Create ScoreLabelUpdater
		ScoreLabelUpdater scoreLabelUpdater = new ScoreLabelUpdater();
		scoreLabelUpdater.Name = "ScoreLabelUpdater";
		hudLayer.AddChild(scoreLabelUpdater);

		pauseButton.Pressed += OnPauseButtonPressed;

		// Get ScoreManager instance
		scoreManager = ScoreManager.Instance;
		if (scoreManager == null)
		{
			GD.PushError("ScoreManager Instance is null in World!");
		}

		if (enemySpawner != null)
			GD.PushError("EnemySpawner is not set in World!");
		}
		if (Player != null)
		{
			Player.PlayerDied += OnPlayerDied;
		}
		else
		{
			GD.PushError("Player is not set in World!");
		}
	}

	private void OnPlayerDied()
	{
		gameStatsManager.EndGame();
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel") && !GetTree().Paused)
		{
			Pause();
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

	private void OnEnemySpawned(BaseEnemy enemy)
	{
		enemy.EnemyKilled += OnEnemyKilled;
	}

	private void OnEnemyKilled()
	{
		if (scoreManager != null)
		{
			scoreManager.IncrementScore(); // Increment score when enemy is killed
		}
	}
}
