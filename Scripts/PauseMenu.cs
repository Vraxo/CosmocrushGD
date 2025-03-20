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

	private void OnContinueButtonLeftClicked()
	{
		GetTree().Paused = false;
		Hide();
	}

	private void OnQuitButtonLeftClicked()
	{
		GetTree().Quit();
	}
}
