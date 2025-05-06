using Godot;
using System;
using System.Collections.Generic;

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
	protected override Color ParticleColor => new(1.0f, 0.5f, 0.15f);
	protected override float MeleeKnockbackForce => meleeKnockbackForce;

	private static readonly Color BaseExplosionProjectileColor = new(255f / 255f, 127f / 255f, 39f / 255f);
	private const float ProjectileGlowIntensity = 1.8f;
	private static readonly Color ExplosionProjectileColor = new(
		BaseExplosionProjectileColor.R * ProjectileGlowIntensity,
		BaseExplosionProjectileColor.G * ProjectileGlowIntensity,
		BaseExplosionProjectileColor.B * ProjectileGlowIntensity
	);
	private const float ProjectileSpawnDelay = 0.01f;

	protected override void PerformAttackAction()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			return;
		}

		TargetPlayer.TakeDamage(Damage);
		var knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		TargetPlayer.ApplyKnockback(knockbackDir * MeleeKnockbackForce);
	}

	protected override async void Die()
	{
		if (Dead)
		{
			return;
		}

		base.Die();


		if (projectileScene is null || projectileCount <= 0)
		{
			GD.PrintErr($"ExplodingEnemy ({Name}): Cannot spawn projectiles on death - Missing Scene or Count <= 0.");
			return;
		}

		GD.Print($"ExplodingEnemy ({Name}): Starting staggered projectile spawn ({projectileCount} projectiles).");
		var angleStep = Mathf.Tau / projectileCount;
		var spawnPosition = GlobalPosition;
		var enemyTexture = Sprite?.Texture;

		for (int i = 0; i < projectileCount; i++)
		{
			if (!IsInstanceValid(this))
			{
				GD.Print($"ExplodingEnemy ({Name}): Instance became invalid during projectile spawn loop. Aborting.");
				return;
			}

			var projectile = projectileScene.Instantiate<Projectile>();
			if (projectile is null)
			{
				GD.PrintErr($"ExplodingEnemy ({Name}): Failed to instantiate projectile {i + 1}/{projectileCount}. Skipping.");
				continue;
			}

			GetTree().Root.AddChild(projectile);

			var angle = i * angleStep;
			var direction = Vector2.Right.Rotated(angle);

			projectile.SetupAndActivate(spawnPosition, direction, enemyTexture, ExplosionProjectileColor);
			GD.Print($"ExplodingEnemy ({Name}): Spawned and activated projectile {i + 1}/{projectileCount}.");

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		GD.Print($"ExplodingEnemy ({Name}): Finished projectile spawn sequence.");
	}
}
