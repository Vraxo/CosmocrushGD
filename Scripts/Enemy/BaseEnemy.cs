using Godot;
using System;

namespace CosmocrushGD;

public partial class BaseEnemy : Area2D
{
	[Signal]
	public delegate void EnemyDiedEventHandler(BaseEnemy enemy);

	protected static readonly Vector2 FlipThresholdVelocity = new Vector2(0.1f, 0.1f);
	private const float DefaultMeleeKnockback = 500f;
	// Increased recovery rate significantly for faster deceleration from knockback
	protected const float KnockbackRecovery = 5.0f; // Changed to protected
	private const float PushForce = 100.0f; // Force to push overlapping areas

	protected int Health;
	protected Vector2 currentVelocity = Vector2.Zero; // Changed to protected


	[Export] protected Sprite2D Sprite;
	[Export] public Timer DeathTimer { get; private set; }
	[Export] public Timer DamageCooldownTimer { get; private set; }
	[Export] public CollisionShape2D Collider;
	[Export] public AnimationPlayer HitAnimationPlayer { get; private set; }
	[Export] protected PackedScene damageParticleEffectScene;
	[Export] protected PackedScene deathParticleEffectScene;
	[Export] private AudioStream damageAudio;

	public bool Dead { get; protected set; } = false;
	public bool CanShoot { get; set; } = true;
	public Vector2 Knockback { get; set; } = Vector2.Zero;
	public Player TargetPlayer { get; set; }
	public PackedScene SourceScene { get; set; }

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

