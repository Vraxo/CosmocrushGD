using Godot;

namespace CosmocrushGD;

public partial class RangedEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene;

	// Corrected property names to match BaseEnemy
	protected override float PlayerProximityThreshold => 320f;
	protected override float AttackRadius => 320f;
	protected override float AttackInterval => 1.5f;

	protected override void AttemptAttack()
	{
		if (TargetPlayer is null || !CanAttack || projectileScene is null)
		{
			return;
		}

		// Use the correct base class property name here too
		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

		if (distance <= AttackRadius) // Use AttackRadius
		{
			ShootProjectile();
			CanAttack = false;
			DamageCooldownTimer?.Start(); // Use the timer inherited from BaseEnemy
		}
	}

	private void ShootProjectile()
	{
		// Ensure scene is valid before instantiating
		if (projectileScene is null)
		{
			GD.PrintErr("RangedEnemy: Projectile scene is not set!");
			return;
		}

		Projectile projectile = projectileScene.Instantiate<Projectile>();

		if (projectile is null)
		{
			GD.PrintErr("RangedEnemy: Failed to instantiate projectile scene.");
			return;
		}

		projectile.GlobalPosition = GlobalPosition;
		Vector2 direction = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		projectile.Direction = direction;

		// Add to the main scene tree or appropriate container
		GetTree().Root.AddChild(projectile);
	}
}
