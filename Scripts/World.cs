using Godot;

namespace CosmocrushGD;

public partial class World : WorldEnvironment
{
	private const int EnemyKillBonus = 10;

	private int currentEnemyCount = 0;
	private PauseMenu pauseMenu;
	private GameOverMenu gameOverMenu;
	private Player player;
	private EnemySpawner enemySpawner;
	private bool isPlayerDead = false;

	[Export] private PackedScene pauseMenuScene;
	[Export] private PackedScene gameOverMenuScene;
	[Export] private Button pauseButton;
	[Export] private CanvasLayer hudLayer;
	[Export] private Label scoreLabel;
	[Export] private Label enemyCountLabel;
	[Export] private Label fpsLabel;
	[Export] private NodePath playerPath;
	[Export] private NodePath enemySpawnerPath;
	[Export] private AnimationPlayer scoreAnimationPlayer;

	public int Score { get; private set; } = 0;

	public override void _Ready()
	{
		DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

		if (fpsLabel is not null)
		{
			fpsLabel.Text = "FPS: 0";
		}

		if (pauseButton is not null)
		{
			pauseButton.Pressed += OnPauseButtonPressed;
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

		StatisticsManager.Instance?.IncrementGamesPlayed();
		UpdateScoreLabel();
		UpdateEnemyCountLabel();
	}

	public override void _Process(double delta)
	{
		if (!isPlayerDead && Input.IsActionJustPressed("ui_cancel") && !GetTree().Paused)
		{
			Pause();
		}

		fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
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

		scoreAnimationPlayer?.Stop(true);
		scoreAnimationPlayer?.Play("ScorePunch");
	}

	private void UpdateScoreLabel()
	{
		scoreLabel.Text = $"Score: {Score}";
	}

	private void UpdateEnemyCountLabel()
	{
		enemyCountLabel.Text = $"Enemies: {currentEnemyCount}";
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

		var callable = Callable.From<BaseEnemy>(OnEnemyDied);

		if (enemy.IsConnected(BaseEnemy.SignalName.EnemyDied, callable))
		{
			enemy.EnemyDied -= OnEnemyDied;
		}

		currentEnemyCount = int.Max(0, currentEnemyCount - 1);
		AddScore(EnemyKillBonus);
		UpdateEnemyCountLabel();
	}

	private void OnPauseButtonPressed()
	{
		if (isPlayerDead || GetTree().Paused)
		{
			return;
		}

		if (pauseMenu?.IsInsideTree() ?? false)
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
			pauseMenu.Show();
			GetTree().Paused = true;
		}
		else
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
		if (gameOverMenu?.IsInsideTree() ?? false)
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

		StatisticsManager.Instance?.UpdateScores(Score);

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