		// Connect AreaEntered signal for soft collisions
		AreaEntered += OnAreaEntered;
	}

	public override void _Process(double delta)
	{
		float fDelta = (float)delta;

		if (Dead)
		{
			if (Knockback.LengthSquared() > 0.01f)
			{
				// Apply knockback decay using exponential damping (framerate independent)
				float decayFactor = 1.0f - Mathf.Exp(-KnockbackRecovery * fDelta);
				Knockback = Knockback.Lerp(Vector2.Zero, decayFactor);
				GlobalPosition += Knockback * fDelta; // Manual position update
			}
			// Dead enemies should not do anything else in _Process besides their visual knockback
			return;
		}

		// Calculate desired movement when alive
		var desiredMovement = Vector2.Zero;
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			var directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			float distanceToPlayerSq = GlobalPosition.DistanceSquaredTo(TargetPlayer.GlobalPosition);

			// Only move if further than proximity threshold
			if (distanceToPlayerSq > ProximityThreshold * ProximityThreshold)
			{
				desiredMovement = directionToPlayer * Speed;
			}
		}

		// Combine desired movement and remaining knockback
		currentVelocity = desiredMovement + Knockback;

			// Apply movement
			GlobalPosition += currentVelocity * fDelta;

			UpdateSpriteDirection();
			AttemptAttack();
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

		// Disconnect AreaEntered signal
		AreaEntered -= OnAreaEntered;

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
		currentVelocity = Vector2.Zero; // Reset custom velocity

		Visible = true;
		ProcessMode = ProcessModeEnum.Pausable; // Make sure _Process runs
		// SetPhysicsProcess(true); // Not needed for Area2D
		Collider?.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);

		DamageCooldownTimer?.Stop();
		DeathTimer?.Stop();
		HitAnimationPlayer?.Stop(true);
		Sprite?.Set("material:shader_parameter/flash_value", 0.0);
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

	public virtual void ResetForPooling()
	{
		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled; // Disable _Process when pooled
		// SetPhysicsProcess(false); // Not needed for Area2D
		Collider?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		currentVelocity = Vector2.Zero; // Reset custom velocity
		Knockback = Vector2.Zero;
		Dead = false;
		TargetPlayer = null;
		CanShoot = true;

		DeathTimer?.Stop();
		DamageCooldownTimer?.Stop();
		HitAnimationPlayer?.Stop(true);

		if (Sprite is not null)
		{
			Sprite.Visible = true;
			Sprite.Modulate = Colors.White;
			Sprite.FlipH = false;
			Sprite.FlipV = false;
			if (Sprite.Material is ShaderMaterial shaderMat)
			{
				shaderMat.SetShaderParameter("flash_value", 0.0);
			}
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

		if (damageParticleEffectScene is not null)
		{
			ParticlePoolManager.Instance?.GetParticleEffect(damageParticleEffectScene, GlobalPosition, ParticleColor);
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
			// Even if dead, if knockback is applied (e.g. by an explosion after death),
			// we might want to accumulate it for the visual effect.
			// The current KnockbackResistanceMultiplier will apply.
		}
		Knockback += force * (1.0f - float.Clamp(KnockbackResistanceMultiplier, 0f, 1f));
	}

	// Method to apply push force from soft collisions
	public void ApplyPush(Vector2 force)
	{
		currentVelocity += force;
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (Sprite is null)
		{
			return;
		}

		if (Mathf.Abs(currentVelocity.X) > FlipThresholdVelocity.X)
		{
			Sprite.FlipH = currentVelocity.X < 0;
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

		float distanceSq = GlobalPosition.DistanceSquaredTo(TargetPlayer.GlobalPosition);
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

		// SetPhysicsProcess(false); // Not needed for Area2D
		Collider?.CallDeferred(CollisionShape2D.MethodName.SetDisabled, true);
		// ProcessMode is PContainerausable, so _Process will continue to run for manual knockback

		Sprite?.SetDeferred(Sprite2D.PropertyName.Visible, false);

		if (deathParticleEffectScene is not null)
		{
			ParticlePoolManager.Instance?.GetParticleEffect(deathParticleEffectScene, GlobalPosition, ParticleColor);
		}

		DeathTimer?.Start();
		if (DeathTimer is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): DeathTimer node not found! Using temporary timer to return to pool.");
			GetTree().CreateTimer(1.0).Timeout += ReturnEnemyToPool;
		}
	}

	public void ReturnEnemyToPool()
	{
		if (EnemyPoolManager.Instance is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): EnemyPoolManager instance not found. Cannot return to pool. Freeing instead.");
			QueueFree();
		}
		else
		{
			EnemyPoolManager.Instance.ReturnEnemy(this);
		}
	}

	private void ShowDamageIndicator(int damage, int currentHealth, int maxHealth)
	{
		if (DamageIndicatorPoolManager.Instance is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): DamageIndicatorPoolManager instance not found.");
			return;
		}

		DamageIndicator indicator = DamageIndicatorPoolManager.Instance.GetDamageIndicator();
		if (indicator is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): Failed to get DamageIndicator from pool.");
			return;
		}

		float verticalOffset = -20f;
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
		ReturnEnemyToPool();
	}

	private void PlayDamageSound()
	{
		if (damageAudio is not null)
		{
			GlobalAudioPlayer.Instance?.PlaySound2D(damageAudio, GlobalPosition);
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		// Basic soft collision: push overlapping areas away
		if (area is Player player)
		{
			Vector2 pushDirection = (GlobalPosition - player.GlobalPosition).Normalized();
			// Apply force to both the enemy and the player
			currentVelocity += pushDirection * PushForce * (float)GetProcessDeltaTime();
			player.ApplyPush(pushDirection * PushForce * (float)GetProcessDeltaTime()); // Assuming Player has an ApplyPush method
		}
		else if (area is BaseEnemy otherEnemy)
		{
			Vector2 pushDirection = (GlobalPosition - otherEnemy.GlobalPosition).Normalized();
			// Apply force to both enemies
			currentVelocity += pushDirection * PushForce * (float)GetProcessDeltaTime();
			otherEnemy.ApplyPush(pushDirection * PushForce * (float)GetProcessDeltaTime());
		}
	}
}
