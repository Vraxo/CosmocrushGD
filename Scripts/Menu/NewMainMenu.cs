using Godot;
using System;

namespace CosmocrushGD
{
	public partial class NewMainMenu : ColorRect
	{
		[Export] private Button startButton;
		[Export] private Button settingsButton;
		[Export] private Button quitButton;
		[Export] private Node starParticlesNode;
		private CpuParticles2D _starParticles;

		private const string GameScenePath = "res://Scenes/World.tscn";
		private const string SettingsScenePath = "res://Scenes/Menu/SettingsMenu.tscn";

		public override void _Ready()
		{
			GD.Print("--- NewMainMenu _Ready: Start ---");

			try
			{
				var _ = CosmocrushGD.Settings.Instance;
				GD.Print("Settings Instance accessed.");
			}
			catch (Exception e)
			{
				GD.PrintErr($"Error accessing Settings.Instance: {e.Message}");
			}

			startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
			if (startButton == null || settingsButton == null || quitButton == null)
			{
				GD.PrintErr("NewMainMenu: One or more buttons Null!");
			}
			GD.Print("Buttons checked/retrieved.");

			GD.Print("Attempting to get StarParticles...");
			if (starParticlesNode != null && starParticlesNode is CpuParticles2D specificParticles)
			{
				_starParticles = specificParticles;
				GD.Print("StarParticles obtained from Exported Node.");
			}
			else
			{
				GD.Print($"Exported starParticlesNode is {(starParticlesNode == null ? "null" : "not a CpuParticles2D")}. Trying GetNode...");
				_starParticles = GetNode<CpuParticles2D>("StarParticles");
				if (_starParticles == null)
				{
					GD.PrintErr("Failed to get StarParticles node by path 'StarParticles'. Effect disabled, but continuing.");
				}
				else
				{
					GD.Print("StarParticles obtained via GetNode.");
				}
			}

			if (startButton != null) startButton.Pressed += OnStartButtonPressed;
			if (settingsButton != null) settingsButton.Pressed += OnSettingsButtonPressed;
			if (quitButton != null) quitButton.Pressed += OnQuitButtonPressed;
			GD.Print("Button signals connected.");

			var root = GetTree()?.Root;
			if (root != null)
			{
				root.CloseRequested += OnWindowCloseRequested;
				GD.Print("CloseRequested signal connected.");
			}
			else
			{
				GD.PrintErr("GetTree().Root is null, cannot connect CloseRequested signal.");
			}

			GD.Print("Connecting Resized signal...");
			Resized += UpdateParticleEmitterBounds;
			GD.Print("Resized signal connected.");

			GD.Print("Scheduling deferred call to UpdateParticleEmitterBounds...");
			if (_starParticles != null)
			{
				_starParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
				CallDeferred(nameof(UpdateParticleEmitterBounds));
				GD.Print("Deferred call scheduled.");
			}
			else
			{
				GD.Print("Skipping deferred call because _starParticles is null.");
			}

			GD.Print("--- NewMainMenu _Ready: End ---");
		}

		private void UpdateParticleEmitterBounds()
		{
			GD.Print("--- UpdateParticleEmitterBounds: Start ---");

			if (_starParticles == null)
			{
				GD.PrintErr("UpdateParticleEmitterBounds: _starParticles is null. Aborting.");
				return;
			}

			if (!IsInsideTree())
			{
				GD.PrintErr("UpdateParticleEmitterBounds: Node is not inside the tree. Aborting.");
				return;
			}

			var viewport = GetViewport();
			if (viewport == null)
			{
				GD.PrintErr("UpdateParticleEmitterBounds: GetViewport() returned null. Aborting.");
				return;
			}

			var viewportHeight = GetViewportRect().Size.Y;
			GD.Print($"Viewport height: {viewportHeight}");

			// --- CHANGE HERE: Shift X position slightly left ---
			const float spawnOffsetX = -10.0f; // Adjust this value as needed
			_starParticles.Position = new Vector2(spawnOffsetX, viewportHeight / 2.0f);
			// --- END CHANGE ---
			GD.Print($"Set particle position to: {_starParticles.Position}");

			_starParticles.EmissionRectExtents = new Vector2(1.0f, viewportHeight / 2.0f);
			GD.Print($"Set EmissionRectExtents to: {_starParticles.EmissionRectExtents}");

			_starParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;

			// No Restart needed

			GD.Print("--- UpdateParticleEmitterBounds: End ---");
		}

		// --- Button Handlers ---
		private void OnStartButtonPressed()
		{
			GD.Print("Start button pressed.");
			ScoreManager.Instance?.ResetScore();
			GameStatsManager.Instance?.GameStarted();
			GetTree().ChangeSceneToFile(GameScenePath);
		}

		private void OnSettingsButtonPressed()
		{
			GD.Print("Settings button pressed.");
			GetTree().ChangeSceneToFile(SettingsScenePath);
		}

		private void OnQuitButtonPressed()
		{
			GD.Print("Quit button pressed. Quitting application...");
			GetTree().Quit();
		}
		// --- End Button Handlers ---

		// --- Window Close Handler ---
		private void OnWindowCloseRequested()
		{
			GD.Print("Window close requested via signal. Quitting application...");
			GetTree().Quit();
		}
		// --- End Window Close Handler ---

		// --- Cleanup ---
		public override void _ExitTree()
		{
			GD.Print("--- NewMainMenu _ExitTree ---");
			Resized -= UpdateParticleEmitterBounds;

			var root = GetTree()?.Root;
			if (root != null && IsInstanceValid(root))
			{
				if (root.IsConnected(Window.SignalName.CloseRequested, Callable.From(OnWindowCloseRequested)))
				{
					GD.Print("Disconnecting CloseRequested signal.");
					root.Disconnect(Window.SignalName.CloseRequested, Callable.From(OnWindowCloseRequested));
				}
			}

			if (IsInstanceValid(startButton)) startButton.Pressed -= OnStartButtonPressed;
			if (IsInstanceValid(settingsButton)) settingsButton.Pressed -= OnSettingsButtonPressed;
			if (IsInstanceValid(quitButton)) quitButton.Pressed -= OnQuitButtonPressed;

			base._ExitTree();
		}
		// --- End Cleanup ---
	}
}
