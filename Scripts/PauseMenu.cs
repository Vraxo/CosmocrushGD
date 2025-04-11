using Godot;

namespace Cosmocrush; // Assuming this namespace is correct

public partial class PauseMenu : ColorRect
{
	[Export] private Button continueButton;
	[Export] private Button returnButton;
	[Export] private Button quitButton;

	private const string MainMenuScenePath = "res://Scenes/Menu/NewMainMenu.tscn";

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always; // Ensure input is processed even when paused

		if (continueButton is not null)
		{
			continueButton.Pressed += OnContinueButtonPressed;
		}
		else
		{
			GD.PushWarning("PauseMenu: Continue button not assigned.");
		}

		if (returnButton is not null)
		{
			returnButton.Pressed += OnReturnButtonPressed;
		}
		else
		{
			GD.PushWarning("PauseMenu: Return button not assigned.");
		}

		if (quitButton is not null)
		{
			quitButton.Pressed += OnQuitButtonPressed;
		}
		else
		{
			GD.PushWarning("PauseMenu: Quit button not assigned.");
		}
	}

	public void TriggerContinue()
	{
		OnContinueButtonPressed();
	}

	private void SetGamePaused(bool isPaused)
	{
		SceneTree tree = GetTree();
		if (tree is not null)
		{
			tree.Paused = isPaused;
		}
	}

	private void OnContinueButtonPressed()
	{
		SetGamePaused(false);
		Hide(); // Hide the pause menu itself
	}

	private void OnReturnButtonPressed()
	{
		SetGamePaused(false); // Unpause before changing scene

		// --- End Game Session ---
		CosmocrushGD.StatisticsManager.Instance.EndGame(); // Use full namespace if needed
		// --- End Game Session ---

		SceneTree tree = GetTree();
		Error changeSceneError = tree?.ChangeSceneToFile(MainMenuScenePath) ?? Error.CantOpen;

		if (changeSceneError is not Error.Ok)
		{
			GD.PrintErr($"Failed to change scene to {MainMenuScenePath}: {changeSceneError}");
		}
	}

	private void OnQuitButtonPressed()
	{
		SetGamePaused(false); // Ensure game is unpaused before quitting
		GetTree()?.Quit();
	}

	public override void _Input(InputEvent inputEvent)
	{
		// Only handle input if the menu is visible and the game is paused
		if (!Visible || !GetTree().Paused)
		{
			return;
		}

		if (inputEvent.IsActionPressed("ui_cancel") && !inputEvent.IsEcho())
		{
			OnContinueButtonPressed();
			GetViewport()?.SetInputAsHandled(); // Prevent event bubbling
		}
	}

	public override void _ExitTree()
	{
		// Ensure the game is unpaused if this node is removed unexpectedly
		if (GetTree()?.Paused ?? false)
		{
			SetGamePaused(false);
		}

		// Explicitly disconnect signals if connected in _Ready
		if (continueButton is not null && continueButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnContinueButtonPressed)))
		{
			continueButton.Pressed -= OnContinueButtonPressed;
		}
		if (returnButton is not null && returnButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnReturnButtonPressed)))
		{
			returnButton.Pressed -= OnReturnButtonPressed;
		}
		if (quitButton is not null && quitButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnQuitButtonPressed)))
		{
			quitButton.Pressed -= OnQuitButtonPressed;
		}
	}
}
