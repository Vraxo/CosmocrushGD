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
	[Export] protected PackedScene DamageIndicatorScene;
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
	protected virtual float MeleeKnockbackForce => 500f;

	public override void _Ready()
	{
		TargetPlayer = GetNode<Player>("/root/World/Player");
		if (TargetPlayer is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): Could not find Player node at /root/World/Player. Disabling enemy.");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

		if (DeathTimer is not null)
		{
			DeathTimer.Timeout += OnDeathTimerTimeout;
		}
		if (DamageCooldownTimer is not null)
		{
			DamageCooldownTimer.WaitTime = AttackCooldown;
			DamageCooldownTimer.Timeout += OnDamageCooldownTimerTimeout;
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): DamageCooldownTimer is null. Attack will not function correctly.");
		}

		Health = MaxHealth;

		// Validate essential components
		if (Sprite is null) GD.PrintErr($"BaseEnemy ({Name}): Sprite is not assigned.");
		if (Collider is null) GD.PrintErr($"BaseEnemy ({Name}): Collider is not assigned.");
		if (HitAnimationPlayer is null) GD.Print($"BaseEnemy ({Name}): HitAnimationPlayer is not assigned (optional).");
		if (DamageIndicatorScene is null) GD.Print($"BaseEnemy ({Name}): DamageIndicatorScene is not assigned (optional).");
		if (damageParticleEffectScene is null) GD.Print($"BaseEnemy ({Name}): damageParticleEffectScene is not assigned (optional).");
		if (deathParticleEffectScene is null) GD.Print($"BaseEnemy ({Name}): deathParticleEffectScene is not assigned (optional).");
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
			if (Knockback.LengthSquared() > 0.1f)
			{
				Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery * 2.0f * (float)delta);
				Velocity = Knockback;
				MoveAndSlide();
			}
			else if (Velocity != Vector2.Zero)
			{
				Velocity = Vector2.Zero;
			}
			return;
		}

		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery * (float)delta * 60f); // Make recovery frame-rate independent

		Vector2 desiredMovement = Vector2.Zero;

		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Vector2 directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

			if (distanceToPlayer > ProximityThreshold)
			{
				desiredMovement = directionToPlayer * Speed;
			}
		}
		else if (!Dead) // Only log if not dead and player is missing
		{
			GD.PrintErr($"BaseEnemy ({Name}): TargetPlayer is null or invalid in _PhysicsProcess. Stopping movement.");
			desiredMovement = Vector2.Zero;
			// Optionally disable the enemy entirely if this persists
			// SetPhysicsProcess(false);
			// SetProcess(false);
		}

		Velocity = desiredMovement + Knockback;

		if (Velocity != Vector2.Zero)
		{
			MoveAndSlide();
		}
	}


	private void OnDamageCooldownTimerTimeout()
	{
		CanShoot = true;
	}

	private void OnDeathTimerTimeout()
	{
		QueueFree();
	}

	public void TakeDamage(int damage)
	{
		if (Dead || Health <= 0)
		{
			return;
		}

		Health -= damage;
		Health = Mathf.Max(0, Health); // Ensure health doesn't go below 0

		HitAnimationPlayer?.Play("HitFlash");
		ShowDamageIndicator(damage);

		var worldNode = GetNode<World>("/root/World");
		if (worldNode is not null)
		{
			worldNode.AddScore(damage);
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): Could not find World node to add score.");
		}

		TrySpawnDamageParticles();

		if (Health <= 0)
		{
			Die();
		}
	}

	private void TrySpawnDamageParticles()
	{
		if (damageParticleEffectScene is null)
		{
			// GD.Print($"BaseEnemy ({Name}): No damage particle scene assigned."); // Optional: reduce log spam
			return;
		}
		if (!IsInstanceValid(damageParticleEffectScene))
		{
			GD.PrintErr($"BaseEnemy ({Name}): Assigned damage particle scene is invalid.");
			return;
		}

		if (GlobalAudioPlayer.Instance is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): GlobalAudioPlayer instance not found. Cannot spawn damage particles.");
			return;
		}

		GlobalAudioPlayer.Instance.GetParticleEffect(damageParticleEffectScene, GlobalPosition);
	}

	private void TrySpawnDeathParticles()
	{
		if (deathParticleEffectScene is null)
		{
			// GD.Print($"BaseEnemy ({Name}): No death particle scene assigned."); // Optional: reduce log spam
			return;
		}
		if (!IsInstanceValid(deathParticleEffectScene))
		{
			GD.PrintErr($"BaseEnemy ({Name}): Assigned death particle scene is invalid.");
			return;
		}

		if (GlobalAudioPlayer.Instance is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): GlobalAudioPlayer instance not found. Cannot spawn death particles.");
			return;
		}

		GlobalAudioPlayer.Instance.GetParticleEffect(deathParticleEffectScene, GlobalPosition);
	}


	public void ApplyKnockback(Vector2 force)
	{
		if (Dead)
		{
			return;
		}
		Knockback += force * KnockbackResistanceMultiplier;
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (Sprite is null)
		{
			return;
		}

		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			// Prioritize velocity for direction if moving significantly
			if (Velocity.LengthSquared() > 10f) // Use a small threshold to avoid jitter
			{
				if (Mathf.Abs(Velocity.X) > 0.1f)
				{
					Sprite.FlipH = Velocity.X < 0;
				}
			}
			// Otherwise, face the player
			else
			{
				Sprite.FlipH = GlobalPosition.X > TargetPlayer.GlobalPosition.X;
			}
		}
	}


	protected virtual void AttemptAttack()
	{
		if (!CanShoot || Dead) // Check Dead flag here too
		{
			return;
		}

		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			// GD.PrintErr($"BaseEnemy ({Name}): TargetPlayer is null or invalid during attack attempt."); // Can be spammy
			return;
		}

		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

		if (distance > DamageRadius)
		{
			return;
		}

		PerformAttackAction();

		CanShoot = false;
		if (DamageCooldownTimer is not null)
		{
			DamageCooldownTimer.Start();
		}
		else // Should not happen if _Ready checks pass, but good safeguard
		{
			GD.PrintErr($"BaseEnemy ({Name}): Cannot start DamageCooldownTimer, it's null!");
			GetTree().CreateTimer(AttackCooldown).Timeout += OnDamageCooldownTimerTimeout; // Fallback timer
		}
	}


	protected virtual void PerformAttackAction()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			return; // Already checked in AttemptAttack, but double-check
		}

		TargetPlayer.TakeDamage(Damage);
		Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
		TargetPlayer.ApplyKnockback(knockbackDir * MeleeKnockbackForce);
	}

	protected virtual void Die()
	{
		if (Dead)
		{
			return;
		}

		Dead = true;
		EmitSignal(SignalName.EnemyDied, this);

		// Keep physics for knockback, disable AI
		SetProcess(false);
		SetPhysicsProcess(true);
		Velocity = Knockback; // Initial velocity is remaining knockback

		if (Collider is not null)
		{
			Collider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): Collider is null during Die(), cannot disable.");
		}

		if (Sprite is not null)
		{
			Sprite.Visible = false;
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): Sprite is null during Die(), cannot hide.");
		}

		TrySpawnDeathParticles();

		if (DeathTimer is not null)
		{
			DeathTimer.Start();
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): DeathTimer is null. Freeing immediately.");
			QueueFree(); // Fallback if no timer
		}
	}


	private void ShowDamageIndicator(int damage)
	{
		if (DamageIndicatorScene is null)
		{
			return;
		}
		if (!IsInstanceValid(DamageIndicatorScene))
		{
			GD.PrintErr($"BaseEnemy ({Name}): Assigned DamageIndicatorScene is invalid.");
			return;
		}

		Node instance = DamageIndicatorScene.Instantiate();
		if (instance is not DamageIndicator indicator)
		{
			GD.PrintErr($"BaseEnemy ({Name}): Failed to instantiate DamageIndicatorScene as DamageIndicator.");
			instance?.QueueFree();
			return;
		}

		indicator.Text = damage.ToString();
		indicator.Health = Health;
		indicator.MaxHealth = MaxHealth;

		float verticalOffset = -20f;
		if (Sprite is not null && Sprite.Texture is not null)
		{
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Scale.Y - 10f;
		}

		indicator.Position = new(0, verticalOffset);

		AddChild(indicator);
	}

	public override void _ExitTree()
	{
		// Use null-conditional access and check IsConnected before disconnecting
		if (DeathTimer is not null && IsInstanceValid(DeathTimer))
		{
			var callable = Callable.From(OnDeathTimerTimeout);
			if (DeathTimer.IsConnected(Timer.SignalName.Timeout, callable))
			{
				DeathTimer.Timeout -= OnDeathTimerTimeout;
			}
			DeathTimer.Stop(); // Ensure timer is stopped
		}

		if (DamageCooldownTimer is not null && IsInstanceValid(DamageCooldownTimer))
		{
			var callable = Callable.From(OnDamageCooldownTimerTimeout);
			if (DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, callable))
			{
				DamageCooldownTimer.Timeout -= OnDamageCooldownTimerTimeout;
			}
			DamageCooldownTimer.Stop(); // Ensure timer is stopped
		}
		base._ExitTree();
	}
}
