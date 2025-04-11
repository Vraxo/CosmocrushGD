using System.Runtime.InteropServices;
using Godot;

namespace CosmocrushGD;

public abstract partial class BaseEnemy : CharacterBody2D
{
	[Signal]
	public delegate void EnemyKilledEventHandler(int scoreValue);

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

	// New properties for pooling
	public EnemyPoolManager PoolManager { get; set; }
	public PackedScene SourceScene { get; set; }


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
		// Health is reset in ResetState
		// Connect signals once
		DeathTimer.Timeout += ReturnToPool; // Changed from OnDeathTimeout
		DamageCooldownTimer.WaitTime = AttackCooldown;
		DamageCooldownTimer.Timeout += () => CanShoot = true;
	}

	// New method to reset the enemy state when reused
	public virtual void ResetState(Vector2 spawnPosition)
	{
		GlobalPosition = spawnPosition;
		Health = MaxHealth;
		Dead = false;
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;
		CanShoot = true; // Reset attack capability

		// Re-enable components
		Visible = true; // Make sure the root node is visible
		Sprite.Visible = true;
		Collider.Disabled = false;

		// Reset particles
		DamageParticles.Emitting = false;
		DamageParticles.Restart();
		DeathParticles.Emitting = false;
		DeathParticles.Restart();

		// Reset timers if needed (stop them if they might be running)
		DeathTimer.Stop();
		DamageCooldownTimer.Stop();

		// Ensure animations are stopped or reset if applicable
		if (HitAnimationPlayer.IsPlaying())
		{
			HitAnimationPlayer.Stop(true); // Reset animation
		}

		// Re-enable processing
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
			TargetPlayer = null; // Clear invalid reference
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

		// Optimization: Avoid frequent NavigationServer calls if possible or not needed every frame
		// However, standard use requires checking the map
		Rid mapRid = NavigationServer2D.AgentGetMap(Navigator.GetRid());
		if (!mapRid.IsValid)
		{
			return Vector2.Zero;
		}
		// Consider if you need iteration ID check every frame

		if (Navigator.IsNavigationFinished())
		{
			return Vector2.Zero; // Or maybe direct movement if close enough but not finished?
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

		Health -= damage;
		HitAnimationPlayer?.Play("HitFlash");
		ShowDamageIndicator(damage);

		DamageParticles.Emitting = true;

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

		Knockback += force;
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer) || Sprite == null)
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

		// Add score when the enemy dies
		//GetNode<ScoreManager>("/root/ScoreManager")?.AddScore(1); // Use null-conditional for safety
		EmitSignal(SignalName.EnemyKilled, 1); // Emit signal with score value of 1

		Dead = true;
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;

		Collider.Disabled = true;
		Sprite.Visible = false;
		DeathParticles.Emitting = true;

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
		// Optional: Check if Health/MaxHealth properties exist before setting
		indicator.Health = Health;
		indicator.MaxHealth = MaxHealth;
		if (Sprite is not null && Sprite.Texture is not null)
		{
			indicator.Position = new(0, -Sprite.Texture.GetHeight() / 2f);
		}
		else
		{
			indicator.Position = new(0, -20);
		}

		AddChild(indicator);
	}

	private void ReturnToPool()
	{
		if (PoolManager is null)
		{
			GD.PushError("Enemy cannot return to pool: PoolManager reference missing!");
			QueueFree();
			return;
		}
		
		PoolManager.ReturnEnemy(this);
		return;
	}
}
