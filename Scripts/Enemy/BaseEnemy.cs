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

	// Export particle scenes for the pool manager to use
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
			GD.PrintErr($"BaseEnemy ({Name}): Could not find Player node at /root/World/Player");
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

		Health = MaxHealth;
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
			// Apply knockback decay even when dead until velocity is near zero
			if (Knockback.LengthSquared() > 0.1f)
			{
				Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery * 2.0f * (float)delta); // Faster decay when dead
				Velocity = Knockback;
				MoveAndSlide();
			}
			else if (Velocity != Vector2.Zero)
			{
				Velocity = Vector2.Zero; // Stop completely once knockback is negligible
			}
			return;
		}

		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);

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
			GD.PrintErr($"BaseEnemy ({Name}): Could not find World node to add score.");
		}


		if (damageParticleEffectScene is not null && GlobalAudioPlayer.Instance is not null)
		{
			// Request damage particle effect from pool
			GlobalAudioPlayer.Instance.GetParticleEffect(damageParticleEffectScene, GlobalPosition);
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
		Knockback += force * KnockbackResistanceMultiplier;
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer) || Sprite is null)
		{
			return;
		}

		if (Math.Abs(Velocity.X) > 0.1f)
		{
			Sprite.FlipH = Velocity.X < 0;
		}
		else if (TargetPlayer is not null && IsInstanceValid(TargetPlayer)) // Check again for safety
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
	}


	protected virtual void PerformAttackAction()
	{
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			TargetPlayer.TakeDamage(Damage);
			Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			TargetPlayer.ApplyKnockback(knockbackDir * MeleeKnockbackForce); // Use property
		}
	}


	protected virtual float MeleeKnockbackForce => 500f; // Keep consistent naming


	protected virtual void Die()
	{
		if (Dead)
		{
			return;
		}

		EmitSignal(SignalName.EnemyDied, this);
		Dead = true;
		// Keep knockback active, but stop seeking player
		Velocity = Knockback; // Initial velocity is remaining knockback
		SetPhysicsProcess(true); // Keep physics process active for knockback decay
		SetProcess(false); // Disable AI processing (_Process)


		if (Collider is not null)
		{
			Collider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}


		if (Sprite is not null)
		{
			Sprite.Visible = false;
		}


		if (deathParticleEffectScene is not null && GlobalAudioPlayer.Instance is not null)
		{
			// Request death particle effect from pool
			GlobalAudioPlayer.Instance.GetParticleEffect(deathParticleEffectScene, GlobalPosition);
		}

		if (DeathTimer is not null)
		{
			DeathTimer.Start(); // Timer to eventually QueueFree
		}
		else
		{
			QueueFree(); // Fallback if no timer
		}
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
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Scale.Y - 10f; // Add a bit more offset
		}

		indicator.Position = new(0, verticalOffset);

		AddChild(indicator);
	}

	public override void _ExitTree()
	{
		DeathTimer?.Stop();
		DamageCooldownTimer?.Stop();

		if (DeathTimer is not null && IsInstanceValid(DeathTimer))
		{
			if (DeathTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathTimerTimeout)))
			{
				DeathTimer.Timeout -= OnDeathTimerTimeout;
			}
		}
		if (DamageCooldownTimer is not null && IsInstanceValid(DamageCooldownTimer))
		{
			if (DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDamageCooldownTimerTimeout)))
			{
				DamageCooldownTimer.Timeout -= OnDamageCooldownTimerTimeout;
			}
		}
		base._ExitTree();
	}
}
