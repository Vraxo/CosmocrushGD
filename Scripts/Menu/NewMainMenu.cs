using Godot;

namespace CosmocrushGD;

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
		GD.Print("NewMainMenu: _Ready() started");

		// Access Settings.Instance to ensure it's initialized
		GD.Print("NewMainMenu: Accessing Settings.Instance");
		var _ = CosmocrushGD.Settings.Instance;
		GD.Print("NewMainMenu: Settings.Instance accessed");

		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		GD.Print("NewMainMenu: Checking and assigning startButton event");
		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning settingsButton event");
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning quitButton event");
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GD.Print("NewMainMenu: Connecting to CloseRequested signal");
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
		GD.Print("NewMainMenu: CloseRequested signal connected");

		GD.Print("NewMainMenu: _Ready() finished");

		// Add space stars effect
		CPUParticles2D spaceStars = new CPUParticles2D();
		spaceStars.Amount = 50;
		spaceStars.Lifetime = 2.0f;
		spaceStars.Randomness = 0.5f;
		spaceStars.SpeedScale = 1.0f;
		spaceStars.ProcessMaterial = new ParticleProcessMaterial();
		spaceStars.ProcessMaterial.Color = new Color(1, 1, 1); // White color
		spaceStars.ProcessMaterial.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Line;
		spaceStars.ProcessMaterial.EmissionShapeLineLength = 1;
		spaceStars.ProcessMaterial.ParticleFlagDisableRotation = true;
		spaceStars.ProcessMaterial.Gravity = new Vector2(20, 0);
		spaceStars.ProcessMaterial.InitialVelocity = 10f;
		spaceStars.ProcessMaterial.AngularVelocityMax = 0f;
		spaceStars.ProcessMaterial.AngularVelocityMin = 0f;
		spaceStars.ProcessMaterial.RadialAccelMax = 0f;
		spaceStars.ProcessMaterial.RadialAccelMin = 0f;
		spaceStars.ProcessMaterial.TangentialAccelMax = 0f;
		spaceStars.ProcessMaterial.TangentialAccelMin = 0f;
		spaceStars.ProcessMaterial.ScaleMin = 0.01f;
		spaceStars.ProcessMaterial.ScaleMax = 0.02f;
		AddChild(spaceStars);

		// Set emission position and direction
		spaceStars.Position = new Vector2(0, 0);
		spaceStars.Direction = new Vector2(1, 0);
		spaceStars.Spread = 90; // Spread from top to bottom
		spaceStars.Emitting = true;
	}
using Godot;

namespace CosmocrushGD;

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
		GD.Print("NewMainMenu: _Ready() started");

		// Access Settings.Instance to ensure it's initialized
		GD.Print("NewMainMenu: Accessing Settings.Instance");
		var _ = CosmocrushGD.Settings.Instance;
		GD.Print("NewMainMenu: Settings.Instance accessed");

		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		GD.Print("NewMainMenu: Checking and assigning startButton event");
		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning settingsButton event");
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning quitButton event");
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GD.Print("NewMainMenu: Connecting to CloseRequested signal");
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
		GD.Print("NewMainMenu: CloseRequested signal connected");

		GD.Print("NewMainMenu: _Ready() finished");

		// Space stars effect
		CPUParticles2D spaceStars = new CPUParticles2D
		{
			Amount = 50,
			Lifetime = 2.0f,
			Randomness = 0.5f,
			SpeedScale = 1.0f,
			ProcessMaterial = new ParticleProcessMaterial
			{
				Color = Colors.White,
				EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Line,
				EmissionShapeLineLength = 1,
				ParticleFlagDisableRotation = true,
				Gravity = new Vector2(20, 0),
				InitialVelocity = 10f,
				AngularVelocityMax = 0f,
				AngularVelocityMin = 0f,
				RadialAccelMax = 0f,
				RadialAccelMin = 0f,
				TangentialAccelMax = 0f,
				TangentialAccelMin = 0f,
				ScaleMin = 0.01f,
				ScaleMax = 0.02f
			},
			Position = new Vector2(0, 0),
			Direction = new Vector2(1, 0),
			Spread = 90,
			Emitting = true
		};
		AddChild(spaceStars);
	}
}
using Godot;

