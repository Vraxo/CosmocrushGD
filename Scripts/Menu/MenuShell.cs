using Godot;

namespace CosmocrushGD;

public partial class MenuShell : Control
{
	[Export] private Node menuContainer;
	[Export] private CpuParticles2D starParticles;
	[Export] private PackedScene mainMenuScene;
	[Export] private PackedScene settingsMenuScene;
	[Export] private PackedScene statisticsMenuScene;

	private Node currentMenuInstance;
	// No need for member variables for Autoloads if accessed via Instance

	private const string GameScenePath = "res://Scenes/World.tscn";
	private const float ParticleVerticalPaddingMultiplier = 2.0f;

	public override void _Ready()
	{
		if (menuContainer is null)
		{
			GD.PrintErr("MenuShell: Menu Container node not assigned!");
			return;
		}
		if (starParticles is null)
		{
			GD.PrintErr("MenuShell: Star Particles node not assigned!");
		}
		if (mainMenuScene is null)
		{
			GD.PrintErr("MenuShell: Main Menu Scene not assigned!");
			return;
		}
		if (settingsMenuScene is null)
		{
			GD.PrintErr("MenuShell: Settings Menu Scene not assigned!");
			return;
		}
		if (statisticsMenuScene is null)
		{
			GD.PrintErr("MenuShell: Statistics Menu Scene not assigned!");
			return;
		}

		// Access instance and connect signals directly
		if (TransitionScreen.Instance is not null)
		{
			GD.Print("MenuShell: Found TransitionScreen Instance. Connecting signal.");
			TransitionScreen.Instance.TransitionMidpointReached += OnTransitionMidpointReached;
			TransitionScreen.Instance.StartFadeIn(); // Start initial fade-in
		}
		else
		{
			GD.PrintErr("MenuShell: Could not find TransitionScreen Instance in _Ready!");
		}

		ProcessMode = ProcessModeEnum.Always;

		var root = GetTree()?.Root;
		if (root is not null)
		{
			root.CloseRequested += OnWindowCloseRequested;
		}
		else
		{
			GD.PrintErr("MenuShell: GetTree().Root is null, cannot connect CloseRequested signal.");
		}

		Resized += UpdateParticleEmitterBounds;
		if (starParticles is not null)
		{
			starParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
			CallDeferred(nameof(UpdateParticleEmitterBounds));
		}

		ShowMainMenu();
	}

	// Removed InitializeAutoloads method

	public override void _ExitTree()
	{
		// Check instance validity before unsubscribing
		if (TransitionScreen.Instance is not null && IsInstanceValid(TransitionScreen.Instance))
		{
			if (TransitionScreen.Instance.IsConnected(TransitionScreen.SignalName.TransitionMidpointReached, Callable.From<string>(OnTransitionMidpointReached)))
			{
				TransitionScreen.Instance.TransitionMidpointReached -= OnTransitionMidpointReached;
				GD.Print("MenuShell: Unsubscribed from TransitionMidpointReached.");
			}
		}

		if (IsInstanceValid(this))
		{
			Resized -= UpdateParticleEmitterBounds;
		}

		var root = GetTree()?.Root;
		if (root is not null && IsInstanceValid(root))
		{
			var callable = Callable.From(OnWindowCloseRequested);
			if (root.IsConnected(Window.SignalName.CloseRequested, callable))
			{
				root.Disconnect(Window.SignalName.CloseRequested, callable);
			}
		}

		base._ExitTree();
	}

	private void UpdateParticleEmitterBounds()
	{
		if (starParticles is null || !IsInsideTree())
		{
			return;
		}

		var viewport = GetViewport();
		if (viewport is null)
		{
			return;
		}

		var viewportSize = viewport.GetVisibleRect().Size;
		var viewportHeight = viewportSize.Y;

		const float spawnOffsetX = -10.0f;
		float totalEmissionHeight = viewportHeight * ParticleVerticalPaddingMultiplier;
		float emissionExtentsY = totalEmissionHeight / 2.0f;

		starParticles.Position = new Vector2(spawnOffsetX, viewportHeight / 2.0f);
		starParticles.EmissionRectExtents = new Vector2(1.0f, emissionExtentsY);
		starParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
	}


	private void ClearMenuContainer()
	{
		if (currentMenuInstance is not null && IsInstanceValid(currentMenuInstance))
		{
			currentMenuInstance.QueueFree();
			currentMenuInstance = null;
		}

		foreach (Node child in menuContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void ShowMenu(PackedScene menuScene)
	{
		if (menuScene is null || menuContainer is null)
		{
			GD.PrintErr("MenuShell: Cannot show menu, scene or container is null.");
			return;
		}

		ClearMenuContainer();
		currentMenuInstance = menuScene.Instantiate();
		menuContainer.AddChild(currentMenuInstance);
	}

	public void ShowMainMenu()
	{
		ShowMenu(mainMenuScene);
	}

	public void ShowSettingsMenu()
	{
		ShowMenu(settingsMenuScene);
	}

	public void ShowStatisticsMenu()
	{
		ShowMenu(statisticsMenuScene);
	}

	public void StartGame()
	{
		if (TransitionScreen.Instance is not null)
		{
			GD.Print($"MenuShell: StartGame called. Requesting transition. Current time: {Time.GetTicksMsec()}"); // Added timestamp
			TransitionScreen.Instance.TransitionToScene(GameScenePath);
		}
		else
		{
			GD.PrintErr("MenuShell: Cannot StartGame, TransitionScreen Instance is null. Changing scene directly.");
			GetTree().ChangeSceneToFile(GameScenePath); // Fallback
		}
	}

	public void QuitGame()
	{
		StatisticsManager.Instance.Save();
		GetTree().Quit();
	}

	private void OnWindowCloseRequested()
	{
		QuitGame();
	}

	private void OnTransitionMidpointReached(string scenePathToLoad)
	{
		GD.Print($"MenuShell: OnTransitionMidpointReached received, loading: {scenePathToLoad}. Current time: {Time.GetTicksMsec()}"); // Added timestamp
		GetTree().ChangeSceneToFile(scenePathToLoad);
	}
}
