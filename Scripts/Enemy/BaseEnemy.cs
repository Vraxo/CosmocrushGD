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
	private bool _navigationMapNeedsUpdate = true; // Flag to control map setting

	public EnemyPoolManager PoolManager { get; set; }
	public PackedScene SourceScene { get; set; }

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

	// Modified ResetState to remove navigation map RID parameter
	public virtual void ResetState(Vector2 spawnPosition)
	{
		GlobalPosition = spawnPosition;
		Health = MaxHealth;
		Dead = false;
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;
		CanShoot = true;
		_navigationMapNeedsUpdate = true; // Signal that map needs to be set

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

		// Clear any previous navigation path
		if (Navigator is not null)
		{
			Navigator.TargetPosition = GlobalPosition;
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

		// Attempt to set the navigation map if needed
		if (_navigationMapNeedsUpdate)
		{
			if (Navigator is not null && IsInsideTree())
			{
				Rid currentWorldMap = GetWorld2D().NavigationMap;
				if (currentWorldMap.IsValid)
				{
					Navigator.SetNavigationMap(currentWorldMap);
					// GD.Print($"{Name} successfully set navigation map: {currentWorldMap}"); // Optional debug log
					_navigationMapNeedsUpdate = false; // Map is set, don't try again until reset
				}
				else
				{
					// Map not ready yet, will retry next physics frame
					// GD.Print($"{Name} waiting for valid navigation map..."); // Optional debug log
				}
			}
			else
			{
				// Navigator null or not in tree, cannot set map yet
				_navigationMapNeedsUpdate = true;
			}
		}


		// Only calculate movement if the map has been successfully set
		Vector2 movement = Vector2.Zero;
		if (!_navigationMapNeedsUpdate)
		{
			movement = CalculateMovement();
		}

		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);
		Velocity = movement + Knockback;
		MoveAndSlide();
	}

	protected virtual Vector2 CalculateMovement()
	{
		// Basic checks first
		if (_navigationMapNeedsUpdate || TargetPlayer is null || !IsInstanceValid(TargetPlayer) || Navigator is null)
		{
			return Vector2.Zero;
		}

		float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
		if (distanceToPlayer <= ProximityThreshold)
		{
			Navigator.TargetPosition = GlobalPosition; // Stop moving
			return Vector2.Zero;
		}

		// Check if the map assigned to the agent is valid and active
		Rid currentMap = Navigator.GetNavigationMap();
		if (!currentMap.IsValid || !NavigationServer2D.MapIsActive(currentMap))
		{
			// GD.Print($"Navigation map invalid or inactive for {Name}. RID: {currentMap}"); // Optional: Debug log
			_navigationMapNeedsUpdate = true; // Attempt to re-acquire map next frame
			return Vector2.Zero;
		}

		Navigator.TargetPosition = TargetPlayer.GlobalPosition;

		if (Navigator.IsNavigationFinished() || Navigator.IsTargetReached())
		{
			return Vector2.Zero;
		}

		// Check reachability *after* setting target position
		if (!Navigator.IsTargetReachable())
		{
			// Target might be outside the navmesh, maybe stop or use direct path?
			// For now, just stop movement calculated via navigation.
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

		var worldNode = GetNode<World>("/root/World");
		if (worldNode is not null)
		{
			worldNode.AddScore(damage);
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

		float verticalOffset = -20f;
		if (Sprite is not null && Sprite.Texture is not null)
		{
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Scale.Y;
		}

		indicator.Position = new(0, verticalOffset);


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
	}
}