namespace CosmocrushGD;

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
		GD.Print("NewMainMenu: _Ready() started");

		// Access Settings.Instance to ensure it's initialized
		GD.Print("NewMainMenu: Accessing Settings.Instance");
		var _ = CosmocrushGD.Settings.Instance;
		GD.Print("NewMainMenu: Settings.Instance accessed");

		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		GD.Print("NewMainMenu: Checking and assigning startButton event");
		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning settingsButton event");
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning quitButton event");
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GD.Print("NewMainMenu: Connecting to CloseRequested signal");
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
		GD.Print("NewMainMenu: CloseRequested signal connected");

		GD.Print("NewMainMenu: _Ready() finished");

		// Add space stars effect
		CPUParticles2D spaceStars = new CPUParticles2D();
		spaceStars.Amount = 50;
		spaceStars.Lifetime = 2.0f;
		spaceStars.Randomness = 0.5f;
		spaceStars.SpeedScale = 1.0f;
		spaceStars.ProcessMaterial = new ParticleProcessMaterial();
		spaceStars.ProcessMaterial.Color = new Color(1, 1, 1); // White color
		spaceStars.ProcessMaterial.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Line;
		spaceStars.ProcessMaterial.EmissionShapeLineLength = 1;
		spaceStars.ProcessMaterial.ParticleFlagDisableRotation = true;
		spaceStars.ProcessMaterial.Gravity = new Vector2(20, 0);
		spaceStars.ProcessMaterial.InitialVelocity = 10f;
		spaceStars.ProcessMaterial.AngularVelocityMax = 0f;
		spaceStars.ProcessMaterial.AngularVelocityMin = 0f;
		spaceStars.ProcessMaterial.RadialAccelMax = 0f;
		spaceStars.ProcessMaterial.RadialAccelMin = 0f;
		spaceStars.ProcessMaterial.TangentialAccelMax = 0f;
		spaceStars.ProcessMaterial.TangentialAccelMin = 0f;
		spaceStars.ProcessMaterial.ScaleMin = 0.01f;
		spaceStars.ProcessMaterial.ScaleMax = 0.02f;
		AddChild(spaceStars);

		// Set emission position and direction
		spaceStars.Position = new Vector2(0, 0);
		spaceStars.Direction = new Vector2(1, 0);
		spaceStars.Spread = 90; // Spread from top to bottom
		spaceStars.Emitting = true;
	}
using Godot;

namespace CosmocrushGD;

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
		GD.Print("NewMainMenu: _Ready() started");

		// Access Settings.Instance to ensure it's initialized
		GD.Print("NewMainMenu: Accessing Settings.Instance");
		var _ = CosmocrushGD.Settings.Instance;
		GD.Print("NewMainMenu: Settings.Instance accessed");

		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		GD.Print("NewMainMenu: Checking and assigning startButton event");
		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning settingsButton event");
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning quitButton event");
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GD.Print("NewMainMenu: Connecting to CloseRequested signal");
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
		GD.Print("NewMainMenu: CloseRequested signal connected");

		GD.Print("NewMainMenu: _Ready() finished");

		// Space stars effect
		CPUParticles2D spaceStars = new CPUParticles2D
		{
			Amount = 50,
			Lifetime = 2.0f,
			Randomness = 0.5f,
			SpeedScale = 1.0f,
			ProcessMaterial = new ParticleProcessMaterial
			{
				Color = Colors.White,
				EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Line,
				EmissionShapeLineLength = 1,
				ParticleFlagDisableRotation = true,
				Gravity = new Vector2(20, 0),
				InitialVelocity = 10f,
				AngularVelocityMax = 0f,
				AngularVelocityMin = 0f,
				RadialAccelMax = 0f,
				RadialAccelMin = 0f,
				TangentialAccelMax = 0f,
				TangentialAccelMin = 0f,
				ScaleMin = 0.01f,
				ScaleMax = 0.02f
			},
			Position = new Vector2(0, 0),
			Direction = new Vector2(1, 0),
			Spread = 90,
			Emitting = true
		};
		AddChild(spaceStars);
	}
using Godot;

