using Godot;
using CosmocrushGD;

namespace CosmocrushGD;

public partial class PauseMenu : ColorRect
{
	[Export] private Button continueButton;
	[Export] private Button returnButton;
	[Export] private Button quitButton;

	// Ensure this path points to the MenuShell scene
	private const string MainMenuScenePath = "res://Scenes/Menu/MenuShell.tscn";

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

		if (GetTree() is SceneTree tree)
		{
			tree.Paused = false;
			// This line uses the MainMenuScenePath constant defined above
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
	}
}
