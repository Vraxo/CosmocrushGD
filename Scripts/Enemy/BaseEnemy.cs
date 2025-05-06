using Godot;
using System;

namespace CosmocrushGD;

public partial class BaseEnemy : CharacterBody2D
{
	[Signal]
	public delegate void EnemyDiedEventHandler(BaseEnemy enemy);

	protected static readonly Vector2 FlipThresholdVelocity = new Vector2(0.1f, 0.1f);
	private const float DefaultMeleeKnockback = 500f;
	private const float KnockbackRecovery = 5.0f;

	protected int Health;

	[Export] protected Sprite2D Sprite;
	[Export] public Timer DeathTimer { get; private set; }
	[Export] public Timer DamageCooldownTimer { get; private set; }
	[Export] public CollisionShape2D Collider;
	[Export] public AnimationPlayer HitAnimationPlayer { get; private set; }
	[Export] protected PackedScene damageIndicatorScene;
	[Export] private AudioStream damageAudio;
	[Export] protected CpuParticles2D damageParticles { get; private set; }
	[Export] protected CpuParticles2D deathParticles { get; private set; }

	public bool Dead { get; protected set; } = false;
	public bool CanShoot { get; set; } = true;
	public Vector2 Knockback { get; set; } = Vector2.Zero;
	public Player TargetPlayer { get; set; }

	protected virtual float KnockbackResistanceMultiplier => 0.5f;
	protected virtual int MaxHealth => 20;
	protected virtual int Damage => 1;
	protected virtual float Speed => 100f;
	protected virtual float DamageRadius => 50f;
	protected virtual float ProximityThreshold => 32f;
	protected virtual float AttackCooldown => 0.5f;
	protected virtual Color ParticleColor => Colors.White;
	protected virtual float MeleeKnockbackForce => DefaultMeleeKnockback;

