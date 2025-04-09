using Godot;

namespace Cosmocrush;

public partial class World : WorldEnvironment
{
	[Export] private PackedScene pauseMenuScene;

	private PauseMenu pauseMenu;

	public override void _Ready()
	{
		pauseMenu = GetNode<PauseMenu>("/root/World/Player/Camera2D/PauseMenu");
	}

	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Space) && !GetTree().Paused)
		{
			Pause();
		}
	}

	public void _on_pause_button_pressed()
	{
		Pause();
	}

	private void Pause()
	{
		GetTree().Paused = true;
		pauseMenu.Show();
	}
}
