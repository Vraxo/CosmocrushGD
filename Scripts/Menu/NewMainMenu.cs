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

	private const float FadeInDuration = 0.15f;
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

		SetInitialAlphas();
		CallDeferred(nameof(StartFadeInAnimation));
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