	public override void _Ready()
	{
		if (DeathTimer is not null)
		{
			if (!DeathTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathTimerTimeout)))
			{
				DeathTimer.Timeout += OnDeathTimerTimeout;
			}
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): DeathTimer node not found!");
		}

		if (DamageCooldownTimer is not null)
		{
			DamageCooldownTimer.WaitTime = AttackCooldown;
			if (!DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDamageCooldownTimerTimeout)))
			{
				DamageCooldownTimer.Timeout += OnDamageCooldownTimerTimeout;
			}
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): DamageCooldownTimer node not found!");
		}
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
		var fDelta = (float)delta;

		float decayFactor = 1.0f - Mathf.Exp(-KnockbackRecovery * fDelta);
		Knockback = Knockback.Lerp(Vector2.Zero, decayFactor);

		if (Dead)
		{
			if (Knockback.LengthSquared() > 0.01f)
			{
				Velocity = Knockback;
				MoveAndSlide();
			}
			else if (Velocity != Vector2.Zero)
			{
				Velocity = Vector2.Zero;
			}
			return;
		}

		var desiredMovement = Vector2.Zero;
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			var directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			var distanceToPlayerSq = GlobalPosition.DistanceSquaredTo(TargetPlayer.GlobalPosition);

			if (distanceToPlayerSq > ProximityThreshold * ProximityThreshold)
			{
				desiredMovement = directionToPlayer * Speed;
			}
		}

		Velocity = desiredMovement + Knockback;

		if (Velocity.LengthSquared() > 0.01f)
		{
			MoveAndSlide();
		}
		else if (Velocity != Vector2.Zero)
		{
			Velocity = Vector2.Zero;
		}
	}

	public override void _ExitTree()
	{
		DeathTimer?.Stop();
		DamageCooldownTimer?.Stop();

		if (DeathTimer is not null && IsInstanceValid(DeathTimer))
		{
			var callable = Callable.From(OnDeathTimerTimeout);
			if (DeathTimer.IsConnected(Timer.SignalName.Timeout, callable))
			{
				DeathTimer.Timeout -= OnDeathTimerTimeout;
			}
		}

		if (DamageCooldownTimer is not null && IsInstanceValid(DamageCooldownTimer))
		{
			var callable = Callable.From(OnDamageCooldownTimerTimeout);
			if (DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, callable))
			{
				DamageCooldownTimer.Timeout -= OnDamageCooldownTimerTimeout;
			}
		}
		base._ExitTree();
	}

	public virtual void ResetAndActivate(Vector2 position, Player player)
	{
		GlobalPosition = position;
		TargetPlayer = player;
		Health = MaxHealth;
		Dead = false;
		CanShoot = true;
		Knockback = Vector2.Zero;
		Velocity = Vector2.Zero;

		Visible = true;
		ProcessMode = ProcessModeEnum.Pausable;
		SetPhysicsProcess(true);
		Collider?.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);

		DamageCooldownTimer?.Stop();
		DeathTimer?.Stop();
		HitAnimationPlayer?.Stop(true);

		if (Sprite?.Material is ShaderMaterial shaderMat)
		{
			shaderMat.SetShaderParameter("flash_value", 0.0);
		}
		Sprite.Visible = true;

		if (DeathTimer is not null && !DeathTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathTimerTimeout)))
		{
			DeathTimer.Timeout += OnDeathTimerTimeout;
		}

		if (DamageCooldownTimer is not null && !DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDamageCooldownTimerTimeout)))
		{
			DamageCooldownTimer.WaitTime = AttackCooldown;
			DamageCooldownTimer.Timeout += OnDamageCooldownTimerTimeout;
		}
	}

	public void TakeDamage(int damage)
	{
		if (Dead)
		{
			return;
		}

		PlayDamageSound();

		Health -= damage;
		Health = Math.Max(Health, 0);
		HitAnimationPlayer?.Play("HitFlash");
		ShowDamageIndicator(damage, Health, MaxHealth);

		var worldNode = GetNodeOrNull<World>("/root/World");
		worldNode?.AddScore(damage);

		if (damageParticles is not null)
		{
			damageParticles.GlobalPosition = GlobalPosition;
			damageParticles.Modulate = ParticleColor;
			damageParticles.Restart();
			damageParticles.Emitting = true;
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): damageParticles node not assigned.");
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
		Knockback = force * (1.0f - float.Clamp(KnockbackResistanceMultiplier, 0f, 1f));
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (Sprite is null)
		{
			return;
		}

		if (Mathf.Abs(Velocity.X) > FlipThresholdVelocity.X)
		{
			Sprite.FlipH = Velocity.X < 0;
		}
		else if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Sprite.FlipH = GlobalPosition.X > TargetPlayer.GlobalPosition.X;
		}
	}

	protected virtual void AttemptAttack()
	{
		if (!CanShoot || TargetPlayer is null || !IsInstanceValid(TargetPlayer) || Dead)
		{
			return;
		}

		var distanceSq = GlobalPosition.DistanceSquaredTo(TargetPlayer.GlobalPosition);
		if (distanceSq > DamageRadius * DamageRadius)
		{
			return;
		}

		PerformAttackAction();
		CanShoot = false;
		DamageCooldownTimer?.Start();
	}

	protected virtual void PerformAttackAction() { }

	protected virtual void Die()
	{
		if (Dead)
		{
			return;
		}
		Dead = true;
		EmitSignal(SignalName.EnemyDied, this);
		SetProcess(false);
		SetPhysicsProcess(false); // Keep physics for knockback on death
		Collider?.CallDeferred(CollisionShape2D.MethodName.SetDisabled, true);

		Sprite?.SetDeferred(Sprite2D.PropertyName.Visible, false);

		if (deathParticles is not null)
		{
			deathParticles.GlobalPosition = GlobalPosition;
			deathParticles.Modulate = ParticleColor;
			deathParticles.Restart();
			deathParticles.Emitting = true;
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): deathParticles node not assigned.");
		}

		DeathTimer?.Start();
		if (DeathTimer is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): DeathTimer node not found! Using temporary timer to QueueFree.");
			GetTree().CreateTimer(1.0).Timeout += SelfDestruct;
		}
	}

	public void SelfDestruct()
	{
		QueueFree();
	}

	private void ShowDamageIndicator(int damage, int currentHealth, int maxHealth)
	{
		if (damageIndicatorScene is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): damageIndicatorScene is not assigned!");
			return;
		}

		var indicator = damageIndicatorScene.Instantiate<DamageIndicator>();
		if (indicator is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): Failed to get DamageIndicator from scene.");
			return;
		}
		GetTree().Root.AddChild(indicator);

		var verticalOffset = -20f;
		if (Sprite?.Texture is not null)
		{
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Sprite.Scale.Y - 10f;
		}
		var globalStartPosition = GlobalPosition + new Vector2(0, verticalOffset);

		indicator.Setup(damage, currentHealth, maxHealth, globalStartPosition);
	}

	private void OnDamageCooldownTimerTimeout()
	{
		CanShoot = true;
	}

	private void OnDeathTimerTimeout()
	{
		SelfDestruct();
	}

	private void PlayDamageSound()
	{
		if (damageAudio is not null)
		{
			GlobalAudioPlayer.Instance?.PlaySound2D(damageAudio, GlobalPosition);
		}
	}
}
