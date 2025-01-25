using Godot;

public partial class MainMenu : Node2D
{
	[Export]
	private PackedScene worldScene;

	[Export]
	private PackedScene settingsMenuScene;

	private Label title;
	private Button playButton;
	private Button quitButton;
	private Button settingsButton;

	public override void _Ready()
	{
		title = GetNode<Label>("Title");

		playButton = GetNode<Button>("PlayButton");
		playButton.Pressed += OnStartButtonPressed;

		quitButton = GetNode<Button>("QuitButton");
		quitButton.Pressed += OnQuitButtonPressed;

		settingsButton = GetNode<Button>("SettingsButton");
		settingsButton.Pressed += OnSettingsButtonPressed;
	}

	public override void _Process(double delta)
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();

		title.Position = new(windowSize.X / 2 - title.Size.X / 2, title.Position.Y);
		playButton.Position = new(windowSize.X / 2 - playButton.Size.X / 2, windowSize.Y / 2 - 50);
		settingsButton.Position = new(windowSize.X / 2 - settingsButton.Size.X / 2, windowSize.Y / 2);
		quitButton.Position = new(windowSize.X / 2 - quitButton.Size.X / 2, windowSize.Y / 2 + 50);
	}

	private void OnStartButtonPressed()
	{
		if (worldScene is null)
		{
			GD.PrintErr("worldScene is not assigned in the Inspector!");
			return;
		}

		GetTree().ChangeSceneToPacked(worldScene);
	}

	private void OnSettingsButtonPressed()
	{
		GetParent().AddChild(settingsMenuScene.Instantiate<Node2D>());
		QueueFree();
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
}
