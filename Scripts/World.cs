using Godot;
using CosmocrushGD; // Add namespace for StatisticsManager and ScoreDisplay

namespace Cosmocrush; // Assuming this namespace is correct

public partial class World : WorldEnvironment
{
	[Export] private PackedScene pauseMenuScene;
	[Export] private Button pauseButton;
	[Export] private CanvasLayer hudLayer;

	private PauseMenu pauseMenuInstance; // Renamed for clarity

	public override void _Ready()
	{
		// --- Initialize Statistics Manager ---
		StatisticsManager.Instance.EnsureLoaded();
		// Reset current score at the start of a new game world
		// Note: EndGame already resets it, but this ensures it if loading straight into World.
		// StatisticsManager.Instance.ResetCurrentScore(); // Add this method if needed, or rely on EndGame.
		// --- End Initialization ---


		if (hudLayer is null)
		{
			GD.PrintErr("World: HUD Layer reference not set!");
			// Attempt to find it if not set
			hudLayer = GetNodeOrNull<CanvasLayer>("HUD");
			if (hudLayer is null)
			{
				GD.PrintErr("World: Could not find HUD CanvasLayer. Pausing and Score will not work correctly.");
				SetProcess(false); // Disable processing if HUD is critical
				return;
			}
		}

		if (pauseButton is not null)
		{
			pauseButton.Pressed += OnPauseButtonPressed;
		}
		else
		{
			GD.PrintErr("World: Pause Button reference not set!");
			// Attempt to find it
			pauseButton = hudLayer.GetNodeOrNull<Button>("PauseButton");
			if (pauseButton is not null)
			{
				pauseButton.Pressed += OnPauseButtonPressed;
			}
			else
			{
				GD.PrintErr("World: Could not find PauseButton in HUD.");
			}
		}
	}

	public override void _Process(double delta)
	{
		// Handle pause input only if the game is not already paused
		if (Input.IsActionJustPressed("ui_cancel") && !GetTree().Paused)
		{
			PauseGame();
		}
	}

	private void OnPauseButtonPressed()
	{
		if (!GetTree().Paused)
		{
			PauseGame();
		}
		else if (pauseMenuInstance is not null && IsInstanceValid(pauseMenuInstance))
		{
			// If already paused, the pause button should unpause
			pauseMenuInstance.TriggerContinue();
		}
		// Consider adding an else case here if pauseMenuInstance is somehow null while paused
	}

	private void PauseGame()
	{
		if (GetTree().Paused) // Already paused, do nothing
		{
			return;
		}

		// Instantiate pause menu if it doesn't exist or was freed
		if (pauseMenuInstance is null || !IsInstanceValid(pauseMenuInstance))
		{
			if (pauseMenuScene is null)
			{
				GD.PrintErr("World: PauseMenuScene is not assigned!");
				return;
			}

			if (hudLayer is null)
			{
				GD.PrintErr("World: HUD Layer is missing, cannot add Pause Menu!");
				return;
			}

			pauseMenuInstance = pauseMenuScene.Instantiate<PauseMenu>();
			hudLayer.AddChild(pauseMenuInstance); // Add to HUD layer
		}

		pauseMenuInstance.Show(); // Make sure it's visible
		GetTree().Paused = true; // Pause the game
	}

	public override void _ExitTree()
	{
		// Clean up connections
		if (pauseButton is not null && pauseButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnPauseButtonPressed)))
		{
			pauseButton.Pressed -= OnPauseButtonPressed;
		}
	}
}
