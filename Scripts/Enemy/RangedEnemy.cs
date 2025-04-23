using Godot;

namespace CosmocrushGD;

public partial class RangedEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene; // This ensures the scene resource is preloaded

	protected override float ProximityThreshold => 320f;
	protected override float DamageRadius => 320f;
	protected override float AttackCooldown => 1.5f;

	public override void _PhysicsProcess(double delta)
	{
		if (Dead)
		{
			if (Knockback.LengthSquared() > 0.1f)
			{
				Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery * 2.0f * (float)delta);
				Velocity = Knockback;
				MoveAndSlide();
			}
			else if (Velocity != Vector2.Zero) Velocity = Vector2.Zero;
			return;
		}

		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);
		Vector2 desiredMovement = Vector2.Zero;
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Vector2 directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
			// Keep away logic
			if (distanceToPlayer > DamageRadius + 20f) // Move closer if too far
				desiredMovement = directionToPlayer * Speed;
			else if (distanceToPlayer < ProximityThreshold - 20f) // Move away if too close
				desiredMovement = -directionToPlayer * Speed;
		}
		Velocity = desiredMovement + Knockback;
		if (Velocity.LengthSquared() > 0.01f) MoveAndSlide();
		else if (Velocity != Vector2.Zero) Velocity = Vector2.Zero; // Stop if velocity is negligible
	}


	protected override void AttemptAttack()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer) || !CanShoot || projectileScene is null) return;
		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
		// Shoot if within range (using DamageRadius for attack range)
		if (distance <= DamageRadius)
		{
			ShootProjectile();
			CanShoot = false;
			DamageCooldownTimer?.Start();
		}
	}

	private void ShootProjectile()
	{
		// Removed check for GlobalAudioPlayer.Instance
		if (projectileScene is null || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			GD.PrintErr("RangedEnemy: Preconditions not met for shooting (Scene or Player missing/invalid).");
			return;
		}

		// Get a reference to add the projectile
		Node worldNode = GetNode<Node>("/root/World");
		if (worldNode is null)
		{
			GD.PrintErr("RangedEnemy: Could not find '/root/World' node to add projectile.");
			return;
		}

		// --- Instantiate directly ---
		Projectile projectile = projectileScene.Instantiate<Projectile>();
		if (projectile is null)
		{
			GD.PrintErr("RangedEnemy: Failed to instantiate projectile.");
			return;
		}

		// --- Add to scene tree ---
		worldNode.AddChild(projectile);

		Vector2 direction = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();

		// Setup and Activate
		projectile.Setup(GlobalPosition, direction); // Texture/color defaults are fine here
		projectile.Activate();
	}
}
// </file>
