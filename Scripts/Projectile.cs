using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	public Vector2 Direction { get; private set; } = Vector2.Zero; // Make setter private
	public PackedScene SourceScene { get; set; } // Needed to return to correct pool

	[Export] public Sprite2D Sprite;
	[Export] public CpuParticles2D DestructionParticles;

	private bool active = false;
	private Timer lifeTimer;
	private Timer destructionTimer; // Timer for delay before returning to pool

	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DestructionDuration = 0.6f; // Should roughly match particle lifetime + buffer
	public const int ProjectileZIndex = 5; // Public constant for Z-index


	public override void _Ready()
	{
		// Connect signals using += syntax
		BodyEntered += OnBodyEntered;

		lifeTimer = GetNodeOrNull<Timer>("LifeTimer");
		if (lifeTimer is null)
		{
			GD.Print("Projectile: LifeTimer not found, creating one.");
			lifeTimer = new Timer { Name = "LifeTimer", OneShot = true };
			AddChild(lifeTimer);
		}
		// Connect only once
		if (!lifeTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLifeTimerTimeout)))
		{
			lifeTimer.Timeout += OnLifeTimerTimeout;
		}

		destructionTimer = GetNodeOrNull<Timer>("DestructionTimer");
		if (destructionTimer is null)
		{
			GD.Print("Projectile: DestructionTimer not found, creating one.");
			destructionTimer = new Timer { Name = "DestructionTimer", OneShot = true };
			AddChild(destructionTimer);
		}
		// Connect only once for ReturnToPool, careful not to double-connect in StartDestructionSequence
		if (!destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
		{
			destructionTimer.Timeout += ReturnToPool;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// Early return if not active or no direction
		if (!active || Direction == Vector2.Zero)
		{
			return;
		}

		GlobalPosition += Direction * Speed * (float)delta;
	}

	public override void _ExitTree()
	{
		// Ensure signals are disconnected if the node is valid
		if (IsInstanceValid(this))
		{
			BodyEntered -= OnBodyEntered;
		}

		if (lifeTimer is not null && IsInstanceValid(lifeTimer))
		{
			if (lifeTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLifeTimerTimeout)))
			{
				lifeTimer.Timeout -= OnLifeTimerTimeout;
			}
		}
		if (destructionTimer is not null && IsInstanceValid(destructionTimer))
		{
			if (destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
			{
				destructionTimer.Timeout -= ReturnToPool;
			}
		}
		base._ExitTree();
	}

	public void Setup(Vector2 startPosition, Vector2 direction, Texture2D spriteTexture = null, Color? particleColor = null)
	{
		GlobalPosition = startPosition;
		Direction = direction.Normalized();
		active = false; // Ensure not active until Activate() is called

		// Reset state fully before activation
		Visible = false; // Start invisible
		ProcessMode = ProcessModeEnum.Disabled; // Start disabled
		SetDeferred(Area2D.PropertyName.Monitoring, false); // Disable collision detection
		SetDeferred(Area2D.PropertyName.Monitorable, false);


		if (Sprite is not null)
		{
			if (spriteTexture is not null)
			{
				Sprite.Texture = spriteTexture; // Optional texture override
			}
			Sprite.Visible = true; // Keep sprite technically visible, parent controls overall visibility
		}

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false; // Ensure particles are not emitting
			if (particleColor.HasValue)
			{
				DestructionParticles.Color = particleColor.Value; // Optional color override
			}
			// Position particles locally or globally depending on desired effect
			DestructionParticles.Position = Vector2.Zero; // Reset local position
		}

		// Stop timers and ensure signals related to previous lifetime/destruction are cleared if needed
		lifeTimer?.Stop();
		destructionTimer?.Stop();
		// No need to disconnect here if connection is managed carefully
	}

	public void Activate()
	{
		if (active) // Prevent re-activation
		{
			GD.Print($"Projectile {GetInstanceId()}: Already active, ignoring Activate call.");
			return;
		}
		GD.Print($"Projectile {GetInstanceId()}: Activating.");

		active = true;
		Visible = true; // Make visible
		ProcessMode = ProcessModeEnum.Pausable; // Enable physics and processing

		// Enable collision detection using deferred calls for safety
		SetDeferred(Area2D.PropertyName.Monitoring, true);
		SetDeferred(Area2D.PropertyName.Monitorable, true);

		lifeTimer?.Start(DefaultLifetime); // Start lifetime timer
	}


	private void OnBodyEntered(Node2D body)
	{
		if (!active) // Only react if active
		{
			return;
		}

		if (body is Player player)
		{
			GD.Print($"Projectile {GetInstanceId()}: Hit Player.");
			player.TakeDamage(1); // Apply damage
			player.ApplyKnockback(Direction * KnockbackForce); // Apply knockback
			StartDestructionSequence(); // Start dying
		}
		// else if (body is Obstacle) // Example: Destroy on hitting walls
		// {
		//     GD.Print($"Projectile {GetInstanceId()}: Hit Obstacle.");
		//     StartDestructionSequence();
		// }
	}

	private void OnLifeTimerTimeout()
	{
		if (!active) // Check if already dying
		{
			return;
		}
		GD.Print($"Projectile {GetInstanceId()}: Life timer timeout.");
		StartDestructionSequence();
	}

	private void StartDestructionSequence()
	{
		if (!active) // Prevent starting destruction multiple times
		{
			GD.Print($"Projectile {GetInstanceId()}: Already destroying, ignoring StartDestructionSequence call.");
			return;
		}
		GD.Print($"Projectile {GetInstanceId()}: Starting destruction sequence.");

		active = false; // Mark as inactive/dying
		lifeTimer?.Stop(); // Stop lifetime timer

		// Disable physics, visibility, and collision using deferred calls
		CallDeferred(Node.MethodName.SetProcessMode, (int)ProcessModeEnum.Disabled);
		CallDeferred(CanvasItem.MethodName.SetVisible, false);
		CallDeferred(Area2D.PropertyName.Monitoring, false);
		CallDeferred(Area2D.PropertyName.Monitorable, false);

		Direction = Vector2.Zero; // Stop movement calculation

		// Play destruction particles if available
		if (DestructionParticles is not null)
		{
			// Make particles appear at the projectile's last known global position
			// Since the particle node might be a child, setting its GlobalPosition is correct.
			DestructionParticles.GlobalPosition = this.GlobalPosition;
			DestructionParticles.Restart(); // Emits particles (assumes one_shot = true)
			GD.Print($"Projectile {GetInstanceId()}: Started destruction particles.");
		}

		// Start the timer to delay returning to the pool
		if (destructionTimer is not null)
		{
			// Signal should already be connected from _Ready
			destructionTimer.Start(DestructionDuration);
			GD.Print($"Projectile {GetInstanceId()}: Started destruction timer ({DestructionDuration}s).");
		}
		else
		{
			// Fallback if timer node is missing
			GD.PrintErr($"Projectile {GetInstanceId()}: DestructionTimer node missing! Using temporary timer.");
			GetTree().CreateTimer(DestructionDuration, false, true).Timeout += ReturnToPool; // Args: time, process_always=false, process_pause=true
		}
	}

	private void ReturnToPool()
	{
		GD.Print($"Projectile {GetInstanceId()}: ReturnToPool called.");
		// Ensure particles are stopped before returning (might already be stopped if one-shot)
		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
		}

		// Stop the destruction timer explicitly
		destructionTimer?.Stop();

		// Use the new ProjectilePoolManager
		if (ProjectilePoolManager.Instance is not null)
		{
			ProjectilePoolManager.Instance.ReturnProjectileToPool(this);
			GD.Print($"Projectile {GetInstanceId()}: Returned to pool via ProjectilePoolManager.");
		}
		else
		{
			// Fallback if pool manager isn't available
			GD.PrintErr($"Projectile {GetInstanceId()}: Cannot return to pool. ProjectilePoolManager instance not found. Freeing.");
			QueueFree();
		}
	}

	// Called by ProjectilePoolManager before enqueueing
	public void ResetForPooling()
	{
		GD.Print($"Projectile {GetInstanceId()}: Resetting for pooling.");
		active = false;
		lifeTimer?.Stop();
		destructionTimer?.Stop();
		// Disconnecting signals is generally handled in _ExitTree or managed by checking connection status

		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled;

		// Ensure collision is off using deferred calls
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);

		// Reset sprite visibility/state if needed (though visibility is controlled by parent now)
		// if (Sprite is not null)
		// {
		// 	Sprite.Visible = true; // Or false depending on pooling strategy
		// }
		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false; // Ensure particles stopped
		}
		Direction = Vector2.Zero; // Reset direction
		GlobalPosition = Vector2.Zero; // Reset position
	}
}
