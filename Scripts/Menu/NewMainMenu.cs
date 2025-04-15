using Godot;
using System;

namespace CosmocrushGD;

public partial class NewMainMenu : ColorRect
{
	[Export] private Label titleLabel;
	[Export] private Button startButton;
	[Export] private Button settingsButton;
	[Export] private Button statisticsButton;
	[Export] private Button quitButton;
	[Export] private Node starParticlesNode;
	[Export] private PackedScene statisticsMenuScene;
	private CpuParticles2D _starParticles;

	private const string GameScenePath = "res://Scenes/World.tscn";
	private const string SettingsScenePath = "res://Scenes/Menu/SettingsMenu.tscn";

	private const float FadeInDuration = 0.3f;
	private const float StaggerDelay = 0.15f;

	public override void _Ready()
	{
		GD.Print("--- NewMainMenu _Ready: Start ---");

		try
		{
			var _settings = CosmocrushGD.Settings.Instance;
			GD.Print("Settings Instance accessed.");

			var _stats = CosmocrushGD.StatisticsManager.Instance;
			GD.Print("Statistics Manager Instance accessed.");
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error accessing Singletons (Settings/Statistics): {e.Message}");
		}

		titleLabel ??= GetNode<Label>("CenterContainer/VBoxContainer/TitleLabel");
		startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
		settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
		statisticsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StatisticsButton");
		quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");

		if (titleLabel is null) GD.PrintErr("NewMainMenu: Title Label Null!");
		if (startButton is null) GD.PrintErr("NewMainMenu: Start Button Null!");
		if (settingsButton is null) GD.PrintErr("NewMainMenu: Settings Button Null!");
		if (statisticsButton is null) GD.PrintErr("NewMainMenu: Statistics Button Null!");
		if (quitButton is null) GD.PrintErr("NewMainMenu: Quit Button Null!");
		GD.Print("UI Elements checked/retrieved.");

		if (statisticsMenuScene is null)
		{
			GD.PrintErr("NewMainMenu: statisticsMenuScene is not assigned in the inspector!");
		}

		GD.Print("Attempting to get StarParticles...");
		if (starParticlesNode is not null && starParticlesNode is CpuParticles2D specificParticles)
		{
			_starParticles = specificParticles;
			GD.Print("StarParticles obtained from Exported Node.");
		}
		else
		{
			GD.Print($"Exported starParticlesNode is {(starParticlesNode is null ? "null" : "not a CpuParticles2D")}. Trying GetNode...");
			_starParticles = GetNode<CpuParticles2D>("StarParticles");
			if (_starParticles is null)
			{
				GD.PrintErr("Failed to get StarParticles node by path 'StarParticles'. Effect disabled, but continuing.");
			}
			else
			{
				GD.Print("StarParticles obtained via GetNode.");
			}
		}

		if (startButton is not null) startButton.Pressed += OnStartButtonPressed;
		if (settingsButton is not null) settingsButton.Pressed += OnSettingsButtonPressed;
		if (statisticsButton is not null) statisticsButton.Pressed += OnStatisticsButtonPressed;
		if (quitButton is not null) quitButton.Pressed += OnQuitButtonPressed;
		GD.Print("Button signals connected.");

		var root = GetTree()?.Root;
		if (root is not null)
		{
			root.CloseRequested += OnWindowCloseRequested;
			GD.Print("CloseRequested signal connected.");
		}
		else
		{
			GD.PrintErr("GetTree().Root is null, cannot connect CloseRequested signal.");
		}


		GD.Print("Connecting Resized signal...");
		Resized += UpdateParticleEmitterBounds;
		GD.Print("Resized signal connected.");

		GD.Print("Scheduling deferred call to UpdateParticleEmitterBounds...");
		if (_starParticles is not null)
		{
			_starParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
			CallDeferred(nameof(UpdateParticleEmitterBounds));
			GD.Print("Deferred call scheduled.");
		}
		else
		{
			GD.Print("Skipping deferred call because _starParticles is null.");
		}

		SetInitialAlphas();
		CallDeferred(nameof(StartFadeInAnimation));

		GD.Print("--- NewMainMenu _Ready: End ---");
	}