namespace CosmocrushGD;

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
		GD.Print("NewMainMenu: _Ready() started");

		// Access Settings.Instance to ensure it's initialized
		GD.Print("NewMainMenu: Accessing Settings.Instance");
		var _ = CosmocrushGD.Settings.Instance;
		GD.Print("NewMainMenu: Settings.Instance accessed");

		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		GD.Print("NewMainMenu: Checking and assigning startButton event");
		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning settingsButton event");
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning quitButton event");
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GD.Print("NewMainMenu: Connecting to CloseRequested signal");
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
		GD.Print("NewMainMenu: CloseRequested signal connected");

		GD.Print("NewMainMenu: _Ready() finished");

		// Add space stars effect
		CPUParticles2D spaceStars = new CPUParticles2D();
		spaceStars.Amount = 50;
		spaceStars.Lifetime = 2.0f;
		spaceStars.Randomness = 0.5f;
		spaceStars.SpeedScale = 1.0f;
		spaceStars.ProcessMaterial = new ParticleProcessMaterial();
		spaceStars.ProcessMaterial.Color = new Color(1, 1, 1); // White color
		spaceStars.ProcessMaterial.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Line;
		spaceStars.ProcessMaterial.EmissionShapeLineLength = 1;
		spaceStars.ProcessMaterial.ParticleFlagDisableRotation = true;
		spaceStars.ProcessMaterial.Gravity = new Vector2(20, 0);
		spaceStars.ProcessMaterial.InitialVelocity = 10f;
		spaceStars.ProcessMaterial.AngularVelocityMax = 0f;
		spaceStars.ProcessMaterial.AngularVelocityMin = 0f;
		spaceStars.ProcessMaterial.RadialAccelMax = 0f;
		spaceStars.ProcessMaterial.RadialAccelMin = 0f;
		spaceStars.ProcessMaterial.TangentialAccelMax = 0f;
		spaceStars.ProcessMaterial.TangentialAccelMin = 0f;
		spaceStars.ProcessMaterial.ScaleMin = 0.01f;
		spaceStars.ProcessMaterial.ScaleMax = 0.02f;
		AddChild(spaceStars);

		// Set emission position and direction
		spaceStars.Position = new Vector2(0, 0);
		spaceStars.Direction = new Vector2(1, 0);
		spaceStars.Spread = 90; // Spread from top to bottom
		spaceStars.Emitting = true;
	}
using Godot;

namespace CosmocrushGD;

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
		GD.Print("NewMainMenu: _Ready() started");

		// Access Settings.Instance to ensure it's initialized
		GD.Print("NewMainMenu: Accessing Settings.Instance");
		var _ = CosmocrushGD.Settings.Instance;
		GD.Print("NewMainMenu: Settings.Instance accessed");

		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		GD.Print("NewMainMenu: Checking and assigning startButton event");
		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning settingsButton event");
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning quitButton event");
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GD.Print("NewMainMenu: Connecting to CloseRequested signal");
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
		GD.Print("NewMainMenu: CloseRequested signal connected");

		GD.Print("NewMainMenu: _Ready() finished");

		// Space stars effect
		CPUParticles2D spaceStars = new CPUParticles2D
		{
			Amount = 50,
			Lifetime = 2.0f,
			Randomness = 0.5f,
			SpeedScale = 1.0f,
			ProcessMaterial = new ParticleProcessMaterial
			{
				Color = Colors.White,
				EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Line,
				EmissionShapeLineLength = 1,
				ParticleFlagDisableRotation = true,
				Gravity = new Vector2(20, 0),
				InitialVelocity = 10f,
				AngularVelocityMax = 0f,
				AngularVelocityMin = 0f,
				RadialAccelMax = 0f,
				RadialAccelMin = 0f,
				TangentialAccelMax = 0f,
				TangentialAccelMin = 0f,
				ScaleMin = 0.01f,
				ScaleMax = 0.02f
			},
			Position = new Vector2(0, 0),
			Direction = new Vector2(1, 0),
			Spread = 90,
			Emitting = true
		};
		AddChild(spaceStars);
	}
}
using Godot;

