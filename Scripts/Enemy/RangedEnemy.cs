using Godot;

namespace CosmocrushGD;

public partial class RangedEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene;

	protected override float ProximityThreshold => 320f;
	protected override float DamageRadius => 320f;
	protected override float AttackCooldown => 1.5f;
	protected override Color ParticleColor => new(0.64f, 0.29f, 0.64f);

	public override void _Process(double delta)
	{
		float fDelta = (float)delta;

		if (Dead)
		{
			if (Knockback.LengthSquared() > 0.01f)
			{
				// Apply knockback decay using exponential damping (framerate independent)
				float decayFactor = 1.0f - Mathf.Exp(-KnockbackRecovery * fDelta);
				Knockback = Knockback.Lerp(Vector2.Zero, decayFactor);
				GlobalPosition += Knockback * fDelta; // Manual position update
			}
			// Dead enemies should not do anything else in _Process besides their visual knockback
			return;
		}

		// Calculate desired movement based on range (only if alive)
		var desiredMovement = Vector2.Zero;
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

		// Combine desired movement and remaining knockback
		currentVelocity = desiredMovement + Knockback;

		// Apply movement
		GlobalPosition += currentVelocity * fDelta;

		UpdateSpriteDirection();
		AttemptAttack();
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
