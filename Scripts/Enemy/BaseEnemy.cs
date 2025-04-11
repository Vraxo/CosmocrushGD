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
	protected bool IsDead = false;
	protected bool CanAttack = true;
	protected Vector2 KnockbackVelocity = Vector2.Zero;
	protected Player TargetPlayer;

	public EnemyPoolManager PoolManager { get; set; }
	public PackedScene SourceScene { get; set; }

	protected virtual int MaxHealth => 20;
	protected virtual int BaseDamage => 1;
	protected virtual float MovementSpeed => 100f;
	protected virtual float AttackRadius => 50f;
	protected virtual float PlayerProximityThreshold => 32f;
	protected virtual float KnockbackRecoverySpeed => 0.1f;
	protected virtual float AttackInterval => 0.5f;

	public override void _Ready()
	{
		// Note: TargetPlayer might become invalid if the player dies and respawns.
		// Consider acquiring the player reference more dynamically if needed.
		TargetPlayer = GetNode<Player>("/root/World/Player");

		if (DeathTimer is not null)
		{
			DeathTimer.Timeout += ReturnToPool;
		}

		if (DamageCooldownTimer is not null)
		{
			DamageCooldownTimer.WaitTime = AttackInterval;
			DamageCooldownTimer.Timeout += OnAttackCooldownTimeout;
		}
	}

	public virtual void ResetState(Vector2 spawnPosition)
	{
		GlobalPosition = spawnPosition;
		Health = MaxHealth;
		IsDead = false;
		Velocity = Vector2.Zero;
		KnockbackVelocity = Vector2.Zero;
		CanAttack = true;

		Visible = true;

		// *** Explicitly re-enable processing ***
		SetProcess(true);
		SetPhysicsProcess(true);
		// *** End fix ***

		if (Sprite is not null)
		{
			Sprite.Visible = true;
			if (Sprite.Material is ShaderMaterial shaderMaterial)
			{
				shaderMaterial.SetShaderParameter("flash_value", 0.0f);
			}
		}

		if (Collider is not null)
		{
			// Ensure the collider node itself is not hidden or disabled inadvertently
			Collider.Visible = true; // Collision shapes often don't need rendering, but ensure node is active
			Collider.Disabled = false;
		}

		// *** Set Navigation Map Here ***
		if (Navigator is not null)
		{
			Rid navigationMap = GetWorld2D().NavigationMap;
			if (navigationMap.IsValid)
			{
				Navigator.SetNavigationMap(navigationMap);
			}
			else
			{
				GD.PrintErr($"BaseEnemy ({Name}): Could not get valid navigation map in ResetState.");
			}
			// Reset pathfinding state if needed
			Navigator.TargetPosition = GlobalPosition; // Avoid immediate movement calculation based on old target
		}
		// *** End fix ***


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

		HitAnimationPlayer?.Stop(true);
	}

	public override void _Process(double delta)
	{
		// Processing stops when dead via SetProcess(false) in Die()
		UpdateSpriteDirection();
		AttemptAttack();
	}

	public override void _PhysicsProcess(double delta)
	{
		// Physics processing stops when dead via SetPhysicsProcess(false) in Die()
		KnockbackVelocity = KnockbackVelocity.Lerp(Vector2.Zero, KnockbackRecoverySpeed);
		Velocity = CalculateMovementVelocity() + KnockbackVelocity;
		MoveAndSlide();
	}

	protected virtual Vector2 CalculateMovementVelocity()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			// Attempt to re-acquire player if null, common in pooling scenarios
			TargetPlayer = GetNodeOrNull<Player>("/root/World/Player");
			if (TargetPlayer is null)
			{
				return Vector2.Zero; // Still no player
			}
		}

		if (Navigator is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): Navigator is null in CalculateMovementVelocity.");
			return Vector2.Zero;
		}


		float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

		// Stop moving if very close to the player
		if (distanceToPlayer <= PlayerProximityThreshold)
		{
			Navigator.TargetPosition = GlobalPosition; // Stop pathfinding
			return Vector2.Zero;
		}

		// Update target position for the navigator
		Navigator.TargetPosition = TargetPlayer.GlobalPosition;

		// Removed map setting from here

		if (Navigator.IsNavigationFinished() || !Navigator.IsTargetReachable())
		{
			// Maybe add basic direct movement if stuck? Or just stop.
			// GD.Print($"BaseEnemy ({Name}): Navigation finished or target unreachable.");
			return Vector2.Zero;
		}

		Vector2 nextPosition = Navigator.GetNextPathPosition();
		Vector2 direction = (nextPosition - GlobalPosition).Normalized();
		return direction * MovementSpeed;
	}

	public void TakeDamage(int damageAmount)
	{
		if (IsDead || damageAmount <= 0)
		{
			return;
		}

		Health -= damageAmount;
		HitAnimationPlayer?.Play("HitFlash");
		ShowDamageIndicator(damageAmount);

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
		if (IsDead)
		{
			return;
		}

		KnockbackVelocity += force;
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
		if (IsDead)
		{
			return;
		}

		IsDead = true;
		Velocity = Vector2.Zero;
		KnockbackVelocity = Vector2.Zero;

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

		DeathTimer?.Start();

		// Disable processing immediately
		SetProcess(false);
		SetPhysicsProcess(false);

		StatisticsManager.Instance.IncrementScore(1);
	}

	private void ShowDamageIndicator(int damageAmount)
	{
		if (DamageIndicatorScene is null)
		{
			return;
		}

		DamageIndicator indicator = DamageIndicatorScene.Instantiate<DamageIndicator>();
		indicator.Text = damageAmount.ToString();
		indicator.Health = Health;
		indicator.MaxHealth = MaxHealth;

		float verticalOffset = (Sprite?.Texture is not null)
			? -Sprite.Texture.GetHeight() / 2f * Sprite.Scale.Y
			: -20f;

		indicator.Position = new Vector2(0, verticalOffset);

		AddChild(indicator);
	}

	private void ReturnToPool()
	{
		if (PoolManager is null)
		{
			GD.PushError($"Enemy ({Name}) cannot return to pool: PoolManager reference missing! Freeing node instead.");
			QueueFree();
			return;
		}

		PoolManager.ReturnEnemy(this);
	}

	private void OnAttackCooldownTimeout()
	{
		CanAttack = true;
	}
}
