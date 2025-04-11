using Godot;
using System; // Required for Math

namespace CosmocrushGD;

public partial class ExplodingEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene; // Need this to spawn projectiles
	[Export] private int projectileCount = 8; // Number of projectiles to spawn on death
	[Export] private float meleeKnockbackForce = 500f; // Same as MeleeEnemy

	// Inherit most properties like Speed, Damage, Health from BaseEnemy
	// We can override them here if needed, e.g., make it slower or tougher

	protected override void AttemptAttack()
	{
		// Standard melee attack logic
		if (!CanShoot || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			return;
		}

		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

		if (distance > DamageRadius) // Use DamageRadius from BaseEnemy or override
		{
			return;
		}

		TargetPlayer.TakeDamage(Damage); // Use Damage from BaseEnemy or override

		Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		TargetPlayer.ApplyKnockback(knockbackDir * meleeKnockbackForce);

		CanShoot = false;
		DamageCooldownTimer.Start(); // Use AttackCooldown from BaseEnemy or override
	}

	protected override void Die()
	{
		// Call the base Die() method first to handle standard death logic
		// (disable collider, hide sprite, start death timer, etc.)
		base.Die();

		// Now, spawn projectiles in a circle
		if (projectileScene is null || projectileCount <= 0)
		{
			return;
		}

		float angleStep = (float)(2 * Math.PI / projectileCount); // Angle between projectiles

		for (int i = 0; i < projectileCount; i++)
		{
			var projectile = projectileScene.Instantiate<Projectile>();

			if (projectile is null)
			{
				continue;
			}

			projectile.Sprite.Texture = Sprite.Texture;
			projectile.DestructionParticles.Color = new(255, 127, 39);

			float angle = i * angleStep;
			Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)).Normalized();

			projectile.GlobalPosition = GlobalPosition; // Spawn at enemy's death position
			projectile.Direction = direction;

			// Add projectile to the main scene tree
			GetTree().Root.AddChild(projectile);
		}
		// The base.Die() already starts the DeathTimer which calls ReturnToPool
	}
}
