// MODIFIED: Enemy/ExplodingEnemy.cs
// Summary: Removed projectile reparenting in Die method. Setup now uses GlobalPosition.
// - Removed the code block that reparented the projectile to the root node inside the loop.
// - Projectile stays child of GlobalAudioPlayer but uses TopLevel=true.
// - Called projectile.Setup with the enemy's GlobalPosition.
using Godot;
using System;

namespace CosmocrushGD;

public partial class ExplodingEnemy : BaseEnemy
{
	[Export] private PackedScene projectileScene;
	[Export] private int projectileCount = 8;
	[Export] private float meleeKnockbackForce = 500f;

	protected override int MaxHealth => 15;
	protected override float Speed => 80f;
	protected override int Damage => 1;
	protected override float AttackCooldown => 0.6f;

	private static readonly Color ExplosionProjectileColor = new(1.0f, 0.5f, 0.15f);

	protected override void AttemptAttack()
	{
		if (!CanShoot || TargetPlayer is null || !IsInstanceValid(TargetPlayer)) return;
		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
		if (distance > DamageRadius) return;
		TargetPlayer.TakeDamage(Damage);
		Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		TargetPlayer.ApplyKnockback(knockbackDir * meleeKnockbackForce);
		CanShoot = false;
		DamageCooldownTimer?.Start();
	}

	protected override void Die()
	{
		base.Die();

		if (projectileScene is null || projectileCount <= 0 || GlobalAudioPlayer.Instance is null)
		{
			GD.PrintErr("ExplodingEnemy: Cannot spawn projectiles on death.");
			return;
		}

		float angleStep = (float)(2 * Math.PI / projectileCount);
		Vector2 spawnPosition = GlobalPosition; // Cache enemy position
		Texture2D enemyTexture = Sprite?.Texture; // Cache texture

		for (int i = 0; i < projectileCount; i++)
		{
			Projectile projectile = GlobalAudioPlayer.Instance.GetProjectile(projectileScene);
			if (projectile is null)
			{
				GD.PrintErr($"ExplodingEnemy: Failed to get projectile {i + 1}/{projectileCount} from pool.");
				continue;
			}

			// No reparenting needed due to TopLevel = true

			float angle = i * angleStep;
			Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)).Normalized();

			// Setup the projectile using GlobalPosition
			projectile.Setup(spawnPosition, direction, enemyTexture, ExplosionProjectileColor);
		}
	}
}
