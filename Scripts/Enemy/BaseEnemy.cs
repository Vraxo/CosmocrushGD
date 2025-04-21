using Godot;
using System;

namespace CosmocrushGD;

public partial class BaseEnemy : CharacterBody2D
{
	[Signal]
	public delegate void EnemyDiedEventHandler(BaseEnemy enemy);

	[Export] protected Sprite2D Sprite;
	[Export] protected Timer DeathTimer;
	[Export] protected Timer DamageCooldownTimer;
	[Export] public CollisionShape2D Collider;
	[Export] protected AnimationPlayer HitAnimationPlayer;

	[Export] private PackedScene damageParticleEffectScene;
	[Export] private PackedScene deathParticleEffectScene;

	protected int Health;
	protected bool Dead = false;
	protected bool CanShoot = true;
	protected Vector2 Knockback = Vector2.Zero;
	protected Player TargetPlayer;

	protected virtual float KnockbackResistanceMultiplier => 0.1f;
	protected virtual int MaxHealth => 20;
	protected virtual int Damage => 1;
	protected virtual float Speed => 100f;
	protected virtual float DamageRadius => 50f;
	protected virtual float ProximityThreshold => 32f;
	protected virtual float KnockbackRecovery => 0.1f;
	protected virtual float AttackCooldown => 0.5f;

	public override void _Ready()
	{
		TargetPlayer = GetNode<Player>("/root/World/Player");
		if (TargetPlayer is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): Could not find Player node. Disabling AI.");
			SetProcess(false);
			SetPhysicsProcess(false);
		}

		if (DeathTimer is not null) DeathTimer.Timeout += OnDeathTimerTimeout;
		if (DamageCooldownTimer is not null)
		{
			DamageCooldownTimer.WaitTime = AttackCooldown;
			DamageCooldownTimer.Timeout += OnDamageCooldownTimerTimeout;
		}
		Health = MaxHealth;
	}

	public override void _Process(double delta)
	{
		if (Dead) return;
		UpdateSpriteDirection();
		AttemptAttack();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Dead)
		{
			if (Knockback.LengthSquared() > 0.1f)
			{
				Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery * 2.0f * (float)delta);
				Velocity = Knockback;
				MoveAndSlide();
			}
			else if (Velocity != Vector2.Zero) Velocity = Vector2.Zero;
			return;
		}

		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);
		Vector2 desiredMovement = Vector2.Zero;
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Vector2 directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
			if (distanceToPlayer > ProximityThreshold) desiredMovement = directionToPlayer * Speed;
		}
		Velocity = desiredMovement + Knockback;
		if (Velocity.LengthSquared() > 0.01f) MoveAndSlide();
		else if (Velocity != Vector2.Zero) Velocity = Vector2.Zero;
	}


	private void OnDamageCooldownTimerTimeout() { CanShoot = true; }
	private void OnDeathTimerTimeout() { QueueFree(); }

	public void TakeDamage(int damage)
	{
		if (Dead) return;
		Health -= damage;
		Health = Math.Max(Health, 0);
		HitAnimationPlayer?.Play("HitFlash");
		ShowDamageIndicator(damage, Health, MaxHealth);

		var worldNode = GetNode<World>("/root/World");
		worldNode?.AddScore(damage);

		if (damageParticleEffectScene is not null)
		{
			GlobalAudioPlayer.Instance?.GetParticleEffect(damageParticleEffectScene, GlobalPosition);
		}
		if (Health <= 0) Die();
	}

	public void ApplyKnockback(Vector2 force)
	{
		if (Dead) return;
		Knockback += force * KnockbackResistanceMultiplier;
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (Sprite is null) return;
		if (Math.Abs(Velocity.X) > 0.1f) Sprite.FlipH = Velocity.X < 0;
		else if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Sprite.FlipH = GlobalPosition.X > TargetPlayer.GlobalPosition.X;
		}
	}

	protected virtual void AttemptAttack()
	{
		if (!CanShoot || TargetPlayer is null || !IsInstanceValid(TargetPlayer)) return;
		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
		if (distance > DamageRadius) return;
		PerformAttackAction();
		CanShoot = false;
		DamageCooldownTimer?.Start();
	}

	protected virtual void PerformAttackAction()
	{
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			TargetPlayer.TakeDamage(Damage);
			Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			TargetPlayer.ApplyKnockback(knockbackDir * MeleeKnockbackForce);
		}
	}

	protected virtual float MeleeKnockbackForce => 500f;

	protected virtual void Die()
	{
		if (Dead) return;
		Dead = true;
		EmitSignal(SignalName.EnemyDied, this);
		SetPhysicsProcess(true);
		SetProcess(false);
		Collider?.CallDeferred("set_disabled", true);
		if (Sprite is not null) Sprite.Visible = false;
		if (deathParticleEffectScene is not null)
		{
			GlobalAudioPlayer.Instance?.GetParticleEffect(deathParticleEffectScene, GlobalPosition);
		}
		DeathTimer?.Start();
		if (DeathTimer is null) GetTree().CreateTimer(1.0).Timeout += QueueFree;
	}

	private void ShowDamageIndicator(int damage, int currentHealth, int maxHealth)
	{
		if (GlobalAudioPlayer.Instance is null) return;
		DamageIndicator indicator = GlobalAudioPlayer.Instance.GetDamageIndicator();
		if (indicator is null) return;

		// Calculate offset relative to GLOBAL position
		float verticalOffset = -20f;
		if (Sprite is not null && Sprite.Texture is not null)
		{
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Scale.Y - 10f;
		}
		// Calculate the target GLOBAL position for the indicator
		Vector2 globalStartPosition = GlobalPosition + new Vector2(0, verticalOffset);

		// No reparenting needed, just setup the indicator which is TopLevel
		indicator.Setup(damage, currentHealth, maxHealth, globalStartPosition);
	}

	public override void _ExitTree()
	{
		DeathTimer?.Stop();
		DamageCooldownTimer?.Stop();
		if (DeathTimer is not null && IsInstanceValid(DeathTimer))
		{
			if (DeathTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathTimerTimeout)))
				DeathTimer.Timeout -= OnDeathTimerTimeout;
		}
		if (DamageCooldownTimer is not null && IsInstanceValid(DamageCooldownTimer))
		{
			if (DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDamageCooldownTimerTimeout)))
				DamageCooldownTimer.Timeout -= OnDamageCooldownTimerTimeout;
		}
		base._ExitTree();
	}
}
