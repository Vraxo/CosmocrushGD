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

	private const float AnimationDuration = 0.25f; // Slightly longer for effect
	private const float StaggerDelay = 0.075f;
	private readonly Vector2 InitialScale = new(1.2f, 1.2f);
	private readonly Vector2 FinalScale = Vector2.One;

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

		// Ensure Pivot Offset is set for the Title Label *after* it's ready
		if (titleLabel is not null)
		{
			titleLabel.Ready += SetTitlePivot;
		}


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
		CallDeferred(nameof(StartEntranceAnimation));
	}

	private void SetTitlePivot()
	{
		if (titleLabel is not null)
		{
			titleLabel.PivotOffset = titleLabel.Size / 2;
		}
	}


	private void SetInitialState()
	{
		SetControlState(titleLabel, InitialScale, Colors.Transparent);
		SetControlState(startButton, InitialScale, Colors.Transparent);
		SetControlState(settingsButton, InitialScale, Colors.Transparent);
		SetControlState(statisticsButton, InitialScale, Colors.Transparent);
		SetControlState(quitButton, InitialScale, Colors.Transparent);
	}

	private void SetControlState(Control control, Vector2 scale, Color modulate)
	{
		if (control is not null)
		{
			// Ensure pivot is centered for scaling (Buttons handle this in their script)
			if (control is not Button)
			{
				control.PivotOffset = control.Size / 2;
			}
			control.Scale = scale;
			control.Modulate = modulate;
		}
	}

	private void StartEntranceAnimation()
	{
		Tween tween = CreateTween();
		tween.SetParallel(false);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back); // Use Back transition for a nice overshoot effect

		tween.TweenInterval(StaggerDelay); // Initial delay

		// Animate Title
		if (titleLabel is not null)
		{
			tween.SetParallel(true); // Scale and Fade happen at the same time
			tween.TweenProperty(titleLabel, CanvasItem.PropertyName.Modulate.ToString() + ":a", 1.0f, AnimationDuration);
			tween.TweenProperty(titleLabel, Node2D.PropertyName.Scale.ToString(), FinalScale, AnimationDuration);
			tween.SetParallel(false); // Back to sequential for the next delay
			tween.TweenInterval(StaggerDelay);
		}

		// Animate Start Button
		if (startButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(startButton, CanvasItem.PropertyName.Modulate.ToString() + ":a", 1.0f, AnimationDuration);
			tween.TweenProperty(startButton, Node2D.PropertyName.Scale.ToString(), FinalScale, AnimationDuration);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Animate Settings Button
		if (settingsButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(settingsButton, CanvasItem.PropertyName.Modulate.ToString() + ":a", 1.0f, AnimationDuration);
			tween.TweenProperty(settingsButton, Node2D.PropertyName.Scale.ToString(), FinalScale, AnimationDuration);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Animate Statistics Button
		if (statisticsButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(statisticsButton, CanvasItem.PropertyName.Modulate.ToString() + ":a", 1.0f, AnimationDuration);
			tween.TweenProperty(statisticsButton, Node2D.PropertyName.Scale.ToString(), FinalScale, AnimationDuration);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Animate Quit Button
		if (quitButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(quitButton, CanvasItem.PropertyName.Modulate.ToString() + ":a", 1.0f, AnimationDuration);
			tween.TweenProperty(quitButton, Node2D.PropertyName.Scale.ToString(), FinalScale, AnimationDuration);
			// No SetParallel(false) needed after the last element
		}

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
		if (IsInstanceValid(startButton)) startButton.Pressed -= OnStartButtonPressed;
		if (IsInstanceValid(settingsButton)) settingsButton.Pressed -= OnSettingsButtonPressed;
		if (IsInstanceValid(statisticsButton)) statisticsButton.Pressed -= OnStatisticsButtonPressed;
		if (IsInstanceValid(quitButton)) quitButton.Pressed -= OnQuitButtonPressed;

		if (titleLabel is not null && IsInstanceValid(titleLabel))
		{
			var callable = Callable.From(SetTitlePivot);
			if (titleLabel.IsConnected(Node.SignalName.Ready, callable))
			{
				titleLabel.Ready -= SetTitlePivot;
			}
		}


		base._ExitTree();
	}
}
