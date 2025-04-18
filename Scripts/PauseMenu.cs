using Godot;
using CosmocrushGD;

namespace CosmocrushGD;

public partial class PauseMenu : CenterContainer
{
	[Export] private Label titleLabel; // Added export for title
	[Export] private UIButton continueButton; // Changed type
	[Export] private UIButton returnButton; // Changed type
	[Export] private UIButton quitButton; // Changed type

	private const string MainMenuScenePath = "res://Scenes/Menu/MenuShell.tscn";

	private const float FadeInDuration = 0.3f;
	private const float StaggerDelay = 0.1f;
	private const float InitialScaleMultiplier = 2.0f;

	public override void _Ready()
	{
		// Basic null checks
		if (titleLabel is null) GD.PrintErr("PauseMenu: Title Label not assigned!");
		if (continueButton is null) GD.PrintErr("PauseMenu: Continue Button not assigned!");
		if (returnButton is null) GD.PrintErr("PauseMenu: Return Button not assigned!");
		if (quitButton is null) GD.PrintErr("PauseMenu: Quit Button not assigned!");


		if (continueButton is not null)
		{
			continueButton.Pressed += OnContinueButtonPressed;
		}

		if (returnButton is not null)
		{
			returnButton.Pressed += OnReturnButtonPressed;
		}

		if (quitButton is not null)
		{
			quitButton.Pressed += OnQuitButtonPressed;
		}

		CallDeferred(nameof(SetupPivots));
		SetInitialState();
		CallDeferred(nameof(StartFadeInAnimation)); // Run animation when shown
	}

	private void SetupPivots()
	{
		if (titleLabel is not null) titleLabel.PivotOffset = titleLabel.Size / 2;
		if (continueButton is not null) continueButton.PivotOffset = continueButton.Size / 2;
		if (returnButton is not null) returnButton.PivotOffset = returnButton.Size / 2;
		if (quitButton is not null) quitButton.PivotOffset = quitButton.Size / 2;
	}

	private void SetInitialState()
	{
		Vector2 initialScale = Vector2.One;

		if (titleLabel is not null) { titleLabel.Modulate = Colors.Transparent; titleLabel.Scale = initialScale; }
		if (continueButton is not null) { continueButton.Modulate = Colors.Transparent; continueButton.Scale = initialScale; continueButton.TweenScale = false; }
		if (returnButton is not null) { returnButton.Modulate = Colors.Transparent; returnButton.Scale = initialScale; returnButton.TweenScale = false; }
		if (quitButton is not null) { quitButton.Modulate = Colors.Transparent; quitButton.Scale = initialScale; quitButton.TweenScale = false; }
	}

	private void StartFadeInAnimation()
	{
		if (titleLabel is null || continueButton is null || returnButton is null || quitButton is null)
		{
			GD.PrintErr("PauseMenu: Cannot start animation, one or more nodes are null.");
			return;
		}

		// Ensure pivots are set before animating
		SetupPivots();

		Tween tween = CreateTween();
		tween.SetParallel(false);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);

		Vector2 initialScaleValue = Vector2.One * InitialScaleMultiplier;
		Vector2 finalScale = Vector2.One;

		tween.TweenInterval(StaggerDelay); // Initial delay

		// Title
		if (titleLabel is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(titleLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Continue Button
		if (continueButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(continueButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(continueButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Return Button
		if (returnButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(returnButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		// Quit Button
		if (quitButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(quitButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(quitButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			// Enable tweening after the last button animates
			tween.TweenCallback(Callable.From(() =>
			{
				if (continueButton is not null) { continueButton.TweenScale = true; }
				if (returnButton is not null) { returnButton.TweenScale = true; }
				if (quitButton is not null) { quitButton.TweenScale = true; }
			}));
		}

		tween.Play();
	}

	public void TriggerContinue()
	{
		OnContinueButtonPressed();
	}

	private void OnContinueButtonPressed()
	{
		if (GetTree() is SceneTree tree)
		{
			tree.Paused = false;
		}

		Hide();
		// Consider QueueFree() if you always instantiate a new pause menu
		// QueueFree();
	}

	private async void OnReturnButtonPressed()
	{
		var worldNode = GetNode<World>("/root/World");
		if (worldNode is not null)
		{
			StatisticsManager.Instance.UpdateScores(worldNode.Score);
			GD.Print($"Returning to menu. Recorded Score: {worldNode.Score}");
		}
		else
		{
			GD.PrintErr("PauseMenu: Could not find World node at /root/World to update scores.");
			StatisticsManager.Instance.Save(); // Save even if world node not found
		}

		// Get the autoloaded transition screen
		var transitionScreen = GetNode<TransitionScreen>("/root/TransitionScreen");
		if (transitionScreen != null)
		{
			// Start the fade out
			transitionScreen.FadeTransition();
			// Wait until the screen is fully black (signal emitted)
			await ToSignal(transitionScreen, TransitionScreen.SignalName.TransitionFinished);
		}
		else
		{
			GD.PrintErr("TransitionScreen autoload node not found!");
		}


		// Now change the scene
		if (GetTree() is SceneTree tree)
		{
			tree.Paused = false; // Ensure game is unpaused before changing scene
			tree.ChangeSceneToFile(MainMenuScenePath);
		}
	}

	private void OnQuitButtonPressed()
	{
		var worldNode = GetNode<World>("/root/World");
		if (worldNode is not null)
		{
			StatisticsManager.Instance.UpdateScores(worldNode.Score);
			GD.Print($"Quitting game. Recorded Score: {worldNode.Score}");
		}
		else
		{
			GD.PrintErr("PauseMenu: Could not find World node at /root/World to update scores.");
			StatisticsManager.Instance.Save();
		}

		GetTree()?.Quit();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!Visible)
		{
			return;
		}

		if (inputEvent.IsActionPressed("ui_cancel") && !inputEvent.IsEcho())
		{
			OnContinueButtonPressed();
			GetViewport()?.SetInputAsHandled();
		}
	}

	public override void _ExitTree()
	{
		if (GetTree()?.Paused ?? false)
		{
			GetTree().Paused = false;
		}

		// Unsubscribe from events
		if (continueButton is not null && IsInstanceValid(continueButton))
		{
			continueButton.Pressed -= OnContinueButtonPressed;
		}
		if (returnButton is not null && IsInstanceValid(returnButton))
		{
			returnButton.Pressed -= OnReturnButtonPressed;
		}
		if (quitButton is not null && IsInstanceValid(quitButton))
		{
			quitButton.Pressed -= OnQuitButtonPressed;
		}

		base._ExitTree();
	}
}
