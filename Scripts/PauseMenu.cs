using Godot;

namespace Cosmocrush;

public partial class PauseMenu : ColorRect
{
	[Export] private Button continueButton;
	[Export] private Button quitButton;

	public override void _Ready()
	{
		continueButton.Pressed += OnContinueButtonLeftClicked;
		quitButton.Pressed += OnQuitButtonLeftClicked;
	}

	// Public method to allow external triggering (like from World.cs pause button)
	public void TriggerContinue()
	{
		OnContinueButtonLeftClicked();
	}

	private void OnContinueButtonLeftClicked()
	{
		GetTree().Paused = false;
		Hide();
	}

	private void OnQuitButtonLeftClicked()
	{
		GetTree().Quit();
	}

	public override void _Input(InputEvent @event)
	{
		// Allow Esc/Back to also close the pause menu
		if (@event.IsActionPressed("ui_cancel") && Visible && !@event.IsEcho())
		{
			OnContinueButtonLeftClicked();
			GetViewport().SetInputAsHandled(); // Consume the event
		}
	}

	// Ensure the menu is hidden if the scene is changed while paused
	public override void _ExitTree()
	{
		if (GetTree().Paused)
		{
			GetTree().Paused = false;
		}
	}
}
