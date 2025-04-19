using CosmocrushGD;
using Godot;
using System.Diagnostics;
using System.Xml.Linq;
using System;

using Godot;
using System;

namespace CosmocrushGD;

public partial class BaseEnemy : CharacterBody2D
{
	// Removed: Navigator, NavigationUpdateTimer
	[Export] protected Sprite2D Sprite;
	[Export] protected Timer DeathTimer;
	[Export] protected Timer DamageCooldownTimer;
	[Export] public CollisionShape2D Collider;
	[Export] public CpuParticles2D DamageParticles;
	[Export] public CpuParticles2D DeathParticles;
	[Export] protected PackedScene DamageIndicatorScene;
	[Export] protected AnimationPlayer HitAnimationPlayer;

	protected int Health;
	protected bool Dead = false;
	protected bool CanShoot = true;
	protected Vector2 Knockback = Vector2.Zero;
	protected Player TargetPlayer;
	// Removed: _navigationMapReady, _navigationDesiredVelocity

	public EnemyPoolManager PoolManager { get; set; }
	public PackedScene SourceScene { get; set; }

	protected virtual float KnockbackResistanceMultiplier => 0.1f;
	protected virtual int MaxHealth => 20;
	protected virtual int Damage => 1;
	protected virtual float Speed => 100f;
	protected virtual float DamageRadius => 50f; // Used for attack range
	protected virtual float ProximityThreshold => 32f; // Distance at which to stop moving closer
	protected virtual float KnockbackRecovery => 0.1f;
	protected virtual float AttackCooldown => 0.5f;
	// Removed: NavigationReachedThreshold

	public override void _Ready()
	{
		TargetPlayer = GetNode<Player>("/root/World/Player");
		DeathTimer.Timeout += OnDeathTimerTimeout;
		DamageCooldownTimer.WaitTime = AttackCooldown;
		DamageCooldownTimer.Timeout += OnDamageCooldownTimerTimeout;
		// Removed: NavigationUpdateTimer connection
		// Removed: Navigator signal connection
	}

	// Removed: _Notification method for map setting

	public virtual void ResetState(Vector2 spawnPosition)
	{
		GlobalPosition = spawnPosition;
		Health = MaxHealth;
		Dead = false;
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;
		CanShoot = true;
		// Removed: _navigationMapReady reset

		Visible = true;
		if (Sprite is not null)
		{
			Sprite.Visible = true;
		}
		if (Collider is not null)
		{
			Collider.Disabled = false;
		}

		if (DamageParticles is not null)
		{
			DamageParticles.Emitting = false;
			DamageParticles.Restart();
		}
		if (DeathParticles is not null)
		{
			DeathParticles.Emitting = false;
			DeathParticles.Restart();
		}

		DeathTimer.Stop();
		DamageCooldownTimer.Stop();
		// Removed: NavigationUpdateTimer start/stop
		// Removed: Navigator state reset

		if (HitAnimationPlayer is not null && HitAnimationPlayer.IsPlaying())
		{
			HitAnimationPlayer.Stop(true);
		}

		ProcessMode = ProcessModeEnum.Inherit;
	}

	public override void _Process(double delta)
	{
		if (Dead)
		{
			return;
		}

		UpdateSpriteDirection();
		AttemptAttack();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Dead)
		{
			Velocity = Knockback.Lerp(Vector2.Zero, KnockbackRecovery); // Still apply knockback decay
			if (Velocity != Vector2.Zero) { MoveAndSlide(); }
			return;
		}

