using Godot;
using System;
using System.Collections.Generic;

namespace CosmocrushGD;

public partial class ExplodingEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene; // Scene for explosion projectiles
	[Export] private int projectileCount = 8; // Number of projectiles to spawn on death
	[Export] private float meleeKnockbackForce = 500f; // Knockback applied by melee attack

	// --- Overridden Stats ---
	protected override int MaxHealth => 15;
	protected override float Speed => 80f;
	protected override int Damage => 1; // Melee damage
	protected override float AttackCooldown => 0.6f;
	protected override Color ParticleColor => new(1.0f, 0.5f, 0.15f); // Orange color
	protected override float MeleeKnockbackForce => meleeKnockbackForce;

	private static readonly Color ExplosionProjectileColor = new(1.0f, 0.5f, 0.15f); // Orange color for projectiles

	// Override melee attack action (same as base for this enemy)
	protected override void PerformAttackAction()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			return;
		}

		TargetPlayer.TakeDamage(Damage);
		Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		TargetPlayer.ApplyKnockback(knockbackDir * MeleeKnockbackForce);
	}

	// Override Die to add explosion behavior
	protected override void Die()
	{
		base.Die(); // Call base Die logic (particles, sound, disable, etc.)

		// Check if prerequisites for explosion are met
		if (projectileScene is null || projectileCount <= 0 || ProjectilePoolManager.Instance is null)
		{
			GD.PrintErr($"ExplodingEnemy ({Name}): Cannot spawn projectiles on death - Missing Scene, Count <= 0, or ProjectilePoolManager instance.");
			return;
		}

		float angleStep = Mathf.Tau / projectileCount; // Calculate angle between projectiles (Tau = 2 * PI)
		Vector2 spawnPosition = GlobalPosition; // Spawn projectiles at enemy's final position
		Texture2D enemyTexture = Sprite?.Texture; // Use enemy texture for projectiles if available

		var projectilesToActivate = new List<Projectile>(projectileCount);

		// Get projectiles from the pool and set them up
		for (int i = 0; i < projectileCount; i++)
		{
			// Use the new ProjectilePoolManager
			Projectile projectile = ProjectilePoolManager.Instance.GetProjectile(projectileScene);
			if (projectile is null)
			{
				GD.PrintErr($"ExplodingEnemy ({Name}): Failed to get projectile {i + 1}/{projectileCount} from pool.");
				continue; // Skip if projectile couldn't be retrieved
			}

			float angle = i * angleStep; // Calculate direction for this projectile
			var direction = Vector2.Right.Rotated(angle); // Start with Vector2.Right and rotate

			// Setup projectile properties (position, direction, optional texture/color)
			projectile.Setup(spawnPosition, direction, enemyTexture, ExplosionProjectileColor);
			projectilesToActivate.Add(projectile); // Add to list for deferred activation
		}

		// Use CallDeferred to activate projectiles on the next idle frame
		// This ensures they are fully added to the scene tree before activation logic runs
		var godotArray = new Godot.Collections.Array<Projectile>(projectilesToActivate);
		CallDeferred(nameof(ActivateProjectiles), godotArray);
	}

	// Method called deferred to activate the spawned projectiles
	private void ActivateProjectiles(Godot.Collections.Array<Projectile> projectilesToActivate)
	{
		if (projectilesToActivate is null)
		{
			return;
		}

		foreach (Projectile projectile in projectilesToActivate)
		{
			// Check if projectile instance is still valid before activating
			if (projectile is not null && IsInstanceValid(projectile))
			{
				projectile.Activate(); // Start the projectile's movement and collision
			}
		}
	}
}
