using Godot;

namespace CosmocrushGD;

public partial class HealthBar : ProgressBar
{
	private Player player;
	private double currentPlayerHealth = -1;

	public override void _Ready()
	{
		player = GetNode<Player>("/root/World/Player");
		if (player is null)
		{
			SetProcess(false);
			return;
		}

		MaxValue = player.MaxHealth;
		Value = player.Health;
		currentPlayerHealth = player.Health;
	}

	public override void _Process(double delta)
	{
		if (player is not null && player.Health != currentPlayerHealth)
		{
			currentPlayerHealth = player.Health;
			Value = currentPlayerHealth;
		}
	}
}
