using Godot;
using System;

namespace CosmocrushGD;

public partial class BaseEnemy : CharacterBody2D
{
	[Signal]
	public delegate void EnemyDiedEventHandler(BaseEnemy enemy); // Pass self on death

	[Export] protected Sprite2D Sprite;
	[Export] protected Timer DeathTimer; // Timer before QueueFree
	[Export] protected Timer DamageCooldownTimer; // Timer between attacks
	[Export] public CollisionShape2D Collider;
	[Export] protected AnimationPlayer HitAnimationPlayer; // For hit flash

	// Scene references for effects - these should be assigned in the inspector per enemy type
	[Export] private PackedScene damageParticleEffectScene;
	[Export] private PackedScene deathParticleEffectScene;
	[Export] private AudioStream damageAudio; // Sound played when taking damage

	protected int Health;
	protected bool Dead = false; // Flag to prevent actions after death
	protected bool CanShoot = true; // Flag to control attack rate
	protected Vector2 Knockback = Vector2.Zero; // Current knockback velocity
	protected Player TargetPlayer; // Reference to the player

	// --- Enemy Stats (Virtual properties for easy override in derived classes) ---
	protected virtual float KnockbackResistanceMultiplier => 0.5f; // How much knockback is resisted (0=none, 1=full resist)
	protected virtual int MaxHealth => 20;
	protected virtual int Damage => 1; // Damage dealt per attack
	protected virtual float Speed => 100f; // Movement speed
	protected virtual float DamageRadius => 50f; // Range within which melee attacks can hit
	protected virtual float ProximityThreshold => 32f; // How close the enemy tries to get before stopping (for melee)
	protected virtual float KnockbackRecovery => 0.1f; // How quickly knockback decays (higher = faster recovery)
	protected virtual float AttackCooldown => 0.5f; // Time between attacks
	protected virtual Color ParticleColor => Colors.White; // Default particle color
	protected virtual float MeleeKnockbackForce => 500f; // Force applied to player on melee hit


