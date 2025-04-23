using Godot;
using System;
using System.Collections.Generic; // Use Generic List for easier handling before CallDeferred
// No Task needed here anymore

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

	protected override void Die() // No longer async
	{
		base.Die();

		if (projectileScene is null || projectileCount <= 0 || GlobalAudioPlayer.Instance is null)
		{
			GD.PrintErr("ExplodingEnemy: Cannot spawn projectiles on death - missing scene, count <= 0, or GlobalAudioPlayer.");
			return;
		}

		float angleStep = (float)(2 * Math.PI / projectileCount);
		Vector2 spawnPosition = GlobalPosition;
		Texture2D enemyTexture = Sprite?.Texture;

		// Use a standard List for easy adding during the loop
		var projectilesToActivate = new List<Projectile>(projectileCount);

		for (int i = 0; i < projectileCount; i++)
		{
			Projectile projectile = GlobalAudioPlayer.Instance.GetProjectile(projectileScene);
			if (projectile is null)
			{
				GD.PrintErr($"ExplodingEnemy: Failed to get projectile {i + 1}/{projectileCount} from pool.");
				continue;
			}

			float angle = i * angleStep;
			Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)).Normalized();

			// Call Setup, which now leaves the projectile inactive
			projectile.Setup(spawnPosition, direction, enemyTexture, ExplosionProjectileColor);

			projectilesToActivate.Add(projectile);

			// No await here anymore
		}

		// Convert the List to a Godot Array for CallDeferred
		var godotArray = new Godot.Collections.Array<Projectile>(projectilesToActivate);

		// Defer the activation of all collected projectiles to the next frame
		CallDeferred(nameof(ActivateProjectiles), godotArray);
	}

	private void ActivateProjectiles(Godot.Collections.Array<Projectile> projectilesToActivate)
	{
		if (projectilesToActivate is null) return;

		foreach (Projectile projectile in projectilesToActivate)
		{
			if (projectile is not null && IsInstanceValid(projectile))
			{
				projectile.Activate();
			}
		}
	}
}
