// Removed ScoreValue property.
// Moved score addition from Die() to TakeDamage(). Score added is now equal to the damage dealt.
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

	protected virtual int MaxHealth => 20;
	protected virtual int Damage => 1;
	protected virtual float Speed => 100f;
	protected virtual float DamageRadius => 50f;
	protected virtual float ProximityThreshold => 32f;
	protected virtual float KnockbackRecovery => 0.1f;
	protected virtual float AttackCooldown => 0.5f;
	protected virtual float KnockbackResistanceMultiplier => 1f;

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

		DeathTimer?.Stop();
		DamageCooldownTimer?.Stop();

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

		Health -= damage;
		HitAnimationPlayer?.Play("HitFlash");
		ShowDamageIndicator(damage);

		if (DamageParticles is not null)
		{
			DamageParticles.Emitting = true;
		}

		var worldNode = GetNode<World>("/root/World");
		if (worldNode is not null)
		{
			worldNode.AddScore(damage);
		}
		else
		{
			GD.PrintErr($"Could not find World node at /root/World to grant score from damage {damage}.");
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
