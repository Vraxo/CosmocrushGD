using Godot;

namespace CosmocrushGD;

public partial class Menu : Node2D
{
	[Export] private CpuParticles2D particleGenerator;

	private const string SettingsFilePath = "user://Settings.json";

	public override void _Ready()
	{
		Settings.Instance.Load();
	}

	public override void _Process(double delta)
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();

		particleGenerator.Position = new(0, windowSize.Y / 2);
		particleGenerator.EmissionRectExtents = new(0, windowSize.Y);
	}
}
