using Godot;

namespace CosmocrushGD;

public partial class RangedEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene; // Projectile scene to shoot

	// --- Overridden Stats ---
	protected override float ProximityThreshold => 320f; // Tries to stay *at least* this far away
	protected override float DamageRadius => 320f; // Attack range (matches ProximityThreshold for typical ranged)
	protected override float AttackCooldown => 1.5f;
	protected override Color ParticleColor => new(0.64f, 0.29f, 0.64f); // Purple color

	// Override physics process for ranged behavior (maintain distance)
	public override void _PhysicsProcess(double delta)
	{
		if (Dead)
		{
			// Handle knockback decay while dead
			if (Knockback.LengthSquared() > 0.1f)
			{
				Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery * 2.0f * (float)delta);
				Velocity = Knockback;
				MoveAndSlide();
			}
			else if (Velocity != Vector2.Zero)
			{
				Velocity = Vector2.Zero;
			}
			return; // No movement logic when dead
		}

		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery); // Apply knockback decay
		Vector2 desiredMovement = Vector2.Zero;

		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Vector2 directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

			// If too far, move closer
			if (distanceToPlayer > DamageRadius) // Use DamageRadius as preferred max distance
			{
				desiredMovement = directionToPlayer * Speed;
			}
			// If too close, move away
			else if (distanceToPlayer < ProximityThreshold) // Use ProximityThreshold as preferred min distance
			{
				desiredMovement = -directionToPlayer * Speed; // Move directly away
			}
			// Otherwise, stay put (desiredMovement remains Zero)
		}

		Velocity = desiredMovement + Knockback; // Combine desired movement and knockback

		if (Velocity.LengthSquared() > 0.01f)
		{
			MoveAndSlide();
		}
		else if (Velocity != Vector2.Zero)
		{
			Velocity = Vector2.Zero; // Ensure velocity stops fully
		}
	}

	// Override attack action to shoot projectile
	protected override void PerformAttackAction()
	{
		ShootProjectile();
	}

	private void ShootProjectile()
	{
		// Check prerequisites: Pool Manager, projectile scene, and valid target
		if (ProjectilePoolManager.Instance is null || projectileScene is null || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			GD.PrintErr($"RangedEnemy ({Name}): Preconditions not met for shooting. Pool: {ProjectilePoolManager.Instance}, Scene: {projectileScene}, Target: {TargetPlayer}");
			return;
		}

		// Get projectile from the pool using the new manager
		Projectile projectile = ProjectilePoolManager.Instance.GetProjectile(projectileScene);
		if (projectile is null)
		{
			GD.PrintErr($"RangedEnemy ({Name}): Failed to get projectile from pool.");
			return;
		}

		// Calculate direction towards the player
		Vector2 direction = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();

		// Setup and activate the projectile
		projectile.Setup(GlobalPosition, direction); // Set start position and direction
		projectile.Activate(); // Make the projectile live
	}
}
