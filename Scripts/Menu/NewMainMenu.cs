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

	private const float FadeInDuration = 0.3f;
	private const float StaggerDelay = 0.12f;
	private readonly Vector2 InitialScale = new(1.3f, 1.3f);
	private readonly Vector2 TargetScale = Vector2.One;

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

		// Updated Node Paths
		titleLabel ??= GetNode<Label>("CenterContainer/VBoxContainer/TitleContainer/TitleLabel");
		startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButtonContainer/StartButton");
		settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButtonContainer/SettingsButton");
		statisticsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StatisticsButtonContainer/StatisticsButton");
		quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButtonContainer/QuitButton");


		if (titleLabel is null) GD.PrintErr("NewMainMenu: Title Label Null!");
		if (startButton is null) GD.PrintErr("NewMainMenu: Start Button Null!");
		if (settingsButton is null) GD.PrintErr("NewMainMenu: Settings Button Null!");
		if (statisticsButton is null) GD.PrintErr("NewMainMenu: Statistics Button Null!");
		if (quitButton is null) GD.PrintErr("NewMainMenu: Quit Button Null!");

		if (startButton is not null) startButton.Pressed += OnStartButtonPressed;
		if (settingsButton is not null) settingsButton.Pressed += OnSettingsButtonPressed;
		if (statisticsButton is not null) statisticsButton.Pressed += OnStatisticsButtonPressed;
		if (quitButton is not null) quitButton.Pressed += OnQuitButtonPressed;

		SetInitialVisuals();
		CallDeferred(nameof(StartFadeInAnimation));
	}

	private void SetInitialVisuals()
	{
		SetElementVisuals(titleLabel);
		SetElementVisuals(startButton);
		SetElementVisuals(settingsButton);
		SetElementVisuals(statisticsButton);
		SetElementVisuals(quitButton);
	}

	// Apply modulate and scale to the actual element (Label/Button)
	private void SetElementVisuals(Control element)
	{
		if (element is not null)
		{
			element.Modulate = Colors.Transparent;
			element.Scale = InitialScale;
		}
	}


	private void StartFadeInAnimation()
	{
		Tween tween = CreateTween();
		tween.SetParallel(false);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);

		tween.TweenInterval(StaggerDelay);

		AnimateElement(tween, titleLabel);
		AnimateElement(tween, startButton);
		AnimateElement(tween, settingsButton);
		AnimateElement(tween, statisticsButton);
		AnimateElement(tween, quitButton);

		tween.Play();
	}

	// Animate the actual element (Label/Button)
	private void AnimateElement(Tween tween, Control element)
	{
		if (element is not null)
		{
			// Set PivotOffset on the element being scaled, using its own size
			element.PivotOffset = element.Size / 2.0f;

			tween.SetParallel(true);
			tween.TweenProperty(element, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(element, "scale", TargetScale, FadeInDuration);
			tween.SetParallel(false);
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
