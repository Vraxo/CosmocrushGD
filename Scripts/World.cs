using Godot;
using CosmocrushGD;

namespace Cosmocrush;

public partial class World : WorldEnvironment
{
	[Export] private PackedScene pauseMenuScene;
	[Export] private Button pauseButton;
	[Export] private CanvasLayer hudLayer; // Reference the existing HUD layer
	[Export] private Label scoreLabel; // Add reference to the ScoreLabel

	private PauseMenu pauseMenu;

	public override void _Ready()
	{
		if (hudLayer == null)
		{
			GD.PrintErr("HUD Layer reference not set in World!");
		}
		if (scoreLabel == null)
		{
			GD.PrintErr("ScoreLabel reference not set in World!");
		}

		pauseButton.Pressed += OnPauseButtonPressed;

		// Initialize ScoreManager and set ScoreLabel text
		if (ScoreManager.Instance != null && scoreLabel != null)
		{
			UpdateScoreLabel(); // Initial update
		}
		else
		{
			GD.PrintErr("ScoreManager Instance or ScoreLabel is null in World!");
		}

		if (GameStatsManager.Instance != null)
		{
			GameStatsManager.Instance.GameStarted();
		}
		else
		{
			GD.PrintErr("GameStatsManager Instance is null in World!");
		}

		// Connect to EnemyDied signal for existing enemies and future spawned enemies
		EnemySpawner enemySpawner = GetNode<EnemySpawner>("EnemySpawner");
		if (enemySpawner != null)
		{
			ConnectToEnemySignals(enemySpawner);
			enemySpawner.EnemySpawned += ConnectToEnemySignals; // Connect to future spawned enemies
		}
		else
		{
			GD.PrintErr("EnemySpawner not found in World!");
		}
	}

	private void ConnectToEnemySignals(Node enemyContainer)
	{
		foreach (Node enemyNode in enemyContainer.GetChildren())
		{
			if (enemyNode is BaseEnemy enemy)
			{
				enemy.EnemyDied += OnEnemyDied;
			}
		}
	}

	private void OnEnemyDied()
	{
		ScoreManager.Instance?.IncrementScore();
	}

	private void UpdateScoreLabel()
	{
		if (ScoreManager.Instance != null && scoreLabel != null)
		{
			scoreLabel.Text = $"Score: {ScoreManager.Instance.CurrentScore}";
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel") && !GetTree().Paused)
		{
			Pause();
		}

		UpdateScoreLabel(); // Update score label every frame
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
