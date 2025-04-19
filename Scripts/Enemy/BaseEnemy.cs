using Godot;
using System;

namespace CosmocrushGD;

public partial class BaseEnemy : CharacterBody2D
{
	[Signal]
	public delegate void EnemyDiedEventHandler(BaseEnemy enemy);

	[Export] protected NavigationAgent2D Navigator;
	[Export] protected Sprite2D Sprite;
	[Export] protected Timer DeathTimer;
	[Export] protected Timer DamageCooldownTimer;
	[Export] protected Timer NavigationUpdateTimer; // Added Export for the new timer
	[Export] public CollisionShape2D Collider;
	[Export] public CpuParticles2D DamageParticles;
	[Export] public CpuParticles2D DeathParticles;
	[Export] protected PackedScene DamageIndicatorScene;
	[Export] protected AnimationPlayer HitAnimationPlayer;

	protected int Health;
	protected bool Dead = false;
	protected bool CanShoot = true;
	protected Vector2 Knockback = Vector2.Zero;

	private bool _navigationMapNeedsUpdate = true;
	private bool _navigationMapValidCheck = false;
	private bool _canUpdateNavigationTarget = true; // Flag to control target updates

	public EnemyPoolManager PoolManager { get; set; }
	public PackedScene SourceScene { get; set; }
	public Player TargetPlayer { get; set; }
	private Action damageCooldownTimeoutAction;


	protected virtual float KnockbackResistanceMultiplier => 0.1f;
	protected virtual int MaxHealth => 20;
	protected virtual int Damage => 1;
	protected virtual float Speed => 100f;
	protected virtual float DamageRadius => 50f;
	protected virtual float ProximityThreshold => 32f;
	protected virtual float KnockbackRecovery => 0.1f;
	protected virtual float AttackCooldown => 0.5f;
	protected virtual float NavigationUpdateInterval => 0.5f; // Default interval, can be overridden

	public override void _Ready()
	{
		damageCooldownTimeoutAction = () => CanShoot = true;

		if (DeathTimer is not null)
		{
			DeathTimer.Timeout += ReturnToPool;
		}
		if (DamageCooldownTimer is not null)
		{
			DamageCooldownTimer.WaitTime = AttackCooldown;
			DamageCooldownTimer.Timeout += damageCooldownTimeoutAction;
		}
		if (NavigationUpdateTimer is not null) // Setup the new timer
		{
			NavigationUpdateTimer.WaitTime = NavigationUpdateInterval;
			NavigationUpdateTimer.OneShot = false; // Make it repeating
			NavigationUpdateTimer.Timeout += OnNavigationUpdateTimeout;
		}
	}

	// New method called by the NavigationUpdateTimer
	private void OnNavigationUpdateTimeout()
	{
		_canUpdateNavigationTarget = true;
	}

	public virtual void ResetState(Vector2 spawnPosition)
	{
		if (TargetPlayer is null)
		{
		}

		GlobalPosition = spawnPosition;
		Health = MaxHealth;
		Dead = false;
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;
		CanShoot = true;
		_navigationMapNeedsUpdate = true;
		_navigationMapValidCheck = false;
		_canUpdateNavigationTarget = true; // Allow immediate update on reset

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
		NavigationUpdateTimer?.Start(); // Start the navigation timer
		HitAnimationPlayer?.Stop(true);

		if (Navigator is not null)
		{
		}

		ProcessMode = ProcessModeEnum.Inherit;
		SetPhysicsProcess(true);
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		if (Dead || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
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

		if (_navigationMapNeedsUpdate)
		{
			if (Navigator is not null && IsInsideTree())
			{
				var worldMap = GetWorld2D()?.NavigationMap ?? default;
				if (worldMap.IsValid)
				{
					Navigator.SetNavigationMap(worldMap);
					_navigationMapNeedsUpdate = false;
					Navigator.TargetPosition = GlobalPosition; // Set initial target
					_canUpdateNavigationTarget = true; // Allow update after map set
				}
				else
				{
				}
			}
			else
			{
				_navigationMapNeedsUpdate = true;
			}
		}

		Vector2 movement = Vector2.Zero;
		if (!_navigationMapNeedsUpdate && TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			movement = CalculateMovement();
		}

		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);
		Velocity = movement + Knockback;
		MoveAndSlide();
	}

	protected virtual Vector2 CalculateMovement()
	{
		if (TargetPlayer is null || !IsInstanceValid(TargetPlayer) || Navigator is null)
		{
			return Vector2.Zero;
		}

		float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
		if (distanceToPlayer <= ProximityThreshold)
		{
			// Only update target if allowed, to prevent rapid recalculation when close
			if (_canUpdateNavigationTarget)
			{
				Navigator.TargetPosition = GlobalPosition; // Stop moving when close
				_canUpdateNavigationTarget = false; // Consume the update permission
			}
			return Vector2.Zero;
		}

		Rid currentMap = Navigator.GetNavigationMap();
		bool isMapValid = currentMap.IsValid && NavigationServer2D.MapIsActive(currentMap);

		// Update the target position only if the timer allows and the map is valid
		if (_canUpdateNavigationTarget && isMapValid)
		{
			Navigator.TargetPosition = TargetPlayer.GlobalPosition;
			_canUpdateNavigationTarget = false; // We just updated the target
		}
		else if (!isMapValid) // Handle invalid map case
		{
			if (!_navigationMapValidCheck)
			{
				_navigationMapValidCheck = true;
			}
			_navigationMapNeedsUpdate = true; // Try to re-acquire map next frame
			return Vector2.Zero; // Don't move if map is bad
		}
		_navigationMapValidCheck = isMapValid; // Update check flag based on current validity


		if (Navigator.IsNavigationFinished() || Navigator.IsTargetReached())
		{
			return Vector2.Zero; // Reached target or finished path
		}

		// Always get direction towards the next point on the *current* path
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


		if (World.Instance is not null)
		{
			World.Instance.AddScore(damage);
		}
		else
		{
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

	protected virtual void AttemptAttack()
	{
	}

	protected virtual void Die()
	{
		if (Dead)
		{
			return;
		}
		Dead = true;
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;
		SetPhysicsProcess(false);
		SetProcess(false);
		NavigationUpdateTimer?.Stop(); // Stop the navigation timer

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
		EmitSignal(SignalName.EnemyDied, this);
		DeathTimer?.Start(); // Death timer handles returning to pool
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
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Scale.Y - 5f;
		}
		indicator.Position = new(0, verticalOffset);
		AddChild(indicator);
	}


	private void ReturnToPool()
	{
		if (PoolManager is null)
		{
			QueueFree();
			return;
		}
		TargetPlayer = null;
		NavigationUpdateTimer?.Stop(); // Ensure timer is stopped before returning
		PoolManager.ReturnEnemy(this);
	}

	public override void _ExitTree()
	{
		if (DeathTimer is not null && IsInstanceValid(DeathTimer) && DeathTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
		{
			DeathTimer.Timeout -= ReturnToPool;
		}
		if (DamageCooldownTimer is not null && IsInstanceValid(DamageCooldownTimer) && damageCooldownTimeoutAction is not null && DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(damageCooldownTimeoutAction)))
		{
			DamageCooldownTimer.Timeout -= damageCooldownTimeoutAction;
		}
		if (NavigationUpdateTimer is not null && IsInstanceValid(NavigationUpdateTimer) && NavigationUpdateTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnNavigationUpdateTimeout)))
		{
			NavigationUpdateTimer.Timeout -= OnNavigationUpdateTimeout; // Disconnect the new timer signal
		}
		base._ExitTree();
	}
}
