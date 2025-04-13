using Godot;
using System;

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

	protected virtual int ScoreValue => 1;
	protected virtual int MaxHealth => 20;
	protected virtual int Damage => 1;
	protected virtual float Speed => 100f;
	protected virtual float DamageRadius => 50f;
	protected virtual float ProximityThreshold => 32f; // How close to get before attacking
	protected virtual float NavigationStopThreshold => 10f; // How close to get before stopping movement attempts
	protected virtual float KnockbackRecovery => 0.1f;
	protected virtual float AttackCooldown => 0.5f;
	protected virtual float KnockbackResistanceMultiplier => 1.0f;

	public override void _Ready()
	{
		TargetPlayer = GetNode<Player>("/root/World/Player");

		DeathTimer.Timeout += ReturnToPool;
		DamageCooldownTimer.WaitTime = AttackCooldown;
		DamageCooldownTimer.Timeout += () => CanShoot = true;

		if (Navigator is not null)
		{
			// Configure agent properties
			Navigator.PathfindingAlgorithm = NavigationPathQueryParameters2D.PathfindingAlgorithmEnum.Astar;
			Navigator.PathPostprocessing = NavigationPathQueryParameters2D.PathPostProcessing.Edgecentered;
			Navigator.MaxSpeed = this.Speed;
			Navigator.TargetDesiredDistance = NavigationStopThreshold; // Use stop threshold
			Navigator.PathDesiredDistance = 5.0f; // How close to corners
		}
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

		if (Navigator is not null)
		{
			Navigator.TargetPosition = GlobalPosition; // Initialize target to current spot
			Navigator.MaxSpeed = this.Speed; // Update speed in case it changed
		}

		ProcessMode = ProcessModeEnum.Inherit;
		SetPhysicsProcessDeferred(true);
	}

	public override void _Process(double delta)
	{
		if (Dead)
		{
			return;
		}

		UpdateSpriteDirection();
		AttemptAttack(); // Attack logic remains in _Process
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Dead)
		{
			// If dead, only apply knockback decay
			Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);
			Velocity = Knockback;
			MoveAndSlide(); // Still need MoveAndSlide for knockback
			return;
		}

		// Apply knockback decay
		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery);

		Vector2 desiredVelocity = Vector2.Zero;

		if (Navigator is not null)
		{
			Rid currentMap = Navigator.GetNavigationMap();
			if (NavigationServer2D.MapIsActive(currentMap))
			{
				// Update the agent's target position
				RequestNavigationTargetUpdate();

				// Check if the agent has reached its current target
				if (!Navigator.IsNavigationFinished())
				{
					// Get the next point on the path
					Vector2 nextPathPosition = Navigator.GetNextPathPosition();
					// Calculate the direction towards the next point
					Vector2 directionToNextPoint = (nextPathPosition - GlobalPosition).Normalized();
					// Set the desired velocity based on the agent's speed
					desiredVelocity = directionToNextPoint * Navigator.MaxSpeed;
				}
				// If IsNavigationFinished() is true, desiredVelocity remains Zero, stopping agent movement
			}
			else
			{
				// Map not active, don't calculate navigation velocity
				// GD.Print($"Map {currentMap} not active for {Name}.");
			}
		}
		else
		{
			GD.PrintErr($"{Name}: Navigator is null!");
		}

		// Combine desired navigation velocity with knockback
		Velocity = desiredVelocity + Knockback;

		// Apply the final velocity
		MoveAndSlide();
	}

	// Updates the target for the NavigationAgent
	protected virtual void RequestNavigationTargetUpdate()
	{
		if (Navigator is null || Dead || TargetPlayer is null || !IsInstanceValid(TargetPlayer))
		{
			if (Navigator is not null)
			{
				// Stop the agent if target is invalid
				Navigator.TargetPosition = GlobalPosition;
			}
			return;
		}

		// Set the agent's target to the player's position
		Navigator.TargetPosition = TargetPlayer.GlobalPosition;
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

		if (DamageParticles != null)
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
		if (Sprite is null)
		{
			return;
		}

		// Flip based on velocity if moving significantly, otherwise face player
		if (Velocity.LengthSquared() > 1.0f) // Threshold to avoid flipping when stopping
		{
			Sprite.FlipH = Velocity.X < 0;
		}
		else if (TargetPlayer is not null && IsInstanceValid(TargetPlayer)) // Check TargetPlayer validity again
		{
			Sprite.FlipH = GlobalPosition.X > TargetPlayer.GlobalPosition.X;
		}
	}


	protected abstract void AttemptAttack();

	protected virtual void Die()
	{
		if (Dead)
		{
			return;
		}

		Dead = true;
		Velocity = Vector2.Zero; // Reset velocity immediately
		Knockback = Vector2.Zero;
		SetPhysicsProcess(false); // Disable physics

		if (Navigator is not null)
		{
			Navigator.TargetPosition = GlobalPosition; // Stop agent
		}

		if (Collider != null)
		{
			Collider.Disabled = true;
		}

		if (Sprite != null) Sprite.Visible = false;
		if (DeathParticles != null) DeathParticles.Emitting = true;

		var worldNode = GetNode<World>("/root/World");
		if (worldNode != null)
		{
			worldNode.AddScore(ScoreValue);
		}
		else
		{
			GD.PrintErr("Could not find World node at /root/World to grant score.");
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

		// Ensure state is fully reset before returning
		Dead = true; // Mark as dead to stop any lingering processing
		Velocity = Vector2.Zero;
		Knockback = Vector2.Zero;
		SetPhysicsProcess(false);
		if (Navigator is not null)
		{
			Navigator.TargetPosition = GlobalPosition; // Stop agent
		}
		PoolManager.ReturnEnemy(this);
	}


	public override void _ExitTree()
	{
		// No signals to disconnect now
		base._ExitTree();
	}

	private void SetPhysicsProcessDeferred(bool enable)
	{
		CallDeferred(Node.MethodName.SetPhysicsProcess, enable);
	}
}
