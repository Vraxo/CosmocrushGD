using Godot;
using CosmocrushGD; // Needed for StatisticsManager

namespace Cosmocrush;

public partial class PauseMenu : ColorRect
{
	[Export] private Button continueButton;
	[Export] private Button returnButton;
	[Export] private Button quitButton;

	private const string MainMenuScenePath = "res://Scenes/Menu/NewMainMenu.tscn";

	public override void _Ready()
	{
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
	}

	private void OnReturnButtonPressed()
	{
		// Update statistics before returning to menu
		var worldNode = GetNode<World>("/root/World"); // Assumes World node path
		if (worldNode != null)
		{
			StatisticsManager.Instance.UpdateScores(worldNode.Score);
			GD.Print($"Returning to menu. Recorded Score: {worldNode.Score}");
		}
		else
		{
			GD.PrintErr("PauseMenu: Could not find World node at /root/World to update scores.");
			// Still save whatever might be dirty
			StatisticsManager.Instance.Save();
		}

		if (GetTree() is SceneTree tree)
		{
			tree.Paused = false;
			tree.ChangeSceneToFile(MainMenuScenePath);
		}
	}

	private void OnQuitButtonPressed()
	{
		// Update statistics before quitting
		var worldNode = GetNode<World>("/root/World"); // Assumes World node path
		if (worldNode != null)
		{
			StatisticsManager.Instance.UpdateScores(worldNode.Score);
			GD.Print($"Quitting game. Recorded Score: {worldNode.Score}");
		}
		else
		{
			GD.PrintErr("PauseMenu: Could not find World node at /root/World to update scores.");
			// Still save whatever might be dirty
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
		// Ensure game is unpaused if the menu is removed unexpectedly
		if (GetTree()?.Paused ?? false)
		{
			GetTree().Paused = false;
		}
	}
}
