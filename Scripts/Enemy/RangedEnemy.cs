using Godot;

namespace CosmocrushGD;

public partial class RangedEnemy : BaseEnemy
{
	[Export] private PackedScene _projectileScene;

	protected override float ProximityThreshold => 320f;
	protected override float DamageRadius => 320f;
	protected override float AttackCooldown => 1.5f;

	protected override void AttemptAttack()
	{
		if (TargetPlayer == null || !CanShoot) return;

		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
		if (distance <= DamageRadius)
		{
			ShootProjectile();
			CanShoot = false;
			DamageCooldownTimer.Start();
		}
	}

	private void ShootProjectile()
	{
		var projectile = _projectileScene.Instantiate<Projectile>();

		// Set projectile properties
		projectile.GlobalPosition = GlobalPosition;
		Vector2 direction = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		projectile.Direction = direction;

		// Add to scene tree
		GetTree().Root.AddChild(projectile);
	}
}
