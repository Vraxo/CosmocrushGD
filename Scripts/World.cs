using Godot;

namespace Cosmocrush;

public partial class World : WorldEnvironment
{
	[Export] private PackedScene pauseMenuScene;
	[Export] private Button pauseButton;
	[Export] private CanvasLayer hudLayer; // Reference the existing HUD layer

	private PauseMenu pauseMenu;

	public override void _Ready()
	{
		if (hudLayer == null)
		{
			GD.PrintErr("HUD Layer reference not set in World!");
		}

		pauseButton.Pressed += OnPauseButtonPressed;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel") && !GetTree().Paused)
		{
			Pause();
		}
	}

	private void OnPauseButtonPressed()
	{
		if (!GetTree().Paused)
		{
			Pause();
		}
		else if (pauseMenu != null && IsInstanceValid(pauseMenu))
		{
			pauseMenu.TriggerContinue();
		}
	}

	private void Pause()
	{
		if (GetTree().Paused) return;

		if (pauseMenu == null || !IsInstanceValid(pauseMenu))
		{
			if (pauseMenuScene == null)
			{
				GD.PrintErr("PauseMenuScene is not set in World script!");
				return;
			}
			if (hudLayer == null)
			{
				GD.PrintErr("HUD Layer is not set or found in World script!");
				return;
			}
			pauseMenu = pauseMenuScene.Instantiate<PauseMenu>();
			hudLayer.AddChild(pauseMenu); // Add to the HUD layer
		}

		GetTree().Paused = true;
		pauseMenu.Show();
	}
}
