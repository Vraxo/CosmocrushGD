using Godot;

namespace CosmocrushGD;

public partial class World : WorldEnvironment
{
	[Export] private PackedScene pauseMenuScene;
	[Export] private PackedScene gameOverMenuScene;
	[Export] private Button pauseButton;
	[Export] private CanvasLayer hudLayer;
	[Export] private Label scoreLabel;
	[Export] private NodePath playerPath;
	[Export] private AnimationPlayer scoreAnimationPlayer;

	public int Score { get; private set; } = 0;
	private PauseMenu pauseMenu;
	private GameOverMenu gameOverMenu;
	private Player player;

	public override void _Ready()
	{
		GD.Print("World._Ready: Start");
		if (hudLayer is null)
		{
			GD.PrintErr("World._Ready: HUD Layer reference not set!");
		}
		if (scoreLabel is null)
		{
			GD.PrintErr("World._Ready: Score Label reference not set!");
		}
		if (scoreAnimationPlayer is null)
		{
			GD.PrintErr("World._Ready: Score Animation Player reference not set!");
		}
		if (pauseButton is not null)
		{
			pauseButton.Pressed += OnPauseButtonPressed;
		}
		else
		{
			GD.PrintErr("World._Ready: Pause Button reference not set!");
		}
		if (gameOverMenuScene is null)
		{
			GD.PrintErr("World._Ready: GameOverMenuScene is not assigned in the inspector!");
		}

		GD.Print($"World._Ready: Attempting to get player from path: {playerPath}");
		player = GetNode<Player>(playerPath);
		if (player is not null)
		{
			GD.Print("World._Ready: Player node found. Subscribing to GameOver event.");
			player.GameOver += OnGameOver;
		}
		else
		{
			GD.PrintErr("World._Ready: Player node NOT found or path incorrect!");
		}

		StatisticsManager.Instance.IncrementGamesPlayed();
		UpdateScoreLabel();
		GD.Print("World._Ready: End");
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
		GD.Print("World._ExitTree: Start");
		if (player is not null)
		{
			GD.Print("World._ExitTree: Unsubscribing from player GameOver event.");
			player.GameOver -= OnGameOver;
		}
		base._ExitTree();
		GD.Print("World._ExitTree: End");
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
				GD.PrintErr("World.Pause: PauseMenuScene is not set!");
				return;
			}

			if (hudLayer is null)
			{
				GD.PrintErr("World.Pause: HUD Layer is not set or found!");
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
		GD.Print("World.OnGameOver: Game Over event received!");

		if (gameOverMenu is not null && IsInstanceValid(gameOverMenu))
		{
			GD.Print("World.OnGameOver: Game Over menu already exists. Aborting.");
			return;
		}

		if (gameOverMenuScene is null)
		{
			GD.PrintErr("World.OnGameOver: GameOverMenuScene is not set in World script!");
			return;
		}

		if (hudLayer is null)
		{
			GD.PrintErr("World.OnGameOver: HUD Layer is not set or found!");
			return;
		}

		GD.Print($"World.OnGameOver: Updating statistics with final score: {Score}");
		StatisticsManager.Instance.UpdateScores(Score);

		gameOverMenu = gameOverMenuScene.Instantiate<GameOverMenu>();
		gameOverMenu.SetScore(Score);
		hudLayer.AddChild(gameOverMenu);
		gameOverMenu.Show();

		if (pauseButton is not null)
		{
			pauseButton.Disabled = true;
			GD.Print("World.OnGameOver: Pause button disabled.");
		}
	}
}
