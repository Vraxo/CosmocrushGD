using Godot;

namespace Cosmocrush;

public partial class World : WorldEnvironment
{
	[Export] private PackedScene pauseMenuScene;
	[Export] private Button pauseButton;

	private PauseMenu pauseMenu;

	public override void _Ready()
	{
		pauseMenu = GetNode<PauseMenu>("/root/World/Player/Camera2D/PauseMenu");

		pauseButton.Pressed += OnPauseButtonPressed;
	}

	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Space) && !GetTree().Paused)
		{
			Pause();
		}
	}

	private void OnPauseButtonPressed()
	{
		Pause();
	}

	private void Pause()
	{
		GetTree().Paused = true;
		pauseMenu.Show();
	}
}
