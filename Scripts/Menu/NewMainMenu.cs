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

	private const float InitialScaleFactor = 10f;
	private const float FadeInDuration = 1f;   // Slightly longer duration for elastic
	private const float StaggerDelay = 0.15f;   // Slightly longer delay for elastic

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

		// Error checking... (omitted for brevity, same as before)
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
		Vector2 initialScale = Vector2.One * InitialScaleFactor;

		if (titleLabel is not null)
		{
			titleLabel.Modulate = Colors.Transparent;
			titleLabel.Scale = initialScale;
			titleLabel.PivotOffset = titleLabel.Size / 2;
		}
		if (startButton is not null)
		{
			startButton.Modulate = Colors.Transparent;
			startButton.Scale = initialScale;
			// UIButton script should handle PivotOffset
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
		tween.SetEase(Tween.EaseType.Out);         // Ease for the overall motion
		tween.SetTrans(Tween.TransitionType.Elastic); // Changed transition for a bounce effect

		tween.TweenInterval(StaggerDelay);

		AnimateElement(tween, titleLabel);
		AnimateElement(tween, startButton);
		AnimateElement(tween, settingsButton);
		AnimateElement(tween, statisticsButton);
		AnimateElement(tween, quitButton, true);

		tween.Play();
	}

	private void AnimateElement(Tween tween, Control element, bool isLast = false)
	{
		if (element is null)
		{
			return;
		}

		// Ensure pivot is set just before animation for buttons too, in case _Ready wasn't enough
		if (element is Button button)
		{
			button.PivotOffset = button.Size / 2;
		}


		tween.SetParallel(true);
		// Use a slightly faster alpha fade to emphasize the scale bounce
		tween.TweenProperty(element, "modulate:a", 1.0f, FadeInDuration * 0.8f);
		tween.TweenProperty(element, "scale", Vector2.One, FadeInDuration);
		tween.SetParallel(false);

		if (!isLast)
		{
			tween.TweenInterval(StaggerDelay);
		}
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
