using Godot;

public partial class HealthBar : ProgressBar
{
	private Player player;

	public override void _Ready()
	{
		player = GetNode<Player>("/root/World/Player"); // Adjust the path to your Player node
		MaxValue = player.MaxHealth;
		Value = player.Health;
	}

	public override void _Process(double delta)
	{
		Value = player.Health;
	}
}
