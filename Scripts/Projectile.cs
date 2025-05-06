using Godot;

namespace CosmocrushGD;

public partial class Projectile : Area2D
{
	public const int ProjectileZIndex = 5;

	private bool active = false;
	private Timer lifeTimer;
	private Timer destructionTimer;

	[Export] public Sprite2D Sprite;
	[Export] public CpuParticles2D DestructionParticles;

	public Vector2 Direction { get; private set; } = Vector2.Zero;
	public PackedScene SourceScene { get; set; }

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		lifeTimer = GetNodeOrNull<Timer>("LifeTimer");
		if (lifeTimer is null)
		{
			GD.Print("Projectile: LifeTimer not found, creating one.");
			lifeTimer = new Timer { Name = "LifeTimer", OneShot = true };
			AddChild(lifeTimer);
		}
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
		if (!destructionTimer.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)))
		{
			destructionTimer.Timeout += ReturnToPool;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!active || Direction == Vector2.Zero)
		{
			return;
		}

		var movement = Direction * Speed * (float)delta;
		GlobalPosition += movement;
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(this))
		{
			BodyEntered -= OnBodyEntered;
		}

		if (lifeTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(OnLifeTimerTimeout)) ?? false)
		{
			lifeTimer.Timeout -= OnLifeTimerTimeout;
		}

		if (destructionTimer?.IsConnected(Timer.SignalName.Timeout, Callable.From(ReturnToPool)) ?? false)
		{
			destructionTimer.Timeout -= ReturnToPool;
		}

		base._ExitTree();
	}

	public void SetupAndActivate(Vector2 startPosition, Vector2 direction, Texture2D spriteTexture = null, Color? particleColor = null)
	{
		if (active)
		{
			GD.Print($"Projectile {GetInstanceId()}: Already active, ignoring SetupAndActivate call.");
			return;
		}

		GlobalPosition = startPosition;
		Direction = direction.Normalized();

		if (Sprite is not null)
		{
			if (spriteTexture is not null)
			{
				Sprite.Texture = spriteTexture;
			}
			Sprite.Visible = true;
		}

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
			if (particleColor.HasValue)
			{
				DestructionParticles.Color = particleColor.Value;
			}
			DestructionParticles.Position = Vector2.Zero; // Relative to projectile
		}

		lifeTimer?.Stop();
		destructionTimer?.Stop();

		active = true;
		Visible = true;
		ProcessMode = ProcessModeEnum.Pausable;
		SetDeferred(PropertyName.Monitoring, true);
		SetDeferred(PropertyName.Monitorable, true);

		lifeTimer?.Start(DefaultLifetime);
		GD.Print($"Projectile {GetInstanceId()}: Setup and Activated.");
	}

	public void ResetForPooling()
	{
		GD.Print($"Projectile {GetInstanceId()}: Resetting for pooling.");
		active = false;

		lifeTimer?.Stop();
		destructionTimer?.Stop();

		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled;
		SetDeferred(PropertyName.Monitoring, false);
		SetDeferred(PropertyName.Monitorable, false);

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false;
		}
		if (Sprite is not null)
		{
			Sprite.Visible = false; // Ensure sprite is hidden
		}

		Direction = Vector2.Zero;
		GlobalPosition = Vector2.Zero; // Reset position
	}

	private void StartDestructionSequence()
	{
		if (!active)
		{
			GD.Print($"Projectile {GetInstanceId()}: Already destroying or inactive, ignoring StartDestructionSequence call.");
			return;
		}

		GD.Print($"Projectile {GetInstanceId()}: Starting destruction sequence.");
		active = false;
		lifeTimer?.Stop();

		CallDeferred(Node.MethodName.SetProcessMode, (int)ProcessModeEnum.Disabled);
		CallDeferred(CanvasItem.MethodName.SetVisible, false);
		SetDeferred(PropertyName.Monitoring, false);
		SetDeferred(PropertyName.Monitorable, false);

		Direction = Vector2.Zero; // Stop internal movement calculation

		if (DestructionParticles is not null)
		{
			DestructionParticles.GlobalPosition = this.GlobalPosition;
			DestructionParticles.Restart();
			GD.Print($"Projectile {GetInstanceId()}: Started destruction particles at {DestructionParticles.GlobalPosition}.");
		}

		if (destructionTimer is not null)
		{
			destructionTimer.Start(DestructionDuration);
			GD.Print($"Projectile {GetInstanceId()}: Started destruction timer ({DestructionDuration}s).");
		}
		else
		{
			GD.PrintErr($"Projectile {GetInstanceId()}: DestructionTimer node missing! Using temporary timer.");
			GetTree().CreateTimer(DestructionDuration, false, true).Timeout += ReturnToPool;
		}
	}

	private void ReturnToPool()
	{
		GD.Print($"Projectile {GetInstanceId()}: ReturnToPool called.");

		if (DestructionParticles is not null)
		{
			DestructionParticles.Emitting = false; // Ensure particles stop emitting if timer finishes early
		}

		destructionTimer?.Stop(); // Stop the timer if it's still running

		var poolManager = ProjectilePoolManager.Instance;

		if (poolManager is null)
		{
			GD.PrintErr($"Projectile {GetInstanceId()}: Cannot return to pool. ProjectilePoolManager instance not found. Freeing.");
			QueueFree();
			return;
		}

		poolManager.ReturnProjectileToPool(this);
		GD.Print($"Projectile {GetInstanceId()}: Returned to pool via ProjectilePoolManager.");
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!active)
		{
			return;
		}

		if (body is Player player)
		{
			GD.Print($"Projectile {GetInstanceId()}: Hit Player.");
			player.TakeDamage(1);
			player.ApplyKnockback(Direction * KnockbackForce);
			StartDestructionSequence();
		}
		else if (body is BaseEnemy enemy) // Projectiles can hit other enemies
		{
			// Optional: Add logic if projectiles should damage enemies
			// enemy.TakeDamage(1); // Example
			// StartDestructionSequence(); // Example: Destroy on enemy hit
		}
		else if (body.IsInGroup("Obstacles") || body is StaticBody2D) // Check for obstacles or static bodies
		{
			GD.Print($"Projectile {GetInstanceId()}: Hit Obstacle/StaticBody.");
			StartDestructionSequence();
		}
	}

	private void OnLifeTimerTimeout()
	{
		if (!active)
		{
			return;
		}

		GD.Print($"Projectile {GetInstanceId()}: Life timer timeout.");
		StartDestructionSequence();
	}

	// --- Constants ---
	private const float Speed = 300f;
	private const float KnockbackForce = 300f;
	private const float DefaultLifetime = 10.0f;
	private const float DestructionDuration = 0.6f;
}
