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
	private const float ParticleVerticalPaddingMultiplier = 2.0f;

	public override void _Ready()
	{
		if (menuContainer is null)
		{
			return;
		}
		if (starParticles is null)
		{
		}
		if (mainMenuScene is null)
		{
			return;
		}
		if (settingsMenuScene is null)
		{
			return;
		}
		if (statisticsMenuScene is null)
		{
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
		SceneTransitionManager.Instance?.ChangeScene(GameScenePath);
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
