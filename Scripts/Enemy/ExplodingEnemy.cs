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

	private static readonly Color ExplosionProjectileColor = new(1.0f, 0.5f, 0.15f);
	private const float ProjectileSpawnDelay = 0.01f;
	// Removed _worldNodeCache field

	// Removed _Ready() override as it's no longer needed for caching World node

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

		// --- Get World Node Reference Here ---
		Node worldNode = GetNode<Node>("/root/World");
		if (worldNode is null || !IsInstanceValid(worldNode))
		{
			GD.PrintErr($"ExplodingEnemy ({Name}): Could not find valid World node at '/root/World' during Die(). Aborting projectile spawn.");
			// Proceed with base death logic without projectiles if world node not found
		}
		// --- End Get World Node Reference ---


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

		// Check dependencies *after* getting worldNode
		if (projectileScene is null || projectileCount <= 0 || ProjectilePoolManager.Instance is null || worldNode is null)
		{
			GD.PrintErr($"ExplodingEnemy ({Name}): Cannot spawn projectiles on death - Missing Scene, Count <= 0, Pool Manager instance, or World node.");
			return; // Abort projectile spawn if dependencies missing
		}


		GD.Print($"ExplodingEnemy ({Name}): Starting staggered projectile spawn ({projectileCount} projectiles).");
		float angleStep = Mathf.Tau / projectileCount;
		Vector2 spawnPosition = GlobalPosition;
		Texture2D enemyTexture = Sprite?.Texture;

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

			Node currentParent = projectile.GetParent();
			if (currentParent != worldNode)
			{
				currentParent?.RemoveChild(projectile);
				worldNode.AddChild(projectile);
			}

			projectile.SetupAndActivate(spawnPosition, direction, enemyTexture, ExplosionProjectileColor);
			GD.Print($"ExplodingEnemy ({Name}): Spawned and activated projectile {i + 1}/{projectileCount}.");

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		GD.Print($"ExplodingEnemy ({Name}): Finished projectile spawn sequence.");
	}

	public override void ResetForPooling()
	{
		base.ResetForPooling();
	}
}
