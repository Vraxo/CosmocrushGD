// Summary: No changes needed in this file. It remains as it was before the KillTweensOf attempts.
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

	private const float InitialScaleFactor = 2.0f;
	private const float ScaleInDuration = 0.35f;
	private const float FadeInDuration = 0.25f;
	private const float StaggerDelay = 0.075f;

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
			var _settings = CosmocrushGD.Settings.Instance;
			var _stats = CosmocrushGD.StatisticsManager.Instance;
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

		if (startButton is not null) startButton.Pressed += OnStartButtonPressed;
		if (settingsButton is not null) settingsButton.Pressed += OnSettingsButtonPressed;
		if (statisticsButton is not null) statisticsButton.Pressed += OnStatisticsButtonPressed;
		if (quitButton is not null) quitButton.Pressed += OnQuitButtonPressed;

		SetInitialState();
		CallDeferred(nameof(StartFadeInAnimation));
	}

	private void SetInitialState()
	{
		var initialScale = Vector2.One * InitialScaleFactor;

		if (titleLabel is not null)
		{
			titleLabel.Modulate = Colors.Transparent;
			titleLabel.Scale = initialScale;
			titleLabel.PivotOffset = titleLabel.Size / 2.0f;
		}
		if (startButton is not null)
		{
			startButton.Modulate = Colors.Transparent;
			startButton.Scale = initialScale;
		}
		if (settingsButton is not null)
		{
			settingsButton.Modulate = Colors.Transparent;
			settingsButton.Scale = initialScale;
		}
		if (statisticsButton is not null)
		{
			statisticsButton.Modulate = Colors.Transparent;
			statisticsButton.Scale = initialScale;
		}
		if (quitButton is not null)
		{
			quitButton.Modulate = Colors.Transparent;
			quitButton.Scale = initialScale;
		}
	}

	private void StartFadeInAnimation()
	{
		Tween tween = CreateTween();
		tween.SetParallel(false);

		tween.TweenInterval(StaggerDelay);

		// Title Label
		if (titleLabel is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenProperty(titleLabel, "scale", Vector2.One, ScaleInDuration)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Start Button
		if (startButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(startButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenProperty(startButton, "scale", Vector2.One, ScaleInDuration)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Settings Button
		if (settingsButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(settingsButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenProperty(settingsButton, "scale", Vector2.One, ScaleInDuration)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Statistics Button
		if (statisticsButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(statisticsButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenProperty(statisticsButton, "scale", Vector2.One, ScaleInDuration)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Quit Button
		if (quitButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(quitButton, "modulate:a", 1.0f, FadeInDuration)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenProperty(quitButton, "scale", Vector2.One, ScaleInDuration)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
			tween.SetParallel(false);
		}

		tween.Play();
	}


	private void OnStartButtonPressed()
	{
		menuShell?.StartGame();
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
