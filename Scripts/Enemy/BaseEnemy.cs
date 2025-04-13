using Godot;

namespace CosmocrushGD;

public abstract partial class BaseEnemy : CharacterBody2D
{
	[Export] protected NavigationAgent2D Navigator;
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

	public EnemyPoolManager PoolManager { get; set; }
	public PackedScene SourceScene { get; set; }

	// ScoreValue is no longer used directly for awarding score on death
	// protected virtual int ScoreValue => 1; // Keep or remove as needed for other potential uses
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
		DeathTimer.Timeout += ReturnToPool;
		DamageCooldownTimer.WaitTime = AttackCooldown;
		DamageCooldownTimer.Timeout += () => CanShoot = true;
	}

	public virtual void ResetState(Vector2 spawnPosition)
	{
		GlobalPosition = spawnPosition;
		Health = MaxHealth;
		Dead = false;
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;
		CanShoot = true;

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
			return;
		}

		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);
		Velocity = CalculateMovement() + Knockback;
		MoveAndSlide();
	}

	protected virtual Vector2 CalculateMovement()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			TargetPlayer = null;
			return Vector2.Zero;
		}

		float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

		if (distanceToPlayer <= ProximityThreshold)
		{
			return Vector2.Zero;
		}

		if (Navigator is null)
		{
			return Vector2.Zero;
		}

		Navigator.TargetPosition = TargetPlayer.GlobalPosition;

		Rid mapRid = NavigationServer2D.AgentGetMap(Navigator.GetRid());
		if (!mapRid.IsValid)
		{
			GD.PrintErr($"Navigator map invalid for {Name} at {GlobalPosition}");
			return Vector2.Zero;
		}

		if (Navigator.IsNavigationFinished())
		{
			return Vector2.Zero;
		}

		Vector2 direction = (Navigator.GetNextPathPosition() - GlobalPosition).Normalized();
		return direction * Speed;
	}

	public void TakeDamage(int damage)
	{
		if (Dead)
		{
			return;
		}

		// Store health before taking damage to calculate actual damage taken if needed
		// int healthBeforeDamage = Health;

		Health -= damage;
		HitAnimationPlayer?.Play("HitFlash");
		ShowDamageIndicator(damage);

		// Grant score based on the damage dealt
		var worldNode = GetNode<World>("/root/World");
		if (worldNode is not null)
		{
			// Option 1: Grant score equal to the incoming damage value
			worldNode.AddScore(damage);

			// Option 2: Grant score only for actual health lost (prevents score for overkill)
			// int actualDamage = healthBeforeDamage - Mathf.Max(Health, 0);
			// if (actualDamage > 0)
			// {
			//     worldNode.AddScore(actualDamage);
			// }
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

		Knockback += force * KnockbackResistanceMultiplier;
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer) || Sprite is null)
		{
			return;
		}

		Sprite.FlipH = GlobalPosition.X > TargetPlayer.GlobalPosition.X;
	}

	protected abstract void AttemptAttack();

	protected virtual void Die()
	{
		if (Dead)
		{
			return;
		}

		Dead = true;
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;

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

		// Score is no longer granted here
		// var worldNode = GetNode<World>("/root/World");
		// if (worldNode != null)
		// {
		// 	worldNode.AddScore(ScoreValue);
		// }
		// else
		// {
		// 	GD.PrintErr("Could not find World node at /root/World to grant score.");
		// }

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
		indicator.Health = Health; // Current health after damage
		indicator.MaxHealth = MaxHealth; // Max health for color calculation

		float verticalOffset = -20f; // Default offset
		if (Sprite is not null && Sprite.Texture is not null)
		{
			// Use half the texture height as offset if available
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Scale.Y; // Consider sprite scale
		}

		indicator.Position = new(0, verticalOffset);


		AddChild(indicator);
	}

	private void ReturnToPool()
	{
		if (PoolManager is null)
		{
			GD.PushError("Enemy cannot return to pool: PoolManager reference missing!");
			QueueFree(); // Fallback: just delete if no pool manager
			return;
		}

		PoolManager.ReturnEnemy(this);
	}
}
