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

		private CpuParticles2D starParticlesInstance;

		private const string GameScenePath = "res://Scenes/World.tscn";
		private const string SettingsScenePath = "res://Scenes/Menu/SettingsMenu.tscn";

		public override void _Ready()
		{
			// --- Initialize Settings & Statistics ---
			try
			{
				// Accessing the Instance getter is enough to ensure initialization/loading
				// for both Settings (via its constructor) and StatisticsManager (via EnsureLoaded).
				var settings = Settings.Instance; // Access to ensure loaded
				StatisticsManager.Instance.EnsureLoaded();
				GD.Print("Settings and Statistics Instances accessed/loaded.");
			}
			catch (Exception e)
			{
				GD.PrintErr($"Error accessing Singletons (Settings/Statistics): {e.Message}");
			}
			// --- End Initialization ---

			// (Rest of the _Ready method remains the same...)
			startButton ??= GetNodeOrNull<Button>("CenterContainer/VBoxContainer/StartButton");
			settingsButton ??= GetNodeOrNull<Button>("CenterContainer/VBoxContainer/SettingsButton");
			quitButton ??= GetNodeOrNull<Button>("CenterContainer/VBoxContainer/QuitButton");

			bool buttonsMissing = false;
			if (startButton is null) { GD.PrintErr("NewMainMenu: StartButton is null!"); buttonsMissing = true; }
			if (settingsButton is null) { GD.PrintErr("NewMainMenu: SettingsButton is null!"); buttonsMissing = true; }
			if (quitButton is null) { GD.PrintErr("NewMainMenu: QuitButton is null!"); buttonsMissing = true; }

			if (buttonsMissing)
			{
				GD.PrintErr("One or more critical buttons missing. UI may not function.");
			}
			else
			{
				GD.Print("Buttons checked/retrieved.");
				startButton.Pressed += OnStartButtonPressed;
				settingsButton.Pressed += OnSettingsButtonPressed;
				quitButton.Pressed += OnQuitButtonPressed;
				GD.Print("Button signals connected.");
			}

			if (starParticlesNode is CpuParticles2D specificParticles)
			{
				starParticlesInstance = specificParticles;
				GD.Print("StarParticles obtained from Exported Node.");
			}
			else
			{
				GD.Print($"Exported starParticlesNode is {(starParticlesNode == null ? "null" : "not a CpuParticles2D")}. Trying GetNode...");
				starParticlesInstance = GetNodeOrNull<CpuParticles2D>("StarParticles");

				if (starParticlesInstance is null)
				{
					GD.PrintErr("Failed to get StarParticles node. Effect disabled.");
				}
				else
				{
					GD.Print("StarParticles obtained via GetNode.");
				}
			}

			SceneTree tree = GetTree();
			if (tree?.Root is not null)
			{
				if (!tree.Root.IsConnected(Window.SignalName.CloseRequested, Callable.From(OnWindowCloseRequested)))
				{
					tree.Root.Connect(Window.SignalName.CloseRequested, Callable.From(OnWindowCloseRequested));
					GD.Print("CloseRequested signal connected.");
				}
			}
			else
			{
				GD.PrintErr("GetTree().Root is null, cannot connect CloseRequested signal.");
			}

			if (!IsConnected(Control.SignalName.Resized, Callable.From(UpdateParticleEmitterBounds)))
			{
				Resized += UpdateParticleEmitterBounds;
				GD.Print("Resized signal connected.");
			}


			if (starParticlesInstance is not null)
			{
				starParticlesInstance.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
				CallDeferred(nameof(UpdateParticleEmitterBounds));
				GD.Print("Deferred call to UpdateParticleEmitterBounds scheduled.");
			}
			else
			{
				GD.Print("Skipping deferred call because starParticlesInstance is null.");
			}

			GD.Print("--- NewMainMenu _Ready: End ---");
		}

		// (... UpdateParticleEmitterBounds, Button Handlers, OnWindowCloseRequested remain the same ...)
		private void UpdateParticleEmitterBounds()
		{
			if (starParticlesInstance is null)
			{
				return;
			}

			if (!IsInsideTree())
			{
				GD.PrintErr("UpdateParticleEmitterBounds: Node is not inside the tree. Aborting.");
				return;
			}

			Rect2 viewportRect = GetViewportRect();
			if (viewportRect.Size == Vector2.Zero)
			{
				GD.Print("UpdateParticleEmitterBounds: Viewport size is zero. Skipping update.");
				return;
			}

			float viewportHeight = viewportRect.Size.Y;
			GD.Print($"Viewport height: {viewportHeight}");

			const float spawnOffsetX = -10.0f;
			starParticlesInstance.Position = new Vector2(spawnOffsetX, viewportHeight / 2.0f);
			GD.Print($"Set particle position to: {starParticlesInstance.Position}");

			starParticlesInstance.EmissionRectExtents = new Vector2(1.0f, viewportHeight / 2.0f);
			GD.Print($"Set EmissionRectExtents to: {starParticlesInstance.EmissionRectExtents}");

			starParticlesInstance.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;

			GD.Print("--- UpdateParticleEmitterBounds: End ---");
		}

		private void ChangeSceneSafely(string path)
		{
			SceneTree tree = GetTree();
			Error error = tree?.ChangeSceneToFile(path) ?? Error.CantOpen;
			if (error is not Error.Ok)
			{
				GD.PrintErr($"Failed to change scene to '{path}': {error}");
			}
		}

		private void OnStartButtonPressed()
		{
			GD.Print("Start button pressed.");
			ChangeSceneSafely(GameScenePath);
		}

		private void OnSettingsButtonPressed()
		{
			GD.Print("Settings button pressed.");
			ChangeSceneSafely(SettingsScenePath);
		}

		private void OnQuitButtonPressed()
		{
			GD.Print("Quit button pressed. Quitting application...");
			GetTree()?.Quit();
		}

		private void OnWindowCloseRequested()
		{
			GD.Print("Window close requested via signal. Quitting application...");
			GetTree()?.Quit();
		}

		public override void _ExitTree()
		{
			GD.Print("--- NewMainMenu _ExitTree ---");

			if (IsConnected(Control.SignalName.Resized, Callable.From(UpdateParticleEmitterBounds)))
			{
				Resized -= UpdateParticleEmitterBounds;
			}

			SceneTree tree = GetTree();
			if (tree?.Root is not null && IsInstanceValid(tree.Root))
			{
				if (tree.Root.IsConnected(Window.SignalName.CloseRequested, Callable.From(OnWindowCloseRequested)))
				{
					tree.Root.Disconnect(Window.SignalName.CloseRequested, Callable.From(OnWindowCloseRequested));
					GD.Print("Disconnected CloseRequested signal.");
				}
			}

			if (startButton is not null && IsInstanceValid(startButton) && startButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnStartButtonPressed)))
			{
				startButton.Pressed -= OnStartButtonPressed;
			}
			if (settingsButton is not null && IsInstanceValid(settingsButton) && settingsButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnSettingsButtonPressed)))
			{
				settingsButton.Pressed -= OnSettingsButtonPressed;
			}
			if (quitButton is not null && IsInstanceValid(quitButton) && quitButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnQuitButtonPressed)))
			{
				quitButton.Pressed -= OnQuitButtonPressed;
			}

			base._ExitTree();
		}
	}
}
