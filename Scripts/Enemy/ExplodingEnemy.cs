using Godot;
using System;
using System.Collections.Generic; // Keep using List temporarily if needed

namespace CosmocrushGD;

public partial class ExplodingEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene; // This ensures the scene resource is preloaded
	[Export] private int projectileCount = 8;
	[Export] private float meleeKnockbackForce = 500f;

	protected override int MaxHealth => 15;
	protected override float Speed => 80f;
	protected override int Damage => 1;
	protected override float AttackCooldown => 0.6f;

	private static readonly Color ExplosionProjectileColor = new(1.0f, 0.5f, 0.15f);

	protected override void AttemptAttack()
	{
		if (!CanShoot || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			return;
		}

		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

		if (distance > DamageRadius)
		{
			return;
		}

		TargetPlayer.TakeDamage(Damage);
		Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		TargetPlayer.ApplyKnockback(knockbackDir * meleeKnockbackForce);
		CanShoot = false;
		DamageCooldownTimer?.Start();
	}

	protected override void Die() // No longer needs async
	{
		base.Die(); // Call base Die first to handle common death logic

		if (projectileScene is null || projectileCount <= 0)
		{
			GD.PrintErr("ExplodingEnemy: Cannot spawn projectiles on death - missing scene or count <= 0.");
			return;
		}

		// Get a reference to a node suitable for adding children (e.g., the main World node)
		// Adjust the path if your scene structure is different.
		Node worldNode = GetNode<Node>("/root/World");
		if (worldNode is null)
		{
			GD.PrintErr("ExplodingEnemy: Could not find '/root/World' node to add projectiles. Aborting explosion.");
			return;
		}


		float angleStep = (float)(2 * Math.PI / projectileCount);
		Vector2 spawnPosition = GlobalPosition;
		Texture2D enemyTexture = Sprite?.Texture;


		for (int i = 0; i < projectileCount; i++)
		{
			// --- Instantiate directly instead of getting from pool ---
			Projectile projectile = projectileScene.Instantiate<Projectile>();
			if (projectile is null)
			{
				GD.PrintErr($"ExplodingEnemy: Failed to instantiate projectile {i + 1}/{projectileCount}.");
				continue;
			}

			// --- Add the projectile to the scene tree ---
			// Important: Add before setup/activate if it relies on being in the tree
			worldNode.AddChild(projectile);

			float angle = i * angleStep;
			Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)).Normalized();

			// Call Setup (leaves it inactive, positioned, potentially textured)
			projectile.Setup(spawnPosition, direction, enemyTexture, ExplosionProjectileColor);

			// --- Activate the projectile ---
			projectile.Activate(); // Activate immediately after setup
		}
		// No deferred activation or pooling logic needed here anymore.
	}

	// Remove the ActivateProjectiles method as it's no longer used.
	// private void ActivateProjectiles(Godot.Collections.Array<Projectile> projectilesToActivate)
	// { ... }
}
// </file>
