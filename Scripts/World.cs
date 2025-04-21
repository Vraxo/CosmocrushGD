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

	private int _currentEnemyCount = 0;
	private PauseMenu pauseMenu;
	private GameOverMenu gameOverMenu;
	private Player player;
	private EnemySpawner enemySpawner;
	private bool isPlayerDead = false;

	public override void _Ready()
	{
		if (hudLayer is null)
		{
		}
		if (scoreLabel is null)
		{
		}
		if (enemyCountLabel is null)
		{
		}
		if (scoreAnimationPlayer is null)
		{
		}
		if (pauseButton is not null)
		{
			pauseButton.Pressed += OnPauseButtonPressed;
		}

		if (gameOverMenuScene is null)
		{
		}

		player = GetNode<Player>(playerPath);
		if (player is not null)
		{
			player.GameOver += OnGameOver;
			player.PlayerDied += OnPlayerDied;
		}


		enemySpawner = GetNode<EnemySpawner>(enemySpawnerPath);
		if (enemySpawner is not null)
		{
			enemySpawner.EnemySpawned += OnEnemySpawned;
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
		if (player is not null)
		{
			player.GameOver -= OnGameOver;
			player.PlayerDied -= OnPlayerDied;
		}
		if (enemySpawner is not null)
		{
			enemySpawner.EnemySpawned -= OnEnemySpawned;
		}

		base._ExitTree();
	}

	public void AddScore(int amount)
	{
		Score += amount;
		UpdateScoreLabel();

		if (scoreAnimationPlayer is not null)
		{
			scoreAnimationPlayer.Stop();
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
			enemyCountLabel.Text = $"Enemies: {_currentEnemyCount}";
		}
	}

	private void OnEnemySpawned(BaseEnemy enemy)
	{
		if (enemy is null || !IsInstanceValid(enemy))
		{
			return;
		}
		_currentEnemyCount++;
		enemy.EnemyDied += OnEnemyDied;
		UpdateEnemyCountLabel();
	}

	private void OnEnemyDied(BaseEnemy enemy)
	{
		if (enemy is null || !IsInstanceValid(enemy))
		{
			return;
		}

		if (enemy.IsConnected(BaseEnemy.SignalName.EnemyDied, Callable.From<BaseEnemy>(OnEnemyDied)))
		{
			enemy.EnemyDied -= OnEnemyDied;
		}

		_currentEnemyCount = Mathf.Max(0, _currentEnemyCount - 1);
		AddScore(10);
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
				return;
			}

			if (hudLayer is null)
			{
				return;
			}

			pauseMenu = pauseMenuScene.Instantiate<PauseMenu>();
			hudLayer.AddChild(pauseMenu);
		}

		GetTree().Paused = true;
		pauseMenu.Show();
	}

	private void OnPlayerDied()
	{
		isPlayerDead = true;
	}

	private void OnGameOver()
	{
		if (gameOverMenu is not null && IsInstanceValid(gameOverMenu))
		{
			return;
		}

		if (gameOverMenuScene is null)
		{
			return;
		}

		if (hudLayer is null)
		{
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
