using Godot;
using System;

namespace CosmocrushGD;

public partial class NewMainMenu : CenterContainer
{
	[Export] private Label titleLabel;
	[Export] private UIButton startButton;
	[Export] private UIButton settingsButton;
	[Export] private UIButton statisticsButton;
	[Export] private UIButton quitButton;

	private const float FadeInDuration = 0.3f;
	private const float StaggerDelay = 0.1f;
	private const float InitialScaleMultiplier = 2.0f;
	private const string GameScenePath = "res://Scenes/World.tscn";

	private MenuShell menuShell;

	public override void _Ready()
	{
		menuShell = GetParent()?.GetParent<MenuShell>();
		if (menuShell is null)
		{
			GD.PrintErr("NewMainMenu: Could not find MenuShell parent!");
		}

		try
		{
			var settingsInstance = Settings.Instance;
			var statsInstance = StatisticsManager.Instance;
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error accessing Singletons (Settings/Statistics): {e.Message}");
		}

		if (startButton is not null) startButton.Pressed += OnStartButtonPressed;
		if (settingsButton is not null) settingsButton.Pressed += OnSettingsButtonPressed;
		if (statisticsButton is not null) statisticsButton.Pressed += OnStatisticsButtonPressed;
		if (quitButton is not null) quitButton.Pressed += OnQuitButtonPressed;

		if (titleLabel is not null)
		{
			CallDeferred(nameof(CenterTitleLabelPivot));
		}

		SetInitialState();
		CallDeferred(nameof(StartFadeInAnimation));
	}

	private void CenterTitleLabelPivot()
	{
		if (titleLabel is not null)
		{
			titleLabel.PivotOffset = titleLabel.Size / 2;
		}
	}

	private void SetInitialState()
	{
		if (titleLabel is not null)
		{
			titleLabel.Modulate = Colors.Transparent;
			titleLabel.Scale = Vector2.One;
		}
		if (startButton is not null)
		{
			startButton.Modulate = Colors.Transparent;
			startButton.Scale = Vector2.One;
			startButton.TweenScale = false;
		}
		if (settingsButton is not null)
		{
			settingsButton.Modulate = Colors.Transparent;
			settingsButton.Scale = Vector2.One;
			settingsButton.TweenScale = false;
		}
		if (statisticsButton is not null)
		{
			statisticsButton.Modulate = Colors.Transparent;
			statisticsButton.Scale = Vector2.One;
			statisticsButton.TweenScale = false;
		}
		if (quitButton is not null)
		{
			quitButton.Modulate = Colors.Transparent;
			quitButton.Scale = Vector2.One;
			quitButton.TweenScale = false;
		}
	}

	private void StartFadeInAnimation()
	{
		Tween tween = CreateTween();
		tween.SetParallel(false);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);

		Vector2 initialScale = Vector2.One * InitialScaleMultiplier;
		Vector2 finalScale = Vector2.One;

		tween.TweenInterval(StaggerDelay);

		if (titleLabel is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(titleLabel, "scale", finalScale, FadeInDuration).From(initialScale);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}
		if (startButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(startButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(startButton, "scale", finalScale, FadeInDuration).From(initialScale);
			tween.SetParallel(false);
			tween.TweenCallback(Callable.From(() => { if (startButton is not null) startButton.TweenScale = true; }));
			tween.TweenInterval(StaggerDelay);
		}
		if (settingsButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(settingsButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(settingsButton, "scale", finalScale, FadeInDuration).From(initialScale);
			tween.SetParallel(false);
			tween.TweenCallback(Callable.From(() => { if (settingsButton is not null) settingsButton.TweenScale = true; }));
			tween.TweenInterval(StaggerDelay);
		}
		if (statisticsButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(statisticsButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(statisticsButton, "scale", finalScale, FadeInDuration).From(initialScale);
			tween.SetParallel(false);
			tween.TweenCallback(Callable.From(() => { if (statisticsButton is not null) statisticsButton.TweenScale = true; }));
			tween.TweenInterval(StaggerDelay);
		}
		if (quitButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(quitButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(quitButton, "scale", finalScale, FadeInDuration).From(initialScale);
			tween.SetParallel(false);
			tween.TweenCallback(Callable.From(() => { if (quitButton is not null) quitButton.TweenScale = true; }));
		}

		tween.Play();
	}

	private void OnStartButtonPressed()
	{
		SceneTransitionManager.Instance?.ChangeScene(GameScenePath);
	}

	private void OnSettingsButtonPressed()
	{
		menuShell?.ShowSettingsMenu();
	}

	private void OnStatisticsButtonPressed()
	{
		menuShell?.ShowStatisticsMenu();
	}

	private void OnQuitButtonPressed()
	{
		menuShell?.QuitGame();
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(startButton)) startButton.Pressed -= OnStartButtonPressed;
		if (IsInstanceValid(settingsButton)) settingsButton.Pressed -= OnSettingsButtonPressed;
		if (IsInstanceValid(statisticsButton)) statisticsButton.Pressed -= OnStatisticsButtonPressed;
		if (IsInstanceValid(quitButton)) quitButton.Pressed -= OnQuitButtonPressed;

		base._ExitTree();
	}
}
