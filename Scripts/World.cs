using Godot;

namespace CosmocrushGD;

public partial class World : WorldEnvironment
{
	[Export] private PackedScene pauseMenuScene;
	[Export] private PackedScene gameOverMenuScene;
	[Export] private Button pauseButton;
	[Export] private CanvasLayer hudLayer;
	[Export] private Label scoreLabel;
	[Export] private Label enemyCountLabel;
	[Export] private NodePath playerPath;
	[Export] private NodePath enemySpawnerPath;
	[Export] private AnimationPlayer scoreAnimationPlayer;

	public int Score { get; private set; } = 0;

	private int currentEnemyCount = 0;
	private PauseMenu pauseMenu;
	private GameOverMenu gameOverMenu;
	private Player player;
	private EnemySpawner enemySpawner;
	private bool isPlayerDead = false;

	public override void _Ready()
	{
		if (hudLayer is null)
		{
			GD.PrintErr("World: HUD Layer node not assigned!");
		}
		if (scoreLabel is null)
		{
			GD.PrintErr("World: Score Label node not assigned!");
		}
		if (enemyCountLabel is null)
		{
			GD.PrintErr("World: Enemy Count Label node not assigned!");
		}
		if (scoreAnimationPlayer is null)
		{
			GD.PrintErr("World: Score Animation Player node not assigned!");
		}
		if (pauseButton is not null)
		{
			pauseButton.Pressed += OnPauseButtonPressed;
		}

		if (gameOverMenuScene is null)
		{
			GD.PrintErr("World: Game Over Menu Scene not assigned!");
		}

		player = GetNode<Player>(playerPath);
		if (player is not null)
		{
			player.GameOver += OnGameOver;
			player.PlayerDied += OnPlayerDied;
		}
		else
		{
			GD.PrintErr("World: Player node not found or path invalid!");
		}

		enemySpawner = GetNode<EnemySpawner>(enemySpawnerPath);
		if (enemySpawner is not null)
		{
			enemySpawner.EnemySpawned += OnEnemySpawned;
		}
		else
		{
			GD.PrintErr("World: Enemy Spawner node not found or path invalid!");
		}

		StatisticsManager.Instance.IncrementGamesPlayed();
		UpdateScoreLabel();
		UpdateEnemyCountLabel();
	}

	public override void _Process(double delta)
	{
		if (!isPlayerDead && Input.IsActionJustPressed("ui_cancel") && !GetTree().Paused)
		{
			Pause();
		}
	}

	public override void _ExitTree()
	{
		if (player is not null && IsInstanceValid(player))
		{
			if (player.IsConnected(Player.SignalName.GameOver, Callable.From(OnGameOver)))
			{
				player.GameOver -= OnGameOver;
			}
			if (player.IsConnected(Player.SignalName.PlayerDied, Callable.From(OnPlayerDied)))
			{
				player.PlayerDied -= OnPlayerDied;
			}
		}
		if (enemySpawner is not null && IsInstanceValid(enemySpawner))
		{
			if (enemySpawner.IsConnected(EnemySpawner.SignalName.EnemySpawned, Callable.From<BaseEnemy>(OnEnemySpawned)))
			{
				enemySpawner.EnemySpawned -= OnEnemySpawned;
			}
		}
		if (pauseButton is not null && IsInstanceValid(pauseButton))
		{
			if (pauseButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnPauseButtonPressed)))
			{
				pauseButton.Pressed -= OnPauseButtonPressed;
			}
		}

		base._ExitTree();
	}

	public void AddScore(int amount)
	{
		Score += amount;
		UpdateScoreLabel();

		if (scoreAnimationPlayer is not null)
		{
			scoreAnimationPlayer.Stop(true); // Reset animation before playing
			scoreAnimationPlayer.Play("ScorePunch");
		}
	}

	private void UpdateScoreLabel()
	{
		if (scoreLabel is not null)
		{
			scoreLabel.Text = $"Score: {Score}";
		}
	}

	private void UpdateEnemyCountLabel()
	{
		if (enemyCountLabel is not null)
		{
			enemyCountLabel.Text = $"Enemies: {currentEnemyCount}";
		}
	}

	private void OnEnemySpawned(BaseEnemy enemy)
	{
		if (enemy is null || !IsInstanceValid(enemy))
		{
			return;
		}
		currentEnemyCount++;
		enemy.EnemyDied += OnEnemyDied;
		UpdateEnemyCountLabel();
	}

	private void OnEnemyDied(BaseEnemy enemy)
	{
		if (enemy is null || !IsInstanceValid(enemy))
		{
			return;
		}

		// Ensure signal is disconnected only if connected
		var callable = Callable.From<BaseEnemy>(OnEnemyDied);
		if (enemy.IsConnected(BaseEnemy.SignalName.EnemyDied, callable))
		{
			enemy.EnemyDied -= OnEnemyDied;
		}

		currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
		// AddScore(10); // REMOVED: Do not add fixed score on death
		UpdateEnemyCountLabel();
	}


	private void OnPauseButtonPressed()
	{
		if (isPlayerDead || GetTree().Paused)
		{
			return;
		}

		if (pauseMenu is not null && IsInstanceValid(pauseMenu))
		{
			pauseMenu.TriggerContinue();
		}
		else
		{
			Pause();
		}
	}

	private void Pause()
	{
		if (isPlayerDead || GetTree().Paused)
		{
			return;
		}

		if (pauseMenu is null || !IsInstanceValid(pauseMenu))
		{
			if (pauseMenuScene is null)
			{
				GD.PrintErr("World: Pause Menu Scene not assigned, cannot pause!");
				return;
			}

			if (hudLayer is null)
			{
				GD.PrintErr("World: HUD Layer is null, cannot add Pause Menu!");
				return;
			}

			pauseMenu = pauseMenuScene.Instantiate<PauseMenu>();
			hudLayer.AddChild(pauseMenu);
			pauseMenu.Show(); // Show immediately after adding
			GetTree().Paused = true;
		}
		else // If menu exists but isn't visible (shouldn't happen often with this logic)
		{
			pauseMenu.Show();
			GetTree().Paused = true;
		}
	}

	private void OnPlayerDied()
	{
		isPlayerDead = true;
	}

	private void OnGameOver()
	{
		if (gameOverMenu is not null && IsInstanceValid(gameOverMenu))
		{
			return; // Already showing
		}

		if (gameOverMenuScene is null)
		{
			GD.PrintErr("World: Game Over Menu Scene is null!");
			return;
		}

		if (hudLayer is null)
		{
			GD.PrintErr("World: HUD Layer is null, cannot add Game Over Menu!");
			return;
		}

		StatisticsManager.Instance.UpdateScores(Score);

		gameOverMenu = gameOverMenuScene.Instantiate<GameOverMenu>();
		gameOverMenu.SetScore(Score);
		hudLayer.AddChild(gameOverMenu);
		gameOverMenu.Show();

		if (pauseButton is not null)
		{
			pauseButton.Disabled = true;
		}
	}
}
