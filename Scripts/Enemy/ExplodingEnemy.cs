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
	private const float ProjectileSpawnDelay = 0.01f; // Small delay between spawns

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

		// Standard death sequence (particles, sound, disable visuals/collision, start death timer)
		Dead = true;
		EmitSignal(SignalName.EnemyDied, this);
		SetProcess(false);
		SetPhysicsProcess(false); // Stop physics process immediately
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

		// Staggered Projectile Spawning
		if (projectileScene is null || projectileCount <= 0 || ProjectilePoolManager.Instance is null)
		{
			GD.PrintErr($"ExplodingEnemy ({Name}): Cannot spawn projectiles on death - Missing Scene, Count <= 0, or ProjectilePoolManager instance.");
			return; // Still proceed with base death logic, just no explosion
		}

		GD.Print($"ExplodingEnemy ({Name}): Starting staggered projectile spawn ({projectileCount} projectiles).");
		float angleStep = Mathf.Tau / projectileCount;
		Vector2 spawnPosition = GlobalPosition;
		Texture2D enemyTexture = Sprite?.Texture;

		for (int i = 0; i < projectileCount; i++)
		{
			// Check if the enemy instance is still valid (might have been cleaned up)
			if (!IsInstanceValid(this))
			{
				GD.Print($"ExplodingEnemy ({Name}): Instance became invalid during projectile spawn loop. Aborting.");
				return;
			}

			Projectile projectile = ProjectilePoolManager.Instance.GetProjectile(projectileScene);
			if (projectile is null)
			{
				GD.PrintErr($"ExplodingEnemy ({Name}): Failed to get projectile {i + 1}/{projectileCount} from pool. Skipping.");
				continue; // Skip if projectile couldn't be retrieved
			}

			float angle = i * angleStep;
			var direction = Vector2.Right.Rotated(angle);

			// Get the projectile ready in the scene tree but keep it inactive
			// Ensure it's parented to something sensible if the enemy is visually gone
			// Reparenting to the ProjectilePoolManager itself is a safe bet
			if (projectile.GetParent() != ProjectilePoolManager.Instance)
			{
				projectile.GetParent()?.RemoveChild(projectile);
				ProjectilePoolManager.Instance.AddChild(projectile);
			}

			// Use the consolidated method
			projectile.SetupAndActivate(spawnPosition, direction, enemyTexture, ExplosionProjectileColor);
			GD.Print($"ExplodingEnemy ({Name}): Spawned and activated projectile {i + 1}/{projectileCount}.");

			// IMPORTANT: Wait for the next process frame to stagger the activation load
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			// Alternatively, use a small timer delay if frame yielding isn't enough
			// await ToSignal(GetTree().CreateTimer(ProjectileSpawnDelay), Timer.SignalName.Timeout);
		}
		GD.Print($"ExplodingEnemy ({Name}): Finished projectile spawn sequence.");
	}

	// Override ResetForPooling to ensure any async operations are handled if needed
	// (In this case, Die() doesn't leave long-running state that needs explicit cleanup on pool return,
	// as the async operation completes before the DeathTimer typically finishes)
	public override void ResetForPooling()
	{
		base.ResetForPooling();
		// Add any specific cleanup for ExplodingEnemy if needed
	}

	// ReturnEnemyToPool is inherited from BaseEnemy and called by DeathTimer timeout
}
