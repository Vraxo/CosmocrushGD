using Godot;

namespace CosmocrushGD;

public partial class World : WorldEnvironment
{
	public static World Instance { get; private set; }

	[Export] private PackedScene pauseMenuScene;
	[Export] private PackedScene gameOverMenuScene;
	[Export] private Button pauseButton;
	[Export] private CanvasLayer hudLayer;
	[Export] private NodePath scoreLabelPath;
	[Export] private NodePath fpsLabelPath;
	[Export] private NodePath playerPath;
	[Export] private NodePath enemySpawnerPath = "EnemySpawner";
	[Export] private NodePath enemyPoolManagerPath = "EnemyPoolManager";
	[Export] private AnimationPlayer scoreAnimationPlayer;

	private Label scoreLabel;
	private Label fpsLabel;

	public int Score { get; private set; } = 0;
	private PauseMenu pauseMenu;
	private GameOverMenu gameOverMenu;
	private Player player;
	private EnemySpawner enemySpawner;
	private EnemyPoolManager enemyPoolManager;

	public override void _EnterTree()
	{
		if (Instance is not null)
		{
		}
		Instance = this;
		base._EnterTree();
	}


	public override void _Ready()
	{
		if (scoreLabelPath is not null) scoreLabel = GetNode<Label>(scoreLabelPath);
		else { }

		if (fpsLabelPath is not null) fpsLabel = GetNode<Label>(fpsLabelPath);
		else { }


		if (hudLayer is null) { }
		if (scoreAnimationPlayer is null) { }

		if (pauseButton is not null)
		{
			pauseButton.Pressed += OnPauseButtonPressed;
		}
		else { }

		if (gameOverMenuScene is null) { }

		if (enemySpawnerPath is not null) enemySpawner = GetNode<EnemySpawner>(enemySpawnerPath);
		else { }

		if (enemyPoolManagerPath is not null) enemyPoolManager = GetNode<EnemyPoolManager>(enemyPoolManagerPath);
		else { }

		if (playerPath is not null) player = GetNode<Player>(playerPath);
		else { }


		if (player is not null)
		{
			player.GameOver += OnGameOver;
		}
		else { }


		CallDeferred(nameof(IncrementGamesPlayedDeferred));
		UpdateScoreLabel();
	}

	private void IncrementGamesPlayedDeferred()
	{
		StatisticsManager.Instance.IncrementGamesPlayed();
	}


	public override void _Process(double delta)
	{

		if (Input.IsActionJustPressed("ui_cancel") && !GetTree().Paused)
		{
			Pause();
		}
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}

		if (player is not null && IsInstanceValid(player))
		{
			player.GameOver -= OnGameOver;
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

	private void OnPauseButtonPressed()
	{
		if (gameOverMenu is not null && gameOverMenu.Visible)
		{
			return;
		}

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