	public override void _Ready()
	{
		// Find the player node - essential for AI behavior
		TargetPlayer = GetNode<Player>("/root/World/Player"); // Assumes player is direct child of World root node
		if (TargetPlayer is null)
		{
			GD.PrintErr($"BaseEnemy ({Name}): Could not find Player node at /root/World/Player. Disabling AI.");
			SetProcess(false); // Disable AI logic
			SetPhysicsProcess(false); // Disable movement/physics
			return; // Stop initialization
		}

		// Connect timer signals using +=
		if (DeathTimer is not null)
		{
			if (!DeathTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDeathTimerTimeout)))
			{
				DeathTimer.Timeout += OnDeathTimerTimeout;
			}
		}

		if (DamageCooldownTimer is not null)
		{
			DamageCooldownTimer.WaitTime = AttackCooldown; // Set cooldown duration
			if (!DamageCooldownTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnDamageCooldownTimerTimeout)))
			{
				DamageCooldownTimer.Timeout += OnDamageCooldownTimerTimeout;
			}
		}

		Health = MaxHealth; // Initialize health
	}

	public override void _Process(double delta)
	{
		if (Dead) // Don't process AI if dead
		{
			return;
		}

		UpdateSpriteDirection(); // Flip sprite based on velocity/target
		AttemptAttack(); // Try to perform attack action
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Dead)
		{
			// Apply knockback decay even when dead, then stop moving
			if (Knockback.LengthSquared() > 0.1f)
			{
				// Faster decay when dead
				Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery * 2.0f * (float)delta);
				Velocity = Knockback;
				MoveAndSlide(); // Apply remaining knockback
			}
			else if (Velocity != Vector2.Zero) // Stop completely once knockback is negligible
			{
				Velocity = Vector2.Zero;
			}
			return; // No other physics processing when dead
		}

		// Apply knockback decay
		Knockback = Knockback.Lerp(Vector2.Zero, KnockbackRecovery); // Regular decay

		Vector2 desiredMovement = Vector2.Zero;

		// Calculate movement towards player if player exists and is valid
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Vector2 directionToPlayer = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			float distanceToPlayer = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);

			// Move only if further than the proximity threshold
			if (distanceToPlayer > ProximityThreshold)
			{
				desiredMovement = directionToPlayer * Speed;
			}
		}

		Velocity = desiredMovement + Knockback; // Final velocity is desired movement + knockback

		// Use MoveAndSlide for physics collision handling
		if (Velocity.LengthSquared() > 0.01f) // Only move if velocity is significant
		{
			MoveAndSlide();
		}
		else if (Velocity != Vector2.Zero) // Ensure velocity becomes exactly zero when stopping
		{
			Velocity = Vector2.Zero;
		}
	}

	public override void _ExitTree()
	{
		// Stop timers and disconnect signals cleanly
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

	public void TakeDamage(int damage)
	{
		if (Dead) // Cannot take damage if already dead
		{
			return;
		}

		PlayDamageSound(); // Play sound effect

		Health -= damage;
		Health = Math.Max(Health, 0); // Clamp health to minimum 0
		HitAnimationPlayer?.Play("HitFlash"); // Play visual hit indicator
		ShowDamageIndicator(damage, Health, MaxHealth); // Show floating damage number

		// Add score to the world based on damage dealt
		var worldNode = GetNode<World>("/root/World"); // Find the World node
		worldNode?.AddScore(damage); // Call AddScore method

		// Spawn damage particles using the ParticlePoolManager
		if (damageParticleEffectScene is not null && ParticlePoolManager.Instance is not null)
		{
			ParticlePoolManager.Instance.GetParticleEffect(damageParticleEffectScene, GlobalPosition, ParticleColor);
		}
		else if (damageParticleEffectScene is null)
		{
			// GD.Print($"BaseEnemy ({Name}): Damage particle effect scene not assigned.");
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): ParticlePoolManager instance not found.");
		}


		if (Health <= 0) // Check for death
		{
			Die();
		}
	}

	public void ApplyKnockback(Vector2 force)
	{
		if (Dead) // Cannot be knocked back if dead
		{
			return;
		}
		Knockback += force * KnockbackResistanceMultiplier; // Apply knockback scaled by resistance
	}

	protected virtual void UpdateSpriteDirection()
	{
		if (Sprite is null)
		{
			return;
		}

		// Flip based on horizontal velocity primarily
		if (Math.Abs(Velocity.X) > 0.1f)
		{
			Sprite.FlipH = Velocity.X < 0;
		}
		// If not moving horizontally, flip based on player position
		else if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			Sprite.FlipH = GlobalPosition.X > TargetPlayer.GlobalPosition.X;
		}
	}

	protected virtual void AttemptAttack()
	{
		// Conditions for attacking
		if (!CanShoot || TargetPlayer is null || !IsInstanceValid(TargetPlayer) || Dead)
		{
			return;
		}

		// Check distance for melee attacks
		float distance = GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition);
		if (distance > DamageRadius)
		{
			return; // Too far to attack
		}

		PerformAttackAction(); // Execute the specific attack logic
		CanShoot = false; // Prevent immediate re-attack
		DamageCooldownTimer?.Start(); // Start cooldown timer
	}

	// Base attack action (for melee enemies)
	protected virtual void PerformAttackAction()
	{
		if (TargetPlayer is not null && IsInstanceValid(TargetPlayer))
		{
			TargetPlayer.TakeDamage(Damage); // Damage the player
			Vector2 knockbackDir = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
			TargetPlayer.ApplyKnockback(knockbackDir * MeleeKnockbackForce); // Apply knockback
		}
	}

	protected virtual void Die()
	{
		if (Dead) // Prevent dying multiple times
		{
			return;
		}
		Dead = true;
		EmitSignal(SignalName.EnemyDied, this); // Notify listeners (like World)
		SetPhysicsProcess(true); // Keep physics active briefly for knockback
		SetProcess(false); // Disable AI processing
		Collider?.CallDeferred(CollisionShape2D.MethodName.SetDisabled, true); // Disable collisions safely

		if (Sprite is not null)
		{
			Sprite.Visible = false; // Hide sprite
		}

		// Spawn death particles using the ParticlePoolManager
		if (deathParticleEffectScene is not null && ParticlePoolManager.Instance is not null)
		{
			ParticlePoolManager.Instance.GetParticleEffect(deathParticleEffectScene, GlobalPosition, ParticleColor);
		}
		else if (deathParticleEffectScene is null)
		{
			// GD.Print($"BaseEnemy ({Name}): Death particle effect scene not assigned.");
		}
		else
		{
			GD.PrintErr($"BaseEnemy ({Name}): ParticlePoolManager instance not found for death particles.");
		}


		DeathTimer?.Start(); // Start timer for final removal
		if (DeathTimer is null) // Fallback if timer is missing
		{
			GD.PrintErr($"BaseEnemy ({Name}): DeathTimer node not found! Using temporary timer.");
			GetTree().CreateTimer(1.0).Timeout += QueueFree;
		}
	}

	private void ShowDamageIndicator(int damage, int currentHealth, int maxHealth)
	{
		// Use the DamageIndicatorPoolManager
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

		// Calculate vertical offset above the sprite
		float verticalOffset = -20f; // Default offset
		if (Sprite is not null && Sprite.Texture is not null)
		{
			// Position above the scaled sprite texture
			verticalOffset = -Sprite.Texture.GetHeight() / 2f * Sprite.Scale.Y - 10f;
		}

		Vector2 globalStartPosition = GlobalPosition + new Vector2(0, verticalOffset);

		// Setup the indicator with damage info and position
		indicator.Setup(damage, currentHealth, maxHealth, globalStartPosition);
	}

	private void OnDamageCooldownTimerTimeout()
	{
		CanShoot = true; // Allow attacking again
	}

	private void OnDeathTimerTimeout()
	{
		QueueFree(); // Remove the enemy node from the scene tree
	}

	private void PlayDamageSound()
	{
		// Play sound using GlobalAudioPlayer
		if (damageAudio is not null && GlobalAudioPlayer.Instance is not null)
		{
			// Play at the enemy's position
			GlobalAudioPlayer.Instance.PlaySound2D(damageAudio, GlobalPosition);
		}
	}
}