namespace CosmocrushGD;

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
		GD.Print("NewMainMenu: _Ready() started");

		// Access Settings.Instance to ensure it's initialized
		GD.Print("NewMainMenu: Accessing Settings.Instance");
		var _ = CosmocrushGD.Settings.Instance;
		GD.Print("NewMainMenu: Settings.Instance accessed");

		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		GD.Print("NewMainMenu: Checking and assigning startButton event");
		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning settingsButton event");
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning quitButton event");
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GD.Print("NewMainMenu: Connecting to CloseRequested signal");
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
		GD.Print("NewMainMenu: CloseRequested signal connected");

		GD.Print("NewMainMenu: _Ready() finished");

		// Add space stars effect
		CPUParticles2D spaceStars = new CPUParticles2D();
		spaceStars.Amount = 50;
		spaceStars.Lifetime = 2.0f;
		spaceStars.Randomness = 0.5f;
		spaceStars.SpeedScale = 1.0f;
		spaceStars.ProcessMaterial = new ParticleProcessMaterial();
		spaceStars.ProcessMaterial.Color = new Color(1, 1, 1); // White color
		spaceStars.ProcessMaterial.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Line;
		spaceStars.ProcessMaterial.EmissionShapeLineLength = 1;
		spaceStars.ProcessMaterial.ParticleFlagDisableRotation = true;
		spaceStars.ProcessMaterial.Gravity = new Vector2(20, 0);
		spaceStars.ProcessMaterial.InitialVelocity = 10f;
		spaceStars.ProcessMaterial.AngularVelocityMax = 0f;
		spaceStars.ProcessMaterial.AngularVelocityMin = 0f;
		spaceStars.ProcessMaterial.RadialAccelMax = 0f;
		spaceStars.ProcessMaterial.RadialAccelMin = 0f;
		spaceStars.ProcessMaterial.TangentialAccelMax = 0f;
		spaceStars.ProcessMaterial.TangentialAccelMin = 0f;
		spaceStars.ProcessMaterial.ScaleMin = 0.01f;
		spaceStars.ProcessMaterial.ScaleMax = 0.02f;
		AddChild(spaceStars);

		// Set emission position and direction
		spaceStars.Position = new Vector2(0, 0);
		spaceStars.Direction = new Vector2(1, 0);
		spaceStars.Spread = 90; // Spread from top to bottom
		spaceStars.Emitting = true;
	}
using Godot;

namespace CosmocrushGD;

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
		GD.Print("NewMainMenu: _Ready() started");

		// Access Settings.Instance to ensure it's initialized
		GD.Print("NewMainMenu: Accessing Settings.Instance");
		var _ = CosmocrushGD.Settings.Instance;
		GD.Print("NewMainMenu: Settings.Instance accessed");

		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		GD.Print("NewMainMenu: Checking and assigning startButton event");
		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning settingsButton event");
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning quitButton event");
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GD.Print("NewMainMenu: Connecting to CloseRequested signal");
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
		GD.Print("NewMainMenu: CloseRequested signal connected");

		GD.Print("NewMainMenu: _Ready() finished");
	}
}
using Godot;

namespace CosmocrushGD;

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
		GD.Print("NewMainMenu: _Ready() started");

		// Access Settings.Instance to ensure it's initialized
		GD.Print("NewMainMenu: Accessing Settings.Instance");
		var _ = CosmocrushGD.Settings.Instance;
		GD.Print("NewMainMenu: Settings.Instance accessed");

		// Ensure buttons are assigned in the inspector or find them if not
		if (startButton == null || settingsButton == null || quitButton == null)
		{
			GD.PrintErr("NewMainMenu: One or more buttons not assigned in the inspector!");
			// Optionally try to find them by name/path if not assigned
			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
		}

		GD.Print("NewMainMenu: Checking and assigning startButton event");
		if (startButton != null)
			startButton.Pressed += OnStartButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning settingsButton event");
		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
		GD.Print("NewMainMenu: Checking and assigning quitButton event");
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;

		// Connect to the root window's close request signal
		GD.Print("NewMainMenu: Connecting to CloseRequested signal");
		GetTree().Root.CloseRequested += OnWindowCloseRequested;
		GD.Print("NewMainMenu: CloseRequested signal connected");

		GD.Print("NewMainMenu: _Ready() finished");
	}
using Godot;

namespace CosmocrushGD;

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
		// Access Settings.Instance to ensure it's initialized
		var _ = CosmocrushGD.Settings.Instance;

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
