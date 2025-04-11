using Godot;
using System;

namespace CosmocrushGD;

public partial class ExplodingEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene;
	[Export] private int projectileCount = 8;
	[Export] private float meleeKnockbackForce = 500f;

	// Inherit properties like MovementSpeed, MaxHealth from BaseEnemy
	// Override specific values if needed, e.g.:
	// protected override int MaxHealth => 15; // Make it weaker
	// protected override float MovementSpeed => 80f; // Make it slower

	protected override void AttemptAttack()
	{
		// Use corrected property names from BaseEnemy
		if (!CanAttack || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			return;
		}

		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

		// Use corrected property name AttackRadius
		if (distance > AttackRadius)
		{
			return;
		}

		// Use corrected property name BaseDamage
		TargetPlayer.TakeDamage(BaseDamage);

		Vector2 knockbackDirection = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		TargetPlayer.ApplyKnockback(knockbackDirection * meleeKnockbackForce);

		// Use corrected property name CanAttack
		CanAttack = false;
		DamageCooldownTimer?.Start(); // Use AttackInterval from BaseEnemy or override
	}

	protected override void Die()
	{
		// Call base Die first (handles disabling, particles, score, etc.)
		base.Die();

		// Spawn projectiles specific to this enemy type
		if (projectileScene is null || projectileCount <= 0)
		{
			return;
		}

		float angleStep = Mathf.Tau / projectileCount; // Use Tau for 2 * PI

		for (int i = 0; i < projectileCount; i++)
		{
			Projectile projectile = projectileScene.Instantiate<Projectile>();

			if (projectile is null)
			{
				GD.PrintErr("ExplodingEnemy: Failed to instantiate projectile.");
				continue; // Skip this projectile
			}

			// Optional: Customize projectile appearance/properties
			if (projectile.Sprite is not null && Sprite?.Texture is not null)
			{
				projectile.Sprite.Texture = Sprite.Texture; // Use enemy's texture?
			}
			if (projectile.DestructionParticles is not null)
			{
				// Orange color for explosion fragments
				projectile.DestructionParticles.Color = new Color(1.0f, 0.5f, 0.15f);
			}

			float angle = i * angleStep;
			// Use Godot's Vector2.FromAngle for clarity
			Vector2 direction = Vector2.FromAngle(angle);

			projectile.GlobalPosition = GlobalPosition; // Spawn at enemy's death position
			projectile.Direction = direction;

			// Add projectile to the main scene tree (or a dedicated container if preferred)
			GetTree().Root.AddChild(projectile);
		}
		// base.Die() already starts the DeathTimer which calls ReturnToPool
	}
}
