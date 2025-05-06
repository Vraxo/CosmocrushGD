using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CosmocrushGD;

public partial class ExplodingEnemy : BaseEnemy
{
	private static readonly Color ExplosionProjectileColor = new(1.0f, 0.5f, 0.15f);

	[Export] private PackedScene projectileScene;
	[Export] private int projectileCount = 8;
	[Export] private float meleeKnockbackForce = 500f;

	protected override int MaxHealth => 15;
	protected override float Speed => 80f;
	protected override int Damage => 1;
	protected override float AttackCooldown => 0.6f;
	protected override Color ParticleColor => new(1.0f, 0.5f, 0.15f);
	protected override float MeleeKnockbackForce => meleeKnockbackForce;

	public override void ResetForPooling()
	{
		base.ResetForPooling();
	}

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

	protected override void Die() // No longer async
	{
		if (Dead)
		{
			return;
		}

		Dead = true;
		EmitSignal(SignalName.EnemyDied, this);
		SetProcess(false);
		SetPhysicsProcess(false);
		Collider?.CallDeferred(CollisionShape2D.MethodName.SetDisabled, true);
		Sprite?.SetDeferred(Sprite2D.PropertyName.Visible, false);

		if (deathParticleEffectScene is not null)
		{
			ParticlePoolManager.Instance?.GetParticleEffect(deathParticleEffectScene, GlobalPosition, ParticleColor);
		}

		DeathTimer?.Start();
		if (DeathTimer is null)
		{
			GD.PrintErr($"ExplodingEnemy ({Name}): DeathTimer node not found! Using temporary timer to return to pool.");
			GetTree().CreateTimer(1.0).Timeout += ReturnEnemyToPool;
		}

		if (projectileScene is null || projectileCount <= 0 || ProjectilePoolManager.Instance is null)
		{
			GD.PrintErr($"ExplodingEnemy ({Name}): Cannot spawn projectiles on death - Missing Scene, Count <= 0, or ProjectilePoolManager instance.");
			return;
		}

		float angleStep = Mathf.Tau / projectileCount;
		Vector2 spawnPosition = GlobalPosition;
		var enemyTexture = Sprite?.Texture;

		for (int i = 0; i < projectileCount; i++)
		{
			if (!IsInstanceValid(this))
			{
				GD.Print($"ExplodingEnemy ({Name}): Instance became invalid during projectile spawn loop. Aborting.");
				return;
			}

			Projectile projectile = ProjectilePoolManager.Instance.GetProjectile(projectileScene);

			if (projectile is null)
			{
				GD.PrintErr($"ExplodingEnemy ({Name}): Failed to get projectile {i + 1}/{projectileCount} from pool. Skipping.");
				continue;
			}

			float angle = i * angleStep;
			var direction = Vector2.Right.Rotated(angle);

			ProjectilePoolManager.Instance.EnqueueProjectileActivation(
				projectile,
				spawnPosition,
				direction,
				enemyTexture,
				ExplosionProjectileColor
			);
			// No await here, activation is handled by the pool manager's _Process
		}
	}
}
