using Godot;

namespace Cosmocrush; // Assuming the same namespace as PauseMenu.cs

public partial class NewMainMenu : ColorRect
{
	[Export] private Button startButton;
	[Export] private Button settingsButton;
	[Export] private Button quitButton;

	// Scene paths - adjust if necessary
	private const string GameScenePath = "res://Scenes/World.tscn";
	private const string SettingsScenePath = "res://Scenes/Menu/SettingsMenu.tscn";

	public override void _Ready()
	{
		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
	}

	private void OnStartButtonPressed()
	{
		GD.Print("Start button pressed. Loading game scene...");
		var error = GetTree().ChangeSceneToFile(GameScenePath);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to load game scene: {error}");
		}
	}

	private void OnSettingsButtonPressed()
	{
		GD.Print("Settings button pressed. Loading settings scene...");
		var error = GetTree().ChangeSceneToFile(SettingsScenePath);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to load settings scene: {error}");
		}
		// Alternatively, if SettingsMenu is meant to be an overlay:
		// var settingsScene = GD.Load<PackedScene>(SettingsScenePath);
		// if (settingsScene != null)
		// {
		//     var settingsInstance = settingsScene.Instantiate();
		//     AddChild(settingsInstance); // Or GetTree().Root.AddChild(settingsInstance);
		// }
		// else
		// {
		//     GD.PrintErr("Failed to load settings scene resource.");
		// }
	}

	private void OnQuitButtonPressed()
	{
		GD.Print("Quit button pressed. Quitting application...");
		GetTree().Quit();
	}

	private void OnWindowCloseRequested()
	{
		// This method is called when the user clicks the window's close button.
		GD.Print("Window close requested via signal. Quitting application...");
		GetTree().Quit();
	}
}
