using Godot;
using System;

namespace CosmocrushGD;

public partial class World : Node2D
{
	[Export]
	public PackedScene MobScene { get; set; }

	private int _score;
	private Label scoreLabel; // Reference to the score label
	private ScoreManager scoreManager;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_score = 0;

		// Instantiate ScoreManager if it doesn't exist
		scoreManager = GetNodeOrNull<ScoreManager>("/root/ScoreManager");
		if (scoreManager == null)
		{
			scoreManager = new ScoreManager();
			GetTree().Root.AddChild(scoreManager);
		}

		// Initialize score label and add it to HUD
		scoreLabel = new Label();
		scoreLabel.Name = "ScoreLabel";
		scoreLabel.Position = new Vector2(10, 10); // Top-left corner, adjust as needed
		scoreLabel.HorizontalAlignment = HorizontalAlignment.Left;
		scoreLabel.VerticalAlignment = VerticalAlignment.Top;
		scoreLabel.Text = "Score: " + scoreManager.CurrentScore.ToString(); // Initial score
		AddChild(scoreLabel);


		// Connect to the EnemyKilled signal
		var enemySpawner = GetNode<EnemySpawner>("EnemySpawner");
		enemySpawner.Spawned += ConnectEnemySignals; // Connect to the Spawned signal of EnemySpawner

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

	private void ConnectEnemySignals(BaseEnemy enemy)
	{
		enemy.EnemyKilled += OnEnemyKilled; // Connect each spawned enemy's EnemyKilled signal
	}

	private void OnEnemyKilled(int scoreValue)
	{
		scoreManager.AddScore(scoreValue);
		UpdateScoreLabel();
	}

	private void UpdateScoreLabel()
	{
		if (scoreLabel != null)
		{
			scoreLabel.Text = "Score: " + scoreManager.CurrentScore.ToString();
		}
	}


	public void GameOver()
	{
		scoreManager.GameEnded(); // Update game statistics
		// TODO: Implement game over logic (e.g., show game over screen)
		GD.Print("Game Over");
	}

	public void NewGame()
	{
		_score = 0;
		scoreManager.ResetScore();
		UpdateScoreLabel();

		var player = GetNode<Player>("Player");
		var startPosition = GetNode<Marker2D>("StartPosition");
		player.Start(startPosition.Position);

		GetNode<EnemySpawner>("EnemySpawner").StartSpawning();
	}


	private void _on_player_hit()
	{
		GetNode<EnemySpawner>("EnemySpawner").StopSpawning();
		GameOver();
	}

	private void _on_start_timer_timeout()
	{
		GetNode<EnemySpawner>("EnemySpawner").StartSpawning();
		GetNode<Timer>("StartTimer").Stop();
	}

	private void _on_enemy_spawner_spawn_mob()
	{
		// Code to instantiate mob (if needed here, otherwise EnemySpawner handles it)
	}
}
