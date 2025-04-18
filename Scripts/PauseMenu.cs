using Godot;
using CosmocrushGD;

namespace CosmocrushGD;

public partial class PauseMenu : CenterContainer
{
	[Export] private Label titleLabel;
	[Export] private UIButton continueButton;
	[Export] private UIButton returnButton;
	[Export] private UIButton quitButton;

	private const string MainMenuScenePath = "res://Scenes/Menu/MenuShell.tscn";

	private const float FadeInDuration = 0.3f;
	private const float StaggerDelay = 0.1f;
	private const float InitialScaleMultiplier = 2.0f;

	// No need for member variable for TransitionScreen

	public override void _Ready()
	{
		// Access instance and connect signals directly
		if (TransitionScreen.Instance is not null)
		{
			GD.Print("PauseMenu: Found TransitionScreen Instance. Connecting signal.");
			TransitionScreen.Instance.TransitionMidpointReached += OnTransitionMidpointReached;
		}
		else
		{
			GD.PrintErr("PauseMenu: Could not find TransitionScreen Instance in _Ready!");
		}

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
		CallDeferred(nameof(StartFadeInAnimation));
	}

	// Removed InitializeTransitionScreen method

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

		SetupPivots();

		Tween tween = CreateTween();
		tween.SetParallel(false);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);

		Vector2 initialScaleValue = Vector2.One * InitialScaleMultiplier;
		Vector2 finalScale = Vector2.One;

		tween.TweenInterval(StaggerDelay);

		if (titleLabel is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(titleLabel, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(titleLabel, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		if (continueButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(continueButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(continueButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		if (returnButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(returnButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(returnButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);
			tween.TweenInterval(StaggerDelay);
		}

		if (quitButton is not null)
		{
			tween.SetParallel(true);
			tween.TweenProperty(quitButton, "modulate:a", 1.0f, FadeInDuration);
			tween.TweenProperty(quitButton, "scale", finalScale, FadeInDuration).From(initialScaleValue);
			tween.SetParallel(false);

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
		// QueueFree() is still an option here if you always want a fresh pause menu
		// QueueFree();
	}


	private void OnReturnButtonPressed()
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
			StatisticsManager.Instance.Save();
		}

		// Unpause happens in OnTransitionMidpointReached now
		// if (GetTree() is SceneTree tree)
		// {
		//     tree.Paused = false;
		// }

		if (TransitionScreen.Instance is not null)
		{
			TransitionScreen.Instance.TransitionToScene(MainMenuScenePath);
		}
		else
		{
			GD.PrintErr("PauseMenu: Cannot Return, TransitionScreen Instance is null. Changing scene directly.");
			if (GetTree() is SceneTree tree) { tree.Paused = false; } // Unpause before direct change
			GetTree().ChangeSceneToFile(MainMenuScenePath); // Fallback
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

	private void OnTransitionMidpointReached(string scenePathToLoad)
	{
		GD.Print($"PauseMenu: OnTransitionMidpointReached, loading: {scenePathToLoad}");
		// Ensure we unpause if we are changing scenes from the pause menu
		if (GetTree() is SceneTree tree)
		{
			tree.Paused = false;
		}
		GetTree().ChangeSceneToFile(scenePathToLoad);
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
		// Ensure game is unpaused if exiting tree while paused
		// (e.g., if closed abruptly or returned via another menu)
		if (GetTree()?.Paused ?? false)
		{
			GetTree().Paused = false;
		}

		if (TransitionScreen.Instance is not null && IsInstanceValid(TransitionScreen.Instance))
		{
			if (TransitionScreen.Instance.IsConnected(TransitionScreen.SignalName.TransitionMidpointReached, Callable.From<string>(OnTransitionMidpointReached)))
			{
				TransitionScreen.Instance.TransitionMidpointReached -= OnTransitionMidpointReached;
				GD.Print("PauseMenu: Unsubscribed from TransitionMidpointReached.");
			}
		}

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