		// Apply knockback decay
		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);

		Vector2 desiredMovement = Vector2.Zero;

		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Vector2 directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

			// Only move if further than the proximity threshold
			if (distanceToPlayer > ProximityThreshold)
			{
				desiredMovement = directionToPlayer * Speed;
			}
		}

		// Set the final velocity for the CharacterBody2D
		Velocity = desiredMovement + Knockback;

		// Move the character
		if (Velocity != Vector2.Zero)
		{
			MoveAndSlide();
		}
	}

	// Removed: OnSafeVelocityComputed
	// Removed: TrySetNavigationMap
	// Removed: OnNavigationUpdateTimerTimeout
	// Removed: RequestNavigationUpdate
	// Removed: CalculateDesiredVelocity

	private void OnDamageCooldownTimerTimeout()
	{
		CanShoot = true;
	}

	private void OnDeathTimerTimeout()
	{
		ReturnToPool();
	}

	public void TakeDamage(int damage)
	{
		if (Dead)
		{
			return;
		}

		Health -= damage;
		HitAnimationPlayer?.Play("HitFlash");
		ShowDamageIndicator(damage);

		var worldNode = GetNode<World>("/root/World");
		if (worldNode is not null)
		{
			worldNode.AddScore(damage);
		}
		else
		{
			GD.PrintErr($"Could not find World node at /root/World to grant score in {Name}.TakeDamage.");
		}

		if (DamageParticles is not null)
		{
			DamageParticles.Emitting = true;
		}

		if (Health <= 0)
		{
			Die();
		}
	}

	public void ApplyKnockback(Vector2 force)
	{
		if (Dead)
		{
			return;
		}
		// No navigation velocity to reset anymore
		Knockback += force * KnockbackResistanceMultiplier;
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer) || Sprite is null)
		{
			return;
		}

		// Base flipping on the current velocity or player position if stationary
		if (Math.Abs(Velocity.X) > 0.1f)
		{
			Sprite.FlipH = Velocity.X < 0;
		}
		else // If not moving horizontally, base flip on player position relative to enemy
		{
			Sprite.FlipH = GlobalPosition.X > TargetPlayer.GlobalPosition.X;
		}
	}

	protected virtual void AttemptAttack()
	{
		if (!CanShoot || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			return;
		}

		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

		// Use DamageRadius for attack range check
		if (distance > DamageRadius)
		{
			return;
		}

		PerformAttackAction(); // Call a method to perform the specific attack

		CanShoot = false;
		DamageCooldownTimer.Start();
	}

	// Renamed from PerformMeleeAttack for generality, derived classes implement specifics
	protected virtual void PerformAttackAction()
	{
		// Base implementation could be empty or a default melee attack
		// Example: Melee damage + knockback
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			TargetPlayer.TakeDamage(Damage);
			Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			TargetPlayer.ApplyKnockback(knockbackDir * meleeKnockbackForce); // Ensure meleeKnockbackForce is defined
		}
	}
	// Add a placeholder for meleeKnockbackForce if PerformAttackAction uses it in BaseEnemy
	protected virtual float meleeKnockbackForce => 500f;


	protected virtual void Die()
	{
		if (Dead)
		{
			return;
		}

		Dead = true;
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;
		// Removed: NavigationUpdateTimer.Stop();
		// Removed: Navigator state reset

		if (Collider is not null)
		{
			Collider.Disabled = true;
		}

		if (Sprite is not null)
		{
			Sprite.Visible = false;
		}
		if (DeathParticles is not null)
		{
			DeathParticles.Emitting = true;
		}

		DeathTimer.Start();
	}

	private void ShowDamageIndicator(int damage)
	{
		if (DamageIndicatorScene is null)
		{
			return;
		}

		var indicator = DamageIndicatorScene.Instantiate<DamageIndicator>();
		indicator.Text = damage.ToString();
		indicator.Health = Health;
		indicator.MaxHealth = MaxHealth;

		float verticalOffset = -20f;
		if (Sprite is not null && Sprite.Texture is not null)
		{
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Scale.Y;
		}

		indicator.Position = new(0, verticalOffset);

		AddChild(indicator);
	}

	private void ReturnToPool()
	{
		if (PoolManager is null)
		{
			GD.PushError("Enemy cannot return to pool: PoolManager reference missing!");
			QueueFree(); // Free if pool manager is gone
			return;
		}

		PoolManager.ReturnEnemy(this);
	}

	public override void _ExitTree()
	{
		// Ensure timers are stopped if the node exits prematurely
		DeathTimer?.Stop();
		DamageCooldownTimer?.Stop();

		if (DeathTimer is not null && IsInstanceValid(DeathTimer))
		{
			DeathTimer.Timeout -= OnDeathTimerTimeout;
		}
		if (DamageCooldownTimer is not null && IsInstanceValid(DamageCooldownTimer))
		{
			DamageCooldownTimer.Timeout -= OnDamageCooldownTimerTimeout;
		}
		// Removed: NavigationUpdateTimer disconnect
		// Removed: Navigator signal disconnect
		base._ExitTree();
	}
}
