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

	private const string GameScenePath = "res://Scenes/World.tscn";
	// Reverted to 2.0 multiplier as a balanced default, can be increased if needed
	private const float ParticleVerticalPaddingMultiplier = 2.0f;
	// BaselineHeight, BaselineAmount, MinimumParticleAmount are no longer needed here

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

	public override void _ExitTree()
	{
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

		// No height threshold check needed if we aren't changing amount
		// No amount calculation needed

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
		GetTree().ChangeSceneToFile(GameScenePath);
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
}
