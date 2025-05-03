using Godot;

namespace CosmocrushGD;

public partial class RangedEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene;

	protected override float ProximityThreshold => 320f;
	protected override float DamageRadius => 320f;
	protected override float AttackCooldown => 1.5f;
	protected override Color ParticleColor => new(0.64f, 0.29f, 0.64f);

	// Overriding _PhysicsProcess specifically for ranged movement logic
	// Knockback handling is now entirely done in BaseEnemy._PhysicsProcess
	public override void _PhysicsProcess(double delta)
	{
		// Call base physics process FIRST to handle knockback decay and dead state
		base._PhysicsProcess(delta);

		// If dead, no further logic needed (already handled by base)
		if (Dead)
		{
			return;
		}

		// Calculate desired movement based on range (only if alive)
		Vector2 desiredMovement = Vector2.Zero;
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Vector2 directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			float distanceToPlayerSq = GlobalPosition.DistanceSquaredTo(TargetPlayer.GlobalPosition);

			// If too far, move closer
			if (distanceToPlayerSq > DamageRadius * DamageRadius)
			{
				desiredMovement = directionToPlayer * Speed;
			}
			// If too close, move away
			else if (distanceToPlayerSq < ProximityThreshold * ProximityThreshold)
			{
				desiredMovement = -directionToPlayer * Speed;
			}
			// Otherwise, stay put (desiredMovement remains Zero)
		}

		// Combine desired movement with the current (decayed) knockback from base class
		Velocity = desiredMovement + Knockback;

		// Apply movement if velocity is significant
		if (Velocity.LengthSquared() > 0.01f)
		{
			MoveAndSlide();
		}
		else if (Velocity != Vector2.Zero) // Ensure velocity stops fully if very small
		{
			Velocity = Vector2.Zero;
		}
	}


	protected override void PerformAttackAction()
	{
		ShootProjectile();
	}

	private void ShootProjectile()
	{
		if (ProjectilePoolManager.Instance is null || projectileScene is null || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			GD.PrintErr($"RangedEnemy ({Name}): Preconditions not met for shooting. Pool: {ProjectilePoolManager.Instance}, Scene: {projectileScene}, Target: {TargetPlayer?.Name ?? "null"}");
			return;
		}

		Projectile projectile = ProjectilePoolManager.Instance.GetProjectile(projectileScene);
		if (projectile is null)
		{
			GD.PrintErr($"RangedEnemy ({Name}): Failed to get projectile from pool.");
			return;
		}

		var direction = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();

		if (projectile.GetParent() != ProjectilePoolManager.Instance)
		{
			projectile.GetParent()?.RemoveChild(projectile);
			ProjectilePoolManager.Instance.AddChild(projectile);
		}

		projectile.SetupAndActivate(GlobalPosition, direction);
		GD.Print($"RangedEnemy ({Name}): Fired projectile.");
	}
}