	private void SetInitialAlphas()
	{
		if (titleLabel is not null) titleLabel.Modulate = Colors.Transparent;
		if (startButton is not null) startButton.Modulate = Colors.Transparent;
		if (settingsButton is not null) settingsButton.Modulate = Colors.Transparent;
		if (statisticsButton is not null) statisticsButton.Modulate = Colors.Transparent;
		if (quitButton is not null) quitButton.Modulate = Colors.Transparent;
	}

	private void StartFadeInAnimation()
	{
		Tween tween = CreateTween();
		tween.SetParallel(false);

		tween.TweenInterval(StaggerDelay);

		if (titleLabel is not null)
		{
			tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenInterval(StaggerDelay);
		}
		if (startButton is not null)
		{
			tween.TweenProperty(startButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenInterval(StaggerDelay);
		}
		if (settingsButton is not null)
		{
			tween.TweenProperty(settingsButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenInterval(StaggerDelay);
		}
		if (statisticsButton is not null)
		{
			tween.TweenProperty(statisticsButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenInterval(StaggerDelay);
		}
		if (quitButton is not null)
		{
			tween.TweenProperty(quitButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
		}

		tween.Play();
	}


	private void UpdateParticleEmitterBounds()
	{
		if (_starParticles is null)
		{
			GD.PrintErr("UpdateParticleEmitterBounds: _starParticles is null. Aborting.");
			return;
		}

		if (!IsInsideTree())
		{
			return;
		}

		var viewport = GetViewport();
		if (viewport is null)
		{
			GD.PrintErr("UpdateParticleEmitterBounds: GetViewport() returned null. Aborting.");
			return;
		}

		var viewportHeight = GetViewportRect().Size.Y;

		const float spawnOffsetX = -10.0f;
		_starParticles.Position = new Vector2(spawnOffsetX, viewportHeight / 2.0f);

		_starParticles.EmissionRectExtents = new Vector2(1.0f, viewportHeight / 2.0f);

		_starParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
	}

	private void OnStartButtonPressed()
	{
		GD.Print("Start button pressed.");
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnSettingsButtonPressed()
	{
		GD.Print("Settings button pressed.");
		GetTree().ChangeSceneToFile(SettingsScenePath);
	}

	private void OnStatisticsButtonPressed()
	{
		GD.Print("Statistics button pressed.");
		if (statisticsMenuScene is not null)
		{
			GetTree().ChangeSceneToPacked(statisticsMenuScene);
		}
		else
		{
			GD.PrintErr("Cannot switch to Statistics Menu: Scene not assigned in NewMainMenu script!");
		}
	}

	private void OnQuitButtonPressed()
	{
		GD.Print("Quit button pressed. Saving stats and quitting application...");
		StatisticsManager.Instance.Save();
		GetTree().Quit();
	}

	private void OnWindowCloseRequested()
	{
		GD.Print("Window close requested via signal. Saving stats and quitting application...");
		StatisticsManager.Instance.Save();
		GetTree().Quit();
	}

	public override void _ExitTree()
	{
		GD.Print("--- NewMainMenu _ExitTree ---");
		if (IsInstanceValid(this))
		{
			Resized -= UpdateParticleEmitterBounds;
		}


		var root = GetTree()?.Root;
		if (root is not null && IsInstanceValid(root))
		{
			var callable = Callable.From(OnWindowCloseRequested);
			if (root.IsConnected(Window.SignalName.CloseRequested, callable))
			{
				GD.Print("Disconnecting CloseRequested signal.");
				root.Disconnect(Window.SignalName.CloseRequested, callable);
			}
		}

		if (IsInstanceValid(startButton)) startButton.Pressed -= OnStartButtonPressed;
		if (IsInstanceValid(settingsButton)) settingsButton.Pressed -= OnSettingsButtonPressed;
		if (IsInstanceValid(statisticsButton)) statisticsButton.Pressed -= OnStatisticsButtonPressed;
		if (IsInstanceValid(quitButton)) quitButton.Pressed -= OnQuitButtonPressed;

		base._ExitTree();
	}
}
