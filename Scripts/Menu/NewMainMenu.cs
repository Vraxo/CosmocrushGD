using Godot;
using System;

namespace CosmocrushGD;

public sealed partial class NewMainMenu : ColorRect
{
	[Export] private Button _startButton;
	[Export] private Button _settingsButton;
	[Export] private Button _quitButton;
	[Export] private Node _starParticlesNode;

	private CpuParticles2D _starParticles;

	private const string GameScenePath = "res://Scenes/World.tscn";
	private const string SettingsScenePath = "res://Scenes/Menu/SettingsMenu.tscn";
	private const float StarParticleSpawnOffsetX = -10.0f;

	public override void _Ready()
	{
		try
		{
			_ = CosmocrushGD.Settings.Instance;
		}
		catch (Exception exception)
		{
			GD.PrintErr($"Error accessing Settings.Instance: {exception.Message}");
		}

		_startButton ??= GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
		_settingsButton ??= GetNode<Button>("CenterContainer/VBoxContainer/SettingsButton");
		_quitButton ??= GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");

		if (_startButton is null || _settingsButton is null || _quitButton is null)
		{
			GD.PrintErr("NewMainMenu: One or more required buttons could not be found.");
			// Optionally disable buttons or return if critical
		}

		if (_starParticlesNode is CpuParticles2D specificParticles)
		{
			_starParticles = specificParticles;
		}
		else
		{
			_starParticles = GetNode<CpuParticles2D>("StarParticles");
			if (_starParticles is null)
			{
				GD.PrintErr("Failed to get StarParticles node. Effect disabled.");
			}
		}

		if (_startButton is not null)
		{
			_startButton.Pressed += OnStartButtonPressed;
		}

		if (_settingsButton is not null)
		{
			_settingsButton.Pressed += OnSettingsButtonPressed;
		}

		if (_quitButton is not null)
		{
			_quitButton.Pressed += OnQuitButtonPressed;
		}

		Window root = GetTree()?.Root;
		if (root is not null)
		{
			root.CloseRequested += OnWindowCloseRequested;
		}
		else
		{
			GD.PrintErr("GetTree().Root is null, cannot connect CloseRequested signal.");
		}

		Resized += UpdateParticleEmitterBounds;

		if (_starParticles is not null)
		{
			_starParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
			CallDeferred(nameof(UpdateParticleEmitterBounds));
		}
	}

	public override void _ExitTree()
	{
		Resized -= UpdateParticleEmitterBounds;

		Window root = GetTree()?.Root;
		if (root is not null && IsInstanceValid(root))
		{
			if (root.IsConnected(Window.SignalName.CloseRequested, Callable.From(OnWindowCloseRequested)))
			{
				root.Disconnect(Window.SignalName.CloseRequested, Callable.From(OnWindowCloseRequested));
			}
		}

		if (IsInstanceValid(_startButton))
		{
			_startButton.Pressed -= OnStartButtonPressed;
		}

		if (IsInstanceValid(_settingsButton))
		{
			 _settingsButton.Pressed -= OnSettingsButtonPressed;
		}

		if (IsInstanceValid(_quitButton))
		{
			_quitButton.Pressed -= OnQuitButtonPressed;
		}

		base._ExitTree();
	}

	private void UpdateParticleEmitterBounds()
	{
		if (_starParticles is null)
		{
			return;
		}

		if (!IsInsideTree())
		{
			 GD.PrintErr("UpdateParticleEmitterBounds: Node is not inside the tree.");
			return;
		}

		Viewport viewport = GetViewport();
		if (viewport is null)
		{
			GD.PrintErr("UpdateParticleEmitterBounds: GetViewport() returned null.");
			return;
		}

		float viewportHeight = GetViewportRect().Size.Y;

		_starParticles.Position = new Vector2(StarParticleSpawnOffsetX, viewportHeight / 2.0f);
		_starParticles.EmissionRectExtents = new Vector2(1.0f, viewportHeight / 2.0f);
		_starParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
	}

	private void OnStartButtonPressed()
	{
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnSettingsButtonPressed()
	{
		GetTree().ChangeSceneToFile(SettingsScenePath);
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}

	private void OnWindowCloseRequested()
	{
		GetTree().Quit();
	}
}
