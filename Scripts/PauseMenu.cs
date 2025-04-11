using Godot;

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
		if (GetTree() is SceneTree tree)
		{
			tree.Paused = false;
			tree.ChangeSceneToFile(MainMenuScenePath);
		}
	}

	private void OnQuitButtonPressed()
	{
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
