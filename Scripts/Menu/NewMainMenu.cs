using Godot;
using System;
using System.Linq;
using System.Threading.Tasks; // Required for Task

namespace CosmocrushGD;

public partial class NewMainMenu : ColorRect
{
	[Export] private Label titleLabel;
	[Export] private Button startButton;
	[Export] private Button settingsButton;
	[Export] private Button statisticsButton;
	[Export] private Button quitButton;

	private const float FadeInDuration = 0.25f; // Slightly longer to see effect
	private const float ScaleInDuration = 0.35f; // Slightly longer to see effect
												 // private const float StaggerDelay = 0.075f; // Removed for now
	private const float InitialScale = 2.0f;

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

		SetPivots();
		SetInitialAlphasAndScales();

		if (startButton is not null) startButton.Pressed += OnStartButtonPressed;
		if (settingsButton is not null) settingsButton.Pressed += OnSettingsButtonPressed;
		if (statisticsButton is not null) statisticsButton.Pressed += OnStatisticsButtonPressed;
		if (quitButton is not null) quitButton.Pressed += OnQuitButtonPressed;

		// Use CallDeferred to ensure _Ready completes fully first
		CallDeferred(nameof(InitiateAnimationSequence));
	}

	private void InitiateAnimationSequence()
	{
		// Asynchronously wait for one frame and then start the animation
		WaitForFrameAndStartAnimation();
	}

	private async void WaitForFrameAndStartAnimation()
	{
		// Wait for the next process frame signal
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		// Now start the animation
		StartFadeInAnimation();
	}


	private void SetPivots()
	{
		if (titleLabel is not null)
		{
			titleLabel.PivotOffset = titleLabel.Size / 2;
			GD.Print($"TitleLabel Pivot Set: {titleLabel.PivotOffset}");
		}
		// Buttons handle their own pivot updates in UIButton.cs
	}

	private void SetInitialAlphasAndScales()
	{
		Vector2 initialScaleVector = Vector2.One * InitialScale;
		Control[] elements = new Control[] { titleLabel, startButton, settingsButton, statisticsButton, quitButton };

		GD.Print("--- Setting Initial States ---");
		foreach (var element in elements.Where(e => e is not null))
		{
			element.Modulate = Colors.Transparent;
			element.Scale = initialScaleVector;
			element.Visible = true; // Ensure visible
			GD.Print($"Set {element.Name}: Scale={element.Scale}, Modulate={element.Modulate}");
		}
		GD.Print("--- Finished Setting Initial States ---");
	}

	// Renamed method, simplified drastically
	private void StartFadeInAnimation()
	{
		Tween tween = CreateTween();
		tween.SetProcessMode(Tween.TweenProcessMode.Idle);
		// All animations run in parallel now
		tween.SetParallel(true);

		var elementsToAnimate = new Control[] { titleLabel, startButton, settingsButton, statisticsButton, quitButton };

		GD.Print("--- Starting Simultaneous Animation ---");

		foreach (var element in elementsToAnimate)
		{
			if (element is null || !IsInstanceValid(element))
			{
				continue;
			}

			GD.Print($"Queueing animation for {element.Name}");

			// Queue alpha animation for this element
			tween.TweenProperty(element, "modulate:a", 1.0f, FadeInDuration)
				   .SetEase(Tween.EaseType.Out);

			// Queue scale animation for this element
			tween.TweenProperty(element, "scale", Vector2.One, ScaleInDuration)
				   .SetTrans(Tween.TransitionType.Back)
				   .SetEase(Tween.EaseType.Out);
		}

		GD.Print("--- Playing Tween ---");
		tween.Play();
	}


	private void OnStartButtonPressed()
	{
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
		menuShell?.StartGame();
	}

	private void OnSettingsButtonPressed()
	{
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
		menuShell?.ShowSettingsMenu();
	}

	private void OnStatisticsButtonPressed()
	{
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
		menuShell?.ShowStatisticsMenu();
	}

	private void OnQuitButtonPressed()
	{
		GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
		menuShell?.QuitGame();
	}

	public override void _ExitTree()
	{
		if (startButton is not null && IsInstanceValid(startButton)) startButton.Pressed -= OnStartButtonPressed;
		if (settingsButton is not null && IsInstanceValid(settingsButton)) settingsButton.Pressed -= OnSettingsButtonPressed;
		if (statisticsButton is not null && IsInstanceValid(statisticsButton)) statisticsButton.Pressed -= OnStatisticsButtonPressed;
		if (quitButton is not null && IsInstanceValid(quitButton)) quitButton.Pressed -= OnQuitButtonPressed;

		base._ExitTree();
	}
}
